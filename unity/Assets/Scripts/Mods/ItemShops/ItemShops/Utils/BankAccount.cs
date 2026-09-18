using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ItemShops.Utils
{
    /// <summary>
    /// A bank account for handling money.
    /// </summary>
    public class BankAccount
    {
        private Dictionary<string, int> money = new Dictionary<string, int>();
        /// <summary>
        /// The money currently in the bank account.
        /// </summary>
        public ReadOnlyDictionary<string, int> Money
        {
            get
            {
                return new ReadOnlyDictionary<string, int>(money);
            }
        }
        /// <summary>
        /// Adds money to the bank account.
        /// </summary>
        /// <param name="currency">The type of money to add.</param>
        /// <param name="amount">The amount to add.</param>
        public void Deposit(string currency, int amount)
        {
            if (money.ContainsKey(currency))
            {
                money[currency] += amount;
            }
            else
            {
                money.Add(currency, amount);
            }
        }

        /// <summary>
        /// Adds money to the bank account.
        /// </summary>
        /// <param name="money">The types of currency and the amount to add for each.</param>
        public void Deposit(Dictionary<string, int> money)
        {
            foreach (var deposit in money)
            {
                Deposit(deposit.Key, deposit.Value);
            }
        }

        /// <summary>
        /// If the bank account has the necessary funds, this removes the money from the bank account.
        /// 
        /// If the account does not have the correct amount, nothing is removed.
        /// </summary>
        /// <param name="currency">The type of money to remove.</param>
        /// <param name="amount">The amount to remove.</param>
        /// <returns>True if successful, false if the account lacks the funds.</returns>
        public bool Withdraw(string currency, int amount)
        {
            if (money.TryGetValue(currency, out int deposited))
            {
                if (deposited >= amount)
                {
                    money[currency] -= amount;
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// If the bank account has the necessary funds, this removes the money from the bank account.
        /// 
        /// If the account does not have the correct amount, nothing is removed.
        /// </summary>
        /// <param name="money">The typs and amounts of monet to remove.</param>
        /// <returns>True if successful, false if the account lacks the funds.</returns>
        public bool Withdraw(Dictionary<string, int> money)
        {
            bool canWithdraw = money.All(resource => (this.money.TryGetValue(resource.Key, out int deposited) && deposited >= resource.Value));

            if (canWithdraw)
            {
                foreach (var resource in money)
                {
                    this.money[resource.Key] -= resource.Value;
                }
            }

            return canWithdraw;
        }

        /// <summary>
        /// Checks to see if a bank account has the requisite amount of money.
        /// </summary>
        /// <param name="money">The amount of money to check for.</param>
        /// <returns>True if the bank account has the necessary funds, false if not.</returns>
        public bool HasFunds(Dictionary<string, int> money)
        {
            return money.All(resource => (this.money.TryGetValue(resource.Key, out int deposited) && deposited >= resource.Value));
        }

        /// <summary>
        /// Removes all money in the bank account.
        /// </summary>
        public void RemoveAllMoney()
        {
            money.Clear();
        }
    }
}
