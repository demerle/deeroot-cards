using UnityEngine;
using UnboundLib.Cards;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Overdrive: +50% attack speed, no downsides.
    /// </summary>
    public class OverdriveCard : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // NOTE: CharacterStatModifiers.attackSpeedMultiplier is NOT copied off cards,
            // so we modify the Gun's attackSpeed stat instead. Gun.attackSpeed is the
            // cooldown between attacks: a LOWER value means attacks come out MORE often.
            // 2/3 ~= 0.667 gives a 50% increase in attack rate.
            gun.attackSpeed = 2f / 3f;
            UnityEngine.Debug.Log($"[DEER] OverdriveCard SetupCard: setting gun.attackSpeed to {gun.attackSpeed}");
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            UnityEngine.Debug.Log($"[DEER] OverdriveCard added to player {player.data.name}, fixture attackSpeed={gun.attackSpeed}");
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
        }

        protected override string GetTitle()
        {
            return "Overdrive";
        }

        protected override string GetDescription()
        {
            return "Your weapon operates well past its limits.";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Attack speed",
                    amount = "+50%",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                }
            };
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Common;
        }

        protected override GameObject GetCardArt()
        {
            // No art yet — the card will render with the default blank art.
            return null;
        }

        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.MagicPink;
        }

        public override string GetModName()
        {
            return DeerootCards.modInitials;
        }
    }
}
