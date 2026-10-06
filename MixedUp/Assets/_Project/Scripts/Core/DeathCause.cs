namespace MixedUp
{
    /// <summary>Why a player died. The key is a localization key; Args feed string.Format.</summary>
    public readonly struct DeathCause
    {
        public readonly string Key;
        public readonly object[] Args;

        public DeathCause(string key, params object[] args)
        {
            Key = key;
            Args = args;
        }

        public string Localized => Localization.Get(Key, Args);

        public static DeathCause Heat => new DeathCause("death.heat");
        public static DeathCause ElectricWater => new DeathCause("death.electric_water");
        public static DeathCause Fall => new DeathCause("death.fall");
        public static DeathCause Void => new DeathCause("death.void");
        public static DeathCause Burn => new DeathCause("death.burn");
        public static DeathCause Sweeper => new DeathCause("death.sweeper");
        public static DeathCause Timeout => new DeathCause("death.timeout");
    }
}
