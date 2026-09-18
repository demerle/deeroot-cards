using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ItemShops.Utils
{
    /// <summary>
    /// A class for handling the visualization of the various currencies.
    /// </summary>
    public class CurrencyManager : MonoBehaviour
    {
        private Dictionary<string, Action<Image>> CurrencyImageActions = new Dictionary<string, Action<Image>>();

        private Action<Image> defaultImageAction = (image) => 
            { 
                image.sprite = ItemShops.instance.assets.LoadAsset<Sprite>("dollar-sign");
                image.color = new Color32(118, 117, 35, 255);
            };

        /// <summary>
        /// The instance of the currency manager.
        /// </summary>
        public static CurrencyManager instance;

        /// <summary>
        /// Registers an image action for a particular currency.
        /// </summary>
        /// <param name="currency">The name of the currency.</param>
        /// <param name="imageAction"> The action to run for the currency.
        ///     <param name="imageAction arg1">The image to run the action on.</param>
        /// </param>
        /// <returns>True if the action is registered, false if an action for that currency already exists.</returns>
        public bool RegisterCurrencyIcon(string currency, Action<Image> imageAction)
        {
            if (CurrencyImageActions.ContainsKey(currency))
            {
                return false;
            }
            CurrencyImageActions.Add(currency, imageAction);

            return true;
        }

        /// <summary>
        /// Fetches the image action associated with a currency name.
        /// </summary>
        /// <param name="currency">The name of the currency to find the image action for.</param>
        /// <returns>The image action associated with the currency, or the default image if there is none set.</returns>
        public Action<Image> CurrencyImageAction(string currency)
        {
            Action<Image> imageAction = null;
            if (CurrencyImageActions.TryGetValue(currency, out var action))
            {
                imageAction = action;
            }
            else
            {
                imageAction = defaultImageAction;
            }
            return imageAction;
        }

        private void Awake()
        {
            if (instance != null)
            {
                UnityEngine.GameObject.Destroy(this);
            }
            instance = this;
        }
        private void Start()
        {

        }
    }
}
