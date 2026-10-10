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

        [Tooltip("Puffs of icy mist where a slab forms (optional).")]
        public ParticleSystem frost;

        readonly List<IceSlab> slabs = new List<IceSlab>();
        readonly Stack<IceSlab> pool = new Stack<IceSlab>();
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

        void Recycle(IceSlab slab)
        {
            slabs.Remove(slab);
            slab.gameObject.SetActive(false);
            pool.Push(slab);
        }

        bool CarriesFrozenBox()
        {
            var slots = status.Inventory.Slots;
            for (int i = 0; i < slots.Count; i++)
                if (!slots[i].IsEmpty && slots[i].box.HasEffect<FrozenEffect>()) return true;
            return false;
        }

        /// <summary>Puts a slab of ice at the water surface (the top is flush with the river banks).</summary>
        public IceSlab Place(Vector3 position) => Place(position, true);

        /// <summary>A slab that another player of an online game made: it appears here too (and nobody is told again).</summary>
        public IceSlab PlaceRemote(Vector3 position) => Place(position, false);

        IceSlab Place(Vector3 position, bool own)
        {
            // Melted slabs are recycled, so a long wade does not keep creating and destroying objects.
            IceSlab slab = null;
            while (pool.Count > 0 && slab == null) slab = pool.Pop();
            if (slab == null)
            {
                slab = Instantiate(slabPrefab);
                slab.Released = Recycle;
            }
            slab.Begin(position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            if (frost != null)
            {
                frost.transform.position = position + Vector3.up * 0.1f;
                frost.Emit(7);
            }
            if (own && body != null) slab.IgnoreFor(body, ownerPassSeconds);

            slabs.Add(slab);
            // Too many: the oldest start melting now (they leave the list once they are gone).
            int melting = 0;
            for (int i = 0; i < slabs.Count && slabs.Count - melting > maxSlabs; i++)
            {
                if (slabs[i].IsMelting) { melting++; continue; }
                slabs[i].MeltNow();
                melting++;
            }

            if (own)
            {
                lastSlab = position;
                PlacedCount++;
                // Only the ice of the player on this machine is sent to the others.
                if (TryGetComponent(out PlayerController owner) && owner.isLocal) NetWorld.AnnounceIce(position);
            }
            Placed?.Invoke(position);
            return slab;
        }
    }
}
