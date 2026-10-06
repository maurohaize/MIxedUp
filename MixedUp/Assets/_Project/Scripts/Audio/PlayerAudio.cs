using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// The sounds a character makes: footsteps that depend on what they walk on, jumps and landings, grunts when hurt, a zap
    /// when shocked, a chime when healed. Added automatically to every player.
    /// </summary>
    [RequireComponent(typeof(PlayerStatus))]
    public class PlayerAudio : MonoBehaviour
    {
        [Tooltip("Distance between two footsteps while walking upright.")]
        public float stepLength = 1.35f;

        PlayerStatus status;
        PlayerController controller;
        float stepDistance;
        float nextHurt;
        float nextHeal;
        bool wasCrouching;
        Vector3 lastPosition;

        public SfxId LastStep { get; private set; }

        void Awake()
        {
            status = GetComponent<PlayerStatus>();
            controller = GetComponent<PlayerController>();
        }

        void OnEnable()
        {
            if (status == null) status = GetComponent<PlayerStatus>();
            if (controller == null) controller = GetComponent<PlayerController>();
            status.Damaged += OnDamaged;
            status.Died += OnDied;
            status.Shocked += OnShocked;
            status.Healed += OnHealed;
            status.Inventory.Transferred += OnTransferred;
            lastPosition = transform.position;
            if (controller != null)
            {
                controller.Jumped += OnJumped;
                controller.Landed += OnLanded;
            }
        }

        void OnDisable()
        {
            if (status != null)
            {
                status.Damaged -= OnDamaged;
                status.Died -= OnDied;
                status.Shocked -= OnShocked;
                status.Healed -= OnHealed;
                status.Inventory.Transferred -= OnTransferred;
            }
            if (controller != null)
            {
                controller.Jumped -= OnJumped;
                controller.Landed -= OnLanded;
            }
        }

        void Update()
        {
            if (controller == null || status.IsDead) return;

            // Footsteps: one every few metres actually walked on the ground.
            float walked = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(lastPosition.x, 0f, lastPosition.z));
            lastPosition = transform.position;

            if (controller.IsGrounded && walked < 3f)
            {
                stepDistance += walked;
                float length = stepLength * (controller.IsCrouching ? 1.3f : 1f);
                if (stepDistance >= length)
                {
                    stepDistance = 0f;
                    var id = SurfaceSound();
                    LastStep = id;
                    AudioManager.Play(id, transform.position, controller.IsCrouching ? 0.25f : 0.5f);
                }
            }

            if (controller.IsCrouching != wasCrouching)
            {
                wasCrouching = controller.IsCrouching;
                AudioManager.Play(SfxId.Crouch, transform.position, 0.4f);
            }
        }

        /// <summary>Which footstep sound fits the ground under the feet.</summary>
        public SfxId SurfaceSound()
        {
            var h = status.Hazards;
            if (h.InWater) return SfxId.FootWater;
            if (h.InMud) return SfxId.FootMud;
            if (h.OnIceSheet || h.OnSlippery) return SfxId.FootSnow;

            if (Physics.Raycast(transform.position + Vector3.up * 0.4f, Vector3.down, out var hit, 1.2f, ~0, QueryTriggerInteraction.Ignore))
            {
                string n = hit.collider.name;
                if (n.StartsWith("Step") || n.Contains("Bridge") || n.Contains("Dock") || n.Contains("Ramp") || n.Contains("Raft") || n.Contains("Deck") || n.Contains("Plank"))
                    return n.Contains("IceRamp") ? FootIce() : SfxId.FootWood;
                if (n.Contains("Plateau")) return SfxId.FootSnow;
                if (n.Contains("Platform") || n.Contains("Stone") || n.Contains("Rock")) return SfxId.FootStone;
                if (n.Contains("Road")) return SfxId.FootDirt;
                if (n.Contains("IceSlab")) return SfxId.FootSnow;
            }
            return SfxId.FootGrass;
        }

        static SfxId FootIce() => SfxId.FootSnow;

        void OnJumped() => AudioManager.Play(SfxId.Jump, transform.position, 0.45f);

        void OnLanded(float drop)
        {
            if (drop < 0.35f) return;
            AudioManager.Play(SfxId.Land, transform.position, Mathf.Clamp(0.3f + drop * 0.08f, 0.3f, 0.6f));
        }

        void OnDamaged(float amount, DeathCause cause)
        {
            if (Time.time < nextHurt || cause.Key == DeathCause.ElectricWater.Key) return;
            nextHurt = Time.time + 0.4f;
            AudioManager.Play(SfxId.Hurt, transform.position);
        }

        void OnDied(DeathCause cause) => AudioManager.Play(SfxId.Death, transform.position);
        void OnShocked() => AudioManager.Play(SfxId.Zap, transform.position);

        void OnHealed(float amount)
        {
            if (Time.time < nextHeal) return;
            nextHeal = Time.time + 1.2f;
            AudioManager.Play(SfxId.Heal, transform.position, 0.5f);
        }

        void OnTransferred(bool received) => AudioManager.Play(SfxId.Pass, transform.position, 0.6f);
    }
}
