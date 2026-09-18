using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RarityLib.Utils
{
    public class RarityUtils
    {
        internal static Dictionary<int, Rarity> rarities = new Dictionary<int, Rarity>();
        internal static Dictionary<CardInfo, float> CardRarities = new Dictionary<CardInfo, float>();
        internal static Dictionary<CardInfo, float> CardRaritiesAdd = new Dictionary<CardInfo, float>();
        internal static Dictionary<CardInfo, float> CardRaritiesMul = new Dictionary<CardInfo, float>();
        public static IReadOnlyDictionary<int, Rarity> Rarities { get { return rarities; } }
        internal static bool Finalized = false;
        internal static bool Started = false;
        public static int AddRarity(string name, float relativeRarity, Color color, Color colorOff)
        {
            if (!Started)
            {
                throw new RarityException("The rarity regestry hasnt been set up yet. \n Are you depending on raritylib?");
            }
            if (Finalized)
            {
                throw new RarityException("Raritys can no longer be regestered. \n Is this being called in the mods awake function?");
            }
            int i = rarities.Count;
            if (rarities.Values.Any(r => r.name == name))
            {
                UnityEngine.Debug.LogWarning($"Rarity with name {name} already exists");
                return rarities.Keys.Where(j => rarities[j].name == name).First();
            }
            if (relativeRarity <= 0)
                throw new RarityException("The relative rarity of a rarity must be grater than 0");
            rarities.Add(i, new Rarity(name, relativeRarity, color, colorOff, (CardInfo.Rarity)i));
            return i;
        }

        public static CardInfo.Rarity GetRarity(string rarityName)
        {
            Rarity rarity = rarities.Values.ToList().Find(r => r.name == rarityName);
            if(rarity == null) return (CardInfo.Rarity)0;
            return (CardInfo.Rarity)rarities.Keys.ToList().Find(i => rarities[i] == rarity);
        }

        public static Rarity GetRarityData(CardInfo.Rarity rarity)
        {
            return rarities[(int)rarity];
        }

        public static float GetCardRarityModifier(CardInfo card)
        {
            if (!CardRarities.ContainsKey(card)) CardRarities[card] = 1;
            if (!CardRaritiesAdd.ContainsKey(card)) CardRaritiesAdd[card] = 0;
            if (!CardRaritiesMul.ContainsKey(card)) CardRaritiesMul[card] = 1;
            return (CardRarities[card] + CardRaritiesAdd[card]) * CardRaritiesMul[card];
        }
        public static void SetCardRarityModifier(CardInfo card, float modifier)
        {
                CardRarities[card] = modifier;
        }
        public static void AjustCardRarityModifier(CardInfo card, float add = 0, float mul = 0)
        {
            if (!CardRaritiesAdd.ContainsKey(card)) CardRaritiesAdd[card] = 0;
            if (!CardRaritiesMul.ContainsKey(card)) CardRaritiesMul[card] = 1;
            CardRaritiesAdd[card] += add;
            CardRaritiesMul[card] += mul;
        }

        internal static IEnumerator Reset()
        {
            CardRarities.Clear();
            yield break;
        }

    }

    public class Rarity
    {
        public string name;
        public float relativeRarity;
        public float calculatedRarity;
        public Color color;
        public Color colorOff;
        public CardInfo.Rarity value;

        internal Rarity(string name, float relativeRarity, Color color, Color colorOff, CardInfo.Rarity value)
        {
            this.name = name;
            this.relativeRarity = relativeRarity;
            this.calculatedRarity = relativeRarity;
            this.color = color;
            this.colorOff = colorOff;
            this.value = value;
        }
        public override bool Equals(object obj)
        {
            if(obj.GetType() == typeof(Rarity))
            {
                return ((Rarity)obj).name == this.name;
            }
            return false;
        }
    }

    internal class RarityException : Exception { 
       public RarityException(string message) : base(message)
        {
        }
    }
}
