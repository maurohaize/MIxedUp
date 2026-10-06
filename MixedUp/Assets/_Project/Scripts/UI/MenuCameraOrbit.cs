using UnityEngine;
using UnityEngine.InputSystem;

namespace MixedUp
{
    /// <summary>A slow, dreamy sway of the camera around the main-menu diorama, with a hint of mouse parallax.</summary>
    public class MenuCameraOrbit : MonoBehaviour
    {
        public Transform focus;
        public float distance = 9f;
        public float height = 2.2f;
        [Tooltip("Heading of the camera, degrees. 0 looks towards +Z.")]
        public float yaw = 18f;
        public float swayDegrees = 9f;
        public float swaySpeed = 0.18f;
        [Tooltip("Shifts what the camera looks at to the left of the focus, so the subject sits on the right of the screen.")]
        public float screenShift = 2.6f;
        public float parallax = 0.6f;

        Vector2 smoothed;

        void LateUpdate()
        {
            if (focus == null) return;

            Vector2 mouse = Vector2.zero;
            if (Mouse.current != null)
            {
                var size = new Vector2(Screen.width, Screen.height);
                mouse = (Mouse.current.position.ReadValue() / size - Vector2.one * 0.5f) * 2f;
                mouse = Vector2.ClampMagnitude(mouse, 1f);
            }
            smoothed = Vector2.Lerp(smoothed, mouse, 1f - Mathf.Exp(-3f * Time.unscaledDeltaTime));

            float heading = yaw + Mathf.Sin(Time.unscaledTime * swaySpeed) * swayDegrees;
            var rotation = Quaternion.Euler(0f, heading, 0f);
            Vector3 lookAt = focus.position + rotation * (Vector3.left * screenShift) + Vector3.up * 1.1f;
            Vector3 position = focus.position + rotation * new Vector3(-smoothed.x * parallax, height - smoothed.y * parallax * 0.5f, -distance);
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
        }
    }
}
