namespace WillsWackyManagers.Utils
{
    public static class Rarities
    {
        public static CardInfo.Rarity Trinket
        {
            get
            {
                return RarityLib.Utils.RarityUtils.GetRarity("Trinket");
            }
        }
        public static CardInfo.Rarity Common => CardInfo.Rarity.Common;
        public static CardInfo.Rarity Uncommon => CardInfo.Rarity.Uncommon;
        public static CardInfo.Rarity Scarce
        {
            get
            {
                return RarityLib.Utils.RarityUtils.GetRarity("Scarce");
            }
        }
        public static CardInfo.Rarity Rare => CardInfo.Rarity.Rare;

        public static CardInfo.Rarity Epic
        {
            get
            {
                return RarityLib.Utils.RarityUtils.GetRarity("Epic");
            }
        }

        public static CardInfo.Rarity Legendary
        {
            get
            {
                return RarityLib.Utils.RarityUtils.GetRarity("Legendary");
            }
        }

        public static CardInfo.Rarity Mythical
        {
            get
            {
                return RarityLib.Utils.RarityUtils.GetRarity("Mythical");
            }
        }
    }
}