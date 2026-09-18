using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace CardThemeLib
{
    [Serializable]
    [HarmonyPatch(typeof(CardChoice), "Awake")]
    internal class CardChoicePatch
    {
        public static void Postfix()
        {
            CardThemeLib.instance.SetUpThemes();
        }
    }
}
