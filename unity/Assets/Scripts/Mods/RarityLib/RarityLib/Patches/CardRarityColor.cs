using HarmonyLib;
using System;
using UnityEngine.UI;

namespace RarityLib.Patches
{

    [Serializable]
    [HarmonyPatch(typeof(CardRarityColor), "Toggle")]
    internal class CardRarityColorPatchToggle
    {
        private static bool Prefix(bool isOn, CardRarityColor __instance)
        {
            CardInfo componentInParent = __instance.GetComponentInParent<CardInfo>();
            __instance.GetComponent<Image>().color = (isOn ? RarityLib.Utils.RarityUtils.rarities[(int)componentInParent.rarity].color : RarityLib.Utils.RarityUtils.rarities[(int)componentInParent.rarity].colorOff);
            return false;
        }
    }
}
