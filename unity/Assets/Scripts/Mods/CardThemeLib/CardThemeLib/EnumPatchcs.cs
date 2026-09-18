using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CardThemeLib
{
    [Serializable]
    [HarmonyPatch(typeof(Enum))]
    internal class EnumPatchcs
    {
        [HarmonyPostfix]
        [HarmonyPatch("GetValues")]
        private static void Values(Type enumType, ref Array __result)
        {
            if (enumType == typeof(CardThemeColor.CardThemeColorType))
            {
                __result = CardChoice.instance.cardThemes.ToList().Select(t => t.themeType).ToArray();
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch("GetNames")]
        private static void Names(Type enumType, ref string[] __result)
        {
            if (enumType == typeof(CardThemeColor.CardThemeColorType))
            {
                __result = CardChoice.instance.cardThemes.ToList().Select(t => t.themeType.ToString()).ToArray();
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch("ToString", new Type[] { })]
        private static void String(Enum __instance, ref string __result)
        {
            if (__instance.GetType() == typeof(CardThemeColor.CardThemeColorType))
            {
               var t = CardThemeLib.instance.themes.Keys.ToList().Find(k => CardThemeLib.instance.themes[k].themeType == (CardThemeColor.CardThemeColorType)__instance);
                __result = t!=null? t:__result;
            }
        }
    }
}
