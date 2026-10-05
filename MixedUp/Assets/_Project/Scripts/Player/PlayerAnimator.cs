using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Procedural, exaggerated animation for the primitive character: limb swing, jump pose,
    /// shaking hands when carrying something hot, a drunken sway when toxic, and a death pose.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        public PlayerController controller;
        public PlayerStatus status;
        public Transform armLeft, armRight, legLeft, legRight, body;
        public float swingDegrees = 50f;
        public float swingSpeed = 11f;

        float phase;
        float deathBlend;

        void Update()
        {
            if (status == null || body == null) return;

            float dt = Time.deltaTime;
            float speed = controller != null ? controller.HorizontalVelocity.magnitude : 0f;
            bool airborne = controller != null && !controller.IsGrounded;

            phase += dt * swingSpeed * Mathf.Lerp(0.6f, 1.5f, Mathf.Clamp01(speed / 7.5f));
            float amplitude = Mathf.Clamp01(speed / 4.5f) * swingDegrees;
            float swing = Mathf.Sin(phase) * amplitude;

            float armLift = airborne ? -150f : 0f;
            float shake = HasEffect<HeatEffect>() ? Mathf.Sin(Time.time * 45f) * 18f : 0f;

            Set(legLeft, swing, 0f);
            Set(legRight, -swing, 0f);
            Set(armLeft, -swing + armLift + shake, 6f);
            Set(armRight, swing + armLift - shake, -6f);

            float sway = HasEffect<ToxicEffect>() ? Mathf.Sin(Time.time * 2.2f) * 9f : 0f;
            float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.05f * Mathf.Clamp01(speed / 4.5f);
            body.localPosition = new Vector3(0f, bob, 0f);

            deathBlend = Mathf.MoveTowards(deathBlend, status.IsDead ? 1f : 0f, dt * 3f);
            body.localRotation = Quaternion.Euler(0f, 0f, sway) * Quaternion.Euler(-90f * deathBlend, 0f, 0f);
        }

        bool HasEffect<T>() where T : BoxEffect
        {
            var slots = status.Inventory.Slots;
            for (int i = 0; i < slots.Count; i++)
                if (!slots[i].IsEmpty && slots[i].box.HasEffect<T>()) return true;
            return false;
        }

        static void Set(Transform t, float xDegrees, float zDegrees)
        {
            if (t != null) t.localRotation = Quaternion.Euler(xDegrees, 0f, zDegrees);
        }
    }
}
