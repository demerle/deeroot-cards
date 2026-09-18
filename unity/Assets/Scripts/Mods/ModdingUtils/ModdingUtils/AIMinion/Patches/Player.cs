using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using ModdingUtils.AIMinion.Extensions;
using ModdingUtils.AIMinion;

namespace ModdingUtils.AIMinion.Patches
{
    // patch to prevent game crash
    [Serializable]
    [HarmonyPatch(typeof(Player), "GetFaceOffline")]
    [HarmonyBefore("com.willis.rounds.unbound")]
    class PlayerPatchGetFaceOffline
    {
        private static bool Prefix(Player __instance)
        {

            if (__instance.playerID >= CharacterCreatorHandler.instance.selectedPlayerFaces.Length || __instance.playerID < 0)
            {
                return false;
            }
            return true;
        }
    }
    // patch to return correct team colors for AI
    [Serializable]
    [HarmonyPatch(typeof(Player), "SetColors")]
    class PlayerPatchSetColors
    {
        private static bool Prefix(Player __instance)
        {

            if (__instance.data.GetAdditionalData().isAIMinion && __instance.data.GetAdditionalData().spawner != null)
            {
                SetTeamColor.TeamColorThis(__instance.gameObject, __instance.data.GetAdditionalData().spawner.GetTeamColors());
                return false;
            }
            return true;
        }
    }
    // patch to return correct team colors for AI
    [Serializable]
    [HarmonyPatch(typeof(Player), "GetTeamColors")]
    class PlayerPatchGetTeamColors
    {
        private static bool Prefix(Player __instance, ref PlayerSkin __result)
        {

            if (__instance.data.GetAdditionalData().isAIMinion && __instance.data.GetAdditionalData().spawner != null)
            {
                __result = __instance.data.GetAdditionalData().spawner.GetTeamColors();
                return false;
            }
            return true;
        }
    }
    // patch to prevent unwanted registering of AIs online
    [Serializable]
    [HarmonyPatch(typeof(Player), "Start")]
    class PlayerPatchStart
    {
        private static bool Prefix(Player __instance)
        {

            return AIMinionHandler.playersCanJoin;

        }
    }
    // patch to prevent unwanted registering of AIs online

    [Serializable]
    [HarmonyPatch(typeof(Player), "AssignPlayerID")]
    class PlayerPatchAssignPlayerID
    {
        private static bool Prefix(Player __instance, int ID)
        {
            __instance.playerID = ID;
            __instance.SetColors();
            return AIMinionHandler.playersCanJoin;
        }
    }
    // patch to prevent unwanted registering of AIs online

    [Serializable]
    [HarmonyPatch(typeof(Player), "AssignTeamID")]
    class PlayerPatchAssignTeamID
    {
        private static bool Prefix(Player __instance, int ID)
        {
            __instance.teamID = ID;
            return AIMinionHandler.playersCanJoin;
        }
    }
    // patch to prevent unwanted registering of AIs online

    [Serializable]
    [HarmonyPatch(typeof(Player), "ReadPlayerID")]
    class PlayerPatchReadPlayerID
    {
        private static bool Prefix(Player __instance)
        {
            return AIMinionHandler.playersCanJoin;
        }
    }
    // patch to prevent unwanted registering of AIs online

    [Serializable]
    [HarmonyPatch(typeof(Player), "ReadTeamID")]
    class PlayerPatchReadTeamID
    {
        private static bool Prefix(Player __instance)
        {
            return AIMinionHandler.playersCanJoin;
        }
    }
}
