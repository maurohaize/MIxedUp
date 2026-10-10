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
            if (NetWorld.SharedClock)
            {
                FollowSharedClock();
                return;
            }

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

        /// <summary>
        /// Online: the raft is always where the cycle says it is at this moment of the shared clock
        /// (A to B, wait, B to A, wait), so every player sees it at the same place.
        /// </summary>
        void FollowSharedClock()
        {
            float travel = speed > 0f ? Vector3.Distance(pointA, pointB) / speed : 0f;
            float period = 2f * (travel + pause);
            if (period <= 0f) return;

            float t = (float)(LevelClock.Seconds % period);
            Vector3 target;
            bool moving;
            if (t < travel) { target = Vector3.Lerp(pointA, pointB, travel > 0f ? t / travel : 1f); moving = true; towardsB = true; }
            else if (t < travel + pause) { target = pointB; moving = false; }
            else if (t < 2f * travel + pause) { target = Vector3.Lerp(pointB, pointA, (t - travel - pause) / travel); moving = true; towardsB = false; }
            else { target = pointA; moving = false; }

            Vector3 previous = transform.position;
            transform.position = target;
            waiting = moving ? 0f : 1f;
            if (moving) CarryRiders(previous, target - previous);
            else if (sharedWasMoving) Arrived?.Invoke(this);
            sharedWasMoving = moving;
        }

        bool sharedWasMoving;

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
