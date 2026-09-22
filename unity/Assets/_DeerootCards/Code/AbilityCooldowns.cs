using UnityEngine;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Global ability-cooldown modifier system.
    ///
    /// Every custom ability card with a personal cooldown defines a
    /// `BaseCooldown` constant and applies the modifier at trigger time:
    ///
    ///     cooldownLeft = AbilityCooldowns.Apply(player, BaseCooldown);
    ///
    /// The modifier is a pure function of the player's current deck
    /// (same pattern as BouncyBallCard.HasCard — dev notes "Card-ownership
    /// check"): we scan player.data.currentCards by cardName, so stacking
    /// and card removal (which rebuilds the whole deck and re-fires every
    /// OnAddCard) are handled for free — no effect component, no add/remove
    /// bookkeeping, no drift.
    ///
    /// Effective cooldown = BaseCooldown × (1 + sum of all modifiers).
    /// E.g. "+40% ability cooldowns" turns a 10s cooldown into 14s.
    ///
    /// Future cooldown-modifying cards: add one entry to <see cref="Sources"/>.
    /// Future ability cards: define BaseCooldown and call Apply() on trigger.
    ///
    /// Read only on the owner client (ability input is IsMine-gated), so no
    /// RPCs or sync are needed.
    /// </summary>
    public static class AbilityCooldowns
    {
        // Each entry: in-game card name -> additive cooldown modifier per copy.
        // Names must match the in-game cardName (case-insensitive).
        private static readonly (string cardName, float add)[] Sources =
        {
            ("Quick Attack", 0.25f), // +25% ability cooldowns (Quick Attack downside)
            ("Heart", 0.5f), // +50% ability cooldowns (Heart downside)
            ("Ability Up", -0.5f), // -50% ability cooldowns (Double's unique-card compensation)
        };

        // Safety floor: cooldown multiplier can never drop below ×0.1 (ability
        // spam would break the game). Two Ability Ups (-50%) + Quick Attack
        // (+25%) → ×0.75... but pathological stacks clamp at ×0.1.
        private const float MinMultiplier = 0.1f;

        /// <summary>
        /// Sum of cooldown modifiers from the player's current deck
        /// (0f if the player has no cooldown-modifying cards).
        /// </summary>
        public static float GetModifier(Player player)
        {
            if (player == null || player.data == null || player.data.currentCards == null)
            {
                return 0f;
            }
            float modifier = 0f;
            foreach (CardInfo card in player.data.currentCards)
            {
                if (card == null)
                {
                    continue;
                }
                foreach (var source in Sources)
                {
                    if (string.Equals(card.cardName, source.cardName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        modifier += source.add;
                    }
                }
            }
            return modifier;
        }

        /// <summary>
        /// The one call an ability card makes at trigger time:
        /// base cooldown scaled by the player's deck-derived modifier.
        /// </summary>
        public static float Apply(Player player, float baseCooldown)
        {
            float multiplier = Mathf.Max(1f + GetModifier(player), MinMultiplier);
            return baseCooldown * multiplier;
        }
    }
}
