using UnboundLib.Cards;
using UnityEngine;

namespace DeerootCards.Cards
{
    /// <summary>
    /// "Slow and Steady": +85% damage, -30% bullet speed AND -40% projectile
    /// simulation speed. Plain vanilla stat card — all fields are multipliers
    /// copied off the card's Gun (same recipe as Power Up's gun.damage):
    /// gun.projectileSpeed scales bullet launch force, and the separate
    /// gun.projectielSimulatonSpeed (typo is vanilla) scales the bullet's
    /// simulation/Physics update speed at spawn (Gun.SpawnProjectile,
    /// line 662). No description: stats block only. Stackable.
    /// </summary>
    public class SlowAndSteadyCard : CustomCard
    {
        public const string CardName = "Slow and Steady";

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // +85% damage (multiplier field)
            gun.damage = 1.85f;
            // -30% bullet speed (multiplier field; 0.70 = 70% of normal)
            gun.projectileSpeed = 0.7f;
            // -40% projectile simulation speed (separate vanilla stat, also a
            // multiplier on the bullet's simulation) — typo in the field name
            // is from vanilla.
            gun.projectielSimulatonSpeed = 0.6f;
            UnityEngine.Debug.Log($"[DEER] SlowAndSteadyCard SetupCard: gun.damage={gun.damage}, gun.projectileSpeed={gun.projectileSpeed}, gun.projectielSimulatonSpeed={gun.projectielSimulatonSpeed}");
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // Stat-only card: stats copied in SetupCard.
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
                    amount = "+85%",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Bullet Speed",
                    amount = "-30%",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Projectile Simulation Speed",
                    amount = "-40%",
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
