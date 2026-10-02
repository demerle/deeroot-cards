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
    ///   • Killer attribution: `HealthHandler.DoDamage` overwrites
    ///     `data.lastSourceOfDamage = damagingPlayer` (HealthHandler.cs:241) on
    ///     EVERY tick — including environment ticks with damagingPlayer == null.
    ///     So a "shot them off the map edge" kill runs: bullet tick (writes
    ///     shooter) → wall/void tick (writes NULL, is the lethal one) →
    ///     RPCA_Die sees null and the shooter is swallowed. Fix: our own
    ///     `lastPlayerHurtBy` table, written by a DoDamage PREFIX only when
    ///     damagingPlayer != null (null-ticks can never clobber it), resolved
    ///     with a 5s credit window; vanilla's field stays as fallback.
    ///     The killer's data.view.IsMine gate still applies: the killer's OWN
    ///     client simulated the lethal damage chain, making it the authority
    ///     for its own streak.
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

        /// <summary>
        /// How long after its last damaging hit a player can still be credited
        /// for an "environment finished the job" death (knockback into a wall,
        /// off the map edge, momentum death). Pure "last player ever" would
        /// credit a tap from 30 seconds ago; 5s covers any physical knockback.
        /// </summary>
        private const float CreditWindowSeconds = 5f;

        private static bool harmonyApplied;

        private static readonly Dictionary<int, int> streaks = new Dictionary<int, int>();

        /// <summary>
        /// Per-victim "last player that damaged me (and when)" — DoDamage ticks
        /// with a null damagingPlayer (wall/void/fall) must NOT clobber this,
        /// unlike vanilla's data.lastSourceOfDamage which they do. Keyed by
        /// playerID like `streaks`; per-client state, same semantics.
        /// </summary>
        private struct LastHit
        {
            public Player player;
            public float time;
        }

        /// <summary>
        /// Per-victim "last player that damaged me (and when)" — DoDamage ticks
        /// with a null damagingPlayer (wall/void/fall) must NOT clobber this,
        /// unlike vanilla's data.lastSourceOfDamage which they do. Keyed by
        /// playerID like `streaks`; per-client state, same semantics.
        /// </summary>
        private static readonly Dictionary<int, LastHit> lastPlayerHurtBy = new Dictionary<int, LastHit>();

        public static void Init()
        {
            if (harmonyApplied)
            {
                return;
            }
            harmonyApplied = true;
            var harmony = new Harmony("com.deeroot.cards.killstreak");
            harmony.PatchAll(typeof(KillStreakAttributionPatch));
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
                // Bots share master.playerID, so consume the slot here too —
                // bot deaths must not leave a stale shooter behind (they never
                // reach CreditKiller's consume).
                lastPlayerHurtBy.Remove(victim.playerID);
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

        // ---- killer resolution --------------------------------------------------

        /// <summary>Called by KillStreakAttributionPatch on each real player hit.</summary>
        private static void RecordPlayerHit(int victimPlayerID, Player damagingPlayer)
        {
            lastPlayerHurtBy[victimPlayerID] = new LastHit { player = damagingPlayer, time = Time.time };
        }


        /// <summary>
        /// Prefers our knockback-safe `lastPlayerHurtBy` table (written every
        /// non-null DoDamage tick, never clobbered by environment ticks) when
        /// the victim died within CreditWindowSeconds of the last player hit;
        /// otherwise falls back to vanilla `data.lastSourceOfDamage`.
        /// CONSUME-AFTER-READ: the victim's slot is removed here, immediately
        /// after the lookup — this is the single lifecycle point. An earlier
        /// version removed the slot in OnPlayerDeath BEFORE CreditKiller ran,
        /// wiping the witness and silently disabling the whole feature
        /// (playtest-caught 2026-10).
        /// </summary>
        private static Player ResolveKiller(CharacterData victimData, Player victim)
        {
            if (lastPlayerHurtBy.TryGetValue(victim.playerID, out LastHit lastHit))
            {
                lastPlayerHurtBy.Remove(victim.playerID);
                float since = Time.time - lastHit.time;
                bool withinWindow = since >= 0f && since <= CreditWindowSeconds;
                if (withinWindow)
                {
                    if (lastHit.player != null && lastHit.player != victimData.lastSourceOfDamage)
                    {
                        UnityEngine.Debug.Log(
                            $"[DEER] KillStreak: credited via knockback/started-kill ({since:F1}s after last player hit)"
                        );
                    }
                    return lastHit.player;
                }
                UnityEngine.Debug.Log("[DEER] KillStreak: knockback credit window expired — falling back to vanilla attribution");
            }
            return victimData.lastSourceOfDamage;
        }

        private static void CreditKiller(CharacterData victimData, Player victim)
        {
            Player killer = ResolveKiller(victimData, victim);
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
            lastPlayerHurtBy.Clear();
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

        // Knockback-safe killer attribution: vanilla DoDamage overwrites
        // data.lastSourceOfDamage on EVERY tick, including environment hits
        // with damagingPlayer == null — a knockback-into-wall kill runs its
        // environment tick LAST, wiping the shooter before RPCA_Die. This
        // prefix only ever writes when damagingPlayer != null, so the last
        // real shooter survives until death clears the slot. Prefix (not
        // postfix) keeps writes strictly before same-call death Broadcast.
        internal static class KillStreakAttributionPatch
        {
            [HarmonyPatch(typeof(HealthHandler), "DoDamage")]
            [HarmonyPrefix]
            private static void Prefix(HealthHandler __instance, Player damagingPlayer)
            {
                if (damagingPlayer == null)
                {
                    return; // environment click — leave the last real shooter in place
                }
                var victimData = __instance != null ? __instance.GetComponent<CharacterData>() : null;
                if (victimData == null || victimData.player == null)
                {
                    return;
                }
                KillStreakTracker.RecordPlayerHit(victimData.player.playerID, damagingPlayer);
            }
        }
    }
}
