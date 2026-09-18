using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnboundLib;
using UnityEngine;

namespace ModdingUtils.Patches
{
    [HarmonyPatch(typeof(CardChoice))]
    public class CardChoicePatchGetSourceCard
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(CardChoice.GetSourceCard))]
        private static void CheckHiddenCards(CardInfo info, ref CardInfo __result)
        {
            if (__result != null)
            {
                return;
            }

            CardInfo[] hiddenCards = Utils.Cards.instance.HiddenCards.ToArray();

            for (int i = 0; i < hiddenCards.Length; i++)
            {
                if ((hiddenCards[i].gameObject.name + "(Clone)") == info.gameObject.name)
                {
                    __result = hiddenCards[i];
                    break;
                }
            }
        }
    }
}
