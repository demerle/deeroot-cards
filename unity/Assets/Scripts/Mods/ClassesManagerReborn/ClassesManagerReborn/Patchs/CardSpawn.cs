using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnboundLib;
using UnityEngine;

namespace ClassesManagerReborn.Patchs
{
    [Serializable]
    [HarmonyPatch(typeof(CardChoice), "Spawn")]
    internal class CardSpawn
    {
        internal static CardInfo sourceCard = null;
        internal static int targetCard = 0;
        private static void Prefix(CardChoice __instance, ref GameObject objToSpawn, int ___pickrID, List<GameObject> ___spawnedCards, Transform[] ___children, ref GameObject __result, Vector3 pos, Quaternion rot)
        {
            if (!ClassesManager.Ensure_Class_Card.Value) return;
            var player = PlayerManager.instance.players.Find(p => p.playerID == ___pickrID);
            if (ClassesManager.firstHand)
            {
                ++ClassesManager.cardsToDraw;
                return;
            }
            if(___spawnedCards.Count == 0)
            {
                targetCard = UnityEngine.Random.Range(0, ClassesManager.cardsToDraw);
            }
            if (___spawnedCards.Count != targetCard) return;
            if (___spawnedCards.Any(card => ClassesRegistry.Registry.ContainsKey(card.GetComponent<CardInfo>()))) return;
            var cards = ClassesRegistry.GetClassInfos(~CardType.Entry).Where(card => ModdingUtils.Utils.Cards.instance.PlayerIsAllowedCard(player, card)).Intersect(ModdingUtils.Utils.Cards.active).ToArray();
            if (cards.Length == 0) return;
            cards.Shuffle();
            sourceCard = cards[0];
            objToSpawn = sourceCard.gameObject;
        }

    }
    [Serializable]
    [HarmonyPatch(typeof(CardChoice), "SpawnUniqueCard")]
    class SpawnUniqueCard
    {
        private static void Postfix(ref GameObject __result)
        {
            if (CardSpawn.sourceCard) 
                __result.GetComponent<CardInfo>().sourceCard = CardSpawn.sourceCard;
            CardSpawn.sourceCard = null;
        }
    }
}
