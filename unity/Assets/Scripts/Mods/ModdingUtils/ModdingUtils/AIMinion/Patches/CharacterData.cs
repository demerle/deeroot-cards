using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using ModdingUtils.AIMinion.Extensions;
using UnityEngine;
using UnboundLib;
using ModdingUtils.AIMinion;

namespace ModdingUtils.AIMinion.Patches
{

    // patch to prevent unwanted registering of AIs online

    [Serializable]
    [HarmonyPatch(typeof(CharacterData), "Start")]
    class CharacterDataPatchStart
    {
        private static bool Prefix(CharacterData __instance)
        {
            __instance.SetFieldValue("groundMask", (LayerMask)LayerMask.GetMask(new string[]
            {
                    "Default"
            }));
            return AIMinionHandler.playersCanJoin;
        }
    }
}
