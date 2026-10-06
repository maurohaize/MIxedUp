using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// A springy mushroom: step on its cap and it launches you into the air. The cap squashes and wobbles back.
    /// Triggered by plain distance checks against the registered players, like the other world hazards.
    /// </summary>
    public class BouncePad : MonoBehaviour
    {
        [Tooltip("Upward speed given to the player, metres per second.")]
        public float launchSpeed = 15.5f;
        [Tooltip("Horizontal radius of the cap that triggers the bounce.")]
        public float radius = 0.95f;
        [Tooltip("Height of the cap's top above the pad's origin.")]
        public float capHeight = 0.7f;
        public float cooldown = 0.5f;
        [Tooltip("The part that squashes. Falls back to this transform.")]
        public Transform squashed;

        public static event System.Action<BouncePad> Bounced;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Bounced = null;

        float lastBounce = -10f;
        float squash;
        Vector3 baseScale = Vector3.one;

        void Awake()
        {
            if (squashed == null) squashed = transform;
            baseScale = squashed.localScale;
        }

        void Update()
        {
            foreach (var player in PlayerRegistry.All)
            {
                if (player == null || player.Status.IsDead) continue;
                if (Time.time - lastBounce < cooldown) continue;

                Vector3 offset = player.transform.position - transform.position;
                var flat = new Vector2(offset.x, offset.z);
                bool onCap = flat.magnitude <= radius && offset.y >= -0.3f && offset.y <= capHeight + 0.35f;
                if (!onCap) continue;

                lastBounce = Time.time;
                squash = 1f;
                player.AddImpulse(Vector3.zero, launchSpeed);
                Bounced?.Invoke(this);
            }

            // A damped wobble: squashed flat first, then stretched, then settling.
            squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 2.4f);
            float wobble = Mathf.Sin((1f - squash) * 14f) * squash;
            squashed.localScale = new Vector3(baseScale.x * (1f + 0.25f * wobble), baseScale.y * (1f - 0.35f * wobble), baseScale.z * (1f + 0.25f * wobble));
        }
    }
}
