using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>Makes the order of a game mode. Random orders are only accepted when the boxes can be arranged safely, so every game can be won.</summary>
    public static class OrderFactory
    {
        /// <summary>The map's own order for classic modes, or a fresh random one.</summary>
        public static OrderData Create(GameModeInfo mode, IReadOnlyList<BoxData> kinds, CombinationRules rules, OrderData mapOrder, System.Random rng)
        {
            if (mode.UsesMapOrder || kinds == null || kinds.Count == 0) return mapOrder;

            int count = rng.Next(mode.minBoxes, mode.maxBoxes + 1);
            for (int attempt = 0; attempt < 200; attempt++)
            {
                var boxes = Draw(kinds, count, rng, mode.spawn == SpawnStyle.Hard);
                if (PuzzleSolver.HasSafeArrangement(boxes, rules)) return Build(boxes);
            }

            // Cannot happen with the real rules (plain boxes separate anything); keep the game playable anyway.
            return mapOrder;
        }

        static List<BoxData> Draw(IReadOnlyList<BoxData> kinds, int count, System.Random rng, bool wantsDanger)
        {
            // Plain boxes are the most common; the dangerous kinds each show up now and then.
            var boxes = new List<BoxData>();
            var plain = new List<BoxData>();
            foreach (var kind in kinds)
                if (kind.effects == null || kind.effects.Length == 0) plain.Add(kind);

            for (int i = 0; i < count; i++)
            {
                bool takePlain = plain.Count > 0 && rng.NextDouble() < 0.4;
                boxes.Add(takePlain ? plain[rng.Next(plain.Count)] : kinds[rng.Next(kinds.Count)]);
            }

            if (wantsDanger)
            {
                bool hasEffect = boxes.Exists(b => b.effects != null && b.effects.Length > 0);
                if (!hasEffect) boxes[0] = kinds[rng.Next(1, kinds.Count)];
            }
            return boxes;
        }

        static OrderData Build(List<BoxData> boxes)
        {
            var order = ScriptableObject.CreateInstance<OrderData>();
            order.name = "RandomOrder";
            var lines = new List<OrderLine>();
            foreach (var box in boxes)
            {
                int index = lines.FindIndex(l => l.box == box);
                if (index < 0) lines.Add(new OrderLine { box = box, count = 1 });
                else lines[index] = new OrderLine { box = box, count = lines[index].count + 1 };
            }
            order.lines = lines.ToArray();
            return order;
        }
    }
}
