using UnityEngine;

namespace MixedUp
{
    /// <summary>Burns the carrier, getting worse the longer the box is held.</summary>
    [CreateAssetMenu(fileName = "Effect_Heat", menuName = "MixedUp/Effects/Heat")]
    public class HeatEffect : BoxEffect
    {
        [Tooltip("Damage per second right after picking the box up.")]
        public float startDamagePerSecond = 2f;
        [Tooltip("Damage per second once the ramp is complete.")]
        public float maxDamagePerSecond = 10f;
        [Tooltip("Seconds of carrying until damage reaches its maximum.")]
        public float rampSeconds = 30f;

        public override void Tick(in EffectContext ctx)
        {
            float dps = Mathf.Lerp(startDamagePerSecond, maxDamagePerSecond, Severity01(ctx));
            ctx.Status.Damage(dps * ctx.DeltaTime, DeathCause.Heat);
        }

        public override float Severity01(in EffectContext ctx) =>
            rampSeconds <= 0f ? 1f : Mathf.Clamp01(ctx.HeldTime / rampSeconds);
    }
}
