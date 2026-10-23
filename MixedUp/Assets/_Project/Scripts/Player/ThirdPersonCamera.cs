using UnityEngine;
using UnityEngine.InputSystem;

namespace MixedUp
{
    /// <summary>Orbiting follow camera with simple collision. Follows the local player by default.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 pivotOffset = new Vector3(0f, 1.45f, 0f);
        public float distance = 5.5f;
        public float minPitch = -20f;
        public float maxPitch = 70f;
        public float mouseSensitivity = 0.12f;
        public float gamepadSensitivity = 160f;
        public float collisionRadius = 0.25f;

        float shake;
        PlayerStatus watched;
        PlayerController targetController;
        float yaw;
        float pitch = 22f;
        bool initialised;
        Transform spectated;
        float nextScan;
        readonly System.Collections.Generic.List<Transform> candidates = new System.Collections.Generic.List<Transform>();

        /// <summary>Who the camera is watching while the local player is dead (null when following the own character).</summary>
        public Transform Spectated => spectated;
        public static string SpectatedName { get; private set; }

        /// <summary>The living players the dead one can watch: other real players and the stand-in teammates.</summary>
        void ScanCandidates()
        {
            candidates.Clear();
            foreach (var player in PlayerRegistry.All)
            {
                if (player == null || player == PlayerRegistry.Local) continue;
                var status = player.GetComponent<PlayerStatus>();
                if (status != null && !status.IsDead) candidates.Add(player.transform);
            }
            foreach (var dummy in FindObjectsByType<TeammateDummy>())
            {
                var status = dummy.GetComponent<PlayerStatus>();
                if (status != null && !status.IsDead) candidates.Add(dummy.transform);
            }
            foreach (var avatar in NetAvatar.All)
            {
                if (avatar == null || !avatar.IsSpawned || avatar.IsOwner || avatar.ghost == null) continue;
                if (!avatar.CurrentPose.Has(AvatarPose.Dead)) candidates.Add(avatar.ghost.transform);
            }
        }

        Transform UpdateSpectating(Transform own)
        {
            var ownStatus = own.GetComponent<PlayerStatus>();
            if (ownStatus == null || !ownStatus.IsDead || GameManager.Instance == null || !GameManager.Instance.IsSpectating)
            {
                spectated = null;
                SpectatedName = null;
                return own;
            }

            bool next = !GameManager.InputBlocked && GameInput.Interact.WasPressedThisFrame();
            bool lost = spectated == null || !spectated.gameObject.activeInHierarchy;
            if (lost || next || Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + 0.5f;
                ScanCandidates();
                if (candidates.Count > 0)
                {
                    int current = spectated != null ? candidates.IndexOf(spectated) : -1;
                    if (lost || next || current < 0) spectated = candidates[(current + 1) % candidates.Count];
                }
                else spectated = null;
            }
            SpectatedName = spectated != null ? spectated.name.Replace("Mate_", "") : null;
            return spectated != null ? spectated : own;
        }

        void LateUpdate()
        {
            if (target == null)
            {
                var local = PlayerRegistry.Local;
                if (local == null) return;
                target = local.transform;
            }

            if (watched == null || watched.transform != target)
            {
                if (watched != null) watched.Damaged -= OnDamaged;
                watched = target.GetComponent<PlayerStatus>();
                if (watched != null) watched.Damaged += OnDamaged;
            }

            if (!initialised)
            {
                yaw = target.eulerAngles.y;
                initialised = true;
            }

            if (!GameManager.InputBlocked) ReadLook();
            var follow = UpdateSpectating(target);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            if (targetController == null || targetController.transform != follow) targetController = follow.GetComponent<PlayerController>();
            float crouch = targetController != null ? targetController.CrouchAmount : 0f;
            Vector3 pivot = follow.position + pivotOffset + Vector3.down * (0.45f * crouch);
            Vector3 direction = rotation * Vector3.back;

            float dist = distance;
            if (Physics.SphereCast(pivot, collisionRadius, direction, out var hit, distance, ~0, QueryTriggerInteraction.Ignore))
                dist = Mathf.Max(0.4f, hit.distance - 0.1f);

            transform.SetPositionAndRotation(pivot + direction * dist, rotation);

            // A short rumble when the player gets hurt.
            if (shake > 0.001f)
            {
                transform.position += transform.right * (Random.value - 0.5f) * shake + transform.up * (Random.value - 0.5f) * shake;
                shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 0.9f);
            }
        }

        void OnDamaged(float amount, DeathCause cause) => shake = Mathf.Min(0.35f, shake + 0.06f + amount * 0.004f);

        void OnDestroy()
        {
            if (watched != null) watched.Damaged -= OnDamaged;
        }

        void ReadLook()
        {
            Vector2 look = GameInput.Look.ReadValue<Vector2>();
            bool gamepad = GameInput.Look.activeControl != null && GameInput.Look.activeControl.device is Gamepad;

            float invert = GameSettings.InvertY ? -1f : 1f;
            if (gamepad)
            {
                float speed = gamepadSensitivity * GameSettings.GamepadSensitivity * Time.deltaTime;
                yaw += look.x * speed;
                pitch -= look.y * speed * invert;
            }
            else
            {
                float speed = mouseSensitivity * GameSettings.MouseSensitivity;
                yaw += look.x * speed;
                pitch -= look.y * speed * invert;
            }
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }
}
