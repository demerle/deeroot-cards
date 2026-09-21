using UnityEngine;
using UnboundLib.Cards;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Quick Attack (formerly Overdrive): +35% attack speed,
    /// +25% ability cooldowns (Invisibility/Portal — see AbilityCooldowns).
    /// The cooldown modifier is NOT wired here: AbilityCooldowns.Sources is the
    /// single registry (pure function of the deck, immune to the deck-rebuild
    /// re-fire of OnAddCard). Adding a copy of this card stacks the modifier.
    /// </summary>
    public class OverdriveCard : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // NOTE: CharacterStatModifiers.attackSpeedMultiplier is NOT copied off cards,
            // so we modify the Gun's attackSpeed stat instead. Gun.attackSpeed is the
            // cooldown between attacks: a LOWER value means attacks come out MORE often.
            // 1/1.35 ~= 0.741 gives a 35% increase in attack rate.
            gun.attackSpeed = 1f / 1.35f;
            // (movement speed debuff removed per design change — card is now pure upside + cooldown cost)
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
                    stat = "Attack Speed",
                    amount = "+35%",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "All Ability Cooldowns",
                    amount = "+25%",
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
