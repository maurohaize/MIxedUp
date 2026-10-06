namespace MixedUp
{
    /// <summary>What a player has earned over many games: money and the combinations they have learned.</summary>
    public static class GameProgress
    {
        /// <summary>Forgets every learned combination and empties the wallet, so a new player can start from scratch.</summary>
        public static void ResetAll()
        {
            CombinationManual.ForgetAll();
            Wallet.Reset();
        }
    }
}
