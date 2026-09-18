using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace ModdingUtils.AIMinion.Extensions
{
    // ADD FIELDS TO CHARACTERDATA
    [Serializable]
    public class CharacterDataAdditionalData
    {
        public List<Player> minions;
        public List<Player> oldMinions;
        public bool isAIMinion;
        public bool isEnabled;
        public Player spawner;
        public AIMinionHandler.SpawnLocation spawnLocation;
        public bool autoBlock;

        public CharacterDataAdditionalData()
        {
            minions = new List<Player>() { };
            oldMinions = new List<Player>() { };
            isAIMinion = false;
            isEnabled = true;
            spawner = null;
            spawnLocation = AIMinionHandler.SpawnLocation.Owner_Random;
            autoBlock = false;
        }
    }
    public static class CharacterDataExtension
    {
        public static readonly ConditionalWeakTable<CharacterData, CharacterDataAdditionalData> data =
            new ConditionalWeakTable<CharacterData, CharacterDataAdditionalData>();

        public static CharacterDataAdditionalData GetAdditionalData(this CharacterData characterData)
        {
            return data.GetOrCreateValue(characterData);
        }

        public static void AddData(this CharacterData characterData, CharacterDataAdditionalData value)
        {
            try
            {
                data.Add(characterData, value);
            }
            catch (Exception) { }
        }
    }
    // patch Player.FullReset to properly clear extra stats
    [HarmonyPatch(typeof(Player), "FullReset")]
    class PlayerPatchFullReset_CharacterDataExtension
    {
        private static void Prefix(Player __instance)
        {
            __instance.data.GetAdditionalData().oldMinions = new List<Player>(__instance.data.GetAdditionalData().minions);
            __instance.data.GetAdditionalData().minions = new List<Player>() { };
            __instance.data.GetAdditionalData().isAIMinion = false;
            __instance.data.GetAdditionalData().isEnabled = true;
            __instance.data.GetAdditionalData().spawner = null;
            __instance.data.GetAdditionalData().spawnLocation = AIMinionHandler.SpawnLocation.Owner_Random;
            __instance.data.GetAdditionalData().autoBlock = false;
        }
    }
    
}
