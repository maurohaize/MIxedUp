using System;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// A hug (H or the left shoulder button): both players hold still for a couple of seconds, hearts float up, and both heal.
    /// Health no longer comes back quickly by itself, so a hug is the way to patch up a teammate.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(PlayerStatus))]
    public class PlayerHug : MonoBehaviour
    {
        public float range = 1.9f;
        public float duration = 2.4f;
        [Tooltip("Health per second restored to each of the two huggers.")]
        public float healPerSecond = 12f;
        public float cooldown = 4f;
        [Tooltip("Particle material (a heart sprite) for the hearts; leave empty for no hearts.")]
        public Material heartMaterial;

        readonly Collider[] buffer = new Collider[24];
        PlayerController controller;
        PlayerStatus status;
        PlayerStatus partner;
        ParticleSystem hearts;
        float until;
        float nextHug;
        float nextHeart;

        public bool IsHugging { get; private set; }
        public PlayerStatus Partner => partner;
        /// <summary>Total health given out by this hug so far (tests, achievements).</summary>
        public float HealedTotal { get; private set; }
        public static event Action<PlayerHug, PlayerStatus> Started;
        public static event Action<PlayerHug> Ended;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Started = null; Ended = null; }

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            status = GetComponent<PlayerStatus>();
        }

        void Update()
        {
            if (IsHugging)
            {
                HugAvailable = false;
                TickHug(Time.deltaTime);
                return;
            }
            UpdateAvailability(Time.deltaTime);
            if (!controller.CanControl || controller.MovementLocked || Time.time < nextHug) return;
            if (GameInput.Hug.WasPressedThisFrame()) TryHug();
        }

        /// <summary>True while somebody is close enough to be hugged right now (the prompt on screen follows it).</summary>
        public bool HugAvailable { get; private set; }
        /// <summary>The local player is hurt, so a hug would heal them.</summary>
        public bool WantsHealing => !status.IsDead && status.Health01 < 0.95f;

        float scanTimer;
        float nextHint;

        void UpdateAvailability(float dt)
        {
            scanTimer -= dt;
            if (scanTimer > 0f) return;
            scanTimer = 0.2f;

            HugAvailable = controller.CanControl && !controller.MovementLocked && Time.time >= nextHug && FindPartner() != null;

            // A reminder, now and then, that a hug is how to heal when there are teammates (health no longer comes back by itself).
            if (controller.isLocal && !status.IsDead && PlayerStatus.HugsRequired && status.Health01 < 0.6f
                && Time.time >= nextHint && GameManager.AnotherPlayerAlive())
            {
                nextHint = Time.time + 45f;
                GameEvents.RaiseToast("hint.hug", GameInput.Label(GameInput.Hug));
            }
        }

        /// <summary>Starts hugging the closest living player in range. Returns false when nobody is near.</summary>
        public bool TryHug()
        {
            if (IsHugging || status.IsDead) return false;

            var best = FindPartner();
            if (best == null) return false;

            partner = best;
            return BeginHug();
        }

        PlayerStatus FindPartner()
        {
            PlayerStatus best = null;
            float bestDistance = float.MaxValue;
            int count = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.8f, range, buffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var other = buffer[i].GetComponentInParent<PlayerStatus>();
                if (other == null || other == status || other.IsDead) continue;
                float distance = Vector3.Distance(transform.position, other.transform.position);
                if (distance >= bestDistance) continue;
                best = other;
                bestDistance = distance;
            }
            return best;
        }

        bool BeginHug()
        {
            IsHugging = true;
            until = Time.time + duration;
            nextHeart = 0f;
            HealedTotal = 0f;
            controller.LockMovement(duration);
            if (partner.TryGetComponent(out PlayerController otherController))
            {
                otherController.LockMovement(duration);
                // The one being hugged turns towards the one hugging.
                if (otherController.visual != null)
                {
                    Vector3 toMe = transform.position - otherController.transform.position;
                    toMe.y = 0f;
                    if (toMe.sqrMagnitude > 0.01f) otherController.visual.rotation = Quaternion.LookRotation(toMe);
                }
            }
            // Their arms go round us too (a stand-in teammate or the ghost of an online player has no controller to ask).
            partner.GetComponent<PlayerAnimator>()?.Embrace(duration);
            // An online player: their machine holds them still and heals them (their health is theirs to change).
            NetAvatar.OfGhost(partner.gameObject)?.SendHug(duration, healPerSecond);

            Face(partner.transform.position);
            Started?.Invoke(this, partner);
            return true;
        }

        void Face(Vector3 point)
        {
            if (controller.visual == null) return;
            Vector3 look = point - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f) controller.visual.rotation = Quaternion.LookRotation(look);
        }

        void TickHug(float dt)
        {
            bool done = Time.time >= until || partner == null || partner.IsDead || status.IsDead
                        || Vector3.Distance(transform.position, partner.transform.position) > range * 1.8f;
            if (done)
            {
                End();
                return;
            }

            float heal = healPerSecond * dt;
            float before = status.Health + partner.Health;
            status.Heal(heal);
            partner.Heal(heal);
            HealedTotal += status.Health + partner.Health - before;

            if (Time.time >= nextHeart)
            {
                nextHeart = Time.time + 0.22f;
                EmitHeart(Vector3.Lerp(transform.position, partner.transform.position, 0.5f) + Vector3.up * 1.7f);
            }
        }

        void End()
        {
            IsHugging = false;
            partner = null;
            nextHug = Time.time + cooldown;
            Ended?.Invoke(this);
        }

        void EmitHeart(Vector3 position)
        {
            if (heartMaterial == null) return;
            if (hearts == null) hearts = MakeHearts();

            var p = new ParticleSystem.EmitParams
            {
                position = position + new Vector3(UnityEngine.Random.Range(-0.35f, 0.35f), 0f, UnityEngine.Random.Range(-0.35f, 0.35f)),
                velocity = new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), UnityEngine.Random.Range(0.8f, 1.3f), UnityEngine.Random.Range(-0.2f, 0.2f)),
                startSize = UnityEngine.Random.Range(0.28f, 0.45f),
                startLifetime = 1.3f,
                startColor = new Color(1f, 0.45f, 0.58f, 1f)
            };
            hearts.Emit(p, 1);
        }

        ParticleSystem MakeHearts()
        {
            var go = new GameObject("Hearts");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = heartMaterial;
            ps.Play();
            return ps;
        }
    }
}
