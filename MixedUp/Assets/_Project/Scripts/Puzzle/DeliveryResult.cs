namespace MixedUp
{
    /// <summary>Everything the results and game over screens need to know about a finished delivery.</summary>
    public sealed class DeliveryResult
    {
        public CombinationOutcome Outcome;
        public int BaseReward;
        public int Reward;
        public int DangerCount;
        public int Delivered;
        public int Total;
        public float Seconds;
        public BoxData A;
        public BoxData B;
        public bool LocalPlayerDied;
        public DeathCause? Cause;
        public string ModeId;
        /// <summary>Extra money for the seconds left on the clock in a timed mode.</summary>
        public int TimeBonus;
    }
}
