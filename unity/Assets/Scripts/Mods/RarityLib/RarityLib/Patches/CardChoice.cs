using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RarityLib.Utils;

namespace RarityLib.Patches
{
    [Serializable]
    [HarmonyPatch(typeof(ModdingUtils.Patches.CardChoicePatchGetRanomCard), "OrignialGetRanomCard", new Type[] { typeof(CardInfo[]) })]
    internal class CardChoicePatchGetRanomCard
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(CardInfo[] cards, ref GameObject __result)
        {
            float num = 0f;
            for (int i = 0; i < cards.Length; i++)
            {
                num += RarityUtils.rarities[(int)cards[i].rarity].calculatedRarity * RarityUtils.GetCardRarityModifier(cards[i]);
            }
            float num2 = UnityEngine.Random.Range(0f, num);

			for (int j = 0; j < cards.Length; j++)
            {
                num2 -= RarityUtils.rarities[(int)cards[j].rarity].calculatedRarity * RarityUtils.GetCardRarityModifier(cards[j]);
                if (num2 <= 0f)
				{
                    __result = cards[j].gameObject;
					break;
				}
			}
            return false;
        }
    }
}
