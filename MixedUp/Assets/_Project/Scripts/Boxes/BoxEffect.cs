using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// A behaviour that applies to whoever carries a box. Stateless: all per-player state
    /// lives in PlayerStatus / PlayerInventory, so one asset is shared by every player.
    /// To add a new effect, derive from this class and create an asset from it.
    /// </summary>
    public abstract class BoxEffect : ScriptableObject
    {
        [Header("Presentation")]
        public string displayNameKey;
        public Color hudColor = Color.white;
        public EffectOverlay overlay = EffectOverlay.Vignette;
        [Tooltip("Hand-drawn full-screen frame (transparent centre) shown while the effect is active. Replaces the generated vignette.")]
        public Texture2D screenOverlay;

        /// <summary>Called every frame while a box with this effect is carried.</summary>
        public virtual void Tick(in EffectContext ctx) { }

        public virtual void ModifyMovement(ref MovementModifiers modifiers, in EffectContext ctx) { }

        /// <summary>0..1 target for the screen-obscuring overlay (toxic fog).</summary>
        public virtual float VisionObstruction(float heldTime) => 0f;

        /// <summary>0..1 how bad this effect currently is. Drives HUD bars and overlay strength.</summary>
        public virtual float Severity01(in EffectContext ctx) => 0f;
    }
}
