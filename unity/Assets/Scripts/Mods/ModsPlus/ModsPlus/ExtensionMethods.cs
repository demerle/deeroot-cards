using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnboundLib;

namespace ModsPlus
{
    public static class ExtensionMethods
    {
        public static void RemovePlayerDiedAction(this PlayerManager pm, Action<Player, int> listener)
        {
            var action = (Action<Player, int>)pm.GetFieldValue("PlayerDiedAction");
            action -= listener;
        }

        public static Player GetPlayerWithID(this PlayerManager pm, int playerId)
        {
            return (Player)pm.InvokeMethod("GetPlayerWithID", playerId);
        }

        public static bool TryGetComponent<T>(this GameObject go, out T component) where T : Component
        {
            component = go.GetComponent<T>();
            return component != null;
        }
    }
}
