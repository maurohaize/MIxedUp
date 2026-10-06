using System.Collections.Generic;

namespace MixedUp
{
    /// <summary>Checks that an order can be arranged in the truck so that no two neighbours react: every game mode must be winnable.</summary>
    public static class PuzzleSolver
    {
        /// <summary>Is there an order of the boxes in which every neighbouring pair is safe?</summary>
        public static bool HasSafeArrangement(IReadOnlyList<BoxData> boxes, CombinationRules rules) => FindSafeArrangement(boxes, rules) != null;

        /// <summary>One arrangement without any reacting pair, or null when none exists.</summary>
        public static List<BoxData> FindSafeArrangement(IReadOnlyList<BoxData> boxes, CombinationRules rules)
        {
            var kinds = new List<BoxData>();
            var counts = new List<int>();
            foreach (var box in boxes)
            {
                int index = kinds.IndexOf(box);
                if (index < 0)
                {
                    kinds.Add(box);
                    counts.Add(1);
                }
                else
                {
                    counts[index]++;
                }
            }

            var order = new List<BoxData>();
            return Place(kinds, counts, rules, order, boxes.Count) ? order : null;
        }

        static bool Place(List<BoxData> kinds, List<int> counts, CombinationRules rules, List<BoxData> order, int total)
        {
            if (order.Count == total) return true;

            for (int i = 0; i < kinds.Count; i++)
            {
                if (counts[i] == 0) continue;
                if (order.Count > 0 && rules != null && rules.OutcomeOf(order[order.Count - 1], kinds[i]) != CombinationOutcome.Safe) continue;

                counts[i]--;
                order.Add(kinds[i]);
                if (Place(kinds, counts, rules, order, total)) return true;
                order.RemoveAt(order.Count - 1);
                counts[i]++;
            }
            return false;
        }
    }
}
