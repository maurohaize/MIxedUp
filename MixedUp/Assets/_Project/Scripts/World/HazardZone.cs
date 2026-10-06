using UnityEngine;

namespace MixedUp
{
    public enum HazardType
    {
        Water,
        Slippery,
        /// <summary>Thick mud: slows walking and jumping.</summary>
        Mud,
        /// <summary>Open flames: hurts anyone standing in them.</summary>
        Fire
    }

    /// <summary>A trigger volume that marks the ground inside it as dangerous (water, ice, mud, fire).</summary>
    [RequireComponent(typeof(Collider))]
    public class HazardZone : MonoBehaviour
    {
        public HazardType type;

        void Reset() => GetComponent<Collider>().isTrigger = true;
    }
}
