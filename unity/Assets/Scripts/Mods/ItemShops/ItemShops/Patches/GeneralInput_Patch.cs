using System;
using HarmonyLib;
using UnityEngine;
using ItemShops.Utils;

namespace BetterChat.Patches
{
    [HarmonyPatch(typeof(GeneralInput))]
    class GeneralInput_Patch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        private static bool BlockPlayerInput(GeneralInput __instance)
        {
            return !ShopManager.isLockingInput;
        }
    }
}