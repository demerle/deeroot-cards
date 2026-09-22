using Photon.Pun;
using UnboundLib;
using UnboundLib.Cards;
using UnityEngine;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Meteor: ONE TIME USE ability card (OneShotAbility). Shows the ability
    /// HUD icon like every ability card (bottom-left row), but the card
    /// removes itself from the deck the moment it is used — no cooldown,
    /// consumption is the cost.
    ///
    /// Pressing [C] fires a meteor: a vanilla Bullet_Base projectile spawned a
    /// little above the cursor's world position (position computed on the
    /// caster's client and passed through the network, Shambles lag recipe),
    /// falling straight down at 50% of the default bullet speed (25 of 50),
    /// accelerating by Bullet_Base's built-in gravity (100) — a proper slow
    /// meteor that grows faster as it falls. It hits with a fixed 500 damage
    /// (independent of any gun stats) with the caster credited.
    /// </summary>
    public class MeteorCard : CustomCard
    {
        public const string CardName = "Meteor";

        // Fixed meteor stats (no gun stats involved — verifiable in
        // decompiled Gun.cs ApplyProjectileStats, which we deliberately skip).
        internal const float BaseSpeed = 25f;   // 50% of the vanilla bullet speed 50 (Bullet_Base localForce)
        internal const float BaseDamage = 500f;
        // ProjectileHit.Start scales force by (damage/55)^2; pick a pre-scale
        // value that lands at ~3x a vanilla 55-damage bullet's 5000 knockback.
        internal const float BaseForce = 180f;

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // per-player unique: won't be re-offered once this player holds it
            cardInfo.allowMultiple = false;
            // All meteor strength is defined in MeteorProjectileInit; no gun
            // stat fields copied by design.
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // The vanilla pick pipeline re-runs during UnboundLib's removal
            // rebuild — those re-adds are fine here: the consumed Meteor card
            // is REMOVED before the rebuild, so nothing re-arms it. But if a
            // rebuild is triggered by a different removal while Meteor is held,
            // we still want the ability re-armed on re-add, so no RebuildGuard
            // bail here (unlike Delete-while-pending).
            var effect = player.gameObject.GetOrAddComponent<MeteorEffect>();
            effect.SetPlayer(player);
            UnityEngine.Debug.Log($"[DEER] MeteorCard added to player {player.data.name}");
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.GetComponent<MeteorEffect>();
            if (effect != null)
            {
                Destroy(effect);
            }
        }

        protected override string GetTitle()
        {
            return CardName;
        }

        protected override string GetDescription()
        {
            return "Drop a meteor from the sky where your cursor is. One time use.";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Meteor",
                    amount = "C",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = true,
                    stat = "Damage",
                    amount = "500",
                    simepleAmount = CardInfoStat.SimpleAmount.aLotOf
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Projectile Speed",
                    amount = "50%",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Uses",
                    amount = "1",
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
    /// Attached to the player. Owner client polls [C], snapshots the cursor,
    /// broadcasts the spawn RPC, then consumes the card via OneShotAbility
    /// (RebuildGuard-marked, master-gated rebuild removal).
    /// </summary>
    public class MeteorEffect : MonoBehaviour
    {
        private Player player;

        private KeyCode triggerKey = KeyCode.C;

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
            UnityEngine.Debug.Log($"[DEER] MeteorEffect started on player {player.data.name}");
        }

        private void OnDestroy()
        {
            AbilityHUD.Unregister(this);
        }

        // Per-CLIENT key table (Portal/Shambles recipe): default C; only true
        // split-screen (2+ IsMine players) local index 1 uses V.
        private void ResolveKeys()
        {
            triggerKey = KeyCode.C;
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
                triggerKey = KeyCode.V;
            }
            UnityEngine.Debug.Log($"[DEER] Meteor key resolved: {triggerKey} (local player index {localIndex}/{localCount})");
        }

        private void Update()
        {
            if (player == null || player.data == null || player.data.view == null)
            {
                return;
            }
            if (!player.data.view.IsMine)
            {
                return;
            }

            bool canTrigger = player.data.isPlaying
                && !player.data.dead
                && GameManager.instance.battleOngoing
                && TimeHandler.timeScale > 0f;

            if (!canTrigger || !Input.GetKeyDown(triggerKey))
            {
                return;
            }

            UnityEngine.Debug.Log("[DEER] Meteor trigger");

            // cursor -> world (verified Shambles recipe)
            Vector3 mouse = Input.mousePosition;
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }
            Vector3 cursor = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -cam.transform.position.z));
            cursor.z = 0f;

            // Master the spawn path on every client; only the caster's client
            // calls the networked PhotonNetwork.Instantiate (it synchronizes
            // creation to everyone), then bolts the per-client config on top.
            NetworkingManager.RPC(
                typeof(MeteorEffect),
                nameof(RPC_MeteorStrike),
                player.playerID,
                cursor
            );

            // Consume the card immediately: the meteor is already in the air.
            OneShotAbility.Consume(player, MeteorCard.CardName);
        }

        [UnboundLib.Networking.UnboundRPC]
        private static void RPC_MeteorStrike(int casterPlayerID, Vector3 targetPos)
        {
            UnityEngine.Debug.Log($"[DEER] RPC_MeteorStrike caster {casterPlayerID} target {targetPos}");

            Player caster = GetPlayerByID(casterPlayerID);
            if (caster == null || caster.data == null)
            {
                UnityEngine.Debug.LogWarning($"[DEER] RPC_MeteorStrike could not resolve caster {casterPlayerID}");
                return;
            }

            // PhotonNetwork.Instantiate is itself a networked creation event —
            // every client gets the bullet when the caster calls it once here.
            if (!caster.data.view.IsMine)
            {
                return;
            }

            Vector3 spawnPos = targetPos + Vector3.up * 4f;
            GameObject bullet = PhotonNetwork.Instantiate("Bullet_Base", spawnPos, Quaternion.identity, 0);
            if (bullet == null)
            {
                UnityEngine.Debug.LogWarning("[DEER] RPC_MeteorStrike failed to instantiate Bullet_Base");
                return;
            }

            int viewID = bullet.GetComponent<PhotonView>().ViewID;
            Vector3 fallVelocity = Vector3.down * MeteorCard.BaseSpeed;

            UnityEngine.Debug.Log($"[DEER] Meteor bullet spawned viewID {viewID} at {spawnPos}");

            NetworkingManager.RPC(
                typeof(MeteorEffect),
                nameof(RPC_MeteorConfigure),
                viewID,
                casterPlayerID,
                fallVelocity,
                MeteorCard.BaseDamage,
                MeteorCard.BaseForce
            );
        }

        [UnboundLib.Networking.UnboundRPC]
        private static void RPC_MeteorConfigure(int viewID, int casterPlayerID, Vector3 velocity, float damage, float force)
        {
            PhotonView view = PhotonNetwork.GetPhotonView(viewID);
            if (view == null || view.gameObject == null)
            {
                UnityEngine.Debug.LogWarning($"[DEER] RPC_MeteorConfigure: viewID {viewID} not found");
                return;
            }
            var init = view.gameObject.AddComponent<MeteorProjectileInit>();
            init.casterPlayerID = casterPlayerID;
            init.velocity = velocity;
            init.damage = damage;
            init.force = force;
        }

        private static Player GetPlayerByID(int playerID)
        {
            var method = typeof(PlayerManager).GetMethod(
                "GetPlayerWithID",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
            return method?.Invoke(PlayerManager.instance, new object[] { playerID }) as Player;
        }

        // ---------------------------------------------------------------
        // HUD icon — standard AbilityHUD recipe (registered in Awake,
        // unregistered in OnDestroy). No cooldown: one-time use is always
        // "ready" until the cast consumes the card.
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
                // filled disc with a contrasting inner notch — reads as "impact"
                hudIconTex = AbilityHUD.MakeCircleTexture(128, 0, 56);
            }
        }

        private bool HudVisible()
        {
            return player != null && player.data != null && player.data.view.IsMine && player.data.isPlaying;
        }

        private void HudDraw(Rect area)
        {
            string caption = triggerKey.ToString();
            AbilityHUD.DrawCircle(
                area,
                hudIconTex,
                new Color(0.98f, 0.42f, 0.10f), // ready: molten orange
                new Color(0.30f, 0.30f, 0.34f), // spent: dark gray
                true,
                caption
            );
        }
    }
}
