using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnboundLib;
using UnboundLib.Cards;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ModsPlus
{
    public abstract class SimpleCard : CustomCard
    {
        public abstract CardDetails Details { get; }

        public class CardDetails
        {
            public string Title { get; set; } = null;
            public string Description { get; set; } = $"Simple Card description";
            public string ModName { get; set; } = "Modded";
            public bool OwnerOnly { get; set; } = false;
            public CardInfo.Rarity Rarity { get; set; } = CardInfo.Rarity.Common;
            public CardThemeColor.CardThemeColorType Theme { get; set; } = CardThemeColor.CardThemeColorType.TechWhite;
            public CardInfoStat[] Stats { get; set; }
            public GameObject Art { get; set; }
        }

        public sealed override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            if (Details.OwnerOnly && !player.data.view.IsMine) return;
            Added(player, gun, gunAmmo, data, health, gravity, block, characterStats);
        }

        public sealed override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            if (Details.OwnerOnly && !player.data.view.IsMine) return;
            Removed(player, gun, gunAmmo, data, health, gravity, block, statModifiers);
        }

        public sealed override void OnRemoveCard()
        {
            base.OnRemoveCard();
        }

        protected sealed override string GetTitle()
        {
            if (String.IsNullOrWhiteSpace(Details.Title))
                throw new Exception($"Simple cards must have a name!\nMod Name: {GetModName()}\nDescription: {GetDescription()}");

            return Details.Title;
        }
        protected sealed override string GetDescription() => Details.Description;
        public sealed override string GetModName() => Details.ModName;
        protected sealed override CardInfo.Rarity GetRarity() => Details.Rarity;
        protected sealed override CardThemeColor.CardThemeColorType GetTheme() => Details.Theme;
        protected sealed override CardInfoStat[] GetStats() => Details.Stats;
        protected sealed override GameObject GetCardArt() => Details.Art;

        protected virtual void Added(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats) { }
        protected virtual void Removed(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats) { }
    }
}
