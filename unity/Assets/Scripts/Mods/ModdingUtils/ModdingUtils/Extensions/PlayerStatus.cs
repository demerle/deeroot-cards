using System.Collections.Generic;
using HarmonyLib;
using System;

namespace ModdingUtils.Extensions
{
    [Obsolete("ModdingUtils.Extensions.PlayerStatus is deprecated, use ModdingUtils.Utils.PlayerStatus instead.")]
    public static class PlayerStatus
    {
        [Obsolete("ModdingUtils.Extensions.PlayerStatus is deprecated, use ModdingUtils.Utils.PlayerStatus instead.")]
        public static bool PlayerAlive(Player player)
        {
            return !player.data.dead;
        }
        [Obsolete("ModdingUtils.Extensions.PlayerStatus is deprecated, use ModdingUtils.Utils.PlayerStatus instead.")]
        public static bool PlayerSimulated(Player player)
        {
            return (bool)Traverse.Create(player.data.playerVel).Field("simulated").GetValue();
        }
        [Obsolete("ModdingUtils.Extensions.PlayerStatus is deprecated, use ModdingUtils.Utils.PlayerStatus instead.")]
        public static bool PlayerAliveAndSimulated(Player player)
        {
            return (PlayerAlive(player) && PlayerSimulated(player));
        }
        [Obsolete("ModdingUtils.Extensions.PlayerStatus is deprecated, use ModdingUtils.Utils.PlayerStatus instead.")]
        public static int GetNumberOfEnemyPlayers(Player player)
        {
            int num = 0;
            foreach (Player other_player in PlayerManager.instance.players)
            {
                if (other_player.teamID != player.teamID)
                {
                    num++;
                }
            }
            return num;
        }
        [Obsolete("ModdingUtils.Extensions.PlayerStatus is deprecated, use ModdingUtils.Utils.PlayerStatus instead.")]
        public static List<Player> GetEnemyPlayers(Player player)
        {
            List<Player> res = new List<Player>() { };
            foreach (Player other_player in PlayerManager.instance.players)
            {
                if (other_player.teamID != player.teamID)
                {
                    res.Add(other_player);
                }
            }
            return res;
        }
        [Obsolete("ModdingUtils.Extensions.PlayerStatus is deprecated, use ModdingUtils.Utils.PlayerStatus instead.")]
        public static Player GetRandomEnemyPlayer(Player player)
        {
            List<Player> enemies = GetEnemyPlayers(player);
            if (GetNumberOfEnemyPlayers(player) == 0) { return null; }
            return enemies[UnityEngine.Random.Range(0, GetNumberOfEnemyPlayers(player))];
        }
        [Obsolete("ModdingUtils.Extensions.PlayerStatus is deprecated, use ModdingUtils.Utils.PlayerStatus instead.")]
        public static List<Player> GetOtherPlayers(Player player)
        {
            List<Player> res = new List<Player>() { };
            foreach (Player other_player in PlayerManager.instance.players)
            {
                if (other_player.playerID != player.playerID)
                {
                    res.Add(other_player);
                }
            }
            return res;
        }
    }
}