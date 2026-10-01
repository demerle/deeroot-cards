using UnityEngine;
using UnboundLib.Cards;

namespace DeerootCards.Cards
{
    /// <summary>
    /// KillStreak: passive tracker card (Common). While held, the holder's
    /// kills-in-a-row-without-dying are counted across rounds; crossing the
    /// milestones grants one-shot cards (meteor / simulacrum / time stop on
    /// the 3rd / 4th / 5th kill of each streak cycle, looping past 5).
    ///
    /// ALL mechanics live in KillStreakTracker (Harmony death patches + static
    /// streak state + broadcast grant RPC); this card just registers them and
    /// prints behavior text. No stat fields by design — the reward IS the card.
    /// The HUD lives in KillStreakHud (left-edge circle column).
    /// </summary>
    public class KillStreakCard : CustomCard
    {
        public const string CardName = "KillStreak";

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // Tracker state is static and patch-driven; nothing per-card to arm.
            // (No RebuildGuard bail needed: this add is side-effect-free.) The
            // streak the player had before losing the card resumes once they
            // re-hold it.
            UnityEngine.Debug.Log($"[DEER] KillStreakCard added to player {player.data.name}");
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // Keep the count (they may re-gain the card); the HUD simply hides
            // while the card is not held.
            UnityEngine.Debug.Log($"[DEER] KillStreakCard removed from player {player.data.name}");
        }

        protected override string GetTitle()
        {
            return CardName;
        }

        protected override string GetDescription()
        {
            return "Enable Killstreaks";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "3 kills",
                    amount = "Meteor",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = true,
                    stat = "4 kills",
                    amount = "Simulacrum",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = true,
                    stat = "5 kills",
                    amount = "Time Stop",
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
            return CardThemeColor.CardThemeColorType.MagicPink;
        }

        public override string GetModName()
        {
            return DeerootCards.modInitials;
        }
    }
}
