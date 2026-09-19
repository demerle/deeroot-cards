using UnityEngine;

namespace DeerootCards
{
    /// <summary>
    /// UnboundLib's card removal (Cards.RemoveCardFromPlayer) rebuilds a player's
    /// whole deck from scratch — every remaining card runs through the vanilla
    /// pick pipeline again, so every CustomCard.OnAddCard hook fires again.
    /// This guard lets our own cards swallow those spurious re-adds:
    /// Mark() whenever any of our removal paths runs; our cards' OnAddCard
    /// hooks check IsQuiet and bail if inside the window.
    /// </summary>
    public static class RebuildGuard
    {
        public const float WindowSeconds = 2f;

        private static float quietUntil = -1f;

        public static void Mark()
        {
            quietUntil = Time.unscaledTime + WindowSeconds;
        }

        public static bool IsQuiet
        {
            get { return Time.unscaledTime < quietUntil; }
        }
    }
}
