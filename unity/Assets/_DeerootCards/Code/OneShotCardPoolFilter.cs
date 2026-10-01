using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Keeps every registered one-shot card OUT of the general in-between-rounds
    /// card picker, while leaving them spawnable through direct paths
    /// (CardChoice.AddCard, future reward systems) and fully functional once held.
    ///
    /// Mechanism (decompile-verified, CardChoice.cs): all random offers come from
    /// the single private CardChoice.GetRanomCard(), which rarity-weights
    /// CardChoice.instance.cards. Rework-free exclusion = swap the `cards` array
    /// to a filtered copy in a Prefix, restore it in a Postfix — the orig reads
    /// only that field. Rarity weights are recomputed from the filtered array
    /// every roll, so odds self-normalize. Effects must NOT be re-rolled here;
    /// the possible infinite-retry loop lives in SpawnUniqueCard, the caller.
    /// </summary>
    public static class OneShotCardPoolFilter
    {
        private static bool harmonyApplied;

        private static CardInfo[] originalCards;

        private static bool swapped;

        public static void Init()
        {
            if (harmonyApplied)
            {
                return;
            }
            harmonyApplied = true;
            var harmony = new Harmony("com.deeroot.cards.oneshot");
            var orig = AccessTools.Method(typeof(CardChoice), "GetRanomCard");
            var prefix = new HarmonyMethod(typeof(OneShotCardPoolFilter), nameof(GetRanomCardPrefix));
            var postfix = new HarmonyMethod(typeof(OneShotCardPoolFilter), nameof(GetRanomCardPostfix));
            harmony.Patch(orig, prefix, postfix);
            UnityEngine.Debug.Log($"[DEER] OneShotCardPoolFilter armed — {nameof(OneShotAbility)} registry excluded from the pick pool ({string.Join(", ", NamedOneShots())})");
        }

        /// <summary>Registry names for the boot log (no CardInfo exists yet this early).</summary>
        private static IEnumerable<string> NamedOneShots()
        {
            yield return OneShotAbility.MeteorCardName;
            yield return OneShotAbility.TimeStopCardName;
            yield return OneShotAbility.SimulacrumCardName;
        }

        private static void GetRanomCardPrefix()
        {
            var choice = CardChoice.instance;
            if (choice == null || choice.cards == null)
            {
                return;
            }
            var filtered = GetFiltered(choice.cards);
            if (filtered == choice.cards)
            {
                return; // nothing to exclude — don't swap anything
            }
            originalCards = choice.cards;
            choice.cards = filtered;
            swapped = true;
        }

        private static void GetRanomCardPostfix()
        {
            if (!swapped)
            {
                return;
            }
            swapped = false;
            var choice = CardChoice.instance;
            if (choice != null && originalCards != null)
            {
                choice.cards = originalCards;
            }
            originalCards = null;
        }

        /// <summary>
        /// Returns `cards` untouched when nothing must be excluded; otherwise a
        /// fresh array without every registered one-shot card. Defensive guard:
        /// if filtering would empty the pool entirely, bail out and let vanilla
        /// run — an empty pool feeds SpawnUniqueCard's infinite retry recursion.
        /// </summary>
        private static CardInfo[] GetFiltered(CardInfo[] cards)
        {
            int excluded = 0;
            foreach (var card in cards)
            {
                if (card != null && OneShotAbility.IsOneShot(card.cardName))
                {
                    excluded++;
                }
            }
            if (excluded == 0 || excluded == cards.Length)
            {
                if (excluded == cards.Length && cards.Length > 0)
                {
                    UnityEngine.Debug.LogError("[DEER] OneShotCardPoolFilter: filtered pool would be EMPTY — letting vanilla run this roll");
                }
                return cards;
            }
            var result = new CardInfo[cards.Length - excluded];
            int outIndex = 0;
            foreach (var card in cards)
            {
                if (card == null || !OneShotAbility.IsOneShot(card.cardName))
                {
                    result[outIndex] = card;
                    outIndex++;
                }
            }
            UnityEngine.Debug.Log($"[DEER] OneShotCardPoolFilter: excluded {excluded} one-shot card(s) from this roll");
            return result;
        }
    }
}
