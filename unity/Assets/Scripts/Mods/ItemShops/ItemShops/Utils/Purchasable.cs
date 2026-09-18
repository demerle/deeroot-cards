using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ItemShops.Utils
{
    public abstract class Purchasable
    {
        /// <summary>
        /// The name of the item.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The item's cost.
        /// </summary>
        public abstract Dictionary<string, int> Cost { get; }

        /// <summary>
        /// The action run to create a gameobject for displaying in the store.
        /// 
        /// Note, the display area is a 165x255 rectangle.
        /// </summary>
        /// <param name="parent">The gameobject to create the item in.</param>
        /// <returns>The created item.</returns>
        public abstract GameObject CreateItem(GameObject parent);

        /// <summary>
        /// A function run whenever the item is purchased.
        /// </summary>
        /// <param name="player">The player purchasing the item.</param>
        /// <param name="item">The item purchased.</param>
        public abstract void OnPurchase(Player player, Purchasable item);

        /// <summary>
        /// The tags that the item has.
        /// </summary>
        public abstract Tag[] Tags { get; }

        /// <summary>
        /// A condition checked to see if an item can be purchased.
        /// </summary>
        /// <param name="player">The player for whom the item is checked. Null inputs are valid.</param>
        /// <returns>True if the item is purchasable.</returns>
        public abstract bool CanPurchase(Player player);
    }
}
