using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnboundLib.Cards;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Bouncy Ball: +50% movement speed, but you take double knockback from
    /// everything that hits you (bullets, explosions, boxes, hazards).
    /// </summary>
    public class BouncyBallCard : CustomCard
    {
        public const string CardName = "Bouncy Ball";

        // The speed multiplier (vanilla CharacterStatModifiers.movementSpeed is a
        // plain multiplier, default 1).
        private const float SpeedMultiplier = 1.5f;

        // Per-source knockback multipliers (separation of concerns):
        // the funnel patch (CallTakeForce) applies whichever multiplier the
        // source stamps announce; sources that never announce get Default.
        internal const float DefaultMultiplier = 2f;     // explosions, boxes, damage boxes, line attacks, bullets (no stamp)
        internal const float BulletMultiplier = 2f;      // ProjectileHit.RPCA_DoHit (matches default)
        internal const float OutOfBoundsMultiplier = 2f; // OutOfBoundsHandler.LateUpdate bounce

        // Handoff between source stamps and the funnel patch (0 / "default" = none active).
        internal static float SourceMultiplier;
        internal static string SourceTag = "default";

        internal static bool HasCard(CharacterData data)
        {
            return data != null && data.currentCards != null &&
                   data.currentCards.Any(c => c != null && c.cardName == CardName);
        }

        private static bool harmonyApplied;

        public static void Init()
        {
            if (harmonyApplied) return;
            harmonyApplied = true;
            var harmony = new Harmony("com.deeroot.cards.bouncyball");
            harmony.PatchAll(typeof(BouncyBallKnockbackPatch));
            harmony.PatchAll(typeof(BouncyBallSourcePatch));
        }

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // NOTE: SetupCard re-runs on every spawned pick-card clone too
            // (CustomCard.Awake fires per clone). Never cache cardInfo here for
            // identity checks — clones are NOT the instance stored in currentCards
            // (that's sourceCard = the registered prefab). Match by cardName instead.
            statModifiers.movementSpeed = SpeedMultiplier;
            UnityEngine.Debug.Log($"[DEER] BouncyBallCard SetupCard: movementSpeed {statModifiers.movementSpeed}");
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            UnityEngine.Debug.Log($"[DEER] BouncyBallCard added to player {player.data.name}");
        }

        protected override string GetTitle()
        {
            return CardName;
        }

        protected override string GetDescription()
        {
            return "Move like a monkey at the cost of getting tossed around like a ball";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Movement speed",
                    amount = "+50%",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Knockback taken",
                    amount = "Double",
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
            // Vanilla theme enum has no "green" besides PoisonGreen
            // (DestructiveRed, FirepowerYellow, DefensiveBlue, TechWhite,
            // EvilPurple, PoisonGreen, NatureBrown, ColdBlue, MagicPink).
            return CardThemeColor.CardThemeColorType.PoisonGreen;
        }

        public override string GetModName()
        {
            return DeerootCards.modInitials;
        }
    }

    /// <summary>
    /// The funnel: scales knockback taken by Bouncy Ball holders.
    ///
    /// Patch point: HealthHandler.CallTakeForce — the NETWORKED wrapper every
    /// external knockback source funnels through (bullets, explosions, damage
    /// boxes, physics objects i.e. boxes hitting players, line attacks,
    /// out-of-bounds bounce). The scaled value rides the vanilla
    /// RPCA_SendTakeForce RPC to every client, so it syncs for free.
    ///
    /// Multiplier comes from BouncyBallCard.SourceMultiplier, set by the source
    /// stamps in BouncyBallSourcePatch around their CallTakeForce window:
    /// everything ×2 (bullet stamp matches Default, kept for easy retuning).
    ///
    /// Deliberately NOT affected (they call TakeForce directly, skipping
    /// CallTakeForce): the holder's jump, block self-push, Shield Charge and
    /// DIVE charge loops, thrusters, saws and beam attacks. Blocking still
    /// negates knockback entirely — CallTakeForce's IsBlocking gate is untouched.
    /// </summary>
    [HarmonyPatch]
    public class BouncyBallKnockbackPatch
    {
        [HarmonyPatch(typeof(HealthHandler), nameof(HealthHandler.CallTakeForce))]
        [HarmonyPrefix]
        static void Prefix(HealthHandler __instance, ref Vector2 force)
        {
            // HealthHandler and CharacterData share a GameObject (vanilla resolves
            // it exactly like this in HealthHandler.Awake: data = GetComponent<...>).
            var data = __instance.GetComponent<CharacterData>();
            if (!BouncyBallCard.HasCard(data))
            {
                return;
            }
            float multiplier = BouncyBallCard.SourceMultiplier > 0f
                ? BouncyBallCard.SourceMultiplier
                : BouncyBallCard.DefaultMultiplier;
            string tag = BouncyBallCard.SourceMultiplier > 0f ? BouncyBallCard.SourceTag : "default";
            Vector2 before = force;
            force *= multiplier;
            UnityEngine.Debug.Log($"[DEER] BouncyBall: x{multiplier:F0} ({tag}) force {before.magnitude:F0} -> {force.magnitude:F0} on {data.player?.name ?? data.gameObject.name}");
        }
    }

    /// <summary>
    /// Source stamps: announce which knockback source is resolving, so the
    /// CallTakeForce funnel can pick a per-source multiplier. Pure flag
    /// setters — no game logic. Postfixes reset before the next event.
    /// (If a patched original throws, Harmony skips the postfix — worst case
    /// is one event with a stale multiplier until the next stamp; risk ≈ 0.)
    /// </summary>
    [HarmonyPatch]
    public class BouncyBallSourcePatch
    {
        // Bullet knockback (replicated RpcTarget.All — runs on every client).
        [HarmonyPatch(typeof(ProjectileHit), nameof(ProjectileHit.RPCA_DoHit))]
        [HarmonyPrefix]
        static void BulletPrefix()
        {
            BouncyBallCard.SourceMultiplier = BouncyBallCard.BulletMultiplier;
            BouncyBallCard.SourceTag = "bullet";
        }

        [HarmonyPatch(typeof(ProjectileHit), nameof(ProjectileHit.RPCA_DoHit))]
        [HarmonyPostfix]
        static void BulletPostfix()
        {
            BouncyBallCard.SourceMultiplier = 0f;
            BouncyBallCard.SourceTag = "default";
        }

        // Out-of-bounds bounce: the pit/side-bounds push-back (both the blocking
        // 400x-mass and normal 200x-mass branches, every ~0.1s while out).
        [HarmonyPatch(typeof(OutOfBoundsHandler), "LateUpdate")]
        [HarmonyPrefix]
        static void OutOfBoundsPrefix()
        {
            BouncyBallCard.SourceMultiplier = BouncyBallCard.OutOfBoundsMultiplier;
            BouncyBallCard.SourceTag = "ob";
        }

        [HarmonyPatch(typeof(OutOfBoundsHandler), "LateUpdate")]
        [HarmonyPostfix]
        static void OutOfBoundsPostfix()
        {
            BouncyBallCard.SourceMultiplier = 0f;
            BouncyBallCard.SourceTag = "default";
        }
    }
}
