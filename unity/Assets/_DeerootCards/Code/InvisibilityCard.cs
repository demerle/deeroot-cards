using System.Collections.Generic;
using UnityEngine;
using UnboundLib;
using UnboundLib.Cards;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Invisibility: passive +15% move speed; on block, vanish for 2.5s.
    ///
    /// A player renders through THREE independent component families, across TWO
    /// separate object trees — ALL must be swept or the effect looks broken:
    ///
    ///   Sweep roots:
    ///   - The player object (transform.root): body, skin, face, hands, particles,
    ///     health bar, name, chat, block bar.
    ///   - The GUN — spawned UNPARENTED at scene root and physics-dragged behind
    ///     the hand every FixedUpdate (Holding.cs:32 instantiates without a
    ///     parent; Holding.cs:54-62 steers it with AddForce), so it is invisible
    ///     to any player-subtree sweep. Reached via the PUBLIC Holding.holdable
    ///     field. The gun tree holds the gun model plus the ammo bullet row
    ///     (GunAmmo.populate), reload ring and cooldown ring — exactly the pieces
    ///     a player-only sweep leaves floating in mid-air.
    ///
    ///   Component families (why a naive Renderer-only sweep fails):
    ///   1. UnityEngine.Renderer subclasses (SpriteRenderer / ParticleSystemRenderer
    ///      / TrailRenderer / ...) — model + particles.
    ///   2. UGUI Canvases — CanvasRenderer-based UI is NOT a Renderer. Disabling
    ///      each Canvas hides its whole subtree INCLUDING objects created
    ///      mid-effect (redrawn ammo bullets), while GunAmmo/BlockRechargeUI keep
    ///      writing fillAmount every frame (no NREs, instantly correct on restore).
    ///   3. SFPolygon — the background SHADOW system: SFPolygon.OnEnable registers
    ///      into the static SFPolygon._polygons registry and SFRenderer.OnPreRender
    ///      builds the shadow map straight from that registry every frame
    ///      (SFPolygon.cs:252-260, SFRenderer.cs:555/575-583) — completely
    ///      independent of any Renderer state. Disabling the component drops the
    ///      player's shadow contribution without touching the map's own shadows.
    ///
    /// Zero networking: Block.BlockAction fires on every client (RPCA_DoBlock is
    /// RpcTarget.All) and rendering is client-local, so each client hides the
    /// player on its own screen. No Harmony patches needed. Restore re-enables
    /// exactly what WE hid (tracked lists), never a blanket "enable all".
    ///
    /// Bullets stay visible and sounds stay audible on purpose — the fairness tell.
    /// AI still tracks transforms, so bots hunt you normally (visual-only stealth).
    /// </summary>
    public class InvisibilityCard : CustomCard
    {
        internal const float MoveSpeedMult = 1.15f;

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // movementSpeed is a plain multiplier copied off cards (1.0 = no change).
            statModifiers.movementSpeed = MoveSpeedMult;
            UnityEngine.Debug.Log($"[DEER] InvisibilityCard SetupCard: movementSpeed {statModifiers.movementSpeed}");
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.gameObject.GetOrAddComponent<InvisibilityEffect>();
            effect.SetPlayer(player);
            UnityEngine.Debug.Log($"[DEER] InvisibilityCard added to player {player.data.name}");
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.GetComponent<InvisibilityEffect>();
            if (effect != null)
            {
                Destroy(effect);
            }
        }

        protected override string GetTitle()
        {
            return "Invisibility";
        }

        protected override string GetDescription()
        {
            return "Vanish from sight. Your bullets and footsteps still give you away.";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Move speed",
                    amount = "+15%",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = true,
                    stat = "On block",
                    amount = "Vanish 2.5s",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                }
            };
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Uncommon;
        }

        protected override GameObject GetCardArt()
        {
            return null;
        }

        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.DefensiveBlue;
        }

        public override string GetModName()
        {
            return DeerootCards.modInitials;
        }
    }

    /// <summary>
    /// Attached to a player. On block, hides that player's renderers, UGUI
    /// canvases and SF shadow polygons — on BOTH the player tree and the
    /// unparented gun tree — on EVERY client for Duration seconds.
    /// </summary>
    public class InvisibilityEffect : MonoBehaviour
    {
        internal const float Duration = 2.5f;

        private Player player;
        private Block block;
        private Holding holding;
        private bool active;
        private float remaining;
        private bool dumpedOnce;
        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private readonly List<Canvas> hiddenCanvases = new List<Canvas>();
        private readonly List<SFPolygon> hiddenPolys = new List<SFPolygon>();

        public void SetPlayer(Player player)
        {
            this.player = player;
        }

        private void Start()
        {
            if (player == null)
            {
                player = GetComponent<Player>();
            }
            block = GetComponent<Block>();
            holding = GetComponent<Holding>();
            block.BlockAction += OnBlock;
            UnityEngine.Debug.Log($"[DEER] InvisibilityEffect started on player {player.data.name}");
        }

        private void OnDestroy()
        {
            if (block != null)
            {
                block.BlockAction -= OnBlock;
            }
            Restore();
        }

        private void OnDisable()
        {
            // The player GO deactivates on death (HealthHandler.RPCA_Die does
            // SetActive(false)) and on round transitions — never stay hidden
            // across that boundary.
            Restore();
        }

        private void OnBlock(BlockTrigger.BlockTriggerType triggerType)
        {
            // Vanilla Shield Charge re-blocks for free; ignore those triggers
            // (same guard as Sovereign/DIVE).
            if (triggerType == BlockTrigger.BlockTriggerType.ShieldCharge)
            {
                return;
            }
            if (!dumpedOnce)
            {
                dumpedOnce = true;
                LogTreeDump();
            }
            active = true;
            remaining = Duration; // re-blocking refreshes the timer
            HideSweep();
            UnityEngine.Debug.Log($"[DEER] Invisibility ON player {player.playerID}: {hiddenRenderers.Count} renderers, {hiddenCanvases.Count} canvases, {hiddenPolys.Count} shadow polys hidden");
        }

        private void LateUpdate()
        {
            if (!active)
            {
                return;
            }
            remaining -= TimeHandler.deltaTime;
            if (remaining <= 0f)
            {
                UnityEngine.Debug.Log($"[DEER] Invisibility OFF player {player.playerID}");
                Restore();
                return;
            }
            // Re-apply every frame: objects created mid-effect (redrawn ammo
            // bullets, attachments) must be hidden too. Idempotent.
            HideSweep();
        }

        // The player tree plus the unparented gun tree (Holding spawns the gun at
        // scene root — see class doc). Null-safe: holdable may be destroyed with
        // the player or not yet spawned.
        private IEnumerable<Transform> SweepRoots()
        {
            yield return player.transform.root;
            if (holding != null && holding.holdable != null)
            {
                yield return holding.holdable.transform;
            }
        }

        private void HideSweep()
        {
            foreach (Transform root in SweepRoots())
            {
                foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
                {
                    if (r.enabled)
                    {
                        r.enabled = false;
                        hiddenRenderers.Add(r);
                    }
                }
                foreach (Canvas c in root.GetComponentsInChildren<Canvas>())
                {
                    if (c.enabled)
                    {
                        c.enabled = false;
                        hiddenCanvases.Add(c);
                    }
                }
                foreach (SFPolygon p in root.GetComponentsInChildren<SFPolygon>())
                {
                    if (p.enabled)
                    {
                        // Disabling triggers OnDisable, which drops the polygon
                        // from the static SFPolygon._polygons registry the
                        // SFRenderer shadow pass reads every frame.
                        p.enabled = false;
                        hiddenPolys.Add(p);
                    }
                }
            }
        }

        private void Restore()
        {
            if (!active && hiddenRenderers.Count == 0 && hiddenCanvases.Count == 0 && hiddenPolys.Count == 0)
            {
                return;
            }
            // Re-enable exactly what WE hid (null-safe: some may have been
            // destroyed mid-effect), never a blanket "enable all".
            foreach (Renderer r in hiddenRenderers)
            {
                if (r != null)
                {
                    r.enabled = true;
                }
            }
            foreach (Canvas c in hiddenCanvases)
            {
                if (c != null)
                {
                    c.enabled = true;
                }
            }
            foreach (SFPolygon p in hiddenPolys)
            {
                if (p != null)
                {
                    // OnEnable re-registers it in SFPolygon._polygons.
                    p.enabled = true;
                }
            }
            hiddenRenderers.Clear();
            hiddenCanvases.Clear();
            hiddenPolys.Clear();
            active = false;
        }

        // Temporary [DEER] diagnostic: full transform tree with component types,
        // so we can see exactly what the player and gun subtrees contain (esp.
        // Canvases) and confirm nothing visible escapes the sweep. Strip with the
        // batch.
        private void LogTreeDump()
        {
            foreach (Transform root in SweepRoots())
            {
                UnityEngine.Debug.Log($"[DEER] Invisibility tree dump for '{root.name}' (player {player.playerID}):");
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    Component[] comps = t.GetComponents<Component>();
                    string[] typeNames = new string[comps.Length];
                    for (int i = 0; i < comps.Length; i++)
                    {
                        typeNames[i] = comps[i] == null ? "<missing>" : comps[i].GetType().Name;
                    }
                    int depth = 0;
                    Transform p = t.parent;
                    while (p != null && p != root)
                    {
                        depth++;
                        p = p.parent;
                    }
                    string indent = new string(' ', depth * 2);
                    UnityEngine.Debug.Log($"[DEER]   {indent}{t.name} (active:{t.gameObject.activeInHierarchy}) [{string.Join(", ", typeNames)}]");
                }
            }
        }
    }
}
