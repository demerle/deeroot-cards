using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Photon.Pun;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ItemShops.Extensions;
using ItemShops.Monobehaviours;

namespace ItemShops.Utils
{
    /// <summary>
    /// A Shop Class that handles the purchasing of goods by players.
    /// 
    /// See <see cref="ShopManager.CreateShop(string)"/> for creating one.
    /// </summary>
    public class Shop : MonoBehaviour
    {
        string _name;
        /// <summary>
        /// Returns true if this shop is visible.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return this.gameObject.activeSelf;
            }
        }
        /// <summary>
        /// The display name of the shop.
        /// </summary>
        public string DisplayName { get { return _name; } }
        /// <summary>
        /// If true, items that are invalid for the player to purchase are hidden.
        /// </summary>
        public bool HideInvalidItems { get; set; } = false;

        Dictionary<string, ShopItem> _items = new Dictionary<string, ShopItem>();

        ShopItem currentPurchase = null;

        /// <summary>
        /// The current item that the player is viewing. Null if they are not viewing any items.
        /// </summary>
        public ShopItem CurrentPurchase 
        { 
            get 
            { 
                return currentPurchase; 
            } 
        }

        internal ShopItem highlightedItem = null;
        Player currentPlayer = null;
        /// <summary>
        /// The current player that is viewing the shop.
        /// </summary>
        public Player CurrentPlayer
        {
            get
            {
                return currentPlayer;
            }
        }
        private TextMeshProUGUI _title = null;
        private GameObject _tagContainer = null;
        private TMP_InputField _filter = null;
        private GameObject _itemContainer = null;
        private GameObject _purchaseItemObject = null;
        private GameObject _purchaseCostContainer = null;
        private Button _purchaseButton = null;
        private GameObject _purchaseHighlight = null;
        private GameObject _moneyContainer = null;
        private TextMeshProUGUI _moneyText = null;

        /// <summary>
        /// An action executed on all clients when an item is purchased.
        /// 
        /// The player is the purchasing player, and the ShopItem is the purchased item.
        /// </summary>
        public Action<Player, ShopItem> itemPurchasedAction = null;

        internal TextMeshProUGUI Title
        {
            get
            {
                if (!_title)
                {
                    _title = this.gameObject.transform.Find("Titlebar/Title").GetComponent<TextMeshProUGUI>();
                }

                return _title;
            }
        }
        internal GameObject TagContainer
        {
            get
            {
                if (!_tagContainer)
                {
                    _tagContainer = this.gameObject.transform.Find("Shop Sections/Filter Section/Tag View/Viewport/Content").gameObject;
                }

                return _tagContainer;
            }
        }
        internal bool filterSelected = false;
        internal TMP_InputField Filter
        {
            get
            {
                if (!_filter)
                {
                    _filter = this.gameObject.transform.Find("Shop Sections/Filter Section/Item Filter").GetComponent<TMP_InputField>();
                    var handler = _filter.gameObject.AddComponent<FilterHandler>();
                    handler.shop = this;
                }

                return _filter;
            }
        }

        private class FilterHandler : MonoBehaviour, UnityEngine.EventSystems.ISelectHandler, UnityEngine.EventSystems.IDeselectHandler
        {
            public Shop shop = null;
            public void OnDeselect(UnityEngine.EventSystems.BaseEventData data)
            {
                this.ExecuteAfterFrames(1, () => { shop.filterSelected = false; });
            }

            public void OnSelect(UnityEngine.EventSystems.BaseEventData data)
            {
                shop.filterSelected = true;
            }
        }

        internal GameObject ItemContainer
        {
            get
            {
                if (!_itemContainer)
                {
                    _itemContainer = this.gameObject.transform.Find("Shop Sections/Item Section/Item Area/Item Selection/Viewport/Content").gameObject;
                }

                return _itemContainer;
            }
        }
        internal GameObject PurchaseItemObject
        {
            get
            {
                if (!_purchaseItemObject)
                {
                    _purchaseItemObject = this.gameObject.transform.Find("Shop Sections/Item Section/Item Area/Item Info/Item Container/Item Holder").gameObject;
                }
                return _purchaseItemObject;
            }
        }
        internal GameObject PurchaseCostContainer
        {
            get
            {
                if (!_purchaseCostContainer)
                {
                    _purchaseCostContainer = this.gameObject.transform.Find("Shop Sections/Item Section/Item Area/Item Info/Item Container/Cost View/Viewport/Content").gameObject;
                }
                return _purchaseCostContainer;
            }
        }
        internal Button PurchaseButton
        {
            get
            {
                if (!_purchaseButton)
                {
                    _purchaseButton = this.gameObject.transform.Find("Shop Sections/Item Section/Item Area/Item Info/Item Container/Purchase Button").GetComponent<Button>();
                }
                return _purchaseButton;
            }
        }
        internal GameObject PurchaseHighlight
        {
            get
            {
                if (!_purchaseHighlight)
                {
                    _purchaseHighlight = this.PurchaseButton.transform.Find("Highlight").gameObject;
                }
                return _purchaseHighlight;
            }
        }
        internal GameObject MoneyContainer
        {
            get
            {
                if (!_moneyContainer)
                {
                    _moneyContainer = this.gameObject.transform.Find("Shop Sections/Money Section/Money View/Viewport/Content").gameObject;
                }
                return _moneyContainer;
            }
        }

        internal TextMeshProUGUI MoneyText
        {
            get
            {
                if (!_moneyText)
                {
                    _moneyText = this.gameObject.transform.Find("Shop Sections/Money Section/Money Text").gameObject.GetComponent<TextMeshProUGUI>();
                }
                return _moneyText;
            }
        }

        private ScrollRect _scrollRect = null;

        internal ScrollRect Scroll
        {
            get
            {
                if (!_scrollRect)
                {
                    _scrollRect = this.gameObject.transform.Find("Shop Sections/Item Section/Item Area/Item Selection").GetComponent<ScrollRect>();
                }

                return _scrollRect;
            }
        }
        /// <summary>
        /// A readonly dictionary of items available in the shop.
        /// </summary>
        public ReadOnlyDictionary<string, ShopItem> ShopItems
        {
            get
            {
                return new ReadOnlyDictionary<string, ShopItem>(_items);
            }
        }
        /// <summary>
        /// Updates the displayed name of the shop. The Unique ID that the shop was registered with remains the same.
        /// </summary>
        /// <param name="name">The shop's new name.</param>
        public void UpdateTitle(string name)
        {
            this._name = name;
            this.Title.text = name;
        }

        /// <summary>
        /// Updates the displayed name for the money container.
        /// </summary>
        /// <param name="name">The new name.</param>
        public void UpdateMoneyColumnName(string name)
        {
            this.MoneyText.text = name;
        }

        private ShopItem AddItem(string itemID, Purchasable item, PurchaseLimit purchaseLimit, bool update)
        {
            ShopItem shopItem = null;

            try
            {
                if (_items.ContainsKey(itemID))
                {
                    throw new ArgumentException("'ShopItem::AddItem(string itemID, Purchasable item, PurchaseLimit purchaseLimit, bool update)' The itemID must be unique.");
                }

                shopItem = CreateItem(item, purchaseLimit);
                shopItem.Purchasable = item;
                shopItem.ID = itemID;
                _items.Add(itemID, shopItem);

                if (update)
                {
                    ShopManager.instance.ExecuteAfterFrames(2, UpdateItems);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
            return shopItem;
        }

        /// <summary>
        /// Adds an item to the shop for purchase by players.
        /// </summary>
        /// <param name="itemID">A unique ID for the item.</param>
        /// <param name="item">The item to add to the shop.</param>
        /// <param name="purchaseLimit">A specified purchase limit for the item.</param>
        /// <returns>The created ShopItem, or null if it was invalid.</returns>
        public ShopItem AddItem(string itemID, Purchasable item, PurchaseLimit purchaseLimit)
        {
            return AddItem(itemID, item, purchaseLimit, true);
        }

        /// <summary>
        /// Adds an item to the shop for purchase by players.
        /// </summary>
        /// <param name="itemID">A unique ID for the item.</param>
        /// <param name="item">The item to add to the shop.</param>
        /// <returns>The created ShopItem, or null if it was invalid.</returns>
        public ShopItem AddItem(string itemID, Purchasable item)
        {
            return AddItem(itemID, item, new PurchaseLimit(0, 0), true);
        }

        /// <summary>
        /// Adds an item to the shop for purchase by players.
        /// </summary>
        /// <param name="item">The item to add to the shop.</param>
        /// <param name="purchaseLimit">A specified purchase limit for the item.</param>
        /// <returns>The created ShopItem, or null if it was invalid.</returns>
        public ShopItem AddItem(Purchasable item, PurchaseLimit purchaseLimit)
        {
            return this.AddItem(item.Name, item, purchaseLimit);
        }

        /// <summary>
        /// Adds an item to the shop for purchase by players.
        /// </summary>
        /// <param name="item">The item to add to the shop.</param>
        /// <returns>The created ShopItem, or null if it was invalid.</returns>
        public ShopItem AddItem(Purchasable item)
        {
            return this.AddItem(item.Name, item);
        }

        /// <summary>
        /// Adds an array of items to the shop for purchase by players.
        /// </summary>
        /// <param name="itemIDs">The unique IDs of the items to add.</param>
        /// <param name="items">The items to add.</param>
        /// <returns>An array of the created ShopItems.</returns>
        public ShopItem[] AddItems(string[] itemIDs, Purchasable[] items)
        {
            if (items.Length != itemIDs.Length)
            {
                throw new ArgumentException("'Shop::AddItems(string[] itemIDs, Purchasable[] items)' expects 2 arrays of equal length.");
            }
            List<ShopItem> shopItems = new List<ShopItem>();
            for (int i = 0; i < items.Length; i++)
            {
                shopItems.Add(AddItem(itemIDs[i], items[i], new PurchaseLimit(0, 0), false));
            }

            ShopManager.instance.ExecuteAfterFrames(2, UpdateItems);
            return shopItems.ToArray();
        }

        /// <summary>
        /// Adds an array of items to the shop for purchase by players.
        /// </summary>
        /// <param name="itemIDs">The unique IDs of the items to add.</param>
        /// <param name="items">The items to add.</param>
        /// <param name="purchaseLimit">A single purchase limit to set for each item.</param>
        /// <returns>An array of the created ShopItems.</returns>
        public ShopItem[] AddItems(string[] itemIDs, Purchasable[] items, PurchaseLimit purchaseLimit)
        {
            if (items.Length != itemIDs.Length)
            {
                throw new ArgumentException("'Shop::AddItems(string[] itemIDs, Purchasable[] items, PurchaseLimit purchaseLimit)' The itemIDs and items arrays must be of equal length.");
            }

            List<ShopItem> shopItems = new List<ShopItem>();
            for (int i = 0; i < items.Length; i++)
            {
                shopItems.Add(AddItem(itemIDs[i], items[i], new PurchaseLimit(purchaseLimit), false));
            }

            ShopManager.instance.ExecuteAfterFrames(2, UpdateItems);
            return shopItems.ToArray();
        }

        /// <summary>
        /// Adds an array of items to the shop for purchase by players.
        /// </summary>
        /// <param name="itemIDs">The unique IDs of the items to add.</param>
        /// <param name="items">The items to add.</param>
        /// <param name="purchaseLimits">A purchase limit for each item.</param>
        /// <returns>An array of the created ShopItems.</returns>
        public ShopItem[] AddItems(string[] itemIDs, Purchasable[] items, PurchaseLimit[] purchaseLimits)
        {
            if (items.Length != purchaseLimits.Length || items.Length != itemIDs.Length)
            {
                throw new ArgumentException("'Shop::AddItems(string[] itemIDs, Purchasable[] items, PurchaseLimit[] purchaseLimits)' expects 3 arrays of equal length.");
            }

            List<ShopItem> shopItems = new List<ShopItem>();

            for (int i = 0; i < items.Length; i++)
            {
                shopItems.Add(AddItem(itemIDs[i], items[i], new PurchaseLimit(purchaseLimits[i]), false));
            }

            ShopManager.instance.ExecuteAfterFrames(2, UpdateItems);

            return shopItems.ToArray();
        }

        /// <summary>
        /// Adds an array of items to the shop for purchase by players.
        /// </summary>
        /// <param name="items">The items to add.</param>
        /// <returns>An array of the created ShopItems.</returns>
        public ShopItem[] AddItems(Purchasable[] items)
        {
            return this.AddItems(items.Select(i => i.Name).ToArray(), items);
        }

        /// <summary>
        /// Adds an array of items to the shop for purchase by players.
        /// </summary>
        /// <param name="items">The items to add.</param>
        /// <param name="purchaseLimit">A single purchase limit to set for each item.</param>
        /// <returns>An array of the created ShopItems.</returns>
        public ShopItem[] AddItems(Purchasable[] items, PurchaseLimit purchaseLimit)
        {
            return this.AddItems(items.Select(i => i.Name).ToArray(), items, purchaseLimit);
        }

        /// <summary>
        /// Adds an array of items to the shop for purchase by players.
        /// </summary>
        /// <param name="items">The items to add.</param>
        /// <param name="purchaseLimits">A purchase limit for each item.</param>
        /// <returns>An array of the created ShopItems.</returns>
        public ShopItem[] AddItems(Purchasable[] items, PurchaseLimit[] purchaseLimits)
        {
            return this.AddItems(items.Select(i => i.Name).ToArray(), items, purchaseLimits);
        }

        /// <summary>
        /// Removes an item from the shop.
        /// </summary>
        /// <param name="itemId">The unique Id of the item to remove.</param>
        public void RemoveItem(string itemId)
        {
            if (_items.TryGetValue(itemId, out var item))
            {
                UnityEngine.GameObject.Destroy(item.gameObject);
                _items.Remove(itemId);
            }
            ShopManager.instance.ExecuteAfterFrames(2, UpdateItems);
        }

        /// <summary>
        /// Removes all items from the shop.
        /// </summary>
        public void RemoveAllItems()
        {
            var shopItems = ShopItems.ToArray();
            foreach (var item in shopItems)
            {
                UnityEngine.GameObject.Destroy(item.Value.gameObject);
            }
            _items.Clear();
            UpdateItems();
        }

        private ShopItem CreateItem(Purchasable item, PurchaseLimit purchaseLimit)
        {
            var itemObj = Instantiate<GameObject>(ShopManager.instance.shopItemTemplate, ItemContainer.transform);
            itemObj.GetComponent<RectTransform>().localScale = Vector3.one;
            var interact = itemObj.AddComponent<ButtonInteraction>();

            var shopItem = itemObj.AddComponent<ShopItem>();
            shopItem.Purchasable = item;
            shopItem.PurchaseLimit = purchaseLimit;
            shopItem.UpdateDisplayName(item.Name);

            return shopItem;
        }

        private void DisableItemAnimations(GameObject item, ButtonInteraction interact)
        {
            Animator[] animators = new Animator[0];
            PositionNoise[] noises = new PositionNoise[0];

            animators = item.GetComponentsInChildren<Animator>();
            noises = item.GetComponentsInChildren<PositionNoise>();

            foreach (var animator in animators)
            {
                animator.enabled = false;
            }

            foreach (var noise in noises)
            {
                noise.enabled = false;
            }

            interact.mouseEnter.AddListener(() =>
            {
                foreach (var animator in animators)
                {
                    animator.enabled = true;
                }
                foreach (var noise in noises)
                {
                    noise.enabled = true;
                }
            });

            interact.mouseExit.AddListener(() =>
            {
                foreach (var animator in animators)
                {
                    animator.enabled = false;
                }

                foreach (var noise in noises)
                {
                    noise.enabled = false;
                }
            });
        }

        private GameObject CreateCostItem(GameObject parent, string currency, int amount)
        {
            var costObj = Instantiate<GameObject>(ShopManager.instance.costObjectTemplate, parent.transform);
            costObj.GetComponent<RectTransform>().localScale = Vector3.one;

            var costItem = costObj.AddComponent<CostItem>();
            costItem.Text.text = $"{amount}";
            costItem.Currency = currency;
            CurrencyManager.instance.CurrencyImageAction(currency)(costItem.Image);

            return costObj;
        }

        /// <summary>
        /// Shows the shop for a player.
        /// </summary>
        /// <param name="player">The player to show the show to.</param>
        public void Show(Player player)
        {
            if (player.data.view.IsMine)
            {
                ShopManager.instance.HideAllShops();
                currentPlayer = player;

                UpdateMoney();
                UpdateFilters();
                this.gameObject.SetActive(true);
                ShopManager.instance.CurrentShop = this;

                ExitGames.Client.Photon.Hashtable customProperties = player.data.view.Owner.CustomProperties;

                customProperties["ItemShops-InShop"] = true;

                player.data.view.Owner.SetCustomProperties(customProperties, null, null);
            }
        }

        /// <summary>
        /// Closes the shop.
        /// </summary>
        public void Hide()
        {
            if (!this.gameObject.activeSelf)
            {
                return;
            }

            if (currentPlayer != null && currentPlayer.data.view.IsMine)
            {
                ExitGames.Client.Photon.Hashtable customProperties = currentPlayer.data.view.Owner.CustomProperties;

                customProperties["ItemShops-InShop"] = false;

                currentPlayer.data.view.Owner.SetCustomProperties(customProperties, null, null);
            }

            currentPlayer = null;
            currentPurchase = null;
            ClearPurchaseArea();
            this.gameObject.SetActive(false);
            ShopManager.instance.CurrentShop = null;
            selectedItem = null;
            selectedRow = new int[3];
        }

        private void UpdateMoney()
        {
            foreach (Transform child in MoneyContainer.transform)
            {
                UnityEngine.GameObject.Destroy(child.gameObject);
            }

            if (currentPlayer)
            {
                foreach (var money in currentPlayer.GetAdditionalData().bankAccount.Money)
                {
                    CreateCostItem(this.MoneyContainer, money.Key, money.Value);
                }
            }
        }

        /// <summary>
        /// Forces an update of all items and tags in the store.
        /// </summary>
        public void UpdateItems()
        {
            var tagItems = this.GetComponentsInChildren<TagItem>();
            string[] existingTags = tagItems.Select(tag => tag.Tag.name).ToArray();
            List<string> itemTags = new List<string>();
            
            foreach (var item in ShopItems.Values)
            {
                itemTags.AddRange(item.Purchasable.Tags.Select(tag => tag.name));
            }

            string[] ghostTags = existingTags.Where(tag => (!itemTags.Contains(tag))).ToArray();

            itemTags = itemTags.Distinct().Where(tag => (!existingTags.Contains(tag))).ToList();

            foreach (var item in itemTags)
            {
                var tagObj = Instantiate(ShopManager.instance.tagObjectTemplate, this.TagContainer.transform);
                tagObj.GetComponent<RectTransform>().localScale = Vector3.one;
                tagObj.GetComponentInChildren<TextMeshProUGUI>().text = item;
                tagObj.AddComponent<ButtonInteraction>();

                var tagItem = tagObj.AddComponent<TagItem>();
                tagItem.Tag = new Tag(item);
            }

            foreach (var tagItem in tagItems.Where(item => ghostTags.Contains(item.Tag.name)))
            {
                UnityEngine.GameObject.Destroy(tagItem.gameObject);
            }

            tagItems = this.GetComponentsInChildren<TagItem>();

            UpdateFilters();
        }

        internal void UpdateFilters()
        {
            var tagItems = this.GetComponentsInChildren<TagItem>();

            string[] excludedTags = tagItems.Where(tagItem => tagItem.FilterState == FilterState.Excluded).Select(tagItem => tagItem.Tag).ToArray().Select(tag => tag.name).ToArray();
            string[] requiredTags = tagItems.Where(tagItem => tagItem.FilterState == FilterState.Required).Select(tagItem => tagItem.Tag).ToArray().Select(tag => tag.name).ToArray();

            ShopItem[] validItems = _items.Values.Where(item => (!(excludedTags.Intersect(item.Purchasable.Tags.Select(tag=> tag.name).ToArray()).ToArray().Length > 0)) && (requiredTags.Intersect(item.Purchasable.Tags.Select(tag => tag.name).ToArray()).ToArray().Length == requiredTags.Length)).ToArray();

            if (Filter.text.Trim().Length > 0)
            {
                validItems = validItems.Where(item => 
                {
                    return item.gameObject.GetComponentsInChildren<TextMeshProUGUI>().Select(tmp => tmp.text.ToLower()).Any(text => text.Contains(Filter.text.Trim().ToLower()));
                }).ToArray();
            }

            foreach (var item in this.ShopItems.Values)
            {
                item.gameObject.SetActive(validItems.Contains(item));
            }

            validItems = validItems.Where(item => item.IsItemPurchasable(currentPlayer)).ToArray();

            foreach (var item in this.ShopItems.Values)
            {
                item.gameObject.GetComponent<Button>().interactable = (validItems.Contains(item));
                item.Darken.SetActive(!validItems.Contains(item));
                if (this.HideInvalidItems && !validItems.Contains(item))
                {
                    item.gameObject.SetActive(false);
                }
            }

            tagItems = this.GetComponentsInChildren<TagItem>().OrderBy(item => item.Tag.name).ToArray();

            for (int i = 0; i < tagItems.Length; i++)
            {
                tagItems[i].transform.SetSiblingIndex(i);
            }

            UpdateMoney();
        }

        private void ClearFilters()
        {
            var tagItems = this.GetComponentsInChildren<TagItem>();

            foreach (var item in tagItems)
            {
                item.SetState(FilterState.Allowed);
            }
            UpdateFilters();
        }

        internal void OnItemClicked(ShopItem item)
        {
            ClearPurchaseArea();

            try
            {
                var purchaseItem = item.Purchasable.CreateItem(this.PurchaseItemObject);
                purchaseItem.GetOrAddComponent<RectTransform>().localScale = Vector3.one;
                purchaseItem.GetOrAddComponent<RectTransform>().localPosition = Vector3.zero;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }

            foreach (var cost in item.Purchasable.Cost)
            {
                var costItem = CreateCostItem(PurchaseCostContainer, cost.Key, cost.Value).GetComponent<CostItem>();

                if (currentPlayer && currentPlayer.GetAdditionalData().bankAccount.Money.TryGetValue(cost.Key, out var playerMoney))
                {
                    if (playerMoney < cost.Value)
                    {
                        costItem.Text.color = new Color32(235, 10, 10, 255);
                    }
                    else
                    {
                        costItem.Text.color = new Color32(225, 225, 225, 255);
                    }
                }
                else
                {
                    costItem.Text.color = new Color32(235, 10, 10, 255);
                }
            }

            currentPurchase = item;
            if (selectedItem)
            {
                this.selectedItem.transform.Find("Highlight").gameObject.SetActive(false);
            }
            selectedItem = item.gameObject;
            selectedRow[1] = selectedItem.transform.GetSiblingIndex();
            this.selectedItem.transform.Find("Highlight").gameObject.SetActive(true);
        }

        /// <summary>
        /// Clears the purchase area of displayed items
        /// </summary>
        public void ClearPurchaseArea()
        {
            foreach (Transform child in PurchaseItemObject.transform)
            {
                UnityEngine.GameObject.Destroy(child.gameObject);
            }

            foreach (Transform child in PurchaseCostContainer.transform)
            {
                UnityEngine.GameObject.Destroy(child.gameObject);
            }

            currentPurchase = null;
        }

        private void OnPurchaseClicked()
        {
            OnPurchaseClicked(currentPlayer);
        }

        internal void OnPurchaseClicked(Player player)
        {
            if (currentPurchase != null)
            {
                if (player.GetAdditionalData().bankAccount.HasFunds(currentPurchase.Purchasable.Cost) && currentPurchase.IsItemPurchasable(currentPlayer))
                {
                    NetworkingManager.RPC(typeof(Shop), nameof(URPCA_Purchase), new object[] { ID, player.playerID, currentPurchase.ID });
                    currentPurchase = null;
                    ClearPurchaseArea();
                }
            }
        }

        [UnboundRPC]
        private static void URPCA_Purchase(string shopID, int playerId, string purchase)
        {
            var player = PlayerManager.instance.GetPlayerWithID(playerId);
            var shop = ShopManager.instance.Shops[shopID];
            var item = shop._items[purchase];

            player.GetAdditionalData().bankAccount.Withdraw(item.Purchasable.Cost);
            shop.UpdateMoney();
            item.OnPurchase(player);

            if (shop.itemPurchasedAction != null)
            {
                try
                {
                    shop.itemPurchasedAction(player, item);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }

            shop.UpdateItems();
        }

        private void Start()
        {
            Filter.onValueChanged.AddListener((str)=> { UpdateFilters(); });
            var interact = PurchaseButton.gameObject.AddComponent<ButtonInteraction>();
            interact.mouseClick.AddListener(OnPurchaseClicked);
        }

        private int[] selectedRow = new int[3];
        private int selectedColumn = 1;
        private GameObject selectedItem;
        private float timeHeld = 0f;
        private float delay = 0.5f;
        private float spacing = 0.1f/3f;
        private bool finishedDelay = false;
        private Vector2 lastDirection = Vector2.zero;

        private int GetColumnMax(int column)
        {
            int max = 0;
            switch (column)
            {
                case 0:
                    var visibleTags = this.TagContainer.GetComponentsInChildren<TagItem>().Where(item => item.gameObject.activeSelf).ToArray();
                    max = visibleTags.Count();
                    break;
                case 1:
                    var visibleItems = this.ItemContainer.GetComponentsInChildren<ShopItem>().Where(item => item.gameObject.activeSelf).ToArray();
                    max = visibleItems.Count() - 1;
                    break;
            }

            return max;
        }

        private bool ActionWasPressed(PlayerActions playerActions)
        {
            if (playerActions.Move.WasPressed)
            {
                lastDirection = new Vector2(playerActions.Move.Value.x, playerActions.Move.Value.y * -1f);
            }

            return (playerActions.Move.WasPressed || playerActions.Jump.WasPressed);
        }

        private bool ActionHeldDown(PlayerActions playerActions)
        {
            if (playerActions.Move.IsPressed && !playerActions.Move.WasPressed)
            {
                timeHeld += TimeHandler.deltaTime;

                if ((!finishedDelay && (timeHeld > delay)) || (finishedDelay && (timeHeld > spacing)))
                {
                    timeHeld = 0f;
                    finishedDelay = true;
                    lastDirection = new Vector2(playerActions.Move.Value.x, playerActions.Move.Value.y * -1f);
                    return true;
                }
                else
                {
                    lastDirection = Vector2.zero;
                }
            }

            return false;
        }

        private void CheckForReleasedActions(PlayerActions playerActions)
        {
            if (playerActions.Move.WasReleased)
            {
                finishedDelay = false;
                lastDirection = Vector2.zero;
            }
        }

        private GameObject GetSelectedItem()
        {
            GameObject item = null;
            switch (selectedColumn)
            {
                case 0:
                    if (selectedRow[selectedColumn] == 0)
                    {
                        item = this.Filter.gameObject;
                    }
                    else
                    {
                        var visibleTags = this.TagContainer.GetComponentsInChildren<TagItem>().Where(item2 => item2.gameObject.activeSelf).ToArray();
                        item = visibleTags[selectedRow[selectedColumn]-1].gameObject;
                        this.TagContainer.transform.parent.parent.gameObject.GetComponent<ScrollRect>().VerticalScrollToChild(item);
                    }
                    break;
                case 1:
                    var visibleItems = this.ItemContainer.GetComponentsInChildren<ShopItem>().Where(item2 => item2.gameObject.activeSelf).ToArray();
                    item = visibleItems[selectedRow[selectedColumn]].gameObject;
                    this.ItemContainer.transform.parent.parent.gameObject.GetComponent<ScrollRect>().VerticalScrollToChild(item);
                    break;
                case 2:
                    item = this.PurchaseButton.gameObject;
                    break;
            }

            return item;
        }

        private void HandleControllerMovement(Player player)
        {
            var playerActions = player.data.playerActions;
            this.CheckForReleasedActions(playerActions);
            if (this.ActionWasPressed(playerActions) || this.ActionHeldDown(playerActions))
            {
                if (!this.Filter.isFocused)
                {
                    if (this.selectedItem != null)
                    {
                        int x = Mathf.Clamp((int)(this.lastDirection.x * 10f), -1, 1);
                        int y = Mathf.Clamp((int)(this.lastDirection.y * 10f), -1, 1);

                        this.selectedColumn += x;
                        this.selectedColumn = Mathf.Clamp(this.selectedColumn, 0, 2);

                        this.selectedRow[this.selectedColumn] += y;
                        this.selectedRow[this.selectedColumn] = Mathf.Clamp(this.selectedRow[this.selectedColumn], 0, this.GetColumnMax(this.selectedColumn));

                        //UnityEngine.Debug.Log($"Selected Item: {this.selectedColumn}, {this.selectedRow[this.selectedColumn]}");
                    }

                    GameObject item = this.GetSelectedItem();

                    if (item != null && this.selectedItem != null && item != this.selectedItem)
                    {
                        this.selectedItem.transform.Find("Highlight").gameObject.SetActive(false);
                        this.selectedItem = null;
                        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                    }

                    this.selectedItem = item;
                    this.selectedItem.transform.Find("Highlight").gameObject.SetActive(true);

                    if (playerActions.Jump.WasPressed)
                    {
                        switch (this.selectedColumn)
                        {
                            case 0:
                                if (this.selectedRow[selectedColumn] == 0)
                                {
                                    this.Filter.Select();
                                    this.Filter.ActivateInputField();
                                }
                                else
                                {
                                    this.selectedItem.GetComponent<ButtonInteraction>().mouseClick?.Invoke();
                                }
                                break;
                            case 1:
                                this.selectedItem.GetComponent<ButtonInteraction>().mouseClick?.Invoke();
                                break;
                            case 2:
                                this.selectedItem.GetComponent<ButtonInteraction>().mouseClick?.Invoke();
                                break;
                        }
                    } 
                }
            }
        }

        private void Update()
        {
            if (currentPlayer != null)
            {
                HandleControllerMovement(currentPlayer);
            }
        }

        internal string ID;
    }
}
