using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ClassesManagerReborn
{
    public static class ClassesRegistry
    {
        internal static Dictionary<CardInfo, ClassObject> Registry = new Dictionary<CardInfo, ClassObject>();
        internal static List<CardInfo> ClassInfos = new List<CardInfo>();

        /// <summary>
        /// Registers a card with the manager
        /// </summary>
        /// <param name="card">The card to register</param>
        /// <param name="type">The type of card being registered</param>
        /// <param name="CardLimit">The max number of copies a player is alowed to have, 0 for infinite</param>
        public static ClassObject Register(CardInfo card, CardType type, int CardLimit = 0)
        {
            return Register(card, type, new CardInfo[] { }, CardLimit);
        }

        /// <summary>
        /// Registers a card with the manager
        /// </summary>
        /// <param name="card">The card to register</param>
        /// <param name="type">The type of card being registered</param>
        /// <param name="RequiredClass">The Card a player must have inorder to get this card</param>
        /// <param name="CardLimit">The max number of copies a player is alowed to have, 0 for infinite</param>
        public static ClassObject Register(CardInfo card, CardType type, CardInfo RequiredClass, int CardLimit = 0)
        {
            return Register(card, type, new CardInfo[] { RequiredClass }, CardLimit);
        }

        /// <summary>
        /// Registers a card with the manager
        /// </summary>
        /// <param name="card">The card to register</param>
        /// <param name="type">The type of card being registered</param>
        /// <param name="RequiredClassTree">The Cards a player must have inorder to get this card</param>
        /// <param name="CardLimit">The max number of copies a player is alowed to have, 0 for infinite</param>
        public static ClassObject Register(CardInfo card, CardType type, CardInfo[] RequiredClassTree, int CardLimit = 0)
        {
            return Register(card, type, new CardInfo[][] { RequiredClassTree }, CardLimit);
        }

        /// <summary>
        /// Registers a card with the manager
        /// </summary>
        /// <param name="card">The card to register</param>
        /// <param name="type">The type of card being registered</param>
        /// <param name="RequiredClassesTree">A list of lists of cards, a player must have all the cards on at least on of the lists to get the card</param>
        /// <param name="CardLimit">The max number of copies a player is alowed to have, 0 for infinite</param>
        public static ClassObject Register(CardInfo card, CardType type, CardInfo[][] RequiredClassesTree, int CardLimit = 0)
        {
            if (Registry.ContainsKey(card))
            {
                throw new ArgumentException($"Card {card.cardName} has already been Registered");
            }
            ClassObject classObject = new ClassObject(card, type, RequiredClassesTree, card.allowMultiple? CardLimit : 1);
            Registry.Add(card, classObject);
            if(type == CardType.Entry) ClassInfos.Add(card);
            if (card.allowMultiple)
                classObject.Whitelist(card);
            return classObject;
        }

        /// <summary>
        /// <para>Retrieves the class object for a card that has been registered with the classes registry.</para>
        /// </summary>
        /// <param name="card">The card to fetch the class object for.</param>
        /// <returns>The class object associated with the card, or null if it's not registered.</returns>
        public static ClassObject Get(CardInfo card)
        {
            if(card == null) return null;
            if (Registry.ContainsKey(card)) return Registry[card];
            return null;
        }

        public static List<ClassObject> GetClassObjects(CardType type, bool includeNonClass = false)
        {
            return Registry.Values.Where(v => (type & v.type) != 0 && ((v.type & CardType.NonClassCard) == 0 || includeNonClass)).ToList();
        }

        public static List<ClassObject> GetClassObjects(CardType type, CardType excluded, bool includeNonClass = false)
        {
            return Registry.Values.Where(v => ((type & v.type) != 0) && ((excluded & v.type) == 0) && ((v.type & CardType.NonClassCard) == 0 || includeNonClass)).ToList();
        }

        public static List<CardInfo> GetClassInfos(CardType type, bool includeNonClass = false)
        {
            return Registry.Values.Where(v => (type & v.type) != 0 && ((v.type & CardType.NonClassCard) == 0 || includeNonClass)).Select<ClassObject,CardInfo>(v => v.card).ToList();
        }

        public static List<CardInfo> GetClassInfos(CardType type, CardType excluded, bool includeNonClass = false)
        {
            return Registry.Values.Where(v => ((type & v.type) != 0) && ((excluded & v.type) == 0) && ((v.type & CardType.NonClassCard) == 0 || includeNonClass)).Select<ClassObject, CardInfo>(v => v.card).ToList();
        }

    }

    /// <summary>
    /// <para></para>
    /// </summary>
    [Flags]
    public enum CardType
    {
        /// <summary>
        /// <para>The entry point of a class, normally players can only have one Entry card.</para>
        /// </summary>
        Entry = 1,
        /// <summary>
        /// <para>The entry point of a subclass, normally players can only have one SubClass card.</para>
        /// </summary>
        SubClass = 2,
        /// <summary>
        /// <para>Indicates a decision point on a card tree.</para>
        /// </summary>
        Branch = 4,
        /// <summary>
        /// <para>Indicates a card that locks other cards behind it.</para>
        /// </summary>
        Gate = 8,
        /// <summary>
        /// <para>Indicates a card that is the last node in a branch.</para>
        /// </summary>
        Card = 16,
        /// <summary>
        /// <para>Marks a card that should be ingnored by cards that care about classes.</para>
        /// </summary>
        NonClassCard = 32,
    }
}
