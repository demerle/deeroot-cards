using UnityEngine;
using UnboundLib.Cards;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Quick Attack (formerly Overdrive): +50% attack speed, -25% movement speed.
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
            // downside: -25% movement speed (multiplier field; 0.75 = 75% of normal)
            statModifiers.movementSpeed = 0.75f;
            UnityEngine.Debug.Log($"[DEER] OverdriveCard SetupCard: gun.attackSpeed={gun.attackSpeed}, movementSpeed={statModifiers.movementSpeed}");
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
            return "Quick Attack";
        }

        protected override string GetDescription()
        {
            return "Your weapon operates well past its limits — your legs can't keep up.";
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
                },
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
