using UnityEngine;

namespace MixedUp
{
    /// <summary>Samples hazard zones at the player's feet and publishes the result to PlayerStatus.</summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(PlayerStatus))]
    public class HazardSensor : MonoBehaviour
    {
        public Vector3 feetOffset = new Vector3(0f, 0.1f, 0f);
        public float radius = 0.12f;

        readonly Collider[] buffer = new Collider[16];
        PlayerStatus status;

        void Awake() => status = GetComponent<PlayerStatus>();

        void Update() => status.Hazards = Sample();

        public HazardState Sample()
        {
            var state = new HazardState();
            int count = Physics.OverlapSphereNonAlloc(transform.position + feetOffset, radius, buffer, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (!buffer[i].TryGetComponent(out HazardZone zone)) continue;
                if (zone.type == HazardType.Water) state.InWater = true;
                else if (zone.type == HazardType.Slippery) state.OnSlippery = true;
                else if (zone.type == HazardType.Mud) state.InMud = true;
                else if (zone.type == HazardType.Fire) state.OnFire = true;
            }
            return state;
        }
    }
}
