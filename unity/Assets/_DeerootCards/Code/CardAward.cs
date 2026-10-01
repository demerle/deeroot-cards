using System;
using UnityEngine;
using UnboundLib.Cards;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Shared "hand a specific card to a player" helper (the future programmatic
    /// award path that OneShotCardPoolFilter's dev-note promised).
    ///
    /// Faithful result of vanilla `ApplyCardStats.RPCA_Pick` (decompile
    /// ApplyCardStats.cs:42-104), driven from a CardInfo MASTER prefab instead
    /// of a spawned pick-card clone — i.e. the same house recipe as
    /// DoubleCard.ApplyCardToPlayer, with two deliberate differences:
    ///   1. NO `health.Revive()` — awards can land mid-battle and Revive
    ///      would fully heal / reload the receiving player.
    ///   2. It additionally invokes the master's `CustomCard.OnAddCard(...)`,
    ///      which vanilla `ApplyCardStats` never does and which one-shot
    ///      ability cards REQUIRE (Meteor/Simulacrum/TimeStop arm their
    ///      effect components there — a plain stat copy leaves them inert).
    ///
    /// Call this on every client inside an [UnboundRPC] so grants stay
    /// deterministic across clients (no Photon instantiation involved).
    /// </summary>
    public static class CardAward
    {
        public static void GiveCardToPlayer(Player player, string cardName)
        {
            if (player == null || player.data == null || string.IsNullOrEmpty(cardName))
            {
                return;
            }

            CardInfo master = FindCardMaster(cardName);
            if (master == null)
            {
                UnityEngine.Debug.LogError($"[DEER] CardAward: no registered CardInfo named '{cardName}' — grant aborted");
                return;
            }

            ApplyCardToPlayer(player, master);
            UnityEngine.Debug.Log($"[DEER] CardAward: '{cardName}' granted to {player.data.name}");
        }

        /// <summary>
        /// Registered master.prefab lookup: CardChoice's own pool first (that
        /// array holds the registered instances), then a Resources sweep as the
        /// fallback (DontDestroyOnLoad / pick-phase clones may outlive the pool
        /// state; also covers cards not in the current pool). We only ever
        /// award OUR cards, whose registered CardInfo is always alive.
        /// </summary>
        private static CardInfo FindCardMaster(string cardName)
        {
            if (CardChoice.instance != null && CardChoice.instance.cards != null)
            {
                var cards = CardChoice.instance.cards;
                for (int i = 0; i < cards.Length; i++)
                {
                    if (cards[i] != null && cards[i].cardName == cardName)
                    {
                        return cards[i];
                    }
                }
            }
            var all = Resources.FindObjectsOfTypeAll<CardInfo>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].cardName == cardName)
                {
                    return all[i];
                }
            }
            return null;
        }

        // Faithful copy of vanilla ApplyCardStats.ApplyStats() (decompile), but
        // driven from a CardInfo master prefab instead of the (destroyed)
        // spawned card object. Mirrors DoubleCard.ApplyCardToPlayer except for
        // the two deltas documented in the class comment above.
        private static void ApplyCardToPlayer(Player player, CardInfo cardMaster)
        {
            Gun myGunStats = cardMaster.GetComponent<Gun>();
            CharacterStatModifiers myPlayerStats = cardMaster.GetComponent<CharacterStatModifiers>();
            Block myBlock = cardMaster.GetComponentInChildren<Block>();
            var cardAudio = cardMaster.GetComponent<CardAudioModifier>();

            Gun gun = player.GetComponent<Holding>().holdable.GetComponent<Gun>();
            CharacterStatModifiers playerStats = player.GetComponent<CharacterStatModifiers>();
            CharacterData data = player.GetComponent<CharacterData>();
            HealthHandler health = player.GetComponent<HealthHandler>();
            Gravity gravity = player.GetComponent<Gravity>();
            Block block = player.GetComponent<Block>();
            PlayerAudioModifyers audioMods = player.GetComponent<PlayerAudioModifyers>();

            GunAmmo gunAmmo = gun.GetComponentInChildren<GunAmmo>();
            if (gunAmmo != null && myGunStats != null)
            {
                gunAmmo.ammoReg += myGunStats.ammoReg;
                gunAmmo.maxAmmo += myGunStats.ammo;
                gunAmmo.maxAmmo = Mathf.Clamp(gunAmmo.maxAmmo, 1, 90);
                gunAmmo.reloadTimeMultiplier *= myGunStats.reloadTime;
                gunAmmo.reloadTimeAdd += myGunStats.reloadTimeAdd;
            }

            player.data.currentCards.Add(cardMaster);

            if (myGunStats != null)
            {
                if (myGunStats.lockGunToDefault)
                {
                    gun.defaultCooldown = myGunStats.forceSpecificAttackSpeed;
                    gun.lockGunToDefault = myGunStats.lockGunToDefault;
                }
                if (myGunStats.projectiles.Length != 0)
                {
                    gun.projectiles[0].objectToSpawn = myGunStats.projectiles[0].objectToSpawn;
                }
                ApplyCardStats.CopyGunStats(myGunStats, gun);
            }

            if (myPlayerStats != null)
            {
                playerStats.sizeMultiplier *= myPlayerStats.sizeMultiplier;
                data.maxHealth *= myPlayerStats.health;
                // ConfigureMassAndSize is internal — reflect it (vanilla calls it here).
                typeof(CharacterStatModifiers)
                    .GetMethod("ConfigureMassAndSize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                    ?.Invoke(playerStats, null);
                playerStats.movementSpeed *= myPlayerStats.movementSpeed;
                playerStats.jump *= myPlayerStats.jump;
                data.jumps += myPlayerStats.numberOfJumps;
                gravity.gravityForce *= myPlayerStats.gravity;
                health.regeneration += myPlayerStats.regen;
                if (myPlayerStats.AddObjectToPlayer != null)
                {
                    playerStats.objectsAddedToPlayer.Add(UnityEngine.Object.Instantiate(myPlayerStats.AddObjectToPlayer, player.transform.position, player.transform.rotation, player.transform));
                }
                playerStats.lifeSteal += myPlayerStats.lifeSteal;
                playerStats.respawns += myPlayerStats.respawns;
                playerStats.secondsToTakeDamageOver += myPlayerStats.secondsToTakeDamageOver;
                if (myPlayerStats.refreshOnDamage)
                {
                    playerStats.refreshOnDamage = true;
                }
                if (!myPlayerStats.automaticReload)
                {
                    playerStats.automaticReload = false;
                }
            }

            if (myBlock != null)
            {
                if (myBlock.objectsToSpawn != null)
                {
                    for (int i = 0; i < myBlock.objectsToSpawn.Count; i++)
                    {
                        block.objectsToSpawn.Add(myBlock.objectsToSpawn[i]);
                    }
                }
                block.cdMultiplier *= myBlock.cdMultiplier;
                block.cdAdd += myBlock.cdAdd;
                block.forceToAdd += myBlock.forceToAdd;
                block.forceToAddUp += myBlock.forceToAddUp;
                block.additionalBlocks += myBlock.additionalBlocks;
                block.healing += myBlock.healing;
                if (myBlock.autoBlock)
                {
                    block.autoBlock = myBlock.autoBlock;
                }
            }

            if (audioMods != null && cardAudio != null)
            {
                audioMods.AddToStack(cardAudio);
            }

            // Arm the card's own runtime logic — vanilla never does this for
            // stat copies, but ability cards NEED their OnAddCard to run
            // (MeteorEffect / SimulacrumEffect / TimeStop agent arming happens
            // there). Harmless no-op for pure stat cards.
            var customCard = cardMaster.GetComponent<CustomCard>();
            if (customCard != null)
            {
                customCard.OnAddCard(
                    player,
                    gun,
                    gunAmmo,
                    data,
                    health,
                    gravity,
                    block,
                    playerStats
                );
            }

            playerStats.WasUpdated();

            if (CardBarHandler.instance != null)
            {
                CardBarHandler.instance.AddCard(player.playerID, cardMaster);
            }
        }
    }
}
