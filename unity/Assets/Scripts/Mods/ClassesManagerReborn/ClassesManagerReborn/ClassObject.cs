using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClassesManagerReborn
{
    public class ClassObject
    {
        public CardInfo card;
        public CardType type { internal set; get; }
        public CardInfo[][] RequiredClassesTree;
        public int cap;
        private List<CardInfo> blackList = new List<CardInfo>();
        private bool whitelistAll = false;
        private List<CardInfo> whiteList = new List<CardInfo>();

        ///The list of Class or Subclass cards a player can have while still bein eligable for this Class/Subclass
        public List<CardInfo> WhiteList { get { if (((CardType.SubClass | CardType.Entry) & type) == 0) return null; if (whitelistAll) return ClassesRegistry.GetClassInfos(type); return whiteList; } }
        ///The list of extra cards that make a player unable to get this card.
        public List<CardInfo> BlackList { get { return blackList.ToList(); } }

        public ClassObject(CardInfo card, CardType type, CardInfo[][] requiredClassesTree, int cap = 0)
        {
            this.card = card;
            this.type = type;
            this.cap = cap;
            RequiredClassesTree = requiredClassesTree;
        }

        /// <summary>
        /// Sets wether to act as if all cards are on the whitelist
        /// </summary>
        public ClassObject WhitelistAll(bool value = true)
        {
            whitelistAll = value;
            return this;
        }

        /// <summary>
        /// Adds a card to the whitelist
        /// </summary>
        public ClassObject Whitelist(CardInfo card)
        {
            whitelistAll = false;
            if(!whiteList.Contains(card))
                whiteList.Add(card);
            return this;
        }

        /// <summary>
        /// removes a card from the whitelist
        /// </summary>
        public ClassObject DeWhitelist(CardInfo card)
        {
            whitelistAll = false;
            whiteList.Remove(card);
            return this;
        }

        /// <summary>
        /// Adds a card to the blacklist
        /// </summary>
        public ClassObject Blacklist(CardInfo card)
        {
            if(!blackList.Contains(card))
                blackList.Add(card);
            return this;
        }

        /// <summary>
        /// removes a card from the blacklist
        /// </summary>
        public ClassObject DeBhitelist(CardInfo card)
        {
            blackList.Remove(card);
            return this;
        }

        /// <summary>
        /// Checks if the given player is allowed to have the class card under the classes system
        /// </summary>
        public bool PlayerIsAllowedCard(Player player)
        {
            return SimulatedPlayerIsAllowedCard(player.playerID, player.data.currentCards);
        }

        /// <summary>
        /// Checks if the given player is allowed to have the class card under the classes system, While pretending that they have exactly the cards provided.
        /// </summary>
        public bool SimulatedPlayerIsAllowedCard(int playerID, List<CardInfo> currentCards)
        { 
            if (cap > 0 && currentCards.FindAll(c => c == card).Count >= cap)
            {
                return false;
            }
            if ((type & CardType.NonClassCard) == 0)
            {
                if(currentCards.Contains(Cards.JACK.card) && (type & CardType.Entry) == 0)
                    return false;
                if (ClassesManager.Class_War.Value && (type & CardType.Entry) != 0)
                {
                    foreach (Player p in PlayerManager.instance.players)
                    {
                        if (p.playerID != playerID && p.data.currentCards.Contains(card)) return false;
                    }
                }
                if (!ClassesManager.Ignore_Blacklist.Value && !whitelistAll && ((CardType.Entry | CardType.SubClass) & type) != 0)
                {
                    if (whiteList.Any())
                    {
                        if (currentCards.Where(c => !whiteList.Contains(c)).Intersect(ClassesRegistry.GetClassInfos(type)).Any()) return false;
                    }
                    else
                    {
                        if (currentCards.Intersect(ClassesRegistry.GetClassInfos(type)).Any()) return false;
                    }
                }
            }
            if (blackList.Any())
            {
                if (currentCards.Where(c => blackList.Contains(c)).Any()) return false;
            }
            if ((type & CardType.Entry) != 0) return true;
            foreach(CardInfo[] RequiredClassTree in RequiredClassesTree)
            {
                bool flag = true;
                List<CardInfo> playerCards = currentCards.ToList();
                foreach(CardInfo card in RequiredClassTree)
                {
                    if (playerCards.Contains(card)){
                        playerCards.Remove(card);
                    }
                    else
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag) return true;
            }

            return false;
        }

      
        internal CardInfo GetMissingClass(Player player)
        {
            return GetMissingClass(player.data.currentCards.ToList());
        }

        internal CardInfo GetMissingClass(List<CardInfo> cards)
        {

            List<CardInfo> cardInfos = new List<CardInfo>();
            bool first = true;

            foreach (CardInfo[] RequiredClassTree in RequiredClassesTree)
            {
                List<CardInfo> missing = new List<CardInfo>();
                List<CardInfo> playerCards = cards.ToList();
                foreach (CardInfo card in RequiredClassTree)
                {
                    if (playerCards.Contains(card))
                    {
                        playerCards.Remove(card);
                    }
                    else
                    {
                        missing.Add(card);
                    }
                }
                if (first || cardInfos.Count > missing.Count)
                {
                    first = false;
                    cardInfos = missing;
                }
            }

            return cardInfos.Any() ? cardInfos[0] : null;
        }

        /// <summary>
        /// A funtion requested by willuwontu for creating requiremnt arrays for a card that needs any combanation of N cards from a set with repeats alowed.
        /// </summary>
        /// <param name="cards"> The pool of cards to pull from </param>
        /// <param name="required_count"> the number of cards needed </param>
        public static CardInfo[][] TecTreeHelper(CardInfo[] cards, int required_count)
        {
            List<CardInfo[]> ret = new List<CardInfo[]>();
            List<int> counts = new List<int>(); for(int i = 0; i<required_count; i++) counts.Add(0);
            while (counts[0] < cards.Length)
            {
                List<CardInfo> cardInfos = new List<CardInfo>();
                foreach(int i in counts) cardInfos.Add(cards[i]);
                ret.Add(cardInfos.ToArray());
                counts[counts.Count - 1]++;
                for (int i = counts.Count -1; i <= 0; i--)
                {
                    if (counts[i] == cards.Length && i!=0)
                    {
                        counts[i] = 0;
                        counts[i-1]++;
                    }
                }
            }
            return ret.ToArray();
        }

    }
}
