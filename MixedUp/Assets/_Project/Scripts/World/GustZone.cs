using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// A patch of ground swept by gusts of wind: a short rustle gives warning, then everyone inside is pushed along for a
    /// couple of seconds. Easy to dodge, nasty if you are jumping across a gap.
    /// </summary>
    public class GustZone : MonoBehaviour
    {
        public Vector3 direction = Vector3.right;
        public Vector3 halfExtents = new Vector3(6f, 3f, 5f);
        public float strength = 16f;
        public float gustSeconds = 2.2f;
        public float warningSeconds = 1.4f;
        public float calmSeconds = 5f;
        [Tooltip("Streaks of wind drawn while it blows (optional).")]
        public ParticleSystem wind;

        float timer;

        public enum Phase { Calm, Warning, Blowing }
        public Phase Current { get; private set; } = Phase.Calm;
        public static event System.Action<GustZone, Phase> PhaseChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => PhaseChanged = null;

        void Update()
        {
            timer += Time.deltaTime;
            switch (Current)
            {
                case Phase.Calm when timer >= calmSeconds: Set(Phase.Warning); break;
                case Phase.Warning when timer >= warningSeconds: Set(Phase.Blowing); break;
                case Phase.Blowing when timer >= gustSeconds: Set(Phase.Calm); break;
            }
            if (Current != Phase.Blowing) return;

            // Pushed along directly (a quarter of the strength in metres per second), as a force would be braked away by the legs.
            Vector3 push = direction.normalized * (strength * 0.25f * Time.deltaTime);
            foreach (var player in PlayerRegistry.All)
            {
                if (player == null || player.Status.IsDead) continue;
                Vector3 local = transform.InverseTransformPoint(player.transform.position);
                if (Mathf.Abs(local.x) > halfExtents.x || Mathf.Abs(local.y) > halfExtents.y || Mathf.Abs(local.z) > halfExtents.z) continue;
                player.Carry(push);
            }
        }

        void Set(Phase phase)
        {
            Current = phase;
            timer = 0f;
            if (wind != null)
            {
                var emission = wind.emission;
                emission.enabled = phase != Phase.Calm;
                emission.rateOverTime = phase == Phase.Blowing ? 40f : 8f;
            }
            PhaseChanged?.Invoke(this, phase);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.4f);
            Gizmos.DrawCube(Vector3.zero, halfExtents * 2f);
        }
    }
}
