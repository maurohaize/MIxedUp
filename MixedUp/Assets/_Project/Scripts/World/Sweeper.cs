using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// A heavy log that spins around a post. Anyone it touches is knocked away and hurt; a short cooldown per player
    /// stops it from hitting every frame. The hit test is plain geometry against the registered players, so it works
    /// the same for every player in multiplayer.
    /// </summary>
    public class Sweeper : MonoBehaviour
    {
        [Tooltip("The spinning part (log and, optionally, decorations).")]
        public Transform arm;
        public float degreesPerSecond = 75f;
        [Tooltip("Half-size of the log in its own space: x = half length, y = half height, z = half thickness.")]
        public Vector3 halfExtents = new Vector3(3.2f, 0.45f, 0.3f);
        [Tooltip("Height of the log's centre above the post's origin.")]
        public float height = 0.55f;
        public float damage = 14f;
        public float knockback = 11f;
        public float knockUp = 4.5f;
        public float cooldown = 1.2f;

        public float AngleDegrees { get; private set; }

        readonly string cooldownKey = "sweeper-" + System.Guid.NewGuid();

        void Update()
        {
            float step = degreesPerSecond * Time.deltaTime;
            AngleDegrees = Mathf.Repeat(AngleDegrees + step, 360f);
            if (arm != null) arm.localRotation = Quaternion.Euler(0f, AngleDegrees, 0f);

            foreach (var player in PlayerRegistry.All)
            {
                if (player == null || player.Status.IsDead) continue;
                if (!Touches(player.transform.position + Vector3.up * 0.8f, 0.38f)) continue;
                if (!player.Status.TryUseCooldown(cooldownKey, cooldown)) continue;

                Vector3 push = player.transform.position - transform.position;
                push.y = 0f;
                if (push.sqrMagnitude < 0.01f) push = transform.forward;

                // Throw them off the side the log came from, not along its length.
                Vector3 sideways = arm != null ? arm.right * Mathf.Sign(Vector3.Dot(arm.forward, push) + 0.0001f) : push;
                Vector3 direction = (push.normalized * 0.6f + sideways * 0.4f).normalized;
                player.AddImpulse(direction * knockback, knockUp);
                player.Status.Damage(damage, DeathCause.Sweeper);
            }
        }

        /// <summary>Is a sphere (a player's body) inside the log's box?</summary>
        public bool Touches(Vector3 worldPoint, float radius)
        {
            if (arm == null) return false;
            Vector3 local = arm.InverseTransformPoint(worldPoint) - new Vector3(0f, height, 0f);
            Vector3 limit = halfExtents + Vector3.one * radius;
            return Mathf.Abs(local.x) <= limit.x && Mathf.Abs(local.y) <= limit.y + 0.6f && Mathf.Abs(local.z) <= limit.z;
        }
    }
}
