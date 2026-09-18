using System;
using HarmonyLib;
using UnityEngine;

namespace ModdingUtils.AIMinion.Patches
{
    // patch to fix lag when no enemies are present
    [Serializable]
    [HarmonyPatch(typeof(PlayerAPI), "TowardsOtherPlayer")]
    class PlayerAPIPatchTowardsOtherPlayer
    {
        private static bool Prefix(PlayerAPI __instance, ref Vector2 __result)
        {
            if (PlayerManager.instance.GetOtherPlayer(__instance.player) == null)
            {
                __result = Vector2.zero;
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
