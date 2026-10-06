using UnityEngine;

namespace MixedUp
{
    /// <summary>The full combination table. Pairs without a rule are safe.</summary>
    [CreateAssetMenu(fileName = "CombinationRules", menuName = "MixedUp/Combination Rules")]
    public class CombinationRules : ScriptableObject
    {
        public CombinationRule[] rules = System.Array.Empty<CombinationRule>();

        public CombinationRule Find(BoxData x, BoxData y)
        {
            if (rules == null) return null;
            foreach (var rule in rules)
                if (rule != null && rule.Matches(x, y)) return rule;
            return null;
        }

        public CombinationOutcome OutcomeOf(BoxData x, BoxData y)
        {
            var rule = Find(x, y);
            return rule == null ? CombinationOutcome.Safe : rule.outcome;
        }
    }
}
