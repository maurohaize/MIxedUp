using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// The clock the moving parts of a level (the spinning log, the raft, gusts of wind, drifting things) follow in an online
    /// game. It starts at zero on every machine at the moment the host says "go", so all of them show the same moment of
    /// the same cycle. Offline those parts keep their own timers and this clock is not used.
    /// </summary>
    public static class LevelClock
    {
        static double start;

        /// <summary>Seconds of game time since the level started for everybody.</summary>
        public static double Seconds => Time.timeAsDouble - start;

        /// <summary>Zero point; called when the level is ready on every machine.</summary>
        public static void Begin() => start = Time.timeAsDouble;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => start = 0.0;
    }
}
