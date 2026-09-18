using HarmonyLib;
using ItemShops.Utils;

namespace ItemShops.Patches
{
    [HarmonyPatch(typeof(CardChoice))]
    class CardChoice_Patch
    {
        [HarmonyPatch("DoPlayerSelect")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        static bool Prefix()
        {
            return !ShopManager.isLockingInput;
        }
    }
}