using HarmonyLib;
using UnboundLib.Cards;
using UnityEngine;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Hidden bonus cards used by Double's per-player-unique compensation
    /// (DoubleCard). They are real cards — applied via DoubleEffect's
    /// RPC/AcquireCardToPlayer channel so they show in the card bar and persist
    /// through deck rebuilds — but they must never show up in random pick
    /// offers, which the GetRanomCard postfix below enforces by re-rolling.
    /// </summary>
    public static class BonusCards
    {
        internal static string[] HiddenFromPool =
        {
            AbilityUpCard.CardName,
            PowerUpCard.CardName
        };

        public static void Init()
        {
            new Harmony("com.deeroot.cards.bonuscards").PatchAll(typeof(CardChoicePatchHideBonusCards));
            UnityEngine.Debug.Log("[DEER] BonusCards: pick-pool hide patch applied");
        }
    }

    /// <summary>
    /// "Ability Up": -25% all ability cooldowns. Carries NO vanilla stat fields
    /// on purpose — the effect comes purely from AbilityCooldowns.Sources
    /// ("Ability Up", -0.25), which dead-reckons off the player's deck, so
    /// stacking and removal are handled for free. Stackable by design; the
    /// multiplier is floored in AbilityCooldowns.Apply so cooldowns never hit 0.
    /// </summary>
    public class AbilityUpCard : CustomCard
    {
        public const string CardName = "Ability Up";

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // Effect is deck-driven via AbilityCooldowns.Sources — no vanilla
            // stat fields here because there is no vanilla "ability cooldown"
            // stat to copy.
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // No effect component — AbilityCooldowns scans the deck by name.
        }

        protected override string GetTitle()
        {
            return CardName;
        }

        protected override string GetDescription()
        {
            return "Gain a -25% all ability cooldowns card when a unique card is doubled.";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Ability Cooldowns",
                    amount = "-25%",
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
            return CardThemeColor.CardThemeColorType.TechWhite;
        }

        public override string GetModName()
        {
            return DeerootCards.modInitials;
        }
    }

    /// <summary>
    /// "Power Up": +25% damage. Vanilla stat card — Gun.damage is a copied off
    /// cards (multiplier on the player's accumulated damage), so stacking
    /// copies multiplies ×1.25 each. Compensation default for any per-player
    /// unique card that is not one of this mod's ability cards (DoubleCard
    /// registry), including vanilla uniques.
    /// </summary>
    public class PowerUpCard : CustomCard
    {
        public const string CardName = "Power Up";

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // +25% damage (multiplier field; 1.25 = 125% of normal)
            gun.damage = 1.25f;
            UnityEngine.Debug.Log($"[DEER] PowerUpCard SetupCard: setting gun.damage to {gun.damage}");
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // Stat-only card: gun.damage copied in SetupCard.
        }

        protected override string GetTitle()
        {
            return CardName;
        }

        protected override string GetDescription()
        {
            return "Gain a +25% damage card when a unique card is doubled.";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Damage",
                    amount = "+25%",
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
            return CardThemeColor.CardThemeColorType.FirepowerYellow;
        }

        public override string GetModName()
        {
            return DeerootCards.modInitials;
        }
    }

    /// <summary>
    /// Keeps the bonus cards out of random pick offers: CardChoice.GetRanomCard
    /// is the single source of random card offers ( vanilla SpawnUniqueCard and
    /// every pick-phase replacement funnel through it). If it returns one of
    /// ours, re-roll via reflection until it returns a real pool card. Direct
    /// application (Double's RPC -> ApplyCardToPlayer) is unaffected.
    /// </summary>
    [HarmonyPatch(typeof(CardChoice), "GetRanomCard")]
    public class CardChoicePatchHideBonusCards
    {
        static void Postfix(ref GameObject __result)
        {
            for (int i = 0; i < 20; i++)
            {
                CardInfo info = __result != null ? __result.GetComponent<CardInfo>() : null;
                if (info == null)
                {
                    return;
                }
                bool hidden = false;
                foreach (string name in BonusCards.HiddenFromPool)
                {
                    if (info.cardName == name)
                    {
                        hidden = true;
                        break;
                    }
                }
                if (!hidden)
                {
                    return;
                }
                UnityEngine.Debug.Log("[DEER] Bonus card '" + info.cardName + "' blocked from pick pool — re-rolling");
                var method = AccessTools.Method(typeof(CardChoice), "GetRanomCard");
                GameObject reRolled = method.Invoke(CardChoice.instance, new object[0]) as GameObject;
                if (reRolled == null)
                {
                    return;
                }
                __result = reRolled;
            }
            UnityEngine.Debug.LogWarning("[DEER] Bonus-card re-roll failed after 20 attempts — leaving result as-is");
        }
    }
}
