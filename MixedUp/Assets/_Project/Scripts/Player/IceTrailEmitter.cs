using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Wading through the river with a FROZEN box freezes the water behind you: a trail of ice slabs that someone else can
    /// walk over, even with an electric box, without touching the water. The slabs melt after a while.
    /// </summary>
    [RequireComponent(typeof(PlayerStatus))]
    public class IceTrailEmitter : MonoBehaviour
    {
        public IceSlab slabPrefab;
        [Tooltip("Distance walked between two slabs.")]
        public float spacing = 1.4f;
        [Tooltip("Slabs alive at the same time; the oldest ones melt first.")]
        public int maxSlabs = 28;
        [Tooltip("Seconds the player who makes the ice can walk through it.")]
        public float ownerPassSeconds = 2.5f;

        readonly Queue<IceSlab> slabs = new Queue<IceSlab>();
        PlayerStatus status;
        Collider body;
        Vector3 lastSlab = new Vector3(0f, -999f, 0f);

        /// <summary>Slabs placed so far (for tests and sounds).</summary>
        public int PlacedCount { get; private set; }
        public static event System.Action<Vector3> Placed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Placed = null;

        void Awake()
        {
            status = GetComponent<PlayerStatus>();
            body = GetComponent<Collider>();
        }

        void Update()
        {
            if (slabPrefab == null || status.IsDead) return;
            if (!status.Hazards.InWater || !CarriesFrozenBox()) return;

            var here = transform.position;
            var flat = new Vector2(here.x - lastSlab.x, here.z - lastSlab.z);
            if (flat.magnitude < spacing) return;

            Place(new Vector3(here.x, 0f, here.z));
        }

        bool CarriesFrozenBox()
        {
            var slots = status.Inventory.Slots;
            for (int i = 0; i < slots.Count; i++)
                if (!slots[i].IsEmpty && slots[i].box.HasEffect<FrozenEffect>()) return true;
            return false;
        }

        /// <summary>Puts a slab of ice at the water surface (the top is flush with the river banks).</summary>
        public IceSlab Place(Vector3 position)
        {
            var slab = Instantiate(slabPrefab, position, Quaternion.Euler(0f, Random.Range(-12f, 12f), 0f));
            if (body != null) slab.IgnoreFor(body, ownerPassSeconds);

            slabs.Enqueue(slab);
            while (slabs.Count > maxSlabs)
            {
                var oldest = slabs.Dequeue();
                if (oldest != null) oldest.MeltNow();
            }

            lastSlab = position;
            PlacedCount++;
            Placed?.Invoke(position);
            return slab;
        }
    }
}
