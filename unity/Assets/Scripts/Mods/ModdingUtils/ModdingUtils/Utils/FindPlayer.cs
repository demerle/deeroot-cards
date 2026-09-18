using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Linq;
using UnboundLib;
using Photon.Pun;
using UnboundLib.Cards;
using System.Reflection;
using HarmonyLib;
using UnboundLib.Networking;
using UnityEngine;
using TMPro;
using ModdingUtils.Extensions;
using UnboundLib.Utils;

namespace ModdingUtils.Utils
{
    public static class FindPlayer
    {
        public static Player GetPlayerWithActorAndPlayerIDs(int actorID, int playerID)
        {
            Player res = null;
            foreach (Player player in PlayerManager.instance.players)
            {
                if (player.data.view.ControllerActorNr == actorID && player.playerID == playerID) { res = player; break; }
            }
            return res;
        }
    }
}
