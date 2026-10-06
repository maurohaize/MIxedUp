using UnityEngine;

namespace MixedUp
{
    /// <summary>Harmless until the carrier touches water, then it discharges repeatedly.</summary>
    [CreateAssetMenu(fileName = "Effect_Electric", menuName = "MixedUp/Effects/Electric")]
    public class ElectricEffect : BoxEffect
    {
        public float shockDamage = 60f;
        [Tooltip("Seconds between discharges while standing in water.")]
        public float shockInterval = 0.75f;

        const string CooldownKey = "electric.shock";

        public override void Tick(in EffectContext ctx)
        {
            if (!ctx.Hazards.InWater) return;
            if (!ctx.Status.TryUseCooldown(CooldownKey, shockInterval)) return;

            ctx.Status.RaiseShock();
            ctx.Status.Damage(shockDamage, DeathCause.ElectricWater);
        }

        /// <summary>A faint crackle while carried, full intensity the moment it touches water.</summary>
        public override float Severity01(in EffectContext ctx) => ctx.Hazards.InWater ? 1f : 0.3f;
    }
}
