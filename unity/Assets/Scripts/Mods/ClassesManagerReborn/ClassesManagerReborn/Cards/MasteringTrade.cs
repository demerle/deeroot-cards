using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnboundLib.Cards;
using UnityEngine;
using ModdingUtils.Extensions;
using UnboundLib;

namespace ClassesManagerReborn.Cards
{
    internal class MasteringTrade : CustomCard
    {
        internal static CardInfo card;
        internal static List<Player> masteringPlayers = new List<Player>();
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {
            cardInfo.GetAdditionalData().canBeReassigned = false;
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            masteringPlayers.Add(player);
        }

        internal static IEnumerator IAddClassCards(Player player)
        {
            ClassObject[] classObjects = ClassesRegistry.GetClassObjects(~CardType.Entry).Where(classObj => ModdingUtils.Utils.Cards.instance.PlayerIsAllowedCard(player, classObj.card) && ModdingUtils.Utils.Cards.active.Contains(classObj.card)).ToArray();

            foreach (var classObj in classObjects)
            {
                UnityEngine.Debug.Log(classObj.card.cardName);
            }

            List<CardInfo> classes = ClassesRegistry.GetClassObjects(~CardType.Card).Select(obj => obj.card).Intersect(player.data.currentCards).Distinct().ToList();

            classes = classes.Where(card => { 
                return classObjects.Any(classObj => { 
                    return classObj.RequiredClassesTree.Any(list => {

                        var names = list.Select(item => item.cardName);
                        var reqString = string.Join(", ", names);
                        UnityEngine.Debug.Log($"{card.cardName} ?= {reqString} -> {classObj.card.cardName}");

                        return list.Contains(card); 
                    }); 
                }); 
            }).ToList();

            classes.Shuffle();

            foreach (var classC in classes)
            {
                UnityEngine.Debug.Log(classC.cardName);
            }

            CardInfo chosenClass = classes[0];

            CardInfo[] classCards = classObjects.Where(classObj => classObj.RequiredClassesTree.Any(list => list.Contains(chosenClass))).Select(classObj => classObj.card).ToArray();

            classCards.Shuffle();

            // Add a 2 frame wait before adding the cards
            yield return null;
            yield return null;

            int cardCount = 0;

            foreach (var card in classCards)
            {
                if (!ModdingUtils.Utils.Cards.instance.PlayerIsAllowedCard(player, card))
                {
                    continue;
                }

                cardCount = player.data.currentCards.Count();

                ModdingUtils.Utils.Cards.instance.AddCardToPlayer(player, card, false, "", 2f, 2f, true);

                var time = 0f;

                yield return new WaitUntil(() =>
                {
                    time += Time.deltaTime;
                    return ((player.data.currentCards.Count > cardCount) || (player.data.currentCards[player.data.currentCards.Count - 1] == card) || (time > 5f));
                });

                int i = 0;

                while (i++ < 20)
                {
                    yield return null;
                }
            }

            yield break;
        }

        protected override GameObject GetCardArt()
        {
            return null;
        }

        protected override string GetDescription()
        {
            return "Get a random number of cards for a random class you have.";
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Rare;
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
            return "Mastering the Trade";
        }

        public override string GetModName()
        {
            return "CMR";
        }
    }
}
