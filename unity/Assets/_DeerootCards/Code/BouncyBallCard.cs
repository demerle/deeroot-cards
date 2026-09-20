using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnboundLib.Cards;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Bouncy Ball: +50% movement speed, but you take triple knockback from
    /// everything that hits you (bullets, explosions, boxes, hazards).
    /// </summary>
    public class BouncyBallCard : CustomCard
    {
        public const string CardName = "Bouncy Ball";

        // The speed multiplier (vanilla CharacterStatModifiers.movementSpeed is a
        // plain multiplier, default 1).
        private const float SpeedMultiplier = 1.5f;
        internal const float KnockbackMultiplier = 3f;

        private static bool harmonyApplied;

        public static void Init()
        {
            if (harmonyApplied) return;
            harmonyApplied = true;
            new Harmony("com.deeroot.cards.bouncyball").PatchAll(typeof(BouncyBallKnockbackPatch));
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
            return "Slick and springy. Zoom around the arena — but every hit sends you flying.";
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
                    amount = "Triple",
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
    /// Triples knockback taken by Bouncy Ball holders.
    ///
    /// Patch point: HealthHandler.CallTakeForce — the NETWORKED wrapper every
    /// external knockback source funnels through (bullets, explosions, damage
    /// boxes, physics objects i.e. boxes hitting players, line attacks,
    /// out-of-bounds bounce). The tripled value rides the vanilla
    /// RPCA_SendTakeForce RPC to every client, so it syncs for free.
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
            // Match by cardName: currentCards stores sourceCard (the registered
            // prefab instance), so instance-identity caching from SetupCard is
            // wrong — SetupCard re-runs per spawned pick-card clone.
            if (data == null || data.currentCards == null ||
                !data.currentCards.Any(c => c != null && c.cardName == BouncyBallCard.CardName))
            {
                return;
            }
            Vector2 before = force;
            force *= BouncyBallCard.KnockbackMultiplier;
            UnityEngine.Debug.Log($"[DEER] BouncyBall: tripled force {before.magnitude:F0} -> {force.magnitude:F0} on {data.player?.name ?? data.gameObject.name}");
        }
    }
}
