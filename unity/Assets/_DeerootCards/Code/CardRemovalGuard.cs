using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace DeerootCards
{
    /// <summary>
    /// UnboundLib's card removal (Cards.RemoveCardFromPlayer → Player.FullReset)
    /// fires CustomCard.OnRemoveCard for EVERY held card inside UnboundLib's
    /// Player.FullReset postfix — not just the card being removed. Any deck
    /// rebuild (one-shot consume, Delete card, Double card, foreign mods) would
    /// otherwise destroy our stateful ability effects even though the card is
    /// still held.
    ///
    /// Register() defers the teardown past the rebuild (re-add lands 0.1s after
    /// the removal, plus RPC latency), then re-checks the deck:
    ///   - card still held  → it was a rebuild echo → teardown cancelled,
    ///     optional re-assert callback runs instead,
    ///   - card really gone → the real teardown runs.
    /// </summary>
    public static class CardRemovalGuard
    {
        /// <summary>
        /// UnboundLib schedules the rebuild's re-add via ExecuteAfterSeconds(0.1)
        /// on the master/offline client and an RPC to the rest — a full unscaled
        /// second covers even laggy online arrivals; then two frames of settle.
        /// </summary>
        private const float RebuildEchoDelaySeconds = 1f;

        /// <summary>
        /// Called from a CustomCard.OnRemoveCard instead of destroying/de-spawning
        /// immediately. realTeardown runs only when the card is verifiably gone
        /// from this player's deck after the rebuild window.
        /// </summary>
        public static void Register(Player player, string cardName, Action realTeardown, Action rebuildEcho = null)
        {
            if (player == null || player.data == null)
            {
                // Cannot verify anything — treat as a real removal so state
                // never leaks (matching what the unguarded code did).
                try
                {
                    realTeardown?.Invoke();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"[DEER] CardRemovalGuard teardown threw for '{cardName}': {e.Message}");
                }
                return;
            }
            CardRemovalGuardHost.Get().StartCoroutine(VerifyLater(player, cardName, realTeardown, rebuildEcho));
        }

        private static IEnumerator VerifyLater(Player player, string cardName, Action realTeardown, Action rebuildEcho)
        {
            yield return new WaitForSecondsRealtime(RebuildEchoDelaySeconds);
            yield return null; // let that frame's deck mutations settle

            bool stillHeld;
            if (player == null || player.data == null)
            {
                // Player gone entirely (left the room / scene torn down) — the
                // effect MonoBehaviour died with it; only global state (bots)
                // still needs the teardown, and round-reset hooks cover that.
                stillHeld = false;
            }
            else
            {
                // cardName-scan idiom (BouncyBall/DynamicField): immune to the
                // pick-clone vs registered-instance identity problem.
                stillHeld = player.data.currentCards.Any(c => c != null && c.cardName == cardName);
            }

            if (stillHeld)
            {
                UnityEngine.Debug.Log($"[DEER] CardRemovalGuard: '{cardName}' removal on {(player != null ? player.data.name : "?")} was a rebuild echo — teardown cancelled");
                if (rebuildEcho != null)
                {
                    try
                    {
                        rebuildEcho();
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogWarning($"[DEER] CardRemovalGuard re-assert threw for '{cardName}': {e.Message}");
                    }
                }
                yield break;
            }

            UnityEngine.Debug.Log($"[DEER] CardRemovalGuard: '{cardName}' really gone from {(player != null ? player.data.name : "?")} — running teardown");
            try
            {
                realTeardown?.Invoke();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[DEER] CardRemovalGuard teardown threw for '{cardName}': {e.Message}");
            }
        }
    }

    /// <summary>
    /// DontDestroyOnLoad coroutine host (SovereignRunner/AbilityHUD driver style).
    /// </summary>
    public class CardRemovalGuardHost : MonoBehaviour
    {
        private static CardRemovalGuardHost host;

        internal static CardRemovalGuardHost Get()
        {
            if (host == null)
            {
                var go = new GameObject("DeerootCardRemovalGuardHost");
                UnityEngine.Object.DontDestroyOnLoad(go);
                host = go.AddComponent<CardRemovalGuardHost>();
            }
            return host;
        }

        private void OnDestroy()
        {
            if (host == this)
            {
                host = null;
            }
        }
    }
}
