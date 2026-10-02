using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnboundLib.GameModes;
using UnboundLib.Cards;

namespace DeerootCards.Cards
{
    /// <summary>
    /// The KillStreak engine: counts each player's kills-in-a-row-without-dying
    /// and grants registered one-shot cards when the streak crosses a milestone.
    ///
    /// Death/credit detection (decompile-verified):
    ///   • Both vanilla death paths — `HealthHandler.RPCA_Die` ("really dead")
    ///     and `RPCA_Die_Phoenix` (Phoenix revival) — are `[PunRPC]` with
    ///     RpcTarget.All, so EVERY client learns of EVERY death. Postfixes on
    ///     both cover the whole death surface, including the Phoenix double
    ///     death (each death resets the streak independently, per spec).
    ///   • Killer attribution is FREE: `HealthHandler.DoDamage` always writes
    ///     `data.lastSourceOfDamage = damagingPlayer` (HealthHandler.cs:241)
    ///     right before firing the death RPC. Pit/wall deaths leave it stale
    ///     per client — so we only credit when the killer's data.view.IsMine:
    ///     the killer's OWN client simulated or received the lethal damage
    ///     with itself as source, making it the authority for its own streak.
    ///   • Simulacrum/Sovereign bots inherit their master's playerID, so a bot
    ///     kill increments the master's streak entry (bots are the master's
    ///     proxies). A bot DEATH is out of the ledger entirely: it neither
    ///     resets its master's streak (registry-gated via SovereignBot/
    ///     SimulacrumBot.IsBot — bots share master.playerID, a plain playerID
    ///     reset would wipe the master's real streak with it) NOR credits the
    ///     killer — otherwise an enemy holding KillStreak could farm our bot
    ///     spawns as free milestone kills (anti-farm rule, 2026-10).
    ///
    /// State: static Dictionary keyed by playerID — survives round boundaries
    /// with zero extra plumbing (players keep their playerID within a match),
    /// cleared on HookGameStart so a fresh match starts fresh.
    ///
    /// Loop: displayed count = ((streak - 1) % LoopLength) + 1; entering
    /// displayed 3/4/5 grants Meteor/Simulacrum/Time Stop respectively and the
    /// count wraps — every 5 kills is one full streak cycle.
    /// </summary>
    public static class KillStreakTracker
    {
        /// <summary>Circles displayed / kills per full streak cycle. Retune freely.</summary>
        public const int LoopLength = 5;

        private static bool harmonyApplied;

        private static readonly Dictionary<int, int> streaks = new Dictionary<int, int>();

        public static void Init()
        {
            if (harmonyApplied)
            {
                return;
            }
            harmonyApplied = true;
            var harmony = new Harmony("com.deeroot.cards.killstreak");
            harmony.PatchAll(typeof(KillStreakDeathPatch));
            harmony.PatchAll(typeof(KillStreakDeathPhoenixPatch));

            // PATCH VERIFICATION: Harmony's PatchAll(Type) silently no-ops when
            // given a type without annotated methods (it does NOT recurse into
            // nested types — cost us a debug round). Log exactly what got
            // installed so a silent no-op can never hide again.
            var patched = new List<string>();
            foreach (var method in harmony.GetPatchedMethods())
            {
                patched.Add(method.Name);
            }
            UnityEngine.Debug.Log(
                $"[DEER] KillStreakTracker armed — patched {patched.Count} method(s): {string.Join(", ", patched)}"
            );

            // A new GAME (match) — not a new point/round — wipes the streaks.
            // Hook actions MUST be IEnumerator-returning (Unbound GameModeHook
            // runs them as coroutines; a null return throws an NRE in
            // ErrorTolerantHook after the body ran — cost us a debug round).
            GameModeManager.AddHook(GameModeHooks.HookGameStart, gm => ResetAllRoutine());
            KillStreakHud.EnsureDriver();
            UnityEngine.Debug.Log("[DEER] KillStreakTracker armed — credits require the killer to hold KillStreak");
        }

        private static System.Collections.IEnumerator ResetAllRoutine()
        {
            ResetAll();
            yield break;
        }

        // ---- death pipeline ----------------------------------------------------

        internal static void OnPlayerDeath(HealthHandler healthHandler)
        {
            var data = healthHandler.GetComponent<CharacterData>();
            if (data == null || data.player == null)
            {
                return;
            }
            Player victim = data.player;

            // KillStreak holders may hold it on the master while bots share the
            // playerID — and a bot death is entirely OUT of the streak ledger
            // (anti-farm rule 2026-10): it must neither reset its master's
            // streak NOR credit anyone. Without the credit gate, an enemy with
            // KillStreak could farm our Sovereign/Simulacrum bot spawns as free
            // milestone kills (Meteor/Simulacrum/Time Stop). Bot KILLS still
            // credit the master normally — only bot DEATHS are ignored.
            bool victimIsOurBot = SovereignBot.IsBot(victim) || SimulacrumBot.IsBot(victim);
            if (victimIsOurBot)
            {
                Player botKiller = data.lastSourceOfDamage;
                UnityEngine.Debug.Log(
                    $"[DEER] KillStreak: bot of player {victim.playerID} died (killer: {(botKiller != null && botKiller.data != null ? botKiller.data.name : "none")}) — not a streak kill"
                );
                return;
            }

            if (GetStreak(victim.playerID) != 0)
            {
                UnityEngine.Debug.Log($"[DEER] KillStreak: player {victim.playerID} died — streak reset");
            }
            streaks[victim.playerID] = 0;

            CreditKiller(data, victim);
        }

        private static void CreditKiller(CharacterData victimData, Player victim)
        {
            Player killer = victimData.lastSourceOfDamage;
            if (killer == null || killer == victim)
            {
                return; // environment or suicide — nobody's streak grows
            }
            if (killer.teamID == victim.teamID)
            {
                return; // friendly fire / our own bot's master quirk never feed the streak
            }
            if (killer.data == null || killer.data.view == null || !killer.data.view.IsMine)
            {
                return; // only the killer's own client is authoritative for its streak
            }
            if (!HoldsKillStreak(killer))
            {
                return;
            }

            int oldStreak = GetStreak(killer.playerID);
            int newStreak = oldStreak + 1;
            streaks[killer.playerID] = newStreak;

            int oldDisplayed = DisplayedCount(oldStreak);
            int newDisplayed = DisplayedCount(newStreak);
            UnityEngine.Debug.Log($"[DEER] KillStreak: {killer.data.name} kill #{newStreak} (streak {oldDisplayed} → {newDisplayed})");

            string milestoneCard = MilestoneCardFor(newDisplayed);
            if (milestoneCard == null || newDisplayed == oldDisplayed)
            {
                return;
            }

            // Each client runs the same deterministic grant (no card adds at
            // runtime are reliable single-client edits in ROUNDS).
            UnboundLib.NetworkingManager.RPC(
                typeof(KillStreakTracker),
                nameof(RPCA_GrantMilestoneCard),
                killer.playerID,
                milestoneCard
            );
        }

        // ---- streak math -------------------------------------------------------

        private static int GetStreak(int playerID)
        {
            int value;
            return streaks.TryGetValue(playerID, out value) ? value : 0;
        }

        /// <summary>1..LoopLength; kill 6, 11, ... wrap back to 1.</summary>
        internal static int DisplayedCount(int streak)
        {
            if (streak <= 0)
            {
                return 0;
            }
            return ((streak - 1) % LoopLength) + 1;
        }

        internal static int GetDisplayedStreak(int playerID)
        {
            return DisplayedCount(GetStreak(playerID));
        }

        /// <summary>Milestones ordered by displayed count; add/retune here.</summary>
        private static string MilestoneCardFor(int displayedStreak)
        {
            switch (displayedStreak)
            {
                case 3: return OneShotAbility.MeteorCardName;
                case 4: return OneShotAbility.SimulacrumCardName;
                case 5: return OneShotAbility.TimeStopCardName;
                default: return null;
            }
        }

        private static void ResetAll()
        {
            streaks.Clear();
            UnityEngine.Debug.Log("[DEER] KillStreak: new game — all streaks reset");
        }

        // ---- helpers ------------------------------------------------------------

        internal static bool HoldsKillStreak(Player player)
        {
            if (player == null || player.data == null)
            {
                return false;
            }

            // Players with a KillStreak card held (a bot shares its master's
            // playerID but is NOT in PlayerManager.players, so this resolves to
            // the human master for bot kills automatically).
            foreach (var p in PlayerManager.instance.players)
            {
                if (p == null || p.data == null || p.playerID != player.playerID)
                {
                    continue;
                }
                var cards = p.data.currentCards;
                for (int i = 0; i < cards.Count; i++)
                {
                    if (cards[i] != null && cards[i].cardName == KillStreakCard.CardName)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>The ONE local player's displayed streak (HUD source of truth).</summary>
        internal static int LocalDisplayedStreak()
        {
            foreach (var p in PlayerManager.instance.players)
            {
                if (p == null || p.data == null || p.data.view == null || !p.data.view.IsMine)
                {
                    continue;
                }
                return GetDisplayedStreak(p.playerID);
            }
            return 0;
        }

        internal static bool LocalHoldsKillStreak()
        {
            foreach (var p in PlayerManager.instance.players)
            {
                if (p == null || p.data == null || p.data.view == null || !p.data.view.IsMine)
                {
                    continue;
                }
                return HoldsKillStreak(p);
            }
            return false;
        }

        // ---- award RPC -----------------------------------------------------------

        [UnboundLib.Networking.UnboundRPC]
        public static void RPCA_GrantMilestoneCard(int playerID, string cardName)
        {
            UnityEngine.Debug.Log($"[DEER] KillStreak milestone: granting '{cardName}' to player {playerID}");
            Player target = null;
            foreach (var p in PlayerManager.instance.players)
            {
                if (p != null && p.playerID == playerID)
                {
                    target = p;
                    break;
                }
            }
            if (target == null)
            {
                UnityEngine.Debug.LogWarning($"[DEER] KillStreak grant: no player {playerID} locally — skipping");
                return;
            }
            CardAward.GiveCardToPlayer(target, cardName);
        }

        // ---- Harmony plumbing ----------------------------------------------------
        //
        // Each patch class is passed DIRECTLY to its own PatchAll call.
        // BUG FIX: previously these were nested one level deeper inside a
        // container that was passed to PatchAll — Harmony's PatchAll(Type)
        // processes ONLY the type handed to it (its own annotated methods),
        // it does NOT recurse into nested types. The container had no
        // annotated methods, so the call silently no-oped: no death postfixes
        // ever ran, streaks never counted, no card grants (2026-10-01 playtest).

        internal static class KillStreakDeathPatch
        {
            [HarmonyPatch(typeof(HealthHandler), "RPCA_Die")]
            [HarmonyPostfix]
            private static void Postfix(HealthHandler __instance)
            {
                KillStreakTracker.OnPlayerDeath(__instance);
            }
        }

        internal static class KillStreakDeathPhoenixPatch
        {
            [HarmonyPatch(typeof(HealthHandler), "RPCA_Die_Phoenix")]
            [HarmonyPostfix]
            private static void Postfix(HealthHandler __instance)
            {
                KillStreakTracker.OnPlayerDeath(__instance);
            }
        }
    }
}
