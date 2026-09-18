using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnboundLib;
using Photon.Pun;
using UnboundLib.Cards;
using System.Reflection;
using HarmonyLib;
using UnboundLib.Networking;
using UnityEngine;
using TMPro;
using ModdingUtils.Extensions;
using UnityEngine.UI;

namespace ModdingUtils.Utils
{
    public sealed class CardBarUtils
    {
        // singleton design
        public static readonly CardBarUtils instance = new CardBarUtils();

        private static readonly float displayDuration = 1.5f;
        public static readonly Vector3 localShift = new Vector3(-50f, 0f, 0f);
        public static readonly float barlocalScaleMult = 1.1f;
        public static readonly float cardLocalScaleMult = 1f;

        private DictionaryOfLists<Player, CardInfo> cardsToShow = new DictionaryOfLists<Player, CardInfo>();

        private CardBar[] CardBars
        {
            get
            {
                return (CardBar[])Traverse.Create(CardBarHandler.instance).Field("cardBars").GetValue();
            }
        }

        public CardBar PlayersCardBar(int playerID)
        {
            return CardBars[playerID];
        }
        public CardBar PlayersCardBar(Player player)
        {
            return CardBars[player.playerID];
        }

        private CardBarUtils()
        {
            CardBarUtils instance = this;
        }

        private void Reset()
        {
            cardsToShow = new DictionaryOfLists<Player, CardInfo>();
        }
        private void Reset(Player player)
        {
            cardsToShow[player] = new List<CardInfo>();
        }

        public void ShowAtEndOfPhase(Player player, CardInfo card)
        {
            if (card == null)
            {
                return;
            }

            List<CardInfo> newList = new List<CardInfo>(cardsToShow[player]);
            newList.Add(card);
            cardsToShow[player] = newList;
        }

        public void ShowCard(Player player, CardInfo card)
        {
            ShowCard(player.playerID, card.name);
        }
        public void ShowCard(int playerID, CardInfo card)
        {
            ShowCard(playerID, card.name);
        }
        public void ShowCard(Player player, int cardID)
        {
            ShowCard(player.playerID, Cards.instance.GetCardWithID(cardID).name);
        }
        public void ShowCard(int playerID, int cardID)
        {
            ShowCard(playerID, Cards.instance.GetCardWithID(cardID).name);
        }
        public void ShowCard(int playerID, string objectName)
        {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(CardBarUtils), nameof(RPCA_ShowCard), new object[] { playerID, objectName });
            }
        }

        [UnboundRPC]
        private static void RPCA_ShowCard(int playerID, string objectName)
        {
            int cardID = Cards.instance.GetCardIDFromObjectName(objectName);

            try
            {
                if (Cards.instance.GetCardWithID(cardID) == null) { return; }
            }
            catch
            {
                return;
            }
            instance.PlayersCardBar(playerID).OnHover(Cards.instance.GetCardWithID(cardID), Vector3.zero);
            ((GameObject)Traverse.Create(instance.PlayersCardBar(playerID)).Field("currentCard").GetValue()).gameObject.transform.localScale = Vector3.one * cardLocalScaleMult;

        }

        public void HideCard(Player player)
        {
            HideCard(player.playerID);
        }
        public void HideCard(int playerID)
        {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(CardBarUtils), nameof(RPCA_HideCard), new object[] { playerID });
            }
        }
        [UnboundRPC]
        private static void RPCA_HideCard(int playerID)
        {
            instance.PlayersCardBar(playerID).StopHover();
        }
        [UnboundRPC]
        private static void RPCA_HighlightCardBar(int playerID)
        {
            instance.PlayersCardBar(playerID).gameObject.transform.localScale = Vector3.one * barlocalScaleMult;
            instance.PlayersCardBar(playerID).gameObject.transform.localPosition += localShift;
            instance.ChangePlayersLineColor(playerID, Color.white);
            Color.RGBToHSV(instance.GetPlayersBarColor(playerID), out float h, out float s, out float v);
            instance.ChangePlayersBarColor(playerID, Color.HSVToRGB(h, s + 0.1f, v + 0.1f));
        }
        [UnboundRPC]
        private static void RPCA_UnhighlightCardBar(int playerID, float r, float g, float b, float a)
        {
            instance.PlayersCardBar(playerID).gameObject.transform.localScale = Vector3.one * 1f;
            instance.PlayersCardBar(playerID).gameObject.transform.localPosition -= localShift;
            instance.ResetPlayersLineColor(playerID);
            instance.ChangePlayersBarColor(playerID, new Color(r,g,b,a));
        }
        public Color HighlightCardBar(int playerID)
        {
            Color orig = GetPlayersBarColor(playerID);
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(CardBarUtils), nameof(RPCA_HighlightCardBar), new object[] { playerID });
            }
            return orig;
        }
        public void UnhighlightCardBar(int playerID, Color original_color)
        {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(CardBarUtils), nameof(RPCA_UnhighlightCardBar), new object[] { playerID, original_color.r, original_color.g, original_color.b, original_color.a });
            }
        }
        public GameObject GetCardBarSquare(int playerID, int idx)
        {
            return GetCardBarSquares(playerID)[idx + 1];
        }
        public GameObject GetCardBarSquare(Player player, int idx)
        {
            return GetCardBarSquare(player.playerID, idx);
        }

        public GameObject[] GetCardBarSquares(int playerID)
        {
            List<GameObject> children = new List<GameObject>();

            foreach (Transform child in PlayersCardBar(playerID).transform)
            {
                children.Add(child.gameObject);
            }

            return children.ToArray();
        }

        public GameObject[] GetCardBarSquares(Player player)
        {
            return GetCardBarSquares(player.playerID);
        }
        public void ResetPlayersLineColor(int playerID)
        {
            List<Graphic> graphics = PlayersCardBar(playerID).gameObject.GetComponentsInChildren<Graphic>().Where(gr => !gr.gameObject.name.Contains("CarrdOrange")).ToList();

            foreach (Graphic graphic in graphics)
            {
                if (graphic.gameObject.name.Contains("Card"))
                {
                    graphic.color = new Color(0.462f, 0.462f, 0.462f, 1f);
                }
                else if (graphic.gameObject.name.Contains("Text"))
                {
                    graphic.color = new Color(0.6509f, 0.6509f, 0.6509f, 1f);
                }
            }
        }
        public void ResetPlayersLineColor(Player player)
        {
            ResetPlayersLineColor(player.playerID);
        }
        public void ChangePlayersLineColor(int playerID, Color color)
        {
            List<Graphic> graphics = PlayersCardBar(playerID).gameObject.GetComponentsInChildren<Graphic>().Where(gr => !gr.gameObject.name.Contains("CarrdOrange")).ToList();

            foreach (Graphic graphic in graphics)
            {
                graphic.color = color;
            }
        }
        public void ChangePlayersLineColor(Player player, Color color)
        {
            ChangePlayersLineColor(player.playerID, color);
        }

        public Color GetPlayersBarColor(int playerID)
        {
            List<Graphic> graphics = PlayersCardBar(playerID).gameObject.GetComponentsInChildren<Graphic>().Where(gr => gr.gameObject.name.Contains("CarrdOrange")).ToList();

            return graphics[0].color;
        }
        public Color GetPlayersBarColor(Player player)
        {
            return GetPlayersBarColor(player.playerID);
        }

        public Color ChangePlayersBarColor(int playerID, Color color)
        {
            List<Graphic> graphics = PlayersCardBar(playerID).gameObject.GetComponentsInChildren<Graphic>().Where(gr => gr.gameObject.name.Contains("CarrdOrange")).ToList();

            Color orig = graphics[0].color;

            foreach (Graphic graphic in graphics)
            {
                graphic.color = color;
            }

            return orig;
        }

        public Color ChangePlayersBarColor(Player player, Color color)
        {
            return ChangePlayersBarColor(player.playerID, color);
        }

        public Color GetCardSquareColor(GameObject cardSquare)
        {
            List<Graphic> graphics = cardSquare.GetComponentsInChildren<Graphic>().ToList();
            return graphics[0].color;
        }
        public Color ChangeCardSquareColor(GameObject cardSquare, Color color)
        {
            List<Graphic> graphics = cardSquare.GetComponentsInChildren<Graphic>().ToList();
            Color orig = graphics[0].color;

            foreach(Graphic graphic in graphics)
            {
                graphic.color = color;
            }
            return orig;
        }

        public static void SilentAddToCardBar(int playerID, CardInfo card, string twoLetterCode = "")
        {
            Traverse.Create(instance.PlayersCardBar(playerID)).Field("ci").SetValue(card);
            GameObject source = (GameObject)Traverse.Create(instance.PlayersCardBar(playerID)).Field("source").GetValue();
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(source, source.transform.position, source.transform.rotation, source.transform.parent);
            gameObject.transform.localScale = Vector3.one;
            string text = card.cardName;
            if (twoLetterCode != "") { text = twoLetterCode; }
            while (text.Length < 2)
            {
                text += " ";
            }
            text = text.Substring(0, 2);
            string text2 = text[0].ToString().ToUpper();
            if (text.Length > 1)
            {
                string str = text[1].ToString().ToLower();
                text = text2 + str;
            }
            else
            {
                text = text2;
            }
            gameObject.GetComponentInChildren<TextMeshProUGUI>().text = text;
            Traverse.Create(gameObject.GetComponent<CardBarButton>()).Field("card").SetValue(card);
            gameObject.gameObject.SetActive(true);
        }
        public static void SilentAddToCardBar(Player player, CardInfo card, string twoLetterCode = "")
        {
            SilentAddToCardBar(player.playerID, card, twoLetterCode);
        }

        internal System.Collections.IEnumerator EndPickPhaseShow()
        {
            foreach (Player player in PlayerManager.instance.players)
            {
                Color orig = Color.clear;
                try
                {
                    orig = GetPlayersBarColor(player);
                }
                catch
                {
                    continue;
                }

                if (cardsToShow[player].Count > 0)
                {
                    orig = HighlightCardBar(player.playerID);
                }
                foreach (CardInfo card in cardsToShow[player].Where(card2 => player.data.currentCards.Select(card3 => card3.name).Contains(card2.name)))
                {

                    ShowCard(player, card);
                    yield return new WaitForSecondsRealtime(displayDuration);
                    HideCard(player);
                }
                if (cardsToShow[player].Count > 0)
                {
                    UnhighlightCardBar(player.playerID, orig);
                }
            }
            Reset();
            yield break;
        }
        public System.Collections.IEnumerator ShowImmediate(int playerID, int cardID, float? duration = null)
        {
            float displayDuration = duration ?? CardBarUtils.displayDuration;

            Color orig = HighlightCardBar(playerID);


            ShowCard(playerID, cardID);
            yield return new WaitForSecondsRealtime(displayDuration);
            HideCard(playerID);


            UnhighlightCardBar(playerID, orig);


            yield break;
        }

        public System.Collections.IEnumerator ShowImmediate(Player player, int cardID, float? duration = null)
        {
            return ShowImmediate(player.playerID, cardID, duration);
        }
        public System.Collections.IEnumerator ShowImmediate(Player player, CardInfo card, float? duration = null)
        {
            return ShowImmediate(player.playerID, Cards.instance.GetCardID(card), duration);
        }
        public System.Collections.IEnumerator ShowImmediate(int playerID, CardInfo card, float? duration = null)
        {
            return ShowImmediate(playerID, Cards.instance.GetCardID(card), duration);
        }
        public System.Collections.IEnumerator ShowImmediate(Player player, int[] cardIDs, float? duration = null)
        {
            return ShowImmediate(player.playerID, cardIDs, duration);
        }
        public System.Collections.IEnumerator ShowImmediate(int playerID, CardInfo[] cards, float? duration = null)
        {
            List<int> cardIDs = new List<int>();
            foreach (CardInfo card in cards)
            {
                cardIDs.Add(Cards.instance.GetCardID(card));
            }

            return ShowImmediate(playerID, cardIDs.ToArray(), duration);
        }
        public System.Collections.IEnumerator ShowImmediate(Player player, CardInfo[] cards, float? duration = null)
        {
            List<int> cardIDs = new List<int>();
            foreach (CardInfo card in cards)
            {
                cardIDs.Add(Cards.instance.GetCardID(card));
            }

            return ShowImmediate(player.playerID, cardIDs.ToArray(), duration);
        }
        public System.Collections.IEnumerator ShowImmediate(Player player, int cardID)
        {
            return ShowImmediate(player, cardID, null);
        }
        public System.Collections.IEnumerator ShowImmediate(Player player, CardInfo card)
        {
            return ShowImmediate(player, card, null);
        }
        public System.Collections.IEnumerator ShowImmediate(int playerID, CardInfo card)
        {
            return ShowImmediate(playerID, card, null);
        }
        public System.Collections.IEnumerator ShowImmediate(Player player, int[] cardIDs)
        {
            return ShowImmediate(player, cardIDs, null);
        }
        public System.Collections.IEnumerator ShowImmediate(int playerID, CardInfo[] cards)
        {
            return ShowImmediate(playerID, cards, null);
        }
        public System.Collections.IEnumerator ShowImmediate(Player player, CardInfo[] cards)
        {
            return ShowImmediate(player, cards, null);
        }
        public System.Collections.IEnumerator ShowImmediate(int playerID, int[] cardIDs, float? duration = null)
        {
            float displayDuration = duration ?? CardBarUtils.displayDuration;

            Color orig = HighlightCardBar(playerID);

            foreach (int cardID in cardIDs)
            {
                ShowCard(playerID, cardID);
                yield return new WaitForSecondsRealtime(displayDuration);
                HideCard(playerID);
            }

            UnhighlightCardBar(playerID, orig);


            yield break;
        }
        
        public void ClearCardBar(Player player)
        {
            if (PhotonNetwork.OfflineMode)
            {
                PlayersCardBar(player).ClearBar();
            }
            else if (PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(CardBarUtils), nameof(RPCA_ClearCardBar), new object[] { player.playerID });
            }
        }

        [UnboundRPC]
        private static void RPCA_ClearCardBar(int playerID)
        {
            Player playerToReset = (Player)PlayerManager.instance.InvokeMethod("GetPlayerWithID", playerID);
            instance.PlayersCardBar(playerToReset.playerID).ClearBar();
        }
    }
    public class DictionaryOfLists<TKey, TListValue> : Dictionary<TKey, List<TListValue>>
    {
        public DictionaryOfLists() : base() { }
        public new List<TListValue> this[TKey key]
        {
            get
            {
                return TryGetValue(key, out List<TListValue> t) ? t : new List<TListValue>();
            }
            set { base[key] = value; }
        }
    }
}
