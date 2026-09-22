using UnityEngine;
using UnboundLib;
using UnboundLib.Cards;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Double it and give it to the next: the next card this player picks is
    /// applied a second time for free (full stats + effects + card bar icon).
    /// Detection is owner-client only via currentCards polling; the duplicate
    /// application runs inside an [UnboundRPC] so every client (offline included)
    /// ends up with identical state.
    /// </summary>
    public class DoubleCard : CustomCard
    {
        public const string CardName = "Double It And Give It To The Next";

        // Per-player-unique classification for Double's compensation payout:
        // card names in this set (this mod's ability cards) compensate with
        // "Ability Up" (-25% all ability cooldowns); every other unique card —
        // vanilla uniques included — compensates with "Power Up" (+25% damage).
        internal static readonly System.Collections.Generic.HashSet<string> AbilityCardNames =
            new System.Collections.Generic.HashSet<string>
            {
                "Portals", "Heart", "Invisibility", "Shambles", "Blink", "Sovereign", "Meteor"
            };

        internal static bool IsAbilityCard(CardInfo card)
        {
            if (card == null)
            {
                return false;
            }
            return AbilityCardNames.Contains(card.cardName);
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // The vanilla pick pipeline re-runs during UnboundLib's removal
            // rebuild — swallow those spurious re-adds or pending doubles get
            // re-armed by every rebuild.
            if (RebuildGuard.IsQuiet)
            {
                UnityEngine.Debug.Log("[DEER] DoubleCard add suppressed (rebuild window)");
                return;
            }

            // Arm directly here — the effect is created during THIS card's own
            // application, so its Start() snapshot would miss the self-add.
            var effect = player.gameObject.GetOrAddComponent<DoubleEffect>();
            effect.SetPlayer(player);
            effect.ArmPending();
            UnityEngine.Debug.Log($"[DEER] DoubleCard added to player {player.data.name}");
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.GetComponent<DoubleEffect>();
            if (effect != null)
            {
                Destroy(effect);
            }
        }

        protected override string GetTitle()
        {
            return "Double It And Give It To The Next";
        }

        protected override string GetDescription()
        {
            return "Receive two of your next card you choose.";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Next card",
                    amount = "Given twice",
                    simepleAmount = CardInfoStat.SimpleAmount.aLotOf
                }
            };
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Uncommon;
        }

        protected override GameObject GetCardArt()
        {
            return null;
        }

        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.EvilPurple;
        }

        public override string GetModName()
        {
            return DeerootCards.modInitials;
        }
    }

    /// <summary>
    /// Per-player effect. Owner client watches player.data.currentCards for the
    /// next pick after the Double card was taken and fires an RPC to apply that
    /// card a second time on all clients.
    /// </summary>
    public class DoubleEffect : MonoBehaviour
    {
        private Player player;

        private int lastCount = -1; // -1 until Start() snapshots
        private int pendingStacks; // 0 = none; 1 = "receive 2 of the next card"; each extra Double doubles it (2 = receive 4, 3 = receive 8...)
        private bool armAckPending; // set by ArmPending (OnAddCard); consumed by the next Update detection of that same Double so it is not multiplied twice
        private int applyingCount = -1; // expected currentCards.Count right after our own duplicate application

        public void SetPlayer(Player player)
        {
            this.player = player;
        }

        public void ArmPending()
        {
            // Stacking: picking a Double while one is already pending doubles
            // the multiplier instead of overwriting it — 1 pending (receive 2),
            // 2 pending (receive 4), 3 pending (receive 8), and so on.
            pendingStacks = (pendingStacks == 0) ? 1 : pendingStacks * 2;
            // OnAddCard and the Update deck-scan both see this same Double land;
            // without this ack the card would be multiplied twice per pick
            // (observed: two Doubles -> pendingStacks 4 instead of 2 -> 5 cards).
            armAckPending = true;
        }

        private void Start()
        {
            if (player == null)
            {
                player = GetComponent<Player>();
            }
            // Snapshot AFTER the card's own application has landed (Start runs next frame).
            lastCount = player.data.currentCards.Count;
            UnityEngine.Debug.Log($"[DEER] DoubleEffect started on {player.data.name}, cards={lastCount}, pendingStacks={pendingStacks}");
        }

        private void Update()
        {
            if (!player.data.view.IsMine || player.data.dead)
            {
                return;
            }

            var cards = player.data.currentCards;
            if (cards.Count == lastCount)
            {
                return;
            }

            for (int i = lastCount; i < cards.Count; i++)
            {
                CardInfo added = cards[i];
                lastCount = cards.Count;

                if (added == null)
                {
                    continue;
                }

                UnityEngine.Debug.Log($"[DEER] Double detected added card '{added.cardName}' for {player.data.name}");

                if (added.cardName == DoubleCard.CardName)
                {
                    if (armAckPending)
                    {
                        // This is the Double OnAddCard already armed — acknowledge
                        // it instead of multiplying a second time.
                        armAckPending = false;
                        UnityEngine.Debug.Log($"[DEER] Double arm acknowledged (no re-multiply), pendingStacks={pendingStacks}");
                    }
                    else
                    {
                        // Double arrived without an OnAddCard arm (e.g. removal
                        // rebuild re-add, where OnAddCard was swallowed by the
                        // RebuildGuard) — arm it here instead.
                        pendingStacks = (pendingStacks == 0) ? 1 : pendingStacks * 2;
                        UnityEngine.Debug.Log($"[DEER] Double armed via deck-scan — pendingStacks={pendingStacks}");
                    }
                    continue;
                }

                if (cards.Count == applyingCount)
                {
                    // Our own duplicate application landing on the owner client — skip.
                    UnityEngine.Debug.Log("[DEER] Double: own duplicate confirmed, no re-trigger");
                    continue;
                }

                if (pendingStacks > 0 && !added.allowMultiple)
                {
                    // Per-player unique card: no duplicate copies — compensate
                    // instead. The unique card itself already applied (vanilla
                    // pick flow); Double's payout becomes copies of a bonus
                    // card: "Ability Up" for our ability cards, "Power Up"
                    // (+25% damage) for everything else (vanilla uniques
                    // included). One bonus copy per missed duplicate, so a
                    // ×4 payout (2 stacks) -> 3 bonus copies.
                    string compensation = DoubleCard.IsAbilityCard(added)
                        ? AbilityUpCard.CardName
                        : PowerUpCard.CardName;
                    // Total copies owed = 2 × pendingStacks; the pick itself
                    // was applied once, so that one is "paid".
                    int copies = pendingStacks * 2 - 1;
                    pendingStacks = 0;
                    armAckPending = false;
                    applyingCount = cards.Count + copies;
                    UnityEngine.Debug.Log($"[DEER] Double compensated on unique '{added.cardName}' -> '{compensation}' x{copies} (expect count {applyingCount})");

                    UnboundLib.NetworkingManager.RPC(
                        typeof(DoubleEffect),
                        nameof(RPCA_DoubleApply),
                        player.playerID,
                        compensation,
                        copies
                    );
                    continue;
                }

                if (pendingStacks > 0)
                {
                    // Vanilla pick already applied the card once; total owed is
                    // 2 × pendingStacks (1 stack -> receive 2, 2 -> receive 4,
                    // 3 -> receive 8), so apply 2 × stacks − 1 extra copies.
                    int copies = pendingStacks * 2 - 1;
                    pendingStacks = 0;
                    armAckPending = false;
                    applyingCount = cards.Count + copies;
                    UnityEngine.Debug.Log($"[DEER] Double FIRING '{added.cardName}' x{copies} extra for {player.data.name} (expect count {applyingCount})");

                    UnboundLib.NetworkingManager.RPC(
                        typeof(DoubleEffect),
                        nameof(RPCA_DoubleApply),
                        player.playerID,
                        added.cardName,
                        copies
                    );
                }
            }
        }

        [UnboundLib.Networking.UnboundRPC]
        private static void RPCA_DoubleApply(int playerID, string cardName, int copies)
        {
            Player target = GetPlayerByID(playerID);
            CardInfo cardMaster = FindCardMaster(cardName);
            if (target == null || cardMaster == null)
            {
                UnityEngine.Debug.LogWarning($"[DEER] RPCA_DoubleApply failed: player={playerID} card='{cardName}' copies={copies}");
                return;
            }
            if (copies < 1)
            {
                copies = 1;
            }

            for (int i = 0; i < copies; i++)
            {
                UnityEngine.Debug.Log($"[DEER] RPCA_DoubleApply re-applying '{cardName}' to {target.data.name} ({i + 1}/{copies})");
                ApplyCardToPlayer(target, cardMaster);
            }
        }

        private static Player GetPlayerByID(int playerID)
        {
            var method = typeof(PlayerManager).GetMethod(
                "GetPlayerWithID",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
            return method?.Invoke(PlayerManager.instance, new object[] { playerID }) as Player;
        }

        private static CardInfo FindCardMaster(string cardName)
        {
            if (CardChoice.instance != null)
            {
                foreach (var c in CardChoice.instance.cards)
                {
                    if (c != null && c.cardName == cardName)
                    {
                        return c;
                    }
                }
            }
            foreach (var c in Resources.FindObjectsOfTypeAll<CardInfo>())
            {
                if (c != null && c.cardName == cardName)
                {
                    return c;
                }
            }
            return null;
        }

        // Faithful copy of vanilla ApplyCardStats.ApplyStats() (decompiled, all
        // fields used here are public), but driven from a CardInfo master prefab
        // instead of the (destroyed) spawned card object.
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
                    playerStats.objectsAddedToPlayer.Add(Object.Instantiate(myPlayerStats.AddObjectToPlayer, player.transform.position, player.transform.rotation, player.transform));
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

            playerStats.WasUpdated();
            health.Revive();

            if (CardBarHandler.instance != null)
            {
                CardBarHandler.instance.AddCard(player.playerID, cardMaster);
            }
        }
    }
}
