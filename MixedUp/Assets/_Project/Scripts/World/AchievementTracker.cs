using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Listens to what happens in the level (jumps over the log, ducks, snowman, mushrooms, deliveries...) and feeds the
    /// Achievements. Only what the local player does counts. Shows a toast when something unlocks.
    /// </summary>
    public class AchievementTracker : MonoBehaviour
    {
        public CombinationRules rules;
        [Tooltip("Where the crawl tunnel's box is; picking it up earns the cave achievement.")]
        public Vector2 tunnelCentre = new Vector2(-37f, 35f);
        public float tunnelRadius = 7f;

        static readonly string[] DeathKeys =
            { "death.heat", "death.electric_water", "death.fall", "death.void", "death.burn", "death.sweeper" };

        Truck truck;
        TruckPuzzleController puzzle;
        PlayerStatus watched;

        void OnEnable()
        {
            Sweeper.CleanJump += OnCleanJump;
            RubberDuck.Squeaked += OnDuck;
            Snowman.Collapsed += OnSnowman;
            BouncePad.Bounced += OnBounce;
            PlayerHug.Started += OnHug;
            PlayerPush.Pushed += OnPush;
            WaterEffects.Splashed += OnSplash;
            BoxPickup.Collected += OnCollected;
            CombinationManual.Changed += CheckCombinations;
            Wallet.Changed += CheckWallet;
            PlayerRegistry.LocalChanged += OnLocalChanged;
            Achievements.Unlocked += OnUnlocked;
        }

        void OnDisable()
        {
            Sweeper.CleanJump -= OnCleanJump;
            RubberDuck.Squeaked -= OnDuck;
            Snowman.Collapsed -= OnSnowman;
            BouncePad.Bounced -= OnBounce;
            PlayerHug.Started -= OnHug;
            PlayerPush.Pushed -= OnPush;
            WaterEffects.Splashed -= OnSplash;
            BoxPickup.Collected -= OnCollected;
            CombinationManual.Changed -= CheckCombinations;
            Wallet.Changed -= CheckWallet;
            PlayerRegistry.LocalChanged -= OnLocalChanged;
            Achievements.Unlocked -= OnUnlocked;
            if (truck != null) truck.BoxDelivered -= OnDelivered;
            if (watched != null) watched.Died -= OnDied;
            if (puzzle != null) puzzle.Finished -= OnFinished;
        }

        void Start()
        {
            var game = FindFirstObjectByType<GameManager>();
            truck = game != null ? game.truck : FindFirstObjectByType<Truck>();
            if (truck != null) truck.BoxDelivered += OnDelivered;

            puzzle = FindFirstObjectByType<TruckPuzzleController>();
            if (puzzle != null)
            {
                puzzle.Finished += OnFinished;
                if (rules == null) rules = puzzle.rules;
            }

            OnLocalChanged(PlayerRegistry.Local);
            CheckCombinations();
            CheckWallet();
        }

        static bool IsLocal(PlayerController player) => player != null && player == PlayerRegistry.Local;

        void OnUnlocked(AchievementDef def)
        {
            GameEvents.RaiseToast("ach.unlocked", Localization.Get(def.NameKey));
            AudioManager.Play(SfxId.Bell);
        }

        void OnLocalChanged(PlayerController player)
        {
            if (watched != null) watched.Died -= OnDied;
            watched = player != null ? player.GetComponent<PlayerStatus>() : null;
            if (watched != null) watched.Died += OnDied;
        }

        void OnCleanJump(Sweeper sweeper, PlayerController player, int streak)
        {
            if (IsLocal(player)) Achievements.Add(Achievements.SpinningLog);
        }

        void OnDuck(RubberDuck duck)
        {
            var p = duck.transform.position;
            Achievements.AddToSet(Achievements.Ducks, Mathf.RoundToInt(p.x) + "_" + Mathf.RoundToInt(p.z));
        }

        void OnSnowman(Snowman snowman) => Achievements.Add(Achievements.Snowman);

        void OnBounce(BouncePad pad)
        {
            var local = PlayerRegistry.Local;
            if (local == null) return;
            Vector3 offset = local.transform.position - pad.transform.position;
            offset.y = 0f;
            if (offset.magnitude <= pad.radius + 0.5f) Achievements.Add(Achievements.Mushrooms);
        }

        void OnHug(PlayerHug hug, PlayerStatus partner)
        {
            if (IsLocal(hug.GetComponent<PlayerController>())) Achievements.Add(Achievements.Hugs);
        }

        void OnPush(PlayerPush push, IPushable target)
        {
            if (target is PlayerController && IsLocal(push.GetComponent<PlayerController>())) Achievements.Add(Achievements.Shoves);
        }

        void OnSplash(Vector3 position, float strength)
        {
            var local = PlayerRegistry.Local;
            if (local == null) return;
            if ((local.transform.position - position).sqrMagnitude < 4f) Achievements.Add(Achievements.Splashes);
        }

        void OnCollected(BoxPickup pickup)
        {
            var p = pickup.transform.position;
            if (Vector2.Distance(new Vector2(p.x, p.z), tunnelCentre) <= tunnelRadius) Achievements.Add(Achievements.Cavers);
        }

        void OnDelivered(BoxData box)
        {
            Achievements.Add(Achievements.FirstBox);
            Achievements.Add(Achievements.Deliveries);
        }

        void OnDied(DeathCause cause)
        {
            if (System.Array.IndexOf(DeathKeys, cause.Key) >= 0) Achievements.AddToSet(Achievements.Deaths, cause.Key);
        }

        void OnFinished(DeliveryResult result)
        {
            Achievements.Add(Achievements.Veteran);
            if (result.Outcome >= CombinationOutcome.Explosion) Achievements.Add(Achievements.Boom);
            else if (!result.LocalPlayerDied)
            {
                Achievements.Add(Achievements.Victory);
                Achievements.AddToSet(Achievements.AllModes, result.ModeId);
            }
        }

        void CheckCombinations()
        {
            if (rules == null || rules.rules == null || Achievements.IsUnlocked(Achievements.Combinations)) return;

            int total = 0;
            foreach (var rule in rules.rules)
            {
                if (rule == null || rule.IsTrivial) continue;
                total++;
                if (!CombinationManual.IsKnown(rule)) return;
            }
            if (total > 0) Achievements.Add(Achievements.Combinations);
        }

        void CheckWallet() => Achievements.SetAtLeast(Achievements.Rich, Wallet.Coins);
    }
}
