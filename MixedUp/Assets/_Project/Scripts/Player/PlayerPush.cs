using System;
using UnityEngine;

namespace MixedUp
{
    /// <summary>Lets a player shove whoever stands in front of them (G, left click or the right shoulder button).</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerPush : MonoBehaviour
    {
        public float range = 2.1f;
        public float force = 9f;
        public float upSpeed = 3.4f;
        public float cooldown = 0.9f;
        [Tooltip("Cosine of the half-angle of the cone in front of the player that can be shoved.")]
        [Range(-1f, 1f)] public float frontCone = 0.2f;

        readonly Collider[] buffer = new Collider[24];
        PlayerController controller;
        float nextPush;
        float punch;

        /// <summary>1 right after a shove, fading to 0; the animator thrusts the hands with it.</summary>
        public float PunchAmount => punch;
        public static event Action<PlayerPush, IPushable> Pushed;
        public static event Action<PlayerPush> Whiffed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Pushed = null; Whiffed = null; }

        void Awake() => controller = GetComponent<PlayerController>();

        void Update()
        {
            punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 4f);
            if (!controller.CanControl || controller.MovementLocked) return;
            if (GameInput.Push.WasPressedThisFrame()) TryPush();
        }

        /// <summary>Shoves the best target in front. Returns it, or null when nobody was close enough.</summary>
        public IPushable TryPush()
        {
            if (Time.time < nextPush) return null;
            nextPush = Time.time + cooldown;
            punch = 1f;

            Vector3 forward = controller.visual != null ? controller.visual.forward : transform.forward;
            forward.y = 0f;
            forward.Normalize();

            IPushable best = null;
            float bestDistance = float.MaxValue;
            int count = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.8f, range, buffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var candidate = buffer[i].GetComponentInParent<IPushable>();
                if (candidate == null || !candidate.CanBePushed || candidate.PushTransform == transform) continue;

                Vector3 offset = candidate.PushTransform.position - transform.position;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (distance > 0.01f && Vector3.Dot(offset / distance, forward) < frontCone) continue;
                if (distance >= bestDistance) continue;

                best = candidate;
                bestDistance = distance;
            }

            if (best == null)
            {
                Whiffed?.Invoke(this);
                return null;
            }

            Vector3 direction = best.PushTransform.position - transform.position;
            direction.y = 0f;
            direction = direction.sqrMagnitude < 0.0001f ? forward : direction.normalized;
            best.ReceivePush(direction * force, upSpeed);
            Pushed?.Invoke(this, best);
            return best;
        }
    }
}
