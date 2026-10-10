using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// A heavy log that spins around a post. Anyone it touches is knocked away and hurt; a short cooldown per player
    /// stops it from hitting every frame. The hit test is plain geometry against the registered players, so it works
    /// the same for every player in multiplayer. It also counts clean jumps over the log (see JumpScoreboard).
    /// </summary>
    public class Sweeper : MonoBehaviour
    {
        [Tooltip("The spinning part (log and, optionally, decorations).")]
        public Transform arm;
        public float degreesPerSecond = 75f;
        [Tooltip("Half-size of the log in its own space: x = half length, y = half height, z = half thickness.")]
        public Vector3 halfExtents = new Vector3(3.2f, 0.45f, 0.3f);
        [Tooltip("Height of the log's centre above the post's origin.")]
        public float height = 0.55f;
        public float damage = 14f;
        public float knockback = 11f;
        public float knockUp = 4.5f;
        public float cooldown = 1.2f;
        [Tooltip("Players further than this from the post stop counting their streak of jumps.")]
        public float streakRadius = 8f;
        [Tooltip("Extra height (metres) above the log's top that the body has to reach to clear it. The lower, the easier the jump.")]
        public float verticalMargin = 0.2f;

        public float AngleDegrees { get; private set; }
        JumpScoreboard board;
        /// <summary>Created on first use: it reads PlayerPrefs, which is not allowed while the object is being constructed.</summary>
        public JumpScoreboard Board => board ?? (board = new JumpScoreboard());
        public static event System.Action<Sweeper, PlayerController, int> CleanJump;
        public static event System.Action<Sweeper, PlayerController> Hit;

        readonly string cooldownKey = "sweeper-" + System.Guid.NewGuid();
        readonly Dictionary<PlayerController, float> lastSide = new Dictionary<PlayerController, float>();
        readonly Dictionary<PlayerController, float> lastFeet = new Dictionary<PlayerController, float>();
        readonly Dictionary<PlayerController, float> lastHitTime = new Dictionary<PlayerController, float>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { CleanJump = null; Hit = null; }

        /// <summary>The name shown on the scoreboard for a player.</summary>
        public static string NameOf(PlayerController player)
        {
            if (player == null || !player.isLocal) return Localization.Get("ui.teammate");
            // Online, everybody's jumps share one board, so each player goes by their real name.
            if (NetWorld.Active && NetAvatar.Local != null && !string.IsNullOrEmpty(NetAvatar.Local.DisplayName)) return NetAvatar.Local.DisplayName;
            return Localization.Get("ui.you");
        }

        /// <summary>Every spinning log of the level, in a fixed order (the same on every machine of an online game).</summary>
        public static readonly System.Collections.Generic.List<Sweeper> All = new System.Collections.Generic.List<Sweeper>();

        void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            All.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        void OnDisable() => All.Remove(this);

        void EndStreak(PlayerController player)
        {
            if (Board.Hit(NameOf(player)) && player.isLocal) NetWorld.AnnounceStreak(this, 0);
        }

        void Update()
        {
            // Online, every machine shows the log at the same angle (a function of the shared clock).
            if (NetWorld.SharedClock) AngleDegrees = Mathf.Repeat((float)(LevelClock.Seconds * degreesPerSecond), 360f);
            else AngleDegrees = Mathf.Repeat(AngleDegrees + degreesPerSecond * Time.deltaTime, 360f);
            if (arm != null) arm.localRotation = Quaternion.Euler(0f, AngleDegrees, 0f);

            foreach (var player in PlayerRegistry.All)
            {
                if (player == null) continue;
                if (player.Status.IsDead)
                {
                    EndStreak(player);
                    lastSide.Remove(player);
                    continue;
                }

                Vector3 body = player.transform.position + Vector3.up * 0.8f;
                bool touching = Touches(body, 0.38f);
                TrackPass(player);

                if (!touching) continue;
                if (!player.Status.TryUseCooldown(cooldownKey, cooldown)) continue;

                Vector3 push = player.transform.position - transform.position;
                push.y = 0f;
                if (push.sqrMagnitude < 0.01f) push = transform.forward;

                // Throw them off the side the log came from, not along its length.
                Vector3 sideways = arm != null ? arm.right * Mathf.Sign(Vector3.Dot(arm.forward, push) + 0.0001f) : push;
                Vector3 direction = (push.normalized * 0.6f + sideways * 0.4f).normalized;
                player.AddImpulse(direction * knockback, knockUp);
                player.Status.Damage(damage, DeathCause.Sweeper);
                lastHitTime[player] = Time.time;
                EndStreak(player);
                Hit?.Invoke(this, player);
            }
        }

        /// <summary>
        /// Watches on which side of the log's line a player stands: when the log sweeps past them without touching and they
        /// are in the air, that was a clean jump. The height is judged by the higher of the last two frames, so on a slow
        /// computer (where the log can pass between two frames) a good jump still counts.
        /// </summary>
        void TrackPass(PlayerController player)
        {
            if (arm == null) return;

            float feet = player.transform.position.y;
            float highest = lastFeet.TryGetValue(player, out float previousFeet) ? Mathf.Max(feet, previousFeet) : feet;
            lastFeet[player] = feet;

            Vector3 offset = player.transform.position - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance > streakRadius)
            {
                EndStreak(player);
                lastSide.Remove(player);
                return;
            }
            if (distance < 0.8f || distance > halfExtents.x + 0.4f) { lastSide.Remove(player); return; }

            Vector3 along = arm.right;
            along.y = 0f;
            float side = Vector3.Dot(Vector3.Cross(along.normalized, offset.normalized), Vector3.up);
            bool recentlyHit = lastHitTime.TryGetValue(player, out float hitAt) && Time.time - hitAt < 0.6f;
            if (lastSide.TryGetValue(player, out float previous) && Mathf.Sign(previous) != Mathf.Sign(side) && Mathf.Abs(previous) + Mathf.Abs(side) < 1.2f
                && !player.IsGrounded && !recentlyHit)
            {
                var clearing = new Vector3(player.transform.position.x, highest + 0.8f, player.transform.position.z);
                if (!Touches(clearing, 0.38f)) RegisterCleanJump(player);
            }
            lastSide[player] = side;
        }

        /// <summary>One more jump over the log for this player (also used directly by tests).</summary>
        public int RegisterCleanJump(PlayerController player)
        {
            int streak = Board.Clean(NameOf(player));
            if (player != null && player.isLocal) NetWorld.AnnounceStreak(this, streak);
            CleanJump?.Invoke(this, player, streak);
            return streak;
        }

        /// <summary>Is a sphere (a player's body) inside the log's box?</summary>
        public bool Touches(Vector3 worldPoint, float radius)
        {
            if (arm == null) return false;
            Vector3 local = arm.InverseTransformPoint(worldPoint) - new Vector3(0f, height, 0f);
            Vector3 limit = halfExtents + Vector3.one * radius;
            return Mathf.Abs(local.x) <= limit.x && Mathf.Abs(local.y) <= limit.y + verticalMargin && Mathf.Abs(local.z) <= limit.z;
        }
    }
}
