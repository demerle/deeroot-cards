using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using ModdingUtils.AIMinion.Extensions;
using ModdingUtils.AIMinion;
using UnboundLib;
using ModdingUtils.Utils;
using System.Linq;
using UnityEngine;
using BepInEx.Bootstrap;
using BepInEx;

namespace ModdingUtils.AIMinion.Patches
{
    // patch to fix GetClosestPlayer ONLY IF RWF isn't loaded - patch stolen from RWF
    [HarmonyPatch(typeof(PlayerManager), "GetOtherPlayer")]
    [HarmonyAfter(new string[]{"io.olavim.rounds.rwf"})]
    class PlayerManager_Patch_GetOtherPlayer
    {
        private static bool Prefix(PlayerManager __instance, Player asker, ref Player __result)
        {
            __result = __instance.GetClosestPlayerInOtherTeam(asker.transform.position, asker.teamID, false);
            return false;
        }
    }
    public static class PlayerManagerExtensions
    {
        /*
        private static bool? _RWFLoaded = null;
        internal static bool RWFLoaded
        {
            get
            {
                if (_RWFLoaded != null)
                {
                    return (bool)_RWFLoaded;
                }
                else
                {
                    Dictionary<string, PluginInfo> loadedMods = BepInEx.Bootstrap.Chainloader.PluginInfos;
                    if (loadedMods.Keys.Select(ID => loadedMods[ID]).Where(mod => mod.Metadata.GUID == "io.olavim.rounds.rwf").Any())
                    {
                        _RWFLoaded = true;
                        return (bool)_RWFLoaded;
                    }
                    else
                    {
                        _RWFLoaded = false;
                        return (bool)_RWFLoaded;
                    }

                }
            }
            set { }
        }*/

        public static Player GetClosestPlayerInOtherTeam(this PlayerManager instance, Vector3 position, int team, bool needVision = false)
        {
            float num = float.MaxValue;

            var alivePlayersInOtherTeam = instance.players
                .Where(p => p.teamID != team)
                .Where(p => !p.data.dead)
                .ToList();

            Player result = null;

            for (int i = 0; i < alivePlayersInOtherTeam.Count; i++)
            {
                float num2 = Vector2.Distance(position, alivePlayersInOtherTeam[i].transform.position);
                if ((!needVision || instance.CanSeePlayer(position, alivePlayersInOtherTeam[i]).canSee) && num2 < num)
                {
                    num = num2;
                    result = alivePlayersInOtherTeam[i];
                }
            }

            return result;
        }
    }
    // patch to return the correct number of TEAMS remaining so that Deathmatch works
    [Serializable]
    [HarmonyPatch(typeof(PlayerManager), "PlayerDied")]
    class PlayerManagerPatchPlayerDied
    {
        private static bool Prefix(PlayerManager __instance, Player player)
        {
            int num = PlayerManager.instance.players.Where(p => !p.data.dead).Select(p => p.teamID).Distinct().Count();

            if ((Action<Player,int>)__instance.GetFieldValue("PlayerDiedAction") != null)
            {
                ((Action<Player, int>)__instance.GetFieldValue("PlayerDiedAction"))(player, num);
            }
            return false;

        }
    }
}
