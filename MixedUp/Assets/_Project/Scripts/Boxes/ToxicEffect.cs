using UnityEngine;

namespace MixedUp
{
    /// <summary>Fogs the carrier's screen; the longer the box is held the less they can see.</summary>
    [CreateAssetMenu(fileName = "Effect_Toxic", menuName = "MixedUp/Effects/Toxic")]
    public class ToxicEffect : BoxEffect
    {
        [Tooltip("Seconds of carrying until the fog is at its thickest.")]
        public float secondsToMaxFog = 25f;
        [Range(0f, 1f)] public float maxObstruction = 0.95f;

        public override float VisionObstruction(float heldTime)
        {
            float t = secondsToMaxFog <= 0f ? 1f : Mathf.Clamp01(heldTime / secondsToMaxFog);
            return maxObstruction * Mathf.SmoothStep(0f, 1f, t);
        }

        public override float Severity01(in EffectContext ctx) =>
            secondsToMaxFog <= 0f ? 1f : Mathf.Clamp01(ctx.HeldTime / secondsToMaxFog);
    }
}
