using System;

namespace MixedUp
{
    /// <summary>One entry of the combination table: what happens when box A sits next to box B (order irrelevant).</summary>
    [Serializable]
    public class CombinationRule
    {
        public BoxData a;
        public BoxData b;
        public CombinationOutcome outcome;
        [UnityEngine.Tooltip("Localization key of the hint shown in notes and in the manual once discovered.")]
        public string hintKey;
        [UnityEngine.Tooltip("Known from the start, so the player does not have to discover it.")]
        public bool knownByDefault;

        public bool Matches(BoxData x, BoxData y) => (a == x && b == y) || (a == y && b == x);

        /// <summary>Order-independent identifier, e.g. "electric+hot". Used for saves and networking.</summary>
        public string Key
        {
            get
            {
                string ia = a != null ? a.id : string.Empty;
                string ib = b != null ? b.id : string.Empty;
                return string.CompareOrdinal(ia, ib) <= 0 ? ia + "+" + ib : ib + "+" + ia;
            }
        }

        /// <summary>Same-type safe pairs carry no information, so they stay out of the manual.</summary>
        public bool IsTrivial => a == b && outcome == CombinationOutcome.Safe;
    }
}
