namespace MixedUp
{
    /// <summary>What happens when two boxes end up next to each other. Ordered by severity.</summary>
    public enum CombinationOutcome
    {
        Safe = 0,
        Danger = 1,
        Explosion = 2,
        GameOver = 3
    }
}
