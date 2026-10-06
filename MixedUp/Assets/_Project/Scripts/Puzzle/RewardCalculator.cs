using UnityEngine;

namespace MixedUp
{
    public static class RewardCalculator
    {
        /// <summary>The floor of the payout when only dangers happened: a bad delivery still pays a little.</summary>
        public const float MinimumShare = 0.25f;

        /// <summary>Explosions and game overs pay nothing; each dangerous pair costs a share of the base reward.</summary>
        public static int Compute(CombinationOutcome outcome, int dangerCount, int baseReward, float dangerPenalty)
        {
            if (outcome >= CombinationOutcome.Explosion) return 0;

            float share = Mathf.Max(MinimumShare, 1f - dangerPenalty * Mathf.Max(0, dangerCount));
            return Mathf.RoundToInt(baseReward * share);
        }
    }
}
