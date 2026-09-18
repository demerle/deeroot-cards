using ClassesManagerReborn.Util;
using HarmonyLib;
using System;
using UnityEngine.UI;

namespace ClassesManagerReborn.Patches
{

    [Serializable]
    [HarmonyPatch(typeof(CardRarityColor), "Toggle")]
    internal class CardRarityColorPatchToggle
    {
        private static void Prefix(bool isOn, CardRarityColor __instance)
        {
            ClassNameMono mono = __instance.GetComponentInParent<ClassNameMono>();
            if (mono != null)
                mono.isOn = isOn;
        }
    }

}
