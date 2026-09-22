using UnboundLib.Cards;
using UnityEngine;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Bonus cards used by Double's per-player-unique compensation (DoubleCard).
    /// They are real cards — applied via DoubleEffect's RPC/ApplyCardToPlayer
    /// channel so they show in the card bar and persist through deck rebuilds —
    /// and they also spawn normally from the pick pool (Uncommon rarity).
    /// No descriptions: stats block only.
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
            // No description line — stats block only.
            return "";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "All Ability Cooldowns",
                    amount = "-50%",
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
    /// "Power Up": +50% damage. Vanilla stat card — Gun.damage is copied off
    /// cards (multiplier on the player's accumulated damage), so stacking
    /// copies multiplies ×1.5 each. Compensation default for any per-player
    /// unique card that is not one of this mod's ability cards (DoubleCard
    /// registry), including vanilla uniques.
    /// </summary>
    public class PowerUpCard : CustomCard
    {
        public const string CardName = "Power Up";

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // +50% damage (multiplier field; 1.5 = 150% of normal)
            gun.damage = 1.5f;
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
            // No description line — stats block only.
            return "";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Damage",
                    amount = "+50%",
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
}
