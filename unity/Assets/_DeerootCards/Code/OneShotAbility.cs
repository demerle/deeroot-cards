using Photon.Pun;
using UnityEngine;
using UnboundLib;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Shared "one time use" ability-card mechanism. Cards in this registry are
    /// ability cards whose whole effect fires once and then removes themselves
    /// from the player's deck. They still show the ability HUD icon like every
    /// other ability card, but there is no cooldown — consumption IS the cost.
    /// </summary>
    public static class OneShotAbility
    {
        // Keep in sync with new one-shot cards.
        private static readonly System.Collections.Generic.HashSet<string> oneShotCardNames =
            new System.Collections.Generic.HashSet<string>
            {
                MeteorCardName
            };

        /// <summary>Exact registered cardName of the one-shot cards ("Meteor" currently).</summary>
        public const string MeteorCardName = MeteorCard.CardName;

        internal static bool IsOneShot(string cardName)
        {
            return !string.IsNullOrEmpty(cardName) && oneShotCardNames.Contains(cardName);
        }

        /// <summary>
        /// Removes a card (by exact registered cardName, newest copy first) from
        /// a player's deck using the proven Delete-card path:
        /// RebuildGuard.Mark() → one broadcast RPC → master/offline client only
        /// resolves the index fresh and calls UnboundLib's rebuild removal.
        /// Rebuild re-add echoes of remaining cards are swallowed by
        /// RebuildGuard in each card's own OnAddCard as usual; the consumed
        /// one-shot card isn't in the remaining set so it can't re-arm itself.
        /// </summary>
        public static void Consume(Player player, string cardName)
        {
            if (player == null || string.IsNullOrEmpty(cardName))
            {
                return;
            }
            NetworkingManager.RPC(
                typeof(OneShotAbility),
                nameof(RPCA_OneShotConsume),
                player.playerID,
                cardName
            );
        }

        [UnboundLib.Networking.UnboundRPC]
        public static void RPCA_OneShotConsume(int playerID, string cardName)
        {
            UnityEngine.Debug.Log($"[DEER] OneShot consume '{cardName}' from player {playerID}");

            // The removal rebuild must be quiet-marked on every client so spurious
            // re-adds are swallowed locally too.
            RebuildGuard.Mark();

            // Only one client may perform the rebuild (offline or master).
            if (!PhotonNetwork.OfflineMode && !PhotonNetwork.IsMasterClient)
            {
                return;
            }

            Player target = GetPlayerByID(playerID);
            if (target == null || target.data == null)
            {
                UnityEngine.Debug.LogWarning($"[DEER] OneShot: no player {playerID}");
                return;
            }

            var cards = target.data.currentCards;
            int idx = -1;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null && cards[i].cardName == cardName)
                {
                    idx = i;
                    break;
                }
            }

            if (idx < 0)
            {
                UnityEngine.Debug.LogWarning($"[DEER] OneShot: '{cardName}' not found in {target.data.name}'s deck (already gone?)");
                return;
            }

            UnityEngine.Debug.Log($"[DEER] OneShot removing '{cardName}' from {target.data.name} (idx {idx})");
            ModdingUtils.Utils.Cards.instance.RemoveCardFromPlayer(target, idx, true);
        }

        private static Player GetPlayerByID(int playerID)
        {
            var method = typeof(PlayerManager).GetMethod(
                "GetPlayerWithID",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
            return method?.Invoke(PlayerManager.instance, new object[] { playerID }) as Player;
        }
    }

    /// <summary>
    /// Per-bullet configurator for MeteorProjectiles (configures the locally
    /// instantiated Bullet_Base copy on EACH client). Attached from the init
    /// RPC; runs its configuration in Start(), i.e. after the bullet's own
    /// MoveTransform/ProjectileHit.Start hooks have run, so its values are the
    /// final ones (Start order of later-added components is ours).
    /// </summary>
    public class MeteorProjectileInit : MonoBehaviour
    {
        public int casterPlayerID;
        public Vector3 velocity;
        public float damage;
        public float force;

        private bool configured;
        private PhotonView bulletView;

        private void Start()
        {
            Configure();
        }

        private void Update()
        {
            if (configured)
            {
                return;
            }
            // Component Start ordering isn't formally guaranteed when we are
            // added in the same frame as the bullet spawns, so assert the config
            // once more on the first local Update — one frame of drift max.
            Configure();
        }

        private void Configure()
        {
            if (configured)
            {
                return;
            }
            configured = true;

            var view = gameObject.GetComponent<PhotonView>();
            var ph = GetComponent<ProjectileHit>();
            var mt = GetComponent<MoveTransform>();
            if (view == null || ph == null || mt == null)
            {
                UnityEngine.Debug.LogWarning("[DEER] MeteorProjectileInit missing bullet components");
                return;
            }

            // Freeze the generic MoveTransform start (which would fire its
            // serialized localForce forward) and take full control of the
            // trajectory: straight fall with Bullet_Base's built-in gravity
            // (100) doing the acceleration, identical on every client.
            mt.DontRunStart = true;
            mt.velocity = velocity;
            mt.transform.rotation = Quaternion.LookRotation(velocity, Vector3.forward);
            bulletView = view;

            // Fixed stats: no gun stats involved, so every meteor behaves the
            // same regardless of the caster's build (dev plan requirement).
            ph.damage = damage;
            ph.force = force;
            ph.stun = damage / 150f;

            // Only the spawner's owner client gets "damage control"
            // (vanilla ApplyProjectileStats sets hasControl via CheckIsMine();
            // the field is internal so write it via reflection).
            typeof(ProjectileHit)
                .GetField("hasControl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(ph, view.IsMine);

            // Kill credit / damage attribution, copied off Gun.ApplyPlayerStuff.
            Player caster = ResolveCaster(casterPlayerID);
            if (caster != null)
            {
                ph.ownPlayer = caster;
                var spawned = GetComponent<SpawnedAttack>();
                if (spawned == null)
                {
                    spawned = gameObject.AddComponent<SpawnedAttack>();
                }
                spawned.spawner = caster;
            }
            else
            {
                UnityEngine.Debug.LogWarning("[DEER] MeteorProjectileInit could not resolve caster — no kill credit");
            }

            // Lifetime guard: if it somehow misses everything, clean up on the
            // owner client (the one that may PhotonNetwork.Destroy).
            if (view.IsMine)
            {
                StartCoroutine(DestroyLater(8f));
            }

            UnityEngine.Debug.Log($"[DEER] Meteor configured: viewID {bulletView.ViewID} velocity {velocity} damage {damage} force {force}");
        }

        private System.Collections.IEnumerator DestroyLater(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (bulletView != null && bulletView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }

        private static Player ResolveCaster(int playerID)
        {
            var method = typeof(PlayerManager).GetMethod(
                "GetPlayerWithID",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
            return method?.Invoke(PlayerManager.instance, new object[] { playerID }) as Player;
        }
    }
}
