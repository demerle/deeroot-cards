using System;
using System.Collections.Generic;
using System.Text;
using UnboundLib.Cards;
using UnityEngine;
using ModdingUtils.Extensions;
using UnboundLib;
using System.Linq;
using UnityEngine.UI;
using TMPro;

namespace ClassesManagerReborn.Cards
{
    internal class JACK : CustomCard
    {
        internal static CardInfo card;
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {
            cardInfo.GetAdditionalData().canBeReassigned = false;
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            ClassesManager.instance.ExecuteAfterFrames(2, () =>
            {
                List<CardInfo> classes = ClassesRegistry.GetClassInfos(CardType.Entry).Intersect(ModdingUtils.Utils.Cards.active).ToList();
                foreach (CardInfo card in classes)
                {
                    if (!player.data.currentCards.Contains(card))
                        ModdingUtils.Utils.Cards.instance.AddCardToPlayer(player, card, addToCardBar: true);
                }
            });
        }

        protected override GameObject GetCardArt()
        {
            return ClassesManager.assets.LoadAsset<GameObject>("C_JACK");
        }

        protected override string GetDescription()
        {
            return "Master of none";
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return RarityLib.Utils.RarityUtils.GetRarity("Legendary");
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[] { };
        }

        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.NatureBrown;
        }

        protected override string GetTitle()
        {
            return "JACK";
        }

        public override string GetModName()
        {
            return "CMR";
        }

        public override void Callback()
        {
            gameObject.GetOrAddComponent<Rainbow>();
            base.Callback();
        }
    }

    internal class Rainbow : MonoBehaviour
    {
        CardVisuals visuals;
        float timer;
        List<CardThemeColor> cardThemeColors = CardChoice.instance.cardThemes.ToList();
        System.Random random = new System.Random();
        public void Start()
        {
            visuals = GetComponentInChildren<CardVisuals>();
            timer = 1;
        }
        public void Update()
        {
            timer += TimeHandler.deltaTime;
            if (timer > 0.25f && visuals.isSelected)
            {
                timer = 0;
                visuals.defaultColor = cardThemeColors[random.Next(9)].targetColor;
                visuals.chillColor = visuals.defaultColor;
                for (int i = 0; i < visuals.images.Length; i++)
                {
                    visuals.images[i].color = cardThemeColors[random.Next(9)].targetColor;
                }
                visuals.nameText.color = visuals.defaultColor;
            }
            else if (!visuals.isSelected) timer = 1;
        }
    }
}
