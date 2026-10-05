using UnityEngine;

namespace MixedUp
{
    /// <summary>Environmental state a player is currently standing in.</summary>
    public struct HazardState
    {
        public bool InWater;
        public bool OnSlippery;
    }

    /// <summary>Movement tweaks accumulated from every effect a player is suffering.</summary>
    public struct MovementModifiers
    {
        public float SpeedMul;
        public float AccelerationMul;
        public float BrakingMul;
        /// <summary>0 = never slides on slopes; 1 = slides like ice.</summary>
        public float SlopeSlide;

        public static MovementModifiers Default => new MovementModifiers
        {
            SpeedMul = 1f,
            AccelerationMul = 1f,
            BrakingMul = 1f,
            SlopeSlide = 0f
        };
    }

    public enum EffectOverlay
    {
        None,
        Vignette,
        Fog
    }

    /// <summary>Everything an effect may need to know when it ticks.</summary>
    public readonly struct EffectContext
    {
        public readonly PlayerStatus Status;
        public readonly float HeldTime;
        public readonly float DeltaTime;
        public readonly HazardState Hazards;

        public EffectContext(PlayerStatus status, float heldTime, float deltaTime, HazardState hazards)
        {
            Status = status;
            HeldTime = heldTime;
            DeltaTime = deltaTime;
            Hazards = hazards;
        }
    }
}
