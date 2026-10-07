using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Third-person character movement on a CharacterController: walk, run, jump, falls,
    /// water, and the slippery movement caused by FROZEN boxes and icy ground.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(PlayerStatus))]
    public class PlayerController : MonoBehaviour, IPushable
    {
        [Header("Crouch")]
        [Tooltip("Walking speed while crouched, compared with walking upright.")]
        [Range(0.2f, 1f)] public float crouchSpeedMultiplier = 0.45f;
        public float crouchHeight = 1.0f;

        [Header("Identity")]
        [Tooltip("Only the local player reads input. Remote players are driven by the network (phase 3).")]
        public bool isLocal = true;

        [Header("Speed")]
        public float walkSpeed = 4.5f;
        public float runSpeed = 7.5f;
        [Range(0.2f, 1f)] public float waterSpeedMultiplier = 0.6f;
        [Tooltip("Walking speed while wading through mud.")]
        [Range(0.2f, 1f)] public float mudSpeedMultiplier = 0.45f;
        [Tooltip("Jump height while stuck in mud.")]
        [Range(0.1f, 1f)] public float mudJumpMultiplier = 0.45f;

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
        [Tooltip("Seconds of fall immunity kept after landing from a bounce pad.")]
        public float bounceImmunityGrace = 1.2f;

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
        float standingHeight = 1.6f;
        float crouchAmount;
        bool crouching;
        float lockedUntil;
        float fallImmuneUntil;
        readonly Collider[] headroom = new Collider[8];

        public event System.Action Jumped;
        /// <summary>Raised on landing, with the height dropped (0 for a tiny hop).</summary>
        public event System.Action<float> Landed;

        public bool IsGrounded => grounded;
        public Vector3 HorizontalVelocity => horizontalVelocity;
        public PlayerStatus Status => status;
        public bool IsCrouching => crouching;
        /// <summary>0 = upright, 1 = fully crouched (smoothed).</summary>
        public float CrouchAmount => crouchAmount;
        public bool FallImmune => Time.time < fallImmuneUntil;
        public bool MovementLocked => Time.time < lockedUntil;
        public Transform PushTransform => transform;
        public bool CanBePushed => status != null && !status.IsDead;
        public bool CanControl => isLocal && status != null && !status.IsDead && !GameManager.InputBlocked;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            // The default (1 mm) swallows tiny per-frame moves, so on a very fast frame rate slow movement and slides
            // would freeze: speed * deltaTime must not fall under it.
            controller.minMoveDistance = 0f;
            standingHeight = controller.height;
            status = GetComponent<PlayerStatus>();
            peakY = transform.position.y;
        }

        void OnEnable() => PlayerRegistry.Register(this);
        void OnDisable() => PlayerRegistry.Unregister(this);

        /// <summary>
        /// A sudden push: a bounce pad, or being knocked back by an obstacle. `horizontal` is added to the current
        /// velocity; `upSpeed` (if positive) launches the player at least that fast upwards.
        /// </summary>
        public void AddImpulse(Vector3 horizontal, float upSpeed = 0f)
        {
            horizontal.y = 0f;
            horizontalVelocity += horizontal;
            if (upSpeed > 0f)
            {
                verticalVelocity = Mathf.Max(verticalVelocity, upSpeed);
                lastGroundedTime = -999f;
                grounded = false;
            }
        }

        /// <summary>Moves the player along with something they stand on (a raft).</summary>
        public void Carry(Vector3 delta)
        {
            if (controller != null && controller.enabled) controller.Move(delta);
        }

        /// <summary>No fall damage until the player lands, plus a short grace afterwards (a bounce pad launch).</summary>
        public void GrantBounceImmunity() => fallImmuneUntil = float.PositiveInfinity;

        public void ReceivePush(Vector3 horizontalImpulse, float upSpeed) => AddImpulse(horizontalImpulse, upSpeed);

        /// <summary>Holds the player still (a hug, for example): no walking, jumping or crouching for a while.</summary>
        public void LockMovement(float seconds) => lockedUntil = Mathf.Max(lockedUntil, Time.time + seconds);

        bool HasHeadroom()
        {
            float radius = controller.radius * 0.95f;
            Vector3 bottom = transform.position + Vector3.up * (radius + 0.05f);
            Vector3 top = transform.position + Vector3.up * (standingHeight - radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, headroom, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!headroom[i].transform.IsChildOf(transform)) return false;
            return true;
        }

        void UpdateCrouch(bool wantsCrouch, float dt)
        {
            if (wantsCrouch && grounded) crouching = true;
            else if (!wantsCrouch && crouching && HasHeadroom()) crouching = false;

            crouchAmount = Mathf.MoveTowards(crouchAmount, crouching ? 1f : 0f, dt * 8f);
            float height = Mathf.Lerp(standingHeight, crouchHeight, crouchAmount);
            controller.height = height;
            controller.center = new Vector3(0f, height * 0.5f, 0f);
        }

        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            peakY = position.y;
            fallImmuneUntil = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            bool control = CanControl && !MovementLocked;
            Vector2 input = control ? GameInput.Move.ReadValue<Vector2>() : Vector2.zero;
            bool sprint = control && GameInput.Sprint.IsPressed();
            if (control && GameInput.Jump.WasPressedThisFrame() && !crouching) jumpBufferTimer = jumpBuffer;

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

            UpdateCrouch(control && GameInput.Crouch.IsPressed(), dt);
            if (crouching) sprint = false;

            var mods = status.Modifiers;
            var hazards = status.Hazards;
            bool slippery = hazards.OnSlippery;

            float speed = (sprint ? runSpeed : walkSpeed) * mods.SpeedMul;
            if (hazards.InWater) speed *= waterSpeedMultiplier;
            if (hazards.InMud) speed *= mudSpeedMultiplier;
            if (crouching) speed *= crouchSpeedMultiplier;
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
                verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight * (hazards.InMud ? mudJumpMultiplier : 1f));
                jumpBufferTimer = 0f;
                Jumped?.Invoke();
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
            Landed?.Invoke(Mathf.Max(0f, drop));
            bool immune = FallImmune;
            if (float.IsPositiveInfinity(fallImmuneUntil)) fallImmuneUntil = Time.time + bounceImmunityGrace;
            if (!immune && drop > safeFallHeight && !status.Hazards.InWater)
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
