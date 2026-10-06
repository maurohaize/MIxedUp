using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// A wooden raft that ferries players back and forth across the river. Its deck is dry land, so it is a safe way over for
    /// someone carrying an electric box. Riders are carried along by moving them with the raft.
    /// </summary>
    public class MovingRaft : MonoBehaviour
    {
        public Vector3 pointA, pointB;
        public float speed = 1.6f;
        [Tooltip("Seconds it waits at each bank so people can hop on and off.")]
        public float pause = 3.5f;
        [Tooltip("Half-size of the deck as seen from above, plus how far above the deck feet still count as riding.")]
        public Vector3 deckHalfExtents = new Vector3(1.5f, 0.5f, 1.5f);

        float timer;
        bool towardsB = true;
        float waiting;

        public bool IsMoving => waiting <= 0f;
        public static event System.Action<MovingRaft> Arrived;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Arrived = null;

        void Start() => transform.position = pointA;

        void Update()
        {
            float dt = Time.deltaTime;
            if (waiting > 0f)
            {
                waiting -= dt;
                return;
            }

            Vector3 target = towardsB ? pointB : pointA;
            Vector3 previous = transform.position;
            transform.position = Vector3.MoveTowards(previous, target, speed * dt);
            Vector3 delta = transform.position - previous;
            CarryRiders(previous, delta);

            if ((transform.position - target).sqrMagnitude < 0.0001f)
            {
                towardsB = !towardsB;
                waiting = pause;
                Arrived?.Invoke(this);
            }
        }

        void CarryRiders(Vector3 previousCentre, Vector3 delta)
        {
            foreach (var player in PlayerRegistry.All)
            {
                if (player == null || !player.IsGrounded) continue;
                Vector3 local = player.transform.position - previousCentre;
                if (Mathf.Abs(local.x) > deckHalfExtents.x || Mathf.Abs(local.z) > deckHalfExtents.z) continue;
                if (local.y < -0.15f || local.y > deckHalfExtents.y) continue;
                player.Carry(delta);
            }
        }
    }
}
