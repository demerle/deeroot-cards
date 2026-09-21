using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnboundLib;
using UnboundLib.Cards;
using UnboundLib.GameModes;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Invisibility ability card: passive +15% move speed; press T to vanish
    /// for 2.5s on an 8s personal cooldown (PortalCard input/RPC/HUD recipe).
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
    /// Networking is ONE UnboundLib RPC: only the owner sees the T press, so it
    /// broadcasts RPC_TriggerInvis(playerID) and every client runs the sweep on
    /// its own local copy of the player (rendering is client-local). No Harmony
    /// patches needed. Restore re-enables exactly what WE hid (tracked lists),
    /// never a blanket "enable all".
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
            InvisibilityEffect.RegisterRoundResetHooks();
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
            return "Press T to vanish from sight for 2.5 seconds. Your bullets and footsteps still give you away.";
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
                    stat = "Vanish 2.5s",
                    amount = "T",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Ability Cooldown",
                    amount = "8 seconds",
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
    /// Attached to a player. Pressing T (owner client) broadcasts an UnboundRPC;
    /// on every client it hides that player's renderers, UGUI canvases and SF
    /// shadow polygons — on BOTH the player tree and the unparented gun tree —
    /// for Duration seconds, gated by a personal Cooldown.
    /// </summary>
    public class InvisibilityEffect : MonoBehaviour
    {
        internal const float Duration = 2.5f;
        internal const float Cooldown = 8f;

        private Player player;
        private Holding holding;
        private KeyCode triggerKey = KeyCode.T;
        private float cooldownLeft;
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
            holding = GetComponent<Holding>();
            ResolveKeys();
            UnityEngine.Debug.Log($"[DEER] InvisibilityEffect started on player {player.data.name}");
        }

        // Key table is per-CLIENT (same recipe as PortalEffect.ResolveKeys):
        // online, each client has its own keyboard, so every local player uses T.
        // Only on true local split-screen (2+ IsMine players on one machine) does
        // the local player index pick the key (P1 = T, P2 = Y).
        private void ResolveKeys()
        {
            triggerKey = KeyCode.T;
            if (player == null || player.data == null)
            {
                return;
            }
            int localIndex = 0;
            int localCount = 0;
            foreach (var p in PlayerManager.instance.players)
            {
                if (p == null || p.data == null)
                {
                    continue;
                }
                if (p.data.view.IsMine)
                {
                    if (p == player)
                    {
                        localIndex = localCount;
                    }
                    localCount++;
                }
            }
            if (localCount > 1 && localIndex == 1)
            {
                triggerKey = KeyCode.Y;
            }
            UnityEngine.Debug.Log($"[DEER] Invisibility key resolved: {triggerKey} (local player index {localIndex}/{localCount})");
        }

        private void OnDestroy()
        {
            AbilityHUD.Unregister(this);
            Restore();
        }

        private void OnDisable()
        {
            // The player GO deactivates on death (HealthHandler.RPCA_Die does
            // SetActive(false)) and on round transitions — never stay hidden
            // across that boundary.
            Restore();
        }

        // ---------------------------------------------------------------
        // Ability HUD (bottom-left icon), Portal recipe: register in Awake,
        // unregister in OnDestroy, draw ready/cooldown state. Only the owning
        // client renders it (HudVisible gates on IsMine).
        // ---------------------------------------------------------------
        private static Texture2D hudIconTex;

        private void Awake()
        {
            if (player == null)
            {
                player = GetComponent<Player>();
            }
            EnsureHudTexture();
            AbilityHUD.Register(this, HudVisible, HudDraw);
        }

        private static void EnsureHudTexture()
        {
            if (hudIconTex == null)
            {
                hudIconTex = AbilityHUD.MakeCircleTexture(128, 0, 56);
            }
        }

        private bool HudVisible()
        {
            return player != null && player.data != null && player.data.view.IsMine && player.data.isPlaying;
        }

        private void HudDraw(Rect area)
        {
            bool ready = cooldownLeft <= 0f;
            string caption = ready ? triggerKey.ToString() : cooldownLeft.ToString("F1");
            AbilityHUD.DrawCircle(
                area,
                hudIconTex,
                new Color(0.62f, 0.40f, 0.95f), // ready = violet
                new Color(0.30f, 0.30f, 0.34f), // on cooldown = dark gray
                ready,
                caption
            );
        }

        /// <summary>
        /// Runs on EVERY client via RPC_TriggerInvis. Re-activation while active
        /// just refreshes the timer (harmless: the owner is cooldown-locked
        /// anyway since Cooldown > Duration).
        /// </summary>
        private void Trigger()
        {
            if (!dumpedOnce)
            {
                dumpedOnce = true;
                LogTreeDump();
            }
            active = true;
            remaining = Duration;
            HideSweep();
            UnityEngine.Debug.Log($"[DEER] Invisibility ON player {player.playerID}: {hiddenRenderers.Count} renderers, {hiddenCanvases.Count} canvases, {hiddenPolys.Count} shadow polys hidden");
        }

        // Same gate recipe as PortalEffect.SimulateOwner's canPlace.
        private bool CanTrigger()
        {
            return player.data.isPlaying
                && !player.data.dead
                && GameManager.instance.battleOngoing
                && TimeHandler.timeScale > 0f;
        }

        [UnboundLib.Networking.UnboundRPC]
        private static void RPC_TriggerInvis(int playerID)
        {
            Player target = null;
            foreach (Player p in PlayerManager.instance.players)
            {
                if (p != null && p.playerID == playerID)
                {
                    target = p;
                    break;
                }
            }
            if (target == null)
            {
                UnityEngine.Debug.LogWarning($"[DEER] RPC_TriggerInvis could not resolve player {playerID}");
                return;
            }
            var effect = target.GetComponent<InvisibilityEffect>();
            if (effect == null)
            {
                UnityEngine.Debug.LogWarning($"[DEER] RPC_TriggerInvis: player {playerID} has no InvisibilityEffect");
                return;
            }
            effect.Trigger();
            UnityEngine.Debug.Log($"[DEER] RPC_TriggerInvis triggered player {playerID}");
        }

        private void LateUpdate()
        {
            if (active)
            {
                remaining -= TimeHandler.deltaTime;
                if (remaining <= 0f)
                {
                    UnityEngine.Debug.Log($"[DEER] Invisibility OFF player {player.playerID}");
                    Restore();
                }
                else
                {
                    // Re-apply every frame: objects created mid-effect (redrawn
                    // ammo bullets, attachments) must be hidden too. Idempotent.
                    HideSweep();
                }
            }

            // ---- ability input: owner client only ----
            if (player == null || player.data == null || !player.data.view.IsMine)
            {
                return;
            }
            cooldownLeft = Mathf.Max(0f, cooldownLeft - TimeHandler.deltaTime);
            if (cooldownLeft > 0f || !CanTrigger())
            {
                return;
            }
            if (Input.GetKeyDown(triggerKey))
            {
                cooldownLeft = Cooldown;
                UnityEngine.Debug.Log($"[DEER] Invisibility key {triggerKey} pressed (cooldown {Cooldown}s)");
                UnboundLib.NetworkingManager.RPC(
                    typeof(InvisibilityEffect),
                    nameof(RPC_TriggerInvis),
                    player.playerID
                );
            }
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

        // ---- round reset (same hook set as Portal): force-restore anything
        // still hidden and zero the cooldown so every round starts ready ----
        private static bool hooksRegistered;

        public static void RegisterRoundResetHooks()
        {
            if (hooksRegistered)
            {
                return;
            }
            hooksRegistered = true;
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, gm => ResetAll());
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, gm => ResetAll());
            GameModeManager.AddHook(GameModeHooks.HookGameStart, gm => ResetAll());
        }

        private static IEnumerator ResetAll()
        {
            foreach (Player p in PlayerManager.instance.players)
            {
                if (p == null)
                {
                    continue;
                }
                var effect = p.GetComponent<InvisibilityEffect>();
                if (effect != null)
                {
                    effect.Restore();
                    effect.cooldownLeft = 0f;
                }
            }
            yield break;
        }
    }
}
