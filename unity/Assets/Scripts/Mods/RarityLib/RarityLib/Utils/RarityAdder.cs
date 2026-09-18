using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RarityLib.Utils
{
    /// <summary>
    /// Intended of use in making unity cards only.
    /// </summary>
    internal class RarityAdder : MonoBehaviour
    {
        public string rarityName = "Common";

        void Start()
        {
            GetComponent<CardInfo>().rarity = RarityUtils.GetRarity(rarityName);
        }
    }
}
