using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using ModdingUtils.AIMinion.Extensions;
using ModdingUtils.AIMinion;
using UnboundLib;

namespace ModdingUtils.AIMinion.Patches
{
    // patch to fix DealDamageToPlayer.Go
    [Serializable]
    [HarmonyPatch(typeof(DealDamageToPlayer), "Go")]
    class DealDamageToPlayerPatchGo
    {
        private static void Prefix(DealDamageToPlayer __instance)
        {
            __instance.SetFieldValue("target", null);
        }
    }
}
