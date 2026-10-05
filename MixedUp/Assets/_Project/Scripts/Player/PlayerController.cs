using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Third-person character movement on a CharacterController: walk, run, jump, falls,
    /// water, and the slippery movement caused by FROZEN boxes and icy ground.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(PlayerStatus))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("Only the local player reads input. Remote players are driven by the network (phase 3).")]
        public bool isLocal = true;

        [Header("Speed")]
        public float walkSpeed = 4.5f;
        public float runSpeed = 7.5f;
        [Range(0.2f, 1f)] public float waterSpeedMultiplier = 0.6f;

        [Header("Acceleration")]
        public float groundAcceleration = 50f;
        public float groundBraking = 60f;
        public float airAcceleration = 14f;
        [Tooltip("Acceleration multiplier on slippery ground (ice, wet planks).")]
        [Range(0.05f, 1f)] public float slipperyAccelerationMultiplier = 0.35f;
        [Tooltip("Braking multiplier on slippery ground. Very low so ice keeps you sliding.")]
        [Range(0.01f, 1f)] public float slipperyBrakingMultiplier = 0.05f;

        [Header("Jump")]
        public float jumpHeight = 1.3f;
        public float gravity = 25f;
        public float coyoteTime = 0.12f;
        public float jumpBuffer = 0.12f;

        [Header("Slopes")]
        [Tooltip("Slope angle (degrees) from which sliding effects start to apply.")]
        public float slideStartAngle = 12f;
        [Tooltip("Slide strength on slippery ground for players without a frozen box.")]
        public float slipperySlideStrength = 0.9f;

        [Header("Falling")]
        public float safeFallHeight = 4f;
        public float fallDamagePerMeter = 15f;
        public float killHeight = -30f;

        [Header("Visual")]
        public Transform visual;
        public float turnSpeed = 14f;

        CharacterController controller;
        PlayerStatus status;
        Camera cam;

        Vector3 horizontalVelocity;
        float verticalVelocity;
        bool grounded;
        bool wasGrounded = true;
        float lastGroundedTime;
        float jumpBufferTimer;
        float peakY;
        Vector3 groundNormal = Vector3.up;

        public bool IsGrounded => grounded;
        public Vector3 HorizontalVelocity => horizontalVelocity;
        public PlayerStatus Status => status;
        public bool CanControl => isLocal && status != null && !status.IsDead && !GameManager.InputBlocked;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            status = GetComponent<PlayerStatus>();
            peakY = transform.position.y;
        }

        void OnEnable() => PlayerRegistry.Register(this);
        void OnDisable() => PlayerRegistry.Unregister(this);

        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            peakY = position.y;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            bool control = CanControl;
            Vector2 input = control ? GameInput.Move.ReadValue<Vector2>() : Vector2.zero;
            bool sprint = control && GameInput.Sprint.IsPressed();
            if (control && GameInput.Jump.WasPressedThisFrame()) jumpBufferTimer = jumpBuffer;

            Vector3 moveDir = CameraRelative(input);
            bool hasInput = moveDir.sqrMagnitude > 0.0001f;

            ProbeGround(out bool probeHit);
            grounded = controller.isGrounded || (probeHit && verticalVelocity <= 0.01f);

            if (grounded)
            {
                lastGroundedTime = Time.time;
                if (!wasGrounded) OnLanded();
                peakY = transform.position.y;
            }
            else
            {
                peakY = Mathf.Max(peakY, transform.position.y);
            }
            wasGrounded = grounded;

            var mods = status.Modifiers;
            var hazards = status.Hazards;
            bool slippery = hazards.OnSlippery;

            float speed = (sprint ? runSpeed : walkSpeed) * mods.SpeedMul;
            if (hazards.InWater) speed *= waterSpeedMultiplier;
            Vector3 target = moveDir * speed;

            float accel;
            if (!grounded)
            {
                accel = airAcceleration;
            }
            else
            {
                accel = hasInput ? groundAcceleration * mods.AccelerationMul : groundBraking * mods.BrakingMul;
                if (slippery) accel *= hasInput ? slipperyAccelerationMultiplier : slipperyBrakingMultiplier;
            }
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, accel * dt);

            ApplySlopeSlide(mods, slippery, dt);

            jumpBufferTimer -= dt;
            bool wantsJump = jumpBufferTimer > 0f && (grounded || Time.time - lastGroundedTime <= coyoteTime);
            if (wantsJump && verticalVelocity <= 0.01f)
            {
                verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
                jumpBufferTimer = 0f;
                lastGroundedTime = -999f;
                grounded = false;
            }
            else if (grounded && verticalVelocity <= 0f)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity -= gravity * dt;
            }

            Vector3 velocity = horizontalVelocity;
            if (grounded && verticalVelocity <= 0f && groundNormal.y > 0.5f)
            {
                Vector3 alongSlope = Vector3.ProjectOnPlane(horizontalVelocity, groundNormal);
                if (alongSlope.y < 0f && alongSlope.sqrMagnitude > 1e-6f)
                    velocity = alongSlope.normalized * horizontalVelocity.magnitude;
            }
            velocity.y += verticalVelocity;

            var flags = controller.Move(velocity * dt);
            if ((flags & CollisionFlags.Sides) != 0)
            {
                Vector3 actual = controller.velocity;
                actual.y = 0f;
                horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, actual.magnitude);
            }

            RotateVisual(hasInput ? moveDir : horizontalVelocity, dt);

            if (transform.position.y < killHeight) status.Kill(DeathCause.Void);
        }

        Vector3 CameraRelative(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f) return Vector3.zero;
            if (cam == null) cam = Camera.main;

            Quaternion yaw = cam != null ? Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f) : Quaternion.identity;
            return Vector3.ClampMagnitude(yaw * new Vector3(input.x, 0f, input.y), 1f);
        }

        void ProbeGround(out bool hit)
        {
            Vector3 origin = transform.position + Vector3.up * (controller.radius + 0.05f);
            hit = Physics.SphereCast(origin, controller.radius * 0.9f, Vector3.down, out var info, 0.35f, ~0, QueryTriggerInteraction.Ignore);
            groundNormal = hit ? info.normal : Vector3.up;
        }

        void ApplySlopeSlide(MovementModifiers mods, bool slippery, float dt)
        {
            if (!grounded) return;

            float strength = Mathf.Max(mods.SlopeSlide, slippery ? slipperySlideStrength : 0f);
            if (strength <= 0f) return;

            float angle = Vector3.Angle(groundNormal, Vector3.up);
            if (angle < slideStartAngle) return;

            Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, groundNormal);
            downhill.y = 0f;
            if (downhill.sqrMagnitude < 1e-6f) return;

            horizontalVelocity += downhill.normalized * (gravity * Mathf.Sin(angle * Mathf.Deg2Rad) * strength * dt);
        }

        void OnLanded()
        {
            float drop = peakY - transform.position.y;
            if (drop > safeFallHeight && !status.Hazards.InWater)
                status.Damage((drop - safeFallHeight) * fallDamagePerMeter, DeathCause.Fall);
        }

        void RotateVisual(Vector3 direction, float dt)
        {
            direction.y = 0f;
            if (visual == null || direction.sqrMagnitude < 0.01f) return;

            var targetRotation = Quaternion.LookRotation(direction);
            visual.rotation = Quaternion.Slerp(visual.rotation, targetRotation, 1f - Mathf.Exp(-turnSpeed * dt));
        }
    }
}
