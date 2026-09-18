using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClassesManagerReborn.Patchs
{
    [Serializable]
    [HarmonyPatch(typeof(Enum), "GetValues")]
    internal class RarityEnumValues
    {
        private static void Postfix(Type enumType, ref Array __result)
        {
            if(enumType == typeof(CardInfo.Rarity))
            {
                __result = RarityLib.Utils.RarityUtils.rarities.Keys.ToArray();
            }
        }
    }
    [Serializable]
    [HarmonyPatch(typeof(Enum), "GetNames")]
    internal class RarityEnumNames
    {
        private static void Postfix(Type enumType, ref string[] __result)
        {
            if(enumType == typeof(CardInfo.Rarity))
            {
                __result = RarityLib.Utils.RarityUtils.rarities.Values.Select(r => r.name).ToArray();
            }
        }
    }
    [Serializable]
    [HarmonyPatch(typeof(Enum), "ToString", new Type[] { })]
    internal class RarityEnumToString
    {
        private static void Postfix(Enum __instance, ref string __result)
        {
            if(__instance.GetType() == typeof(CardInfo.Rarity))
            {
                try
                {
                    __result = Enum.GetNames(typeof(CardInfo.Rarity))[(int)(CardInfo.Rarity)__instance];
                }
                catch { }
            }
        }
    }
}
