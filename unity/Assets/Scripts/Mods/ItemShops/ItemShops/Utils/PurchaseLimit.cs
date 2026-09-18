using System;
using System.Collections.Generic;
using System.Text;

namespace ItemShops.Utils
{
    /// <summary>
    /// A struct used to define the purchasing limits for an item.
    /// </summary>
    public struct PurchaseLimit
    {
        /// <summary>
        /// The number of times the item can be bought globally.
        /// </summary>
        public readonly int global;
        /// <summary>
        /// The number of times the item can be bought per player.
        /// </summary>
        public readonly int perPlayer;

        /// <summary>
        /// Dictates how many times a <see cref="Purchasable"/> can be purchased.
        /// 
        /// A value of 0 means there is no limit.
        /// </summary>
        /// <param name="global">The number of times the item can be bought globally.</param>
        /// <param name="perPlayer">The number of times the item can be bought on a per-player basis.</param>
        public PurchaseLimit(int global = 0, int perPlayer = 0)
        {
            this.global = global;
            this.perPlayer = perPlayer;
        }

        /// <summary>
        /// Dictates how many times a <see cref="Purchasable"/> can be purchased.
        /// 
        /// A value of 0 means there is no limit.
        /// </summary>
        /// <param name="purchaseLimit">The Purchase Limit to copy from.</param>
        public PurchaseLimit(PurchaseLimit purchaseLimit)
        {
            this.global = purchaseLimit.global;
            this.perPlayer = purchaseLimit.perPlayer;
        }
    }
}
