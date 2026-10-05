using System;
using UnityEngine;

namespace MixedUp
{
    [Serializable]
    public struct OrderLine
    {
        public BoxData box;
        public int count;
    }

    /// <summary>The delivery a level asks for: which boxes, and how many of each.</summary>
    [CreateAssetMenu(fileName = "Order_", menuName = "MixedUp/Order")]
    public class OrderData : ScriptableObject
    {
        public OrderLine[] lines = Array.Empty<OrderLine>();

        public int TotalBoxes
        {
            get
            {
                int total = 0;
                foreach (var line in lines) total += line.count;
                return total;
            }
        }

        public int RequiredCount(BoxData box)
        {
            int total = 0;
            foreach (var line in lines)
                if (line.box == box) total += line.count;
            return total;
        }
    }
}
