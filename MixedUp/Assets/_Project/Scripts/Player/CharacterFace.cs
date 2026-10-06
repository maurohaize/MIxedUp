using UnityEngine;

namespace MixedUp
{
    /// <summary>The expressions of the character's face. The order matches the cells of Art/Characters/face_atlas.png.</summary>
    public enum FaceExpression
    {
        Neutral, Blink, Happy, Hurt, Dizzy, Scared, Cold, Hot, Shocked, Sick, Love, Effort, Dead, Surprised, Worried, Wink
    }

    /// <summary>
    /// Chooses the face from what is happening to the character: pain, shocks, hugs, shoving, carrying something hot or
    /// frozen or toxic, a long fall, a delivery... It also blinks now and then. Purely visual and derived from the game state.
    /// </summary>
    [RequireComponent(typeof(PlayerStatus))]
    public class CharacterFace : MonoBehaviour
    {
        public Renderer faceRenderer;
        public int columns = 4;

        static readonly int CellId = Shader.PropertyToID("_Cell");

        PlayerStatus status;
        PlayerController controller;
        PlayerHug hug;
        MaterialPropertyBlock block;
        float hurtUntil, shockUntil, surprisedUntil, effortUntil, dizzyUntil, happyUntil, loveUntil, winkUntil;
        float nextBlink = 2f, blinkUntil;
        float lastY;
        float fallSpeed;

        public FaceExpression Current { get; private set; } = FaceExpression.Neutral;

        void Awake()
        {
            status = GetComponent<PlayerStatus>();
            controller = GetComponent<PlayerController>();
            hug = GetComponent<PlayerHug>();
            block = new MaterialPropertyBlock();
            lastY = transform.position.y;
            nextBlink = Time.time + Random.Range(1.5f, 4f);
        }

        void OnEnable()
        {
            if (status == null) status = GetComponent<PlayerStatus>();
            status.Damaged += OnDamaged;
            status.Shocked += OnShocked;
            status.Healed += OnHealed;
            status.Inventory.Transferred += OnTransferred;
            PlayerPush.Pushed += OnPushed;
            PlayerHug.Started += OnHugStarted;
            Sweeper.Hit += OnSweeperHit;
            WaterEffects.Splashed += OnSplash;
            Snowman.Collapsed += OnNearby;
            RubberDuck.Squeaked += OnNearbyDuck;
            if (controller != null) controller.Landed += OnLanded;
        }

        void OnDisable()
        {
            if (status != null)
            {
                status.Damaged -= OnDamaged;
                status.Shocked -= OnShocked;
                status.Healed -= OnHealed;
                status.Inventory.Transferred -= OnTransferred;
            }
            PlayerPush.Pushed -= OnPushed;
            PlayerHug.Started -= OnHugStarted;
            Sweeper.Hit -= OnSweeperHit;
            WaterEffects.Splashed -= OnSplash;
            Snowman.Collapsed -= OnNearby;
            RubberDuck.Squeaked -= OnNearbyDuck;
            if (controller != null) controller.Landed -= OnLanded;
        }

        // ------------------------------------------------------------------ events

        void OnDamaged(float amount, DeathCause cause)
        {
            // Burning and fumes tick away in tiny bites: those show on the face as heat or sickness, not as pain.
            if (amount < 2.5f) return;
            hurtUntil = Time.time + Mathf.Clamp(0.5f + amount * 0.02f, 0.5f, 1.4f);
        }
        void OnShocked() => shockUntil = Time.time + 1.2f;
        void OnHealed(float amount) => happyUntil = Mathf.Max(happyUntil, Time.time + 0.6f);
        void OnTransferred(bool received) => happyUntil = Mathf.Max(happyUntil, Time.time + 1.2f);

        void OnPushed(PlayerPush push, IPushable target)
        {
            if (push.transform == transform) effortUntil = Time.time + 0.6f;
            if (target != null && target.PushTransform == transform) surprisedUntil = Time.time + 1.0f;
        }

        void OnHugStarted(PlayerHug started, PlayerStatus partner)
        {
            if (started.transform == transform || partner == status) loveUntil = Time.time + started.duration;
        }

        void OnSweeperHit(Sweeper sweeper, PlayerController player)
        {
            if (player == controller) dizzyUntil = Time.time + 2.2f;
        }

        void OnSplash(Vector3 position, float strength)
        {
            if ((position - transform.position).sqrMagnitude < 9f) surprisedUntil = Mathf.Max(surprisedUntil, Time.time + 0.5f);
        }

        void OnNearby(Snowman snowman)
        {
            if ((snowman.transform.position - transform.position).sqrMagnitude < 100f) surprisedUntil = Mathf.Max(surprisedUntil, Time.time + 1.2f);
        }

        void OnNearbyDuck(RubberDuck duck)
        {
            if ((duck.transform.position - transform.position).sqrMagnitude < 16f) winkUntil = Time.time + 1.4f;
        }

        void OnLanded(float drop)
        {
            if (drop > 2.5f) dizzyUntil = Mathf.Max(dizzyUntil, Time.time + 0.9f);
        }

        // --------------------------------------------------------------- choosing

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt > 0f) fallSpeed = Mathf.Lerp(fallSpeed, (lastY - transform.position.y) / dt, 0.2f);
            lastY = transform.position.y;

            var wanted = Choose();
            if (wanted != Current) Apply(wanted);
        }

        FaceExpression Choose()
        {
            float now = Time.time;
            if (status.IsDead) return FaceExpression.Dead;
            if (now < shockUntil) return FaceExpression.Shocked;
            if (now < hurtUntil) return FaceExpression.Hurt;
            if ((hug != null && hug.IsHugging) || now < loveUntil) return FaceExpression.Love;
            if (now < surprisedUntil) return FaceExpression.Surprised;
            if (now < effortUntil) return FaceExpression.Effort;
            if (now < dizzyUntil) return FaceExpression.Dizzy;
            if (fallSpeed > 9f && controller != null && !controller.IsGrounded) return FaceExpression.Scared;

            var slots = status.Inventory.Slots;
            bool frozen = false, hot = false;
            float toxic = 0f;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty) continue;
                var box = slots[i].box;
                frozen |= box.HasEffect<FrozenEffect>();
                hot |= box.HasEffect<HeatEffect>();
                if (box.HasEffect<ToxicEffect>()) toxic = Mathf.Max(toxic, box.MaxSeverity(status, slots[i].heldTime));
            }
            if (toxic > 0.55f) return FaceExpression.Sick;
            if (hot) return FaceExpression.Hot;
            if (frozen) return FaceExpression.Cold;
            if (status.Health01 < 0.25f) return FaceExpression.Worried;

            if (now < winkUntil) return FaceExpression.Wink;
            if (now < happyUntil) return FaceExpression.Happy;

            if (now >= nextBlink)
            {
                blinkUntil = now + 0.13f;
                nextBlink = now + Random.Range(2.5f, 5.5f);
            }
            return now < blinkUntil ? FaceExpression.Blink : FaceExpression.Neutral;
        }

        void Apply(FaceExpression expression)
        {
            Current = expression;
            if (faceRenderer == null) return;
            int index = (int)expression;
            faceRenderer.GetPropertyBlock(block);
            block.SetVector(CellId, new Vector4(index % columns, index / columns, 0f, 0f));
            faceRenderer.SetPropertyBlock(block);
        }

        /// <summary>Forces an expression for a moment (cutscenes, tests, the settings preview).</summary>
        public void Show(FaceExpression expression, float seconds)
        {
            switch (expression)
            {
                case FaceExpression.Happy: happyUntil = Time.time + seconds; break;
                case FaceExpression.Love: loveUntil = Time.time + seconds; break;
                case FaceExpression.Surprised: surprisedUntil = Time.time + seconds; break;
                case FaceExpression.Dizzy: dizzyUntil = Time.time + seconds; break;
                case FaceExpression.Hurt: hurtUntil = Time.time + seconds; break;
                case FaceExpression.Shocked: shockUntil = Time.time + seconds; break;
                case FaceExpression.Effort: effortUntil = Time.time + seconds; break;
                case FaceExpression.Wink: winkUntil = Time.time + seconds; break;
            }
        }
    }
}
