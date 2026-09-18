using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;
using ItemShops.Monobehaviours;
using TMPro;

namespace ItemShops.Utils
{
    public class ShopItem : MonoBehaviour
    {
        private ButtonInteraction interact;
        private Shop shop;
        private TextMeshProUGUI _text;

        internal TextMeshProUGUI Text
        {
            get
            {
                if (!_text)
                {
                    _text = this.gameObject.transform.Find("Name").GetComponent<TextMeshProUGUI>();
                }
                return _text;
            }
        }
        private GameObject _highlight = null;

        internal GameObject Highlight
        {
            get
            {
                if (!_highlight)
                {
                    _highlight = this.transform.Find("Highlight").gameObject;
                }
                return _highlight;
            }
        }

        private GameObject _darken = null;

        internal GameObject Darken
        {
            get
            {
                if (!_darken)
                {
                    _darken = this.transform.Find("Darken").gameObject;
                }
                return _darken;
            }
        }

        /// <summary>
        /// The Purchasable containing information about the item.
        /// </summary>
        public Purchasable Purchasable { get; internal set; }

        /// <summary>
        /// The purchase limit for the item.
        /// </summary>
        public PurchaseLimit PurchaseLimit { get; internal set; }

        internal int _timesPurchased = 0;
        internal Dictionary<Player, int> _timesPlayerPurchased = new Dictionary<Player, int>();

        /// <summary>
        /// The number of times the item has been purchased.
        /// </summary>
        public int TimesPurchased { get { return _timesPurchased; } }
        /// <summary>
        /// The number of times the item has been purchased by each player.
        /// 
        /// Note: Only players who've purchased the item are contained in this dictionary.
        /// </summary>
        public ReadOnlyDictionary<Player, int> TimesPlayerPurchased { get { return new ReadOnlyDictionary<Player, int>(_timesPlayerPurchased); } }

        internal void OnPurchase(Player player)
        {
            _timesPurchased++;
            if (_timesPlayerPurchased.TryGetValue(player, out int times))
            {
                times++;
            }
            else
            {
                _timesPlayerPurchased.Add(player, 1);
            }
            Purchasable.OnPurchase(player, Purchasable);
        }

        /// <summary>
        /// Returns whether an item is purchaseable.
        /// </summary>
        /// <param name="player">Optional. A player to check the item's validity against.</param>
        /// <returns>True if the item is purchasable, false if not.</returns>
        public bool IsItemPurchasable(Player player = null)
        {
            bool canPurchase = this.Purchasable.CanPurchase(player);

            if (player && (this.PurchaseLimit.perPlayer > 0))
            {
                if (this.TimesPlayerPurchased.TryGetValue(player, out int times))
                {
                    if (times >= this.PurchaseLimit.perPlayer)
                    {
                        canPurchase = false;
                    }
                }
            }

            if (this.PurchaseLimit.global > 0)
            {
                if (this.TimesPurchased >= this.PurchaseLimit.global)
                {
                    canPurchase = false;
                }
            }

            return canPurchase;
        }

        /// <summary>
        /// Updates the displayed name of the item.
        /// </summary>
        /// <param name="name">The name to display.</param>
        public void UpdateDisplayName(string name)
        {
            this.Text.text = name;
        }

        /// <summary>
        /// Resets the amount of times the item has been purchased.
        /// </summary>
        public void ResetPurchases()
        {
            _timesPurchased = 0;
            _timesPlayerPurchased.Clear();
        }

        private void Start()
        {
            interact = this.gameObject.GetComponent<ButtonInteraction>();
            shop = this.gameObject.GetComponentInParent<Shop>();
            if (!interact || !shop)
            {
                UnityEngine.GameObject.Destroy(this);
                return;
            }
            interact.mouseClick.AddListener(OnClick);
            //interact.mouseEnter.AddListener(() => 
            //{ 
            //    Highlight.SetActive(true);  
            //    if (shop.highlightedItem)
            //    {
            //        shop.highlightedItem.GetComponent<ButtonInteraction>().mouseExit?.Invoke();
            //    }
            //    shop.highlightedItem = this; 
            //});
            //interact.mouseExit.AddListener(() => 
            //{ 
            //    Highlight.SetActive(false); 
            //    if (this == shop.highlightedItem)
            //    {
            //        shop.highlightedItem = null;
            //    }
            //});
        }

        private void OnClick()
        {
            shop.OnItemClicked(this);
        }

        internal string ID;
    }
}
