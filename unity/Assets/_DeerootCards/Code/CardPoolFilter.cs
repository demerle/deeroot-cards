using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ModdingUtils.Utils;

// Our namespace IS "Cards" — alias the ModdingUtils class to dodge the collision.
using ModdingCards = ModdingUtils.Utils.Cards;
using UnityEngine;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Keeps blacklisted cards OUT of the general in-between-rounds card picker
    /// ("choose from 5"), while leaving them spawnable through direct paths
    /// (OneShotAbility rewards, CardAward, future systems) and fully functional
    /// once held.
    ///
    /// ROOT CAUSE of the 2026-10 failures ("exactly the same as before"): the
    /// between-round pick offers on any modded install are NOT drawn by the
    /// vanilla funnel `CardChoice.GetRanomCard()` — pykess's
    /// CardChoiceSpawnUniqueCardPatch (installed with every modded ROUNDS)
    /// replaces `CardChoice.SpawnUniqueCard` wholesale (prefix returns false)
    /// and draws via `Cards.instance.GetRandomCardWithCondition(...)`, which
    /// reads `CardChoice.instance.cards` DIRECTLY. A patch on GetRanomCard
    /// arms fine and never runs.
    ///
    /// Fix: swap `CardChoice.instance.cards` to a filtered copy around every
    /// known pick funnel —
    ///   1. `ModdingUtils.Utils.Cards.GetRandomCardWithCondition` (the real one),
    ///   2. `Cards.NORARITY_GetRandomCardWithCondition` (its variant),
    ///   3. vanilla `CardChoice.GetRanomCard` (belt-and-braces for unmodded paths).
    /// No transpiler; weights self-normalize because each orig recomputes from
    /// whatever array it sees. Never an empty pool (SpawnUniqueCard recurses
    /// infinitely), never re-roll in the postfix. Every funnel logs its own
    /// exclusion lines, so the ACTIVE path on any install is self-evident.
    /// </summary>
    public static class CardPoolFilter
    {
        private const string LogTag = "[DEER] CardPoolFilter";

        private static bool harmonyApplied;

        /// <summary>Banned card names (pick-pool only — never spawn-banned anywhere else).</summary>
        private static readonly HashSet<string> banned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static CardInfo[] originalCards;

        private static bool swapped;

        private static bool loggedFirstRoll;

        /// <summary>
        /// Ban card names (registered titles or vanilla uppercase cardName —
        /// matching is OrdinalIgnoreCase) from ALL random pick offers.
        /// Already-armed bans are idempotent.
        /// </summary>
        public static void BanFromPicks(params string[] cardNames)
        {
            foreach (var name in cardNames)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    banned.Add(name);
                }
            }
        }

        public static void Init()
        {
            if (harmonyApplied)
            {
                return;
            }
            harmonyApplied = true;

            // Current ban list — all this mod's own cards, one visible line each.
            BanFromPicks(
                OneShotAbility.MeteorCardName,
                OneShotAbility.TimeStopCardName,
                OneShotAbility.SimulacrumCardName,
                PowerUpCard.CardName
            );

            // Funnel 1+2 — the REAL pick funnel(s) on modded installs (pykess's
            // CardChoiceSpawnUniqueCardPatch draws through these).
            PatchFunnel(typeof(ModdingCards), nameof(ModdingCards.GetRandomCardWithCondition), "GetRandomCardWithCondition",
                nameof(GetRandomCardWithConditionPrefix), nameof(GetRandomCardWithConditionPostfix));
            PatchFunnel(typeof(ModdingCards), nameof(ModdingCards.NORARITY_GetRandomCardWithCondition), "NORARITY_GetRandomCardWithCondition",
                nameof(NorarityPrefix), nameof(NorarityPostfix));

            // Funnel 3 — the vanilla funnel; dead on pykess installs but armed
            // anyway for coverage of unmodded/vanilla-based paths.
            PatchFunnel(typeof(CardChoice), "GetRanomCard", "GetRanomCard",
                nameof(GetRanomCardPrefix), nameof(GetRanomCardPostfix));

            UnityEngine.Debug.Log($"{LogTag}: armed — banned from offers: ({string.Join(", ", banned)})");
        }

        private static void PatchFunnel(Type type, string methodName, string funnelLabel, string prefixName, string postfixName)
        {
            var harmony = new Harmony("com.deeroot.cards.cardpoolfilter");
            var target = AccessTools.Method(type, methodName);
            if (target == null)
            {
                UnityEngine.Debug.LogError($"{LogTag}: FATAL — funnel {funnelLabel} not found on this install; that path is UNFILTERED now.");
                return;
            }
            try
            {
                var prefix = new HarmonyMethod(typeof(CardPoolFilter), prefixName);
                var postfix = new HarmonyMethod(typeof(CardPoolFilter), postfixName);
                harmony.Patch(target, prefix, postfix);
                UnityEngine.Debug.Log($"{LogTag}: funnel patched — {funnelLabel}");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"{LogTag}: FATAL — patching funnel {funnelLabel} threw: {e}");
            }
        }

        /// <summary>
        /// Shared prefix logic — labels tell you which funnel your install
        /// actually uses for pick offers.
        /// </summary>
        [HarmonyPrefix]
        private static void GetRandomCardWithConditionPrefix()
        {
            SwapIn("GetRandomCardWithCondition");
        }

        [HarmonyPrefix]
        private static void NorarityPrefix()
        {
            SwapIn("NORARITY_GetRandomCardWithCondition");
        }

        [HarmonyPrefix]
        private static void GetRanomCardPrefix()
        {
            SwapIn("GetRanomCard");
        }

        [HarmonyPostfix]
        private static void GetRandomCardWithConditionPostfix(ref CardInfo __result)
        {
            VerifyRoll("GetRandomCardWithCondition", __result);
            SwapOut();
        }

        [HarmonyPostfix]
        private static void NorarityPostfix(ref CardInfo __result)
        {
            VerifyRoll("NORARITY_GetRandomCardWithCondition", __result);
            SwapOut();
        }

        [HarmonyPostfix]
        private static void GetRanomCardPostfix(ref GameObject __result)
        {
            string cardName = null;
            if (__result != null)
            {
                var info = __result.GetComponent<CardInfo>();
                cardName = info != null ? info.cardName : null;
            }
            VerifyRoll("GetRanomCard", null, cardName);
            SwapOut();
        }

        private static void VerifyRoll(string funnel, CardInfo info, string cardName = null)
        {
            var name = info != null ? info.cardName : cardName;
            if (name != null && banned.Contains(name))
            {
                UnityEngine.Debug.LogError($"{LogTag}: ROLL VERIFICATION via {funnel} — banned card '{name}' escaped the ban set");
            }
        }

        /// <summary>First-roll diagnostics + the actual swap-in (funnel-tagged).</summary>
        private static void SwapIn(string funnel)
        {
            var choice = CardChoice.instance;
            if (choice == null || choice.cards == null)
            {
                return;
            }
            if (!loggedFirstRoll)
            {
                loggedFirstRoll = true;
                // One-time diagnostic: prove the ban set CAN match this pool,
                // and surface the literal spellings of every banned card found.
                int found = 0;
                var names = new List<string>();
                foreach (var card in choice.cards)
                {
                    if (card != null && banned.Contains(card.cardName))
                    {
                        found++;
                        names.Add($"'{card.cardName}' (rarity {card.rarity})");
                    }
                }
                UnityEngine.Debug.Log($"{LogTag}: FIRST ROLL via {funnel} — pool of {choice.cards.Length} card(s), banned among them: {(found > 0 ? string.Join("; ", names) : "none")}");
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

        private static void SwapOut()
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
        /// fresh array without every banned card. Defensive guard: if filtering
        /// would empty the pool entirely, bail out and let vanilla run — an
        /// empty pool feeds SpawnUniqueCard's infinite retry recursion.
        /// </summary>
        private static CardInfo[] GetFiltered(CardInfo[] cards)
        {
            int excluded = 0;
            foreach (var card in cards)
            {
                if (card != null && banned.Contains(card.cardName))
                {
                    excluded++;
                }
            }
            if (excluded == 0 || excluded == cards.Length)
            {
                if (excluded == cards.Length && cards.Length > 0)
                {
                    UnityEngine.Debug.LogError($"{LogTag}: filtered pool would be EMPTY — letting vanilla run this roll");
                }
                return cards;
            }
            var result = new CardInfo[cards.Length - excluded];
            int outIndex = 0;
            foreach (var card in cards)
            {
                if (card == null || !banned.Contains(card.cardName))
                {
                    result[outIndex] = card;
                    outIndex++;
                }
            }
            UnityEngine.Debug.Log($"{LogTag}: excluded {excluded} banned card(s) from this roll ({string.Join(", ", banned)})");
            return result;
        }
    }
}
