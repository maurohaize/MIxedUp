using UnityEngine;

namespace MixedUp
{
    /// <summary>Slippery carrier: slow to start, slow to stop, slides down slopes. Worse when wet.</summary>
    [CreateAssetMenu(fileName = "Effect_Frozen", menuName = "MixedUp/Effects/Frozen")]
    public class FrozenEffect : BoxEffect
    {
        [Range(0.02f, 1f)] public float accelerationMultiplier = 0.12f;
        [Range(0.02f, 1f)] public float brakingMultiplier = 0.08f;
        [Range(0.2f, 1f)] public float speedMultiplier = 0.95f;
        [Tooltip("How strongly the carrier slides downhill on any noticeable slope.")]
        public float slopeSlide = 1.2f;
        [Tooltip("Extra loss of control on water or slippery ground.")]
        [Range(0.1f, 1f)] public float wetControlMultiplier = 0.5f;

        public override void ModifyMovement(ref MovementModifiers m, in EffectContext ctx)
        {
            m.SpeedMul *= speedMultiplier;
            m.AccelerationMul *= accelerationMultiplier;
            m.BrakingMul *= brakingMultiplier;
            m.SlopeSlide = Mathf.Max(m.SlopeSlide, slopeSlide);

            if (ctx.Hazards.InWater || ctx.Hazards.OnSlippery)
            {
                m.AccelerationMul *= wetControlMultiplier;
                m.BrakingMul *= wetControlMultiplier;
            }
        }

        public override float Severity01(in EffectContext ctx) =>
            ctx.Hazards.InWater || ctx.Hazards.OnSlippery ? 1f : 0.5f;
    }
}
