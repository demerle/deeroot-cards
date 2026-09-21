using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Photon.Pun;
using UnboundLib;
using UnboundLib.Cards;
using UnityEngine;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Shambles: pressing [F] swaps the player with the "item" closest to their
    /// cursor — any in-flight bullet (own or enemy) or another player. The swap
    /// is an instant teleport (vanilla Teleport recipe). The bullet keeps its
    /// momentum (its MoveTransform.velocity is untouched); players stop where
    /// they land. Personal 6s cooldown.
    /// </summary>
    public class ShamblesCard : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // per-player unique: won't be re-offered once this player holds it
            cardInfo.allowMultiple = false;
            // No stat downside by design — the cooldown is the cost.
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.gameObject.GetOrAddComponent<ShamblesEffect>();
            effect.SetPlayer(player);
            UnityEngine.Debug.Log($"[DEER] ShamblesCard added to player {player.data.name}");
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.GetComponent<ShamblesEffect>();
            if (effect != null)
            {
                Destroy(effect);
            }
        }

        protected override string GetTitle()
        {
            return "Shambles";
        }

        protected override string GetDescription()
        {
            return "Swap places with whatever is closest to your cursor — a bullet or a rival.";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Swap",
                    amount = "F",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Ability Cooldown",
                    amount = "6 seconds",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                }
            };
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Rare;
        }

        protected override GameObject GetCardArt()
        {
            return null;
        }

        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.FirepowerYellow;
        }

        public override string GetModName()
        {
            return DeerootCards.modInitials;
        }
    }

    /// <summary>
    /// Attached to a player. Pressing F (owner client) finds the candidate
    /// closest to the cursor world position — another player or any in-flight
    /// bullet — then broadcasts an UnboundRPC that teleports both sides of the
    /// swap to the caster's snapshot of the two positions on EVERY client.
    /// Players land via the vanilla teleport recipe (velocity zeroed, walls
    /// ignored for 2 frames); bullets land via the Portal bullet recipe
    /// (raw transform write + RayCastTrail.MoveRay snap, velocity untouched so
    /// momentum is preserved).
    /// </summary>
    public class ShamblesEffect : MonoBehaviour
    {
        private Player player;

        private KeyCode triggerKey = KeyCode.F;
        private float cooldownLeft;
        internal const float BaseCooldown = 6f;

        // -1 sentinel: swap-with-player vs swap-with-bullet descriptors in the RPC
        private const int noPlayer = -1;
        private const int noBullet = -1;

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
            ResolveKeys();
            UnityEngine.Debug.Log($"[DEER] ShamblesEffect started on player {player.data.name}");
        }

        private void OnDestroy()
        {
            AbilityHUD.Unregister(this);
        }

        // Key table is per-CLIENT (same recipe as PortalEffect/InvisibilityEffect):
        // online every local player uses F; only on true local split-screen (2+
        // IsMine players on one machine) does the local player index pick the key
        // (P1 = F, P2 = G).
        private void ResolveKeys()
        {
            triggerKey = KeyCode.F;
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
                triggerKey = KeyCode.G;
            }
            UnityEngine.Debug.Log($"[DEER] Shambles key resolved: {triggerKey} (local player index {localIndex}/{localCount})");
        }

        private void Update()
        {
            if (player == null || player.data == null)
            {
                return;
            }

            if (player.data.view.IsMine)
            {
                SimulateOwner(ref cooldownLeft);
            }
        }

        private void SimulateOwner(ref float cooldown)
        {
            bool canTrigger = player.data.isPlaying
                && !player.data.dead
                && GameManager.instance.battleOngoing
                && TimeHandler.timeScale > 0f;

            cooldown = Mathf.Max(0f, cooldown - TimeHandler.deltaTime);

            if (!canTrigger || cooldown > 0f || !Input.GetKeyDown(triggerKey))
            {
                return;
            }

            // cursor -> world (verified pattern, dev notes)
            Vector3 mouse = Input.mousePosition;
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }
            Vector3 cursor = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -cam.transform.position.z));
            cursor.z = 0f;

            var target = FindClosestTarget(cursor, player);
            if (target == null)
            {
                UnityEngine.Debug.Log("[DEER] Shambles: nothing to swap with near the cursor — no cooldown burn");
                return;
            }

            // cooldown only burns on a successful cast; Quick Attack scales it
            cooldown = AbilityCooldowns.Apply(player, BaseCooldown);

            Vector3 casterOldPos = player.transform.root.position;
            casterOldPos.z = 0f;
            Vector3 targetPos = target.position;
            targetPos.z = 0f;

            UnityEngine.Debug.Log($"[DEER] Shambles trigger: caster {player.playerID} swapping with {(target.playerID >= 0 ? $"player {target.playerID}" : $"bullet viewID {target.bulletViewID}")} at {targetPos} (cooldown {BaseCooldown}s)");

            NetworkingManager.RPC(
                typeof(ShamblesEffect),
                nameof(RPC_ShamblesSwap),
                player.playerID,
                target.playerID,
                target.bulletViewID,
                targetPos,
                casterOldPos
            );
        }

        // Target descriptor resolved on the caster's client before broadcasting.
        private class ShamblesTarget
        {
            public int playerID = noPlayer;
            public int bulletViewID = noBullet;
            public Vector3 position;
        }

        private static ShamblesTarget FindClosestTarget(Vector3 cursor, Player caster)
        {
            ShamblesTarget best = null;
            float bestDistSqr = float.MaxValue;

            foreach (var p in PlayerManager.instance.players)
            {
                if (p == null || p.data == null || p == caster)
                {
                    continue;
                }
                if (p.data.dead || !p.data.isPlaying)
                {
                    continue;
                }
                Vector3 pos = p.transform.root.position;
                pos.z = 0f;
                float distSqr = (pos - cursor).sqrMagnitude;
                if (distSqr < bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    best = new ShamblesTarget { playerID = p.playerID, position = pos };
                }
            }

            foreach (var mt in Object.FindObjectsOfType<MoveTransform>())
            {
                // only true projectiles: a bullet carries a ProjectileHit; generic
                // MoveTransform users (UI movers etc.) are skipped (portal recipe).
                var ph = mt.GetComponent<ProjectileHit>();
                if (ph == null)
                {
                    continue;
                }
                // pooled, inactive or already-destroyed bullets are candidates we
                // can't safely swap with; FindObjectsOfType excludes inactive but
                // be defensive about pending destroys.
                if (!mt.gameObject.activeInHierarchy || ph.gameObject == null)
                {
                    continue;
                }

                Vector3 pos = mt.transform.root.position;
                pos.z = 0f;
                float distSqr = (pos - cursor).sqrMagnitude;
                if (distSqr < bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    var view = ph.view != null ? ph.view : mt.GetComponent<PhotonView>();
                    if (view == null)
                    {
                        continue; // cannot address this bullet over the network
                    }
                    best = new ShamblesTarget { bulletViewID = view.ViewID, position = pos };
                }
            }

            return best;
        }

        [UnboundLib.Networking.UnboundRPC]
        private static void RPC_ShamblesSwap(int casterPlayerID, int otherPlayerID, int bulletViewID, Vector3 targetPos, Vector3 casterOldPos)
        {
            UnityEngine.Debug.Log($"[DEER] RPC_ShamblesSwap caster {casterPlayerID} otherPlayer {otherPlayerID} bulletViewID {bulletViewID} -> caster lands {targetPos}, target lands {casterOldPos}");

            Player caster = GetPlayerByID(casterPlayerID);
            if (caster != null)
            {
                TeleportPlayer(caster, targetPos);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[DEER] RPC_ShamblesSwap could not resolve caster player {casterPlayerID}");
            }

            if (otherPlayerID != noPlayer)
            {
                Player other = GetPlayerByID(otherPlayerID);
                if (other != null)
                {
                    TeleportPlayer(other, casterOldPos);
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[DEER] RPC_ShamblesSwap could not resolve swap player {otherPlayerID}");
                }
            }

            if (bulletViewID != noBullet)
            {
                TeleportBullet(bulletViewID, casterOldPos);
            }
        }

        // Vanilla teleport recipe (Teleport.cs decompiled; identical to the
        // portal card's RPC_TouchTeleport): raw position write, brief wall
        // collision ignore, zero the internal PlayerVelocity.velocity.
        private static void TeleportPlayer(Player player, Vector3 dest)
        {
            player.transform.root.position = dest;
            player.GetComponentInParent<PlayerCollision>()?.IgnoreWallForFrames(2);
            player.data.sinceGrounded = 0f;

            var playerVel = player.data.playerVel;
            if (playerVel != null)
            {
                FieldInfo velField = typeof(PlayerVelocity).GetField(
                    "velocity",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                velField?.SetValue(playerVel, Vector2.zero);
            }
            UnityEngine.Debug.Log($"[DEER] Shambles teleported player {player.playerID} to {dest}");
        }

        // Portal-card bullet-teleport recipe: raw transform write with the
        // velocity left UNTOUCHED (momentum preserved), then snap the ray so the
        // bullet doesn't explosion along the span gap. Run on every client — the
        // bullet's owner client is authoritative for SyncProjectile and re-streams
        // the new position outward.
        private static void TeleportBullet(int bulletViewID, Vector3 dest)
        {
            PhotonView view = PhotonNetwork.GetPhotonView(bulletViewID);
            if (view == null || view.gameObject == null)
            {
                // expected when the bullet dies between cast and RPC arrival;
                // the caster still teleported, so just log it
                UnityEngine.Debug.Log($"[DEER] Shambles: bullet viewID {bulletViewID} already gone at RPC time");
                return;
            }
            MoveTransform mt = view.GetComponent<MoveTransform>();
            if (mt == null)
            {
                UnityEngine.Debug.LogWarning($"[DEER] Shambles: viewID {bulletViewID} resolved but has no MoveTransform");
                return;
            }

            mt.transform.root.position = dest;
            var trail = mt.GetComponentInParent<RayCastTrail>();
            if (trail != null)
            {
                trail.MoveRay();
            }
            UnityEngine.Debug.Log($"[DEER] Shambles teleported bullet viewID {bulletViewID} to {dest}, velocity {mt.velocity} (speed {mt.velocity.magnitude:F1})");
        }

        private static Player GetPlayerByID(int playerID)
        {
            MethodInfo method = typeof(PlayerManager).GetMethod(
                "GetPlayerWithID",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            return method?.Invoke(PlayerManager.instance, new object[] { playerID }) as Player;
        }

        // ---------------------------------------------------------------
        // Client-side HUD (bottom-left icon), Portal/Invisibility recipe:
        // register in Awake, unregister in OnDestroy, draw ready/cooldown
        // state. Only the owning client renders it (HudVisible gates on IsMine).
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
                // filled circle with a contrasting inner notch, to read as "swap"
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
                new Color(0.98f, 0.72f, 0.10f), // ready = firepower yellow
                new Color(0.30f, 0.30f, 0.34f), // on cooldown = dark gray
                ready,
                caption
            );
        }
    }
}
