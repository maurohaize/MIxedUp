using UnityEngine;
using UnityEngine.InputSystem;

namespace MixedUp
{
    /// <summary>Orbiting follow camera with simple collision. Follows the local player by default.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
        public float distance = 5.5f;
        public float minPitch = -20f;
        public float maxPitch = 70f;
        public float mouseSensitivity = 0.12f;
        public float gamepadSensitivity = 160f;
        public float collisionRadius = 0.25f;

        float yaw;
        float pitch = 22f;
        bool initialised;

        void LateUpdate()
        {
            if (target == null)
            {
                var local = PlayerRegistry.Local;
                if (local == null) return;
                target = local.transform;
            }

            if (!initialised)
            {
                yaw = target.eulerAngles.y;
                initialised = true;
            }

            if (!GameManager.InputBlocked) ReadLook();

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + pivotOffset;
            Vector3 direction = rotation * Vector3.back;

            float dist = distance;
            if (Physics.SphereCast(pivot, collisionRadius, direction, out var hit, distance, ~0, QueryTriggerInteraction.Ignore))
                dist = Mathf.Max(0.4f, hit.distance - 0.1f);

            transform.SetPositionAndRotation(pivot + direction * dist, rotation);
        }

        void ReadLook()
        {
            Vector2 look = GameInput.Look.ReadValue<Vector2>();
            bool gamepad = GameInput.Look.activeControl != null && GameInput.Look.activeControl.device is Gamepad;

            if (gamepad)
            {
                yaw += look.x * gamepadSensitivity * Time.deltaTime;
                pitch -= look.y * gamepadSensitivity * Time.deltaTime;
            }
            else
            {
                yaw += look.x * mouseSensitivity;
                pitch -= look.y * mouseSensitivity;
            }
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }
}
