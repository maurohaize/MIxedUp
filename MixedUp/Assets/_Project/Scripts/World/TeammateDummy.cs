using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Stand-in teammate for phase 1: it cannot move on its own, but it receives boxes (and gives them back),
    /// suffers their effects, reports its death, and can be shoved or hugged like a real player.
    /// Replaced by real remote players in phase 3.
    /// </summary>
    [RequireComponent(typeof(PlayerStatus))]
    public class TeammateDummy : MonoBehaviour, IPushable
    {
        public float friction = 5f;
        public float gravity = 25f;

        PlayerStatus status;
        Vector3 velocity;
        float verticalVelocity;
        bool airborne;

        public Transform PushTransform => transform;
        public bool CanBePushed => status != null && !status.IsDead;

        void Awake() => status = GetComponent<PlayerStatus>();
        void OnEnable() { if (status == null) status = GetComponent<PlayerStatus>(); status.Died += OnDied; }
        void OnDisable() { if (status != null) status.Died -= OnDied; }

        void OnDied(DeathCause cause) => GameEvents.RaiseToast("toast.teammate_died", cause.Localized);

        public void ReceivePush(Vector3 horizontalImpulse, float upSpeed)
        {
            horizontalImpulse.y = 0f;
            velocity += horizontalImpulse;
            if (upSpeed > 0f)
            {
                verticalVelocity = Mathf.Max(verticalVelocity, upSpeed);
                airborne = true;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Slide along the ground, stopped by whatever is in the way.
            if (velocity.sqrMagnitude > 0.0004f)
            {
                Vector3 step = velocity * dt;
                Vector3 origin = transform.position + Vector3.up * 0.8f;
                if (Physics.SphereCast(origin, 0.34f, step.normalized, out var hit, step.magnitude + 0.05f, ~0, QueryTriggerInteraction.Ignore)
                    && !hit.collider.transform.IsChildOf(transform))
                    velocity = Vector3.zero;
                else
                    transform.position += step;
                velocity = Vector3.MoveTowards(velocity, Vector3.zero, friction * dt * Mathf.Max(1f, velocity.magnitude * 0.5f));
            }

            // Fall back down after a hop.
            float groundY = GroundHeight();
            if (airborne)
            {
                verticalVelocity -= gravity * dt;
                var p = transform.position;
                p.y += verticalVelocity * dt;
                if (p.y <= groundY && verticalVelocity <= 0f)
                {
                    p.y = groundY;
                    airborne = false;
                    verticalVelocity = 0f;
                }
                transform.position = p;
            }
            else if (velocity.sqrMagnitude > 0.0004f)
            {
                var p = transform.position;
                if (p.y > groundY + 0.05f) airborne = true;
                else p.y = groundY;
                transform.position = p;
            }
        }

        float GroundHeight()
        {
            Vector3 origin = transform.position + Vector3.up * 1.2f;
            var hits = Physics.RaycastAll(origin, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity;
            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(transform)) continue;
                best = Mathf.Max(best, h.point.y);
            }
            return float.IsNegativeInfinity(best) ? transform.position.y - 0.05f : best;
        }
    }
}
