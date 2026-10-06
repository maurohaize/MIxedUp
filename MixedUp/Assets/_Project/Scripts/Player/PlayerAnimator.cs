using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Procedural, exaggerated animation for the cartoon character (floating hands, boots, no limbs):
    /// hands swing while walking, hug the carried boxes, shake with something hot, boots step,
    /// a drunken sway when toxic, and a death pose.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        public PlayerController controller;
        public PlayerStatus status;
        public CarriedBoxesView carry;
        public Transform body, handLeft, handRight, bootLeft, bootRight;
        public float handSwing = 0.2f;
        public float bootSwingDegrees = 38f;
        public float swingSpeed = 11f;

        Vector3 handLeftRest, handRightRest, bootLeftRest, bootRightRest;
        bool captured;
        float phase;
        float deathBlend;
        float carryBlend;
        bool wasAirborne;
        float squash;
        float airTime;
        float wasAirTimeBonus;

        void Capture()
        {
            if (captured) return;
            captured = true;
            if (handLeft != null) handLeftRest = handLeft.localPosition;
            if (handRight != null) handRightRest = handRight.localPosition;
            if (bootLeft != null) bootLeftRest = bootLeft.localPosition;
            if (bootRight != null) bootRightRest = bootRight.localPosition;
        }

        void Update()
        {
            if (status == null || body == null) return;
            Capture();

            float dt = Time.deltaTime;
            float speed = controller != null ? controller.HorizontalVelocity.magnitude : 0f;
            bool airborne = controller != null && !controller.IsGrounded;
            float move01 = Mathf.Clamp01(speed / 4.5f);

            phase += dt * swingSpeed * Mathf.Lerp(0.6f, 1.5f, Mathf.Clamp01(speed / 7.5f));
            float swing = Mathf.Sin(phase) * move01;

            bool carrying = carry != null && carry.VisibleCount > 0;
            carryBlend = Mathf.MoveTowards(carryBlend, carrying ? 1f : 0f, dt * 7f);

            float shake = HasEffect<HeatEffect>() ? Mathf.Sin(Time.time * 45f) * 0.03f : 0f;
            Vector3 shakeOffset = new Vector3(shake, Mathf.Abs(shake) * 0.5f, 0f);

            // Boots: a little step and hop, no legs needed.
            Place(bootLeft, bootLeftRest + Vector3.up * Mathf.Max(0f, swing) * 0.07f, swing * bootSwingDegrees);
            Place(bootRight, bootRightRest + Vector3.up * Mathf.Max(0f, -swing) * 0.07f, -swing * bootSwingDegrees);

            // Hands: swing at the sides, fly up when airborne, or hold the boxes.
            float airLift = airborne ? 0.32f : 0f;
            Vector3 freeLeft = handLeftRest + new Vector3(0f, airLift, -swing * handSwing) + shakeOffset;
            Vector3 freeRight = handRightRest + new Vector3(0f, airLift, swing * handSwing) - shakeOffset;
            if (carry != null)
            {
                carry.AnimationOffset = shakeOffset;
                Place(handLeft, Vector3.Lerp(freeLeft, carry.HandPosition(-1), carryBlend), 0f);
                Place(handRight, Vector3.Lerp(freeRight, carry.HandPosition(1), carryBlend), 0f);
            }
            else
            {
                Place(handLeft, freeLeft, 0f);
                Place(handRight, freeRight, 0f);
            }

            float sway = HasEffect<ToxicEffect>() ? Mathf.Sin(Time.time * 2.2f) * 9f : 0f;
            float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.05f * move01;
            body.localPosition = new Vector3(0f, bob, 0f);

            // Squash on landing after a real jump or fall, a little stretch while rising.
            airTime = airborne ? airTime + dt : 0f;
            if (wasAirborne && !airborne && !status.IsDead) squash = Mathf.Clamp01(0.45f + wasAirTimeBonus);
            wasAirborne = airborne;
            wasAirTimeBonus = Mathf.Min(0.55f, airTime * 0.4f);
            squash = Mathf.MoveTowards(squash, 0f, dt * 3.5f);
            float bounce = Mathf.Sin(squash * Mathf.PI * 0.5f);
            if (!status.IsDead)
                body.localScale = new Vector3(1f + 0.16f * bounce, 1f - 0.2f * bounce + (airborne ? 0.04f : 0f), 1f + 0.16f * bounce);

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

        static void Place(Transform t, Vector3 localPosition, float xDegrees)
        {
            if (t == null) return;
            t.localPosition = localPosition;
            t.localRotation = Quaternion.Euler(xDegrees, 0f, 0f);
        }
    }
}
