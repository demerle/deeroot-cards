//using System;
//using System.Linq;
//using System.Collections;
//using System.Collections.Generic;
//using UnboundLib;
//using UnboundLib.Utils;
//using UnityEngine;
//using ItemShops.Monobehaviours;
//using TMPro;

//namespace ItemShops.Utils
//{
//    public class PurchasableTRTCard : Purchasable
//    {
//        CardInfo _card;
//        Dictionary<string, int> _cost;
//        Tag[] _tags;
//        string _name;

//        public CardInfo Card
//        {
//            get
//            {
//                return _card;
//            }
//        }

//        public override Dictionary<string, int> Cost
//        {
//            get
//            {
//                return _cost;
//            }
//        }
//        public override Tag[] Tags
//        {
//            get
//            {
//                return _tags;
//            }
//        }
//        public override string Name
//        {
//            get
//            {
//                return _name;
//            }
//        }
//        public PurchasableTRTCard(CardInfo card, Dictionary<string, int> cost, Tag[] tags)
//        {
//            this._card = card;
//            this._cost = cost;
//            this._tags = tags;
//            this._name = card.cardName.ToUpper();
//        }

//        public PurchasableTRTCard(CardInfo card, Dictionary<string, int> cost, Tag[] tags, string name)
//        {
//            this._card = card;
//            this._cost = cost;
//            this._tags = tags;
//            this._name = name;
//        }

//        public override void OnPurchase(Player player, Purchasable item)
//        {
//            var card = ((PurchasableTRTCard)item)._card;
//            ModdingUtils.Utils.Cards.instance.AddCardToPlayer(player, card, false, "", 2f, 2f);

//            ItemShops.instance.StartCoroutine(ShowCard(player, card));
//        }

//        public override bool CanPurchase(Player player)
//        {
//            return ModdingUtils.Utils.Cards.instance.PlayerIsAllowedCard(player, this.Card);
//        }

//        public override GameObject CreateItem(GameObject parent)
//        {
//            GameObject container = null;
//            GameObject holder = null;

//            try
//            {
//                container = GameObject.Instantiate(ItemShops.instance.assets.LoadAsset<GameObject>("Card Container"));
//            }
//            catch (Exception)
//            {

//                UnityEngine.Debug.Log("Issue with creating the card container");
//            }

//            try
//            {
//                holder = container.transform.Find("Card Holder").gameObject;
//            }
//            catch (Exception)
//            {

//                UnityEngine.Debug.Log("Issue with getting the Card Holder");
//                holder = container.transform.GetChild(0).gameObject;
//            }
//            holder.transform.localPosition = new Vector3(0f, -100f, 0f);
//            holder.transform.localScale = new Vector3(0.125f, 0.125f, 1f);
//            holder.transform.Rotate(0f, 180f, 0f);

//            GameObject cardObj = null;

//            try
//            {
//                cardObj = GetCardVisuals(_card, holder);
//            }
//            catch (Exception e)
//            {
//                UnityEngine.Debug.Log("Issue with getting card visuals");
//                UnityEngine.Debug.LogError(e);
//            }

//            container.transform.SetParent(parent.transform);

//            return container;
//        }

//        public IEnumerator ShowCard(Player player, CardInfo card)
//        {
//            yield return ModdingUtils.Utils.CardBarUtils.instance.ShowImmediate(player, card, 2f);

//            yield break;
//        }

//        private GameObject GetCardVisuals(CardInfo card, GameObject parent)
//        {

//            GameObject cardObj = GameObject.Instantiate<GameObject>(card.gameObject, parent.gameObject.transform);
//            cardObj.SetActive(true);
//            cardObj.GetComponentInChildren<CardVisuals>().firstValueToSet = true;
//            RectTransform rect = cardObj.GetOrAddComponent<RectTransform>();
//            rect.localScale = 100f * Vector3.one;
//            rect.anchorMin = Vector2.zero;
//            rect.anchorMax = Vector2.one;
//            rect.offsetMin = Vector2.zero;
//            rect.offsetMax = Vector2.zero;
//            rect.pivot = new Vector2(0.5f, 0.5f);

//            GameObject back = FindObjectInChildren(cardObj, "Back");
//            try
//            {
//                GameObject.Destroy(back);
//            }
//            catch { }
//            FindObjectInChildren(cardObj, "BlockFront")?.SetActive(false);

//            var canvasGroups = cardObj.GetComponentsInChildren<CanvasGroup>();
//            foreach (var canvasGroup in canvasGroups)
//            {
//                canvasGroup.alpha = 1;
//            }

//            ItemShops.instance.ExecuteAfterSeconds(0.5f, () =>
//            {
//                //var particles = cardObj.GetComponentsInChildren<GeneralParticleSystem>().Select(system => system.gameObject);
//                //foreach (var particle in particles)
//                //{
//                //    UnityEngine.GameObject.Destroy(particle);
//                //}

//                var titleText = FindObjectInChildren(cardObj, "Text_Name").GetComponent<TextMeshProUGUI>();

//                if ((titleText.color.r < 0.18f) && (titleText.color.g < 0.18f) && (titleText.color.b < 0.18f))
//                {
//                    titleText.color = new Color32(200, 200, 200, 255);
//                }
//            });

//            return cardObj;
//        }
//        private static GameObject FindObjectInChildren(GameObject gameObject, string gameObjectName)
//        {
//            Transform[] children = gameObject.GetComponentsInChildren<Transform>(true);
//            return (from item in children where item.name == gameObjectName select item.gameObject).FirstOrDefault();
//        }
//    }
//}
