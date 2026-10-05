using UnityEngine;

namespace MixedUp
{
    public enum HazardType
    {
        Water,
        Slippery
    }

    /// <summary>A trigger volume that marks the ground inside it as dangerous (water, ice).</summary>
    [RequireComponent(typeof(Collider))]
    public class HazardZone : MonoBehaviour
    {
        public HazardType type;

        void Reset() => GetComponent<Collider>().isTrigger = true;
    }
}
