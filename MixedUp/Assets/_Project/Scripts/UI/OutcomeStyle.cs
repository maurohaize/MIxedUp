using UnityEngine;

namespace MixedUp
{
    /// <summary>Colour, symbol and text key used wherever a combination outcome is shown.</summary>
    public static class OutcomeStyle
    {
        public static Color Color(CombinationOutcome outcome, bool known)
        {
            if (!known) return new Color(0.62f, 0.58f, 0.52f);
            switch (outcome)
            {
                case CombinationOutcome.Safe: return new Color(0.45f, 0.70f, 0.40f);
                case CombinationOutcome.Danger: return new Color(0.95f, 0.80f, 0.30f);
                case CombinationOutcome.Explosion: return new Color(0.92f, 0.45f, 0.20f);
                default: return new Color(0.60f, 0.15f, 0.15f);
            }
        }

        public static string Symbol(CombinationOutcome outcome, bool known)
        {
            if (!known) return "?";
            switch (outcome)
            {
                case CombinationOutcome.Safe: return "OK";
                case CombinationOutcome.Danger: return "!";
                case CombinationOutcome.Explosion: return "*";
                default: return "X";
            }
        }

        public static string NameKey(CombinationOutcome outcome, bool known)
        {
            if (!known) return "outcome.unknown";
            switch (outcome)
            {
                case CombinationOutcome.Safe: return "outcome.safe";
                case CombinationOutcome.Danger: return "outcome.danger";
                case CombinationOutcome.Explosion: return "outcome.explosion";
                default: return "outcome.game_over";
            }
        }
    }
}
