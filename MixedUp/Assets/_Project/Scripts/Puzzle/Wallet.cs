using System;
using UnityEngine;

namespace MixedUp
{
    /// <summary>The player's money, kept between sessions. Spent on upgrades and cosmetics in phase 5.</summary>
    public static class Wallet
    {
        const string PrefKey = "wallet.coins";

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Changed = null;

        public static int Coins => PlayerPrefs.GetInt(PrefKey, 0);

        public static void Add(int amount)
        {
            if (amount <= 0) return;
            PlayerPrefs.SetInt(PrefKey, Coins + amount);
            Changed?.Invoke();
        }

        /// <summary>Back to an empty purse (a fresh start for a new player).</summary>
        public static void Reset()
        {
            PlayerPrefs.DeleteKey(PrefKey);
            Changed?.Invoke();
        }

        public static bool TrySpend(int amount)
        {
            if (amount < 0 || Coins < amount) return false;
            PlayerPrefs.SetInt(PrefKey, Coins - amount);
            Changed?.Invoke();
            return true;
        }
    }
}
