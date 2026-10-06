using System;
using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    public enum PuzzlePhase
    {
        Arranging,
        Traveling,
        Resolved
    }

    public sealed class PuzzleResolution
    {
        public CombinationOutcome Outcome;
        public CombinationRule Culprit;
        public BoxData A;
        public BoxData B;
        public int DangerCount;
        /// <summary>True when a lit fuse ran out; false when the trip simply ended.</summary>
        public bool FuseExpired;
    }

    /// <summary>
    /// The shared state of the truck puzzle: which box sits in which slot, which neighbours react,
    /// and the fuses that burn while driving. Plain data with no UI, so it can be synchronised later.
    /// </summary>
    public sealed class TruckPuzzleState
    {
        readonly CombinationRules rules;
        readonly BoxData[] slots;
        readonly float[] fuse;
        readonly string[] fuseKey;

        public readonly float TravelDuration;
        public readonly float ExplosionFuse;
        public readonly float GameOverFuse;

        public PuzzlePhase Phase { get; private set; }
        public float TravelElapsed { get; private set; }
        public PuzzleResolution Resolution { get; private set; }

        public event Action Changed;
        public event Action<PuzzleResolution> Resolved;

        public TruckPuzzleState(CombinationRules rules, IReadOnlyList<BoxData> boxes,
            float travelDuration = 25f, float explosionFuse = 8f, float gameOverFuse = 5f)
        {
            this.rules = rules;
            slots = new BoxData[boxes.Count];
            for (int i = 0; i < slots.Length; i++) slots[i] = boxes[i];

            int pairs = Math.Max(0, slots.Length - 1);
            fuse = new float[pairs];
            fuseKey = new string[pairs];

            TravelDuration = travelDuration;
            ExplosionFuse = explosionFuse;
            GameOverFuse = gameOverFuse;
        }

        public int Count => slots.Length;
        public int PairCount => fuse.Length;
        public BoxData this[int index] => slots[index];
        public float TravelProgress01 => TravelDuration <= 0f ? 1f : Mathf.Clamp01(TravelElapsed / TravelDuration);
        public float TimeLeft => Mathf.Max(0f, TravelDuration - TravelElapsed);

        /// <summary>The rule between slot i and slot i + 1, or null when they are plainly safe.</summary>
        public CombinationRule PairRule(int i) => rules == null ? null : rules.Find(slots[i], slots[i + 1]);

        public CombinationOutcome PairOutcome(int i)
        {
            var rule = PairRule(i);
            return rule == null ? CombinationOutcome.Safe : rule.outcome;
        }

        /// <summary>How much fuse is left on a pair: 1 = just lit, 0 = about to go off. 1 when nothing burns.</summary>
        public float FuseRemaining01(int i)
        {
            if (Phase != PuzzlePhase.Traveling || fuseKey[i] == null) return 1f;
            float full = FuseTime(PairOutcome(i));
            return full <= 0f ? 0f : Mathf.Clamp01(fuse[i] / full);
        }

        public bool IsFuseLit(int i) => Phase == PuzzlePhase.Traveling && fuseKey[i] != null;

        public bool AnyFuseLit()
        {
            for (int i = 0; i < fuseKey.Length; i++)
                if (IsFuseLit(i)) return true;
            return false;
        }

        public bool Swap(int i, int j)
        {
            if (Phase == PuzzlePhase.Resolved) return false;
            if (i == j || i < 0 || j < 0 || i >= slots.Length || j >= slots.Length) return false;

            (slots[i], slots[j]) = (slots[j], slots[i]);
            RefreshFuses(0f);
            Changed?.Invoke();
            return true;
        }

        public void StartTravel()
        {
            if (Phase != PuzzlePhase.Arranging) return;

            Phase = PuzzlePhase.Traveling;
            TravelElapsed = 0f;
            for (int i = 0; i < fuseKey.Length; i++) fuseKey[i] = null;
            RefreshFuses(0f);
            Changed?.Invoke();
        }

        public void Tick(float deltaTime)
        {
            if (Phase != PuzzlePhase.Traveling || deltaTime <= 0f) return;

            TravelElapsed += deltaTime;
            RefreshFuses(deltaTime);

            int expired = -1;
            for (int i = 0; i < fuseKey.Length; i++)
            {
                if (fuseKey[i] == null || fuse[i] > 0f) continue;
                if (expired < 0 || PairOutcome(i) > PairOutcome(expired)) expired = i;
            }

            if (expired >= 0)
            {
                var res = Describe(expired, PairOutcome(expired));
                res.FuseExpired = true;
                Resolve(res);
                return;
            }

            if (TravelElapsed >= TravelDuration) Resolve(Evaluate());
            else Changed?.Invoke();
        }

        /// <summary>Judges the current arrangement: the worst neighbouring pair decides, dangers are counted.</summary>
        public PuzzleResolution Evaluate()
        {
            int worst = -1;
            int dangers = 0;
            for (int i = 0; i < PairCount; i++)
            {
                var outcome = PairOutcome(i);
                if (outcome == CombinationOutcome.Danger) dangers++;
                if (outcome == CombinationOutcome.Safe) continue;
                if (worst < 0 || outcome > PairOutcome(worst)) worst = i;
            }

            if (worst < 0) return new PuzzleResolution { Outcome = CombinationOutcome.Safe };

            var res = Describe(worst, PairOutcome(worst));
            res.DangerCount = dangers;
            return res;
        }

        PuzzleResolution Describe(int pair, CombinationOutcome outcome)
        {
            int dangers = 0;
            for (int i = 0; i < PairCount; i++)
                if (PairOutcome(i) == CombinationOutcome.Danger) dangers++;

            return new PuzzleResolution
            {
                Outcome = outcome,
                Culprit = PairRule(pair),
                A = slots[pair],
                B = slots[pair + 1],
                DangerCount = dangers
            };
        }

        void Resolve(PuzzleResolution resolution)
        {
            Phase = PuzzlePhase.Resolved;
            Resolution = resolution;
            Changed?.Invoke();
            Resolved?.Invoke(resolution);
        }

        float FuseTime(CombinationOutcome outcome) =>
            outcome == CombinationOutcome.GameOver ? GameOverFuse : ExplosionFuse;

        /// <summary>Lights fuses on dangerous pairs, restarts them when the pair changes, and burns them.</summary>
        void RefreshFuses(float deltaTime)
        {
            if (Phase != PuzzlePhase.Traveling) return;

            for (int i = 0; i < fuseKey.Length; i++)
            {
                var rule = PairRule(i);
                bool burns = rule != null && rule.outcome >= CombinationOutcome.Explosion;
                if (!burns)
                {
                    fuseKey[i] = null;
                    continue;
                }

                if (fuseKey[i] != rule.Key)
                {
                    fuseKey[i] = rule.Key;
                    fuse[i] = FuseTime(rule.outcome);
                }
                else
                {
                    fuse[i] -= deltaTime;
                }
            }
        }
    }
}
