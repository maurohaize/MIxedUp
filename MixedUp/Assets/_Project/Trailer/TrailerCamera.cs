using System;
using UnityEngine;

namespace MixedUp.Trailer
{
    /// <summary>
    /// The camera operator. A shot gives it a "move" (a function from shot time to a pose); helpers below build the usual
    /// ones: over-the-shoulder follow, dolly, orbit, locked-off. A little hand-held drift keeps it alive like a player camera.
    /// </summary>
    [DefaultExecutionOrder(9000)]
    public class TrailerCamera : MonoBehaviour
    {
        public struct Pose
        {
            public Vector3 position;
            public Vector3 lookAt;
            public float fov;
            public float roll;
        }

        public Func<float, Pose> move;
        [Tooltip("Hand-held drift (metres / degrees).")]
        public float shake = 0.05f;
        public float time;
        public bool smoothFollow = true;
        [Tooltip("Pull the camera in front of walls and rocks that would hide the subject.")]
        public bool avoidObstacles;
        public float smoothing = 6f;

        Camera cam;
        Vector3 smoothedPosition, smoothedLook;
        bool started;

        void Awake() => cam = GetComponent<Camera>();

        public void Cut()
        {
            started = false;
        }

        void LateUpdate()
        {
            if (move == null) return;
            var pose = move(time);

            if (!started || !smoothFollow)
            {
                smoothedPosition = pose.position;
                smoothedLook = pose.lookAt;
                started = true;
            }
            else
            {
                float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
                smoothedPosition = Vector3.Lerp(smoothedPosition, pose.position, k);
                smoothedLook = Vector3.Lerp(smoothedLook, pose.lookAt, k);
            }

            float n1 = Mathf.PerlinNoise(time * 0.35f, 1.7f) - 0.5f;
            float n2 = Mathf.PerlinNoise(4.1f, time * 0.31f) - 0.5f;
            float n3 = Mathf.PerlinNoise(time * 0.27f, 9.3f) - 0.5f;
            Vector3 drift = new Vector3(n1, n2, n3) * (2f * shake);

            var position = smoothedPosition + drift;
            if (avoidObstacles) position = KeepInView(smoothedLook, position);
            transform.position = position;
            var forward = (smoothedLook + drift * 0.5f) - transform.position;
            if (forward.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, pose.roll + n2 * shake * 20f);
            if (cam != null && pose.fov > 1f) cam.fieldOfView = pose.fov;
        }

        static Vector3 KeepInView(Vector3 subject, Vector3 position)
        {
            var toCamera = position - subject;
            float distance = toCamera.magnitude;
            if (distance < 0.5f) return position;
            var direction = toCamera / distance;
            float nearest = distance;
            foreach (var hit in Physics.SphereCastAll(subject, 0.3f, direction, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance <= 0f || hit.collider.GetComponentInParent<TrailerActor>() != null) continue;
                nearest = Mathf.Min(nearest, hit.distance);
            }
            return nearest < distance ? subject + direction * Mathf.Max(1.2f, nearest - 0.1f) : position;
        }

        // ------------------------------------------------------------------ moves

        public static Func<float, Pose> Static(Vector3 position, Vector3 lookAt, float fov = 50f) =>
            t => new Pose { position = position, lookAt = lookAt, fov = fov };

        public static Func<float, Pose> Dolly(Vector3 from, Vector3 to, Vector3 lookFrom, Vector3 lookTo, float seconds, float fov = 50f, bool ease = true) =>
            t =>
            {
                float k = Mathf.Clamp01(t / Mathf.Max(0.01f, seconds));
                if (ease) k = Mathf.SmoothStep(0f, 1f, k);
                return new Pose { position = Vector3.Lerp(from, to, k), lookAt = Vector3.Lerp(lookFrom, lookTo, k), fov = fov };
            };

        /// <summary>Behind and above a character (or the middle of a group): the player's own camera, like the reference trailer.</summary>
        public static Func<float, Pose> Follow(Func<Vector3> target, Func<float> yaw, float distance = 5.2f, float height = 2.3f,
            float lookHeight = 1.2f, float side = 0f, float fov = 52f) =>
            t =>
            {
                var at = target();
                var rot = Quaternion.Euler(0f, yaw(), 0f);
                var position = at + rot * new Vector3(side, 0f, -distance) + Vector3.up * height;
                return new Pose { position = position, lookAt = at + Vector3.up * lookHeight, fov = fov };
            };

        public static Func<float, Pose> Orbit(Func<Vector3> centre, float radius, float height, float startYaw, float degreesPerSecond,
            float lookHeight = 1f, float fov = 50f) =>
            t =>
            {
                float yaw = startYaw + degreesPerSecond * t;
                var c = centre();
                var position = c + Quaternion.Euler(0f, yaw, 0f) * Vector3.back * radius + Vector3.up * height;
                return new Pose { position = position, lookAt = c + Vector3.up * lookHeight, fov = fov };
            };
    }
}
