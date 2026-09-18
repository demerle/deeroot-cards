using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using ItemShops.Utils;

namespace ItemShops.Extensions
{
    [Serializable]
    public class PlayerAdditionalData
    {
        public BankAccount bankAccount;

        public PlayerAdditionalData()
        {
            bankAccount = new BankAccount();
            //bankAccount.Deposit(new Dictionary<string, int> { { "Credits", 50 }, { "Banana", 32 } });
        }
    }
    public static class PlayerExtension
    {
        public static readonly ConditionalWeakTable<Player, PlayerAdditionalData> data =
            new ConditionalWeakTable<Player, PlayerAdditionalData>();

        public static PlayerAdditionalData GetAdditionalData(this Player player)
        {
            return data.GetOrCreateValue(player);
        }

        public static void AddData(this Player player, PlayerAdditionalData value)
        {
            try
            {
                data.Add(player, value);
            }
            catch (Exception) { }
        }
    }
}
