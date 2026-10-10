using System;
using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Runs the truck puzzle once the order is complete: builds the shared state, drives the trip timer,
    /// and turns the resolution into rewards, damage and the final result.
    /// </summary>
    public class TruckPuzzleController : MonoBehaviour
    {
        public Truck truck;
        public CombinationRules rules;

        [Header("Trip")]
        public float travelSeconds = 25f;
        [Tooltip("Seconds before a lit EXPLOSION pair goes off. 0 = instant.")]
        public float explosionFuseSeconds = 8f;
        [Tooltip("Seconds before a lit GAME OVER pair goes off. 0 = instant.")]
        public float gameOverFuseSeconds = 5f;

        [Header("Rewards and consequences")]
        public int baseReward = 350;
        [Range(0f, 1f)] public float dangerPenalty = 0.15f;
        [Tooltip("Damage every player takes from an explosion.")]
        public float explosionDamage = 60f;

        GameManager game;

        public TruckPuzzleState State { get; private set; }
        public DeliveryResult LastResult { get; private set; }

        public event Action<TruckPuzzleState> Opened;
        public event Action<DeliveryResult> Finished;

        void Start()
        {
            game = GameManager.Instance;
            if (game != null) game.StateChanged += OnGameStateChanged;
        }

        void OnDestroy()
        {
            if (game != null) game.StateChanged -= OnGameStateChanged;
        }

        void OnGameStateChanged(GameState state)
        {
            if (state == GameState.TruckPuzzle) Open();
        }

        void Update()
        {
            if (State != null && State.Phase == PuzzlePhase.Traveling) State.Tick(Time.unscaledDeltaTime);
        }

        /// <summary>Builds the puzzle from the boxes in the truck, in the order they were delivered.</summary>
        public void Open()
        {
            var boxes = new List<BoxData>(truck.DeliveredBoxes);
            State = new TruckPuzzleState(rules, boxes, travelSeconds, explosionFuseSeconds, gameOverFuseSeconds);
            State.Resolved += OnResolved;
            NetWorld.AttachPuzzle(State, this);
            Opened?.Invoke(State);
        }

        /// <summary>Leaves the garage: from now on bad neighbours light their fuse. Reactions become visible.</summary>
        public void StartTravel()
        {
            if (State == null || State.Phase != PuzzlePhase.Arranging) return;

            // Online, the host starts the trip for everybody.
            if (State.Networked)
            {
                NetWorld.RequestTravel();
                return;
            }
            BeginTravelNow();
        }

        /// <summary>Starts the trip on this machine right away (offline, or when the host has said so).</summary>
        public void BeginTravelNow()
        {
            if (State == null || State.Phase != PuzzlePhase.Arranging) return;

            State.StartTravel();
            for (int i = 0; i < State.PairCount; i++)
                if (State.PairOutcome(i) != CombinationOutcome.Safe) CombinationManual.Discover(State.PairRule(i));
        }

        void OnResolved(PuzzleResolution resolution)
        {
            State.Resolved -= OnResolved;
            Finish(resolution);
        }

        void Finish(PuzzleResolution resolution)
        {
            for (int i = 0; i < State.PairCount; i++) CombinationManual.Discover(State.PairRule(i));

            var result = new DeliveryResult
            {
                Outcome = resolution.Outcome,
                BaseReward = baseReward,
                DangerCount = resolution.DangerCount,
                Delivered = truck.TotalDelivered,
                Total = truck.order != null ? truck.order.TotalBoxes : truck.TotalDelivered,
                Seconds = game != null ? game.ElapsedPlaySeconds : 0f,
                A = resolution.A,
                B = resolution.B
            };
            // Harder modes pay more, and finishing early in a timed mode earns a little extra.
            var mode = LevelDirector.Instance != null ? LevelDirector.Instance.Mode : GameModes.Classic;
            int scaled = Mathf.RoundToInt(baseReward * mode.reward);
            result.BaseReward = scaled;
            result.ModeId = mode.id;
            result.Reward = RewardCalculator.Compute(result.Outcome, result.DangerCount, scaled, dangerPenalty);
            if (game != null && game.HasTimeLimit && result.Outcome < CombinationOutcome.Explosion)
            {
                result.TimeBonus = Mathf.RoundToInt(game.TimeLeft * 0.5f);
                result.Reward += result.TimeBonus;
            }

            if (result.Outcome >= CombinationOutcome.Explosion)
            {
                string nameA = resolution.A != null ? resolution.A.DisplayName : "?";
                string nameB = resolution.B != null ? resolution.B.DisplayName : "?";
                bool lethalMix = result.Outcome == CombinationOutcome.GameOver;
                var cause = new DeathCause(lethalMix ? "death.deadly_mix" : "death.explosion", nameA, nameB);

                foreach (var status in FindObjectsByType<PlayerStatus>())
                {
                    if (lethalMix) status.Kill(cause);
                    else status.Damage(explosionDamage, cause);
                }

                var local = PlayerRegistry.Local;
                if (local != null && local.Status.IsDead)
                {
                    result.LocalPlayerDied = true;
                    result.Cause = cause;
                }
            }

            Wallet.Add(result.Reward);
            LastResult = result;
            Finished?.Invoke(result);
            if (game != null) game.CompleteDelivery(result);
        }
    }
}
