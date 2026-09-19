using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnboundLib;
using UnboundLib.Cards;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Delete: while the pick screen animation is still running, everyone sees a
    /// selection overlay. Only the picker can click: first choose ANY player
    /// (opponents and teammates alike), then one of that player's cards. The
    /// chosen card is removed from their deck via UnboundLib's full rebuild
    /// removal (stats + card bar reapplied from remaining cards).
    ///
    /// Integration into card selection: when Delete is picked we broadcast
    /// "pending" to all clients. A Harmony patch on CardChoice.RPCA_DonePicking
    /// swallows that vanilla handoff while pending, so the pick phase (and
    /// everything after it — other pickers, map load) literally waits until the
    /// deletion is resolved (confirm, cancel, or per-client timeout).
    ///
    /// Overlay visibility is replicated: one broadcast, every client renders its
    /// own OnGUI copy; clicks are gated to the picker's client.
    /// </summary>
    public class DeleteCard : CustomCard
    {
        public const string CardName = "Delete";

        private static bool harmonyApplied;

        public static void Init()
        {
            if (harmonyApplied) return;
            harmonyApplied = true;
            new Harmony("com.deeroot.cards.delete").PatchAll(typeof(DeleteBlockDonePickingPatch));
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // The vanilla pick pipeline also re-runs during UnboundLib's removal
            // rebuild — ignore those spurious re-adds (all remaining cards in the
            // deck get re-added, including any Delete cards).
            if (RebuildGuard.IsQuiet)
            {
                UnityEngine.Debug.Log("[DEER] DeleteCard add suppressed (rebuild window)");
                return;
            }

            UnityEngine.Debug.Log($"[DEER] DeleteCard added to player {player.data.name}");

            // The pick pipeline runs on all clients, but only the picker's owner
            // starts the selection flow (and holds the pick phase open).
            if (!player.data.view.IsMine)
            {
                return;
            }

            UnboundLib.NetworkingManager.RPC(
                typeof(DeleteCard),
                nameof(DeleteCard.RPCA_DeleteShowOverlay),
                player.playerID
            );
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // If our Delete card is removed while its own choice is still pending,
            // release the block instead of stalling the game forever.
            DeleteOverlayUI.ReleaseForPlayer(player.playerID);
        }

        protected override string GetTitle()
        {
            return "Delete";
        }

        protected override string GetDescription()
        {
            return "Delete one card of your choosing from any player's deck — doesn't have to be theirs.";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Remove",
                    amount = "1 card",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                }
            };
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Rare;
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

        public static void InitPatch() { Init(); }

        // ---- Harmony plumbing -------------------------------------------------

        /// <summary>
        /// Blocks the vanilla "pick finished" handoff while a deletion is pending:
        /// RPCA_DonePicking's whole body is just `IsPicking = false`, which is
        /// what lets GM_ArmsRace's pick coroutine move on to the next player /
        /// round start. Skipping it while pending makes the game wait for us.
        /// On release we flip IsPicking ourselves — identical to the original body.
        /// </summary>
        [HarmonyPatch(typeof(CardChoice), "RPCA_DonePicking")]
        internal static class DeleteBlockDonePickingPatch
        {
            [HarmonyPrefix]
            private static bool BlockWhileDeleting()
            {
                if (DeleteOverlayUI.PendingCount <= 0)
                {
                    return true;
                }
                UnityEngine.Debug.Log($"[DEER] RPCA_DonePicking held by Delete (pending {DeleteOverlayUI.PendingCount})");
                DeleteOverlayUI.donePickingWasHeld = true;
                return false;
            }
        }

        // ---- Networking ---------------------------------------------------------

        [UnboundLib.Networking.UnboundRPC]
        public static void RPCA_DeleteShowOverlay(int pickerPlayerID)
        {
            // Runs on every client: everyone sees the overlay; everyone's pick
            // phase is held until resolution.
            DeleteOverlayUI.PendingCount++;
            DeleteOverlayUI.Show(pickerPlayerID);
        }

        [UnboundLib.Networking.UnboundRPC]
        public static void RPCA_DeleteOverlayClose()
        {
            DeleteOverlayUI.ReleasePendingPick("[DEER] Delete cancelled/closed");
        }

        [UnboundLib.Networking.UnboundRPC]
        public static void RPCA_DeleteExecute(int pickerPlayerID, int targetPlayerID, int cardIndex, string cardName)
        {
            // Close overlay + release the pick-phase hold on every client first.
            DeleteOverlayUI.ReleasePendingPick("[DEER] Delete confirmed");

            RebuildGuard.Mark();

            // Only one client may perform the rebuild (offline or master).
            if (!Photon.Pun.PhotonNetwork.OfflineMode && !Photon.Pun.PhotonNetwork.IsMasterClient)
            {
                return;
            }

            Player target = GetPlayerByID(targetPlayerID);
            if (target == null)
            {
                UnityEngine.Debug.LogWarning($"[DEER] RPCA_DeleteExecute: no player {targetPlayerID}");
                return;
            }

            var cards = target.data.currentCards;

            // Resolve the victim's index on the master fresh, so a concurrent
            // card add can't shift our target. Prefer the exact index if it
            // still holds the right card; otherwise fall back to name match.
            int idx = -1;
            if (cardIndex >= 0 && cardIndex < cards.Count && cards[cardIndex] != null && cards[cardIndex].cardName == cardName)
            {
                idx = cardIndex;
            }
            else
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    if (cards[i] != null && cards[i].cardName == cardName)
                    {
                        idx = i;
                        break;
                    }
                }
            }

            if (idx < 0)
            {
                UnityEngine.Debug.LogWarning($"[DEER] RPCA_DeleteExecute: card '{cardName}' not found in {target.data.name}'s deck (already gone?)");
                return;
            }

            UnityEngine.Debug.Log($"[DEER] RPCA_DeleteExecute deleting '{cardName}' from {target.data.name} (idx {idx})");
            ModdingUtils.Utils.Cards.instance.RemoveCardFromPlayer(target, idx, true);
        }

        private static Player GetPlayerByID(int playerID)
        {
            var method = typeof(PlayerManager).GetMethod(
                "GetPlayerWithID",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
            return method?.Invoke(PlayerManager.instance, new object[] { playerID }) as Player;
        }
    }

    /// <summary>
    /// Client-local overlay state. Created lazily on whichever client receives
    /// RPCA_DeleteShowOverlay; visible on ALL clients, clickable only on the
    /// picker's client. Every client releases its pick-phase hold on resolution
    /// (close RPC) or after a fallback timeout, so nothing can stall forever.
    /// </summary>
    public class DeleteOverlayUI : MonoBehaviour
    {
        private const float Timeout = 25f;

        private static DeleteOverlayUI instance;

        public static int PendingCount;
        public static bool donePickingWasHeld;

        private int pickerPlayerID = -1;
        private int targetPlayerID = -1;
        private float timeLeft;

        public static DeleteOverlayUI Instance => instance;

        public static void Show(int pickerPlayerID)
        {
            if (instance == null)
            {
                var go = new GameObject("DeleteOverlayUI");
                instance = go.AddComponent<DeleteOverlayUI>();
            }
            instance.pickerPlayerID = pickerPlayerID;
            instance.targetPlayerID = -1;
            instance.timeLeft = Timeout;
            UnityEngine.Debug.Log($"[DEER] Delete overlay shown (picker {pickerPlayerID}), pending {PendingCount}");
        }

        /// <summary>
        /// One deletion resolved everywhere: decrement the hold count and, if this
        /// client had swallowed RPCA_DonePicking, finish the vanilla handoff.
        /// </summary>
        public static void ReleasePendingPick(string logTag)
        {
            if (PendingCount <= 0)
            {
                return;
            }
            PendingCount--;
            UnityEngine.Debug.Log($"{logTag} (remaining pending {PendingCount})");
            if (PendingCount == 0 && donePickingWasHeld)
            {
                donePickingWasHeld = false;
                // Original body of RPCA_DonePicking is just `IsPicking = false`.
                if (CardChoice.instance != null)
                {
                    CardChoice.instance.IsPicking = false;
                }
            }
            instance?.Hide(quiet: true);
        }

        /// <summary>
        /// If a player's remaining Delete card gets removed (by another Delete,
        /// or externally) while its choice is still pending, release its hold so
        /// the game isn't stalled.
        /// </summary>
        public static void ReleaseForPlayer(int playerID)
        {
            if (instance != null && instance.pickerPlayerID == playerID && PendingCount > 0)
            {
                ReleasePendingPick("[DEER] Delete card removed while pending");
            }
        }

        public void Hide(bool quiet)
        {
            pickerPlayerID = -1;
            targetPlayerID = -1;
            if (!quiet)
            {
                UnityEngine.Debug.Log("[DEER] Delete overlay closed");
            }
        }

        private bool IsPicker
        {
            get
            {
                if (pickerPlayerID < 0 || PlayerManager.instance == null)
                {
                    return false;
                }
                foreach (var data in PlayerManager.instance.players)
                {
                    var p = data.GetComponent<Player>();
                    if (p != null && p.playerID == pickerPlayerID)
                    {
                        return p.data.view.IsMine;
                    }
                }
                return false;
            }
        }

        // Vanilla shows names from PhotonView.Owner.NickName (PlayerName.cs) —
        // the GameObject name ("Player(Clone)") is never meant for display.
        // Offline there is no nickname, so fall back to numbered labels.
        private static string GetDisplayName(Player p)
        {
            if (p == null)
            {
                return "???";
            }
            if (!Photon.Pun.PhotonNetwork.OfflineMode)
            {
                var nick = p.data.view.Owner?.NickName;
                if (!string.IsNullOrEmpty(nick))
                {
                    return nick;
                }
            }
            return $"Player {p.playerID + 1}";
        }

        private void Update()
        {
            // Per-client fallback: if the picker vanished without resolving, stop
            // holding the pick phase so nobody stalls forever.
            if (pickerPlayerID >= 0 && timeLeft > 0f)
            {
                timeLeft -= Time.unscaledDeltaTime;
                if (timeLeft <= 0f)
                {
                    UnityEngine.Debug.LogWarning("[DEER] Delete overlay timed out, releasing pick phase");
                    DeleteCard.RPCA_DeleteOverlayClose();
                }
            }
        }

        private void OnGUI()
        {
            if (pickerPlayerID < 0 || PlayerManager.instance == null)
            {
                return;
            }

            var skin = GUI.skin;
            skin.button.fontSize = skin.box.fontSize = skin.label.fontSize = 16;

            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);

            GUILayout.BeginArea(new Rect((Screen.width - 560) / 2, (Screen.height - 420) / 2, 560, 420));

            if (targetPlayerID < 0)
            {
                GUILayout.Box("DELETE — choose a player");

                var players = PlayerManager.instance.players;
                int shown = 0;
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i].GetComponent<Player>();
                    if (p == null)
                    {
                        continue;
                    }
                    var cardCount = p.data.currentCards.Count;
                    if (cardCount == 0)
                    {
                        continue; // nothing to delete from an empty deck
                    }
                    shown++;
                    GUILayout.BeginHorizontal();
                    var label = $"{GetDisplayName(p)}  ({cardCount} cards)";
                    if (CanClick())
                    {
                        if (GUILayout.Button(label, GUILayout.Height(38)))
                        {
                            PickPlayer(p.playerID);
                        }
                    }
                    else
                    {
                        // Real buttons only on the picker's client; everywhere else
                        // the row is an inert box with the same styling.
                        GUILayout.Box(label, GUILayout.Height(38));
                    }
                    GUILayout.EndHorizontal();
                }

                if (shown == 0)
                {
                    GUILayout.Box("No players with cards to delete from.");
                    if (CanClick() && GUILayout.Button("Close", GUILayout.Height(26)))
                    {
                        UnboundLib.NetworkingManager.RPC(typeof(DeleteCard), nameof(DeleteCard.RPCA_DeleteOverlayClose));
                    }
                }
            }
            else
            {
                Player target = null;
                foreach (var data in PlayerManager.instance.players)
                {
                    var p = data.GetComponent<Player>();
                    if (p != null && p.playerID == targetPlayerID)
                    {
                        target = p;
                        break;
                    }
                }

                if (target == null)
                {
                    GUILayout.Box("DELETE — target left");
                    GUILayout.EndArea();
                    return;
                }

                GUILayout.Box($"DELETE — choose a card from {GetDisplayName(target)}");

                int shown = 0;
                var cards = target.data.currentCards;
                for (int i = 0; i < cards.Count; i++)
                {
                    var card = cards[i];
                    if (card == null)
                    {
                        continue;
                    }
                    shown++;
                    GUILayout.BeginHorizontal();
                    var label = $"{card.cardName}  [{i + 1}/{cards.Count}]";
                    if (CanClick())
                    {
                        if (GUILayout.Button(label, GUILayout.Height(30)))
                        {
                            ConfirmDeletion(card.cardName, i);
                        }
                    }
                    else
                    {
                        GUILayout.Box(label, GUILayout.Height(30));
                    }
                    GUILayout.EndHorizontal();
                }

                if (shown == 0)
                {
                    GUILayout.Box("No cards left.");
                    if (CanClick() && GUILayout.Button("Close", GUILayout.Height(26)))
                    {
                        UnboundLib.NetworkingManager.RPC(typeof(DeleteCard), nameof(DeleteCard.RPCA_DeleteOverlayClose));
                    }
                }
            }

            GUILayout.Space(10);
            if (targetPlayerID >= 0 && CanClick() && GUILayout.Button("← Back to player list", GUILayout.Height(26)))
            {
                // Local UI layer change only — no hold release, no RPC.
                targetPlayerID = -1;
            }
            if (CanClick() && GUILayout.Button("Cancel (delete nothing)", GUILayout.Height(26)))
            {
                UnboundLib.NetworkingManager.RPC(typeof(DeleteCard), nameof(DeleteCard.RPCA_DeleteOverlayClose));
            }

            GUILayout.EndArea();
        }

        private bool CanClick() => IsPicker;

        private void PickPlayer(int playerID)
        {
            targetPlayerID = playerID;
            UnityEngine.Debug.Log($"[DEER] Delete target chosen: player {playerID}");
        }

        private void ConfirmDeletion(string cardName, int index)
        {
            UnityEngine.Debug.Log($"[DEER] Delete confirm: '{cardName}' from player {targetPlayerID}");
            UnboundLib.NetworkingManager.RPC(
                typeof(DeleteCard),
                nameof(DeleteCard.RPCA_DeleteExecute),
                pickerPlayerID,
                targetPlayerID,
                index,
                cardName
            );
            // The execute RPC closes everyone's overlay + releases the hold.
        }
    }
}
