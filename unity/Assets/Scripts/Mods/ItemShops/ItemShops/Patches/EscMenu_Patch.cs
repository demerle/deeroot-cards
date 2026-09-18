using HarmonyLib;
using InControl;
using UnityEngine;
using ItemShops.Utils;

namespace ItemShops.Patches
{
    [HarmonyPatch(typeof(EscapeMenuHandler))]
    class EscMenu_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(EscapeMenuHandler.ToggleEsc))]
        static bool CheckShopStatus()
        {
            // Check to see if a shop is open
            var shopOpen = ShopManager.isLockingInput;
            if (shopOpen)
            {
                // Check to see if the filter of the shop is in use.
                // Known issue, there's no way to get when the filter is just being unfocused (and thus typable)
                if (ShopManager.instance.CurrentShop && ShopManager.instance.CurrentShop.filterSelected)
                {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                }
                else // 
                {
                    ShopManager.instance.HideAllShops();
                }
            }
            return !shopOpen;
        }
    }
}
