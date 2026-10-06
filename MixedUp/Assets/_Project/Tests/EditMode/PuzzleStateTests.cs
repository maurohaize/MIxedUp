using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MixedUp.Tests
{
    public class PuzzleStateTests
    {
        TestFactory f;
        BoxData normal, hot, electric, frozen, toxic;
        CombinationRules rules;

        [SetUp]
        public void SetUp()
        {
            f = new TestFactory();
            normal = f.Box("normal");
            hot = f.Box("hot");
            electric = f.Box("electric");
            frozen = f.Box("frozen");
            toxic = f.Box("toxic");

            rules = f.Make<CombinationRules>();
            rules.rules = new[]
            {
                R(hot, electric, CombinationOutcome.Explosion),
                R(toxic, electric, CombinationOutcome.GameOver),
                R(hot, toxic, CombinationOutcome.Danger),
                R(hot, hot, CombinationOutcome.Danger),
                R(hot, frozen, CombinationOutcome.Safe)
            };
        }

        [TearDown] public void TearDown() => f.Cleanup();

        static CombinationRule R(BoxData a, BoxData b, CombinationOutcome outcome) =>
            new CombinationRule { a = a, b = b, outcome = outcome, hintKey = "hint." + a.id + "_" + b.id };

        TruckPuzzleState State(float travel, float explosionFuse, float gameOverFuse, params BoxData[] boxes) =>
            new TruckPuzzleState(rules, new List<BoxData>(boxes), travel, explosionFuse, gameOverFuse);

        // ----------------------------------------------------------- the table

        [Test]
        public void RulesAreFoundInEitherOrderAndUnknownPairsAreSafe()
        {
            Assert.AreEqual(CombinationOutcome.Explosion, rules.OutcomeOf(hot, electric));
            Assert.AreEqual(CombinationOutcome.Explosion, rules.OutcomeOf(electric, hot));
            Assert.AreEqual(CombinationOutcome.Safe, rules.OutcomeOf(normal, electric));
            Assert.AreEqual(CombinationOutcome.Safe, rules.OutcomeOf(frozen, frozen));
            Assert.IsNull(rules.Find(normal, toxic));
        }

        [Test]
        public void RuleKeysDoNotDependOnOrder()
        {
            Assert.AreEqual(R(hot, electric, CombinationOutcome.Safe).Key, R(electric, hot, CombinationOutcome.Safe).Key);
            Assert.AreEqual("electric+hot", R(hot, electric, CombinationOutcome.Safe).Key);
        }

        // ------------------------------------------------------ the arrangement

        [Test]
        public void SwapExchangesTwoBoxesAndNotifies()
        {
            var s = State(10, 8, 5, hot, electric, normal);
            int changes = 0;
            s.Changed += () => changes++;

            Assert.IsTrue(s.Swap(1, 2));

            Assert.AreSame(normal, s[1]);
            Assert.AreSame(electric, s[2]);
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void InvalidSwapsAreRejected()
        {
            var s = State(10, 8, 5, hot, electric);
            Assert.IsFalse(s.Swap(0, 0));
            Assert.IsFalse(s.Swap(-1, 1));
            Assert.IsFalse(s.Swap(0, 2));
            Assert.AreSame(hot, s[0]);
        }

        [Test]
        public void PairsAreNeighboursInTheChain()
        {
            var s = State(10, 8, 5, hot, electric, frozen, toxic);
            Assert.AreEqual(3, s.PairCount);
            Assert.AreEqual(CombinationOutcome.Explosion, s.PairOutcome(0));
            Assert.AreEqual(CombinationOutcome.Safe, s.PairOutcome(1));
            Assert.AreEqual(CombinationOutcome.Safe, s.PairOutcome(2), "frozen+toxic has no rule");
        }

        [Test]
        public void EvaluatePicksTheWorstPairAndCountsDangers()
        {
            var s = State(10, 8, 5, hot, toxic, hot, hot);
            var res = s.Evaluate();
            Assert.AreEqual(CombinationOutcome.Danger, res.Outcome);
            Assert.AreEqual(3, res.DangerCount);

            s = State(10, 8, 5, hot, toxic, electric);
            res = s.Evaluate();
            Assert.AreEqual(CombinationOutcome.GameOver, res.Outcome);
            Assert.AreSame(toxic, res.A.id == "toxic" ? res.A : res.B);
        }

        [Test]
        public void ASafeArrangementEvaluatesAsSafe()
        {
            var s = State(10, 8, 5, electric, normal, hot, frozen);
            var res = s.Evaluate();
            Assert.AreEqual(CombinationOutcome.Safe, res.Outcome);
            Assert.AreEqual(0, res.DangerCount);
        }

        [Test]
        public void SingleBoxHasNoPairs()
        {
            var s = State(10, 8, 5, hot);
            Assert.AreEqual(0, s.PairCount);
            Assert.AreEqual(CombinationOutcome.Safe, s.Evaluate().Outcome);
        }

        // ------------------------------------------------------------ the trip

        [Test]
        public void NothingHappensWhileArranging()
        {
            var s = State(10, 1, 1, hot, electric);
            s.Tick(100f);
            Assert.AreEqual(PuzzlePhase.Arranging, s.Phase);
            Assert.IsFalse(s.IsFuseLit(0));
        }

        [Test]
        public void AnExplosivePairBurnsItsFuseAndThenExplodes()
        {
            var s = State(30, 8, 5, hot, electric);
            PuzzleResolution result = null;
            s.Resolved += r => result = r;
            s.StartTravel();

            Assert.IsTrue(s.IsFuseLit(0));
            s.Tick(7f);
            Assert.IsNull(result);
            Assert.AreEqual(1f / 8f, s.FuseRemaining01(0), 0.001f);

            s.Tick(1.1f);
            Assert.IsNotNull(result);
            Assert.AreEqual(CombinationOutcome.Explosion, result.Outcome);
            Assert.IsTrue(result.FuseExpired);
            Assert.AreEqual(PuzzlePhase.Resolved, s.Phase);
        }

        [Test]
        public void GameOverPairsHaveTheirOwnShorterFuse()
        {
            var s = State(30, 8, 5, toxic, electric);
            PuzzleResolution result = null;
            s.Resolved += r => result = r;
            s.StartTravel();

            s.Tick(4.9f);
            Assert.IsNull(result);
            s.Tick(0.2f);
            Assert.AreEqual(CombinationOutcome.GameOver, result.Outcome);
        }

        [Test]
        public void SeparatingTheBoxesInTimeDefusesTheFuse()
        {
            var s = State(20, 8, 5, hot, electric, normal);
            PuzzleResolution result = null;
            s.Resolved += r => result = r;
            s.StartTravel();

            s.Tick(6f);
            Assert.IsTrue(s.Swap(1, 2));
            Assert.IsFalse(s.IsFuseLit(0));
            Assert.IsFalse(s.IsFuseLit(1));

            s.Tick(30f);
            Assert.AreEqual(CombinationOutcome.Safe, result.Outcome);
            Assert.IsFalse(result.FuseExpired);
        }

        [Test]
        public void ANewBadPairGetsAFullFuseNotTheRemainsOfAnOldOne()
        {
            var s = State(60, 8, 5, hot, normal, electric);
            PuzzleResolution result = null;
            s.Resolved += r => result = r;
            s.StartTravel();
            s.Tick(5f);

            s.Swap(1, 2);
            Assert.IsTrue(s.IsFuseLit(0));
            Assert.AreEqual(1f, s.FuseRemaining01(0), 0.001f);

            s.Tick(7f);
            Assert.IsNull(result);
            s.Tick(1.5f);
            Assert.AreEqual(CombinationOutcome.Explosion, result.Outcome);
        }

        [Test]
        public void TheTripEndsWithTheWorstRemainingPairEvenWithoutAFuse()
        {
            var s = State(5, 8, 5, hot, toxic);
            PuzzleResolution result = null;
            s.Resolved += r => result = r;
            s.StartTravel();
            Assert.IsFalse(s.IsFuseLit(0), "danger pairs have no fuse");

            s.Tick(5.1f);
            Assert.AreEqual(CombinationOutcome.Danger, result.Outcome);
            Assert.AreEqual(1, result.DangerCount);
            Assert.IsFalse(result.FuseExpired);
        }

        [Test]
        public void AZeroFuseMakesBadPairsExplodeImmediately()
        {
            var s = State(30, 0, 0, hot, electric);
            PuzzleResolution result = null;
            s.Resolved += r => result = r;
            s.StartTravel();
            s.Tick(0.01f);
            Assert.AreEqual(CombinationOutcome.Explosion, result.Outcome);
        }

        [Test]
        public void ResolvedPuzzlesIgnoreFurtherInput()
        {
            var s = State(1, 8, 5, hot, frozen);
            s.StartTravel();
            s.Tick(2f);
            Assert.AreEqual(PuzzlePhase.Resolved, s.Phase);
            Assert.IsFalse(s.Swap(0, 1));
        }

        [Test]
        public void TravelProgressAndTimeLeftFollowTheClock()
        {
            var s = State(20, 8, 5, hot, frozen);
            s.StartTravel();
            s.Tick(5f);
            Assert.AreEqual(0.25f, s.TravelProgress01, 0.001f);
            Assert.AreEqual(15f, s.TimeLeft, 0.001f);
        }

        // ------------------------------------------------------------- rewards

        [Test]
        public void RewardsFollowTheOutcome()
        {
            Assert.AreEqual(400, RewardCalculator.Compute(CombinationOutcome.Safe, 0, 400, 0.15f));
            Assert.AreEqual(340, RewardCalculator.Compute(CombinationOutcome.Danger, 1, 400, 0.15f));
            Assert.AreEqual(280, RewardCalculator.Compute(CombinationOutcome.Danger, 2, 400, 0.15f));
            Assert.AreEqual(100, RewardCalculator.Compute(CombinationOutcome.Danger, 20, 400, 0.15f), "never below the floor");
            Assert.AreEqual(0, RewardCalculator.Compute(CombinationOutcome.Explosion, 0, 400, 0.15f));
            Assert.AreEqual(0, RewardCalculator.Compute(CombinationOutcome.GameOver, 0, 400, 0.15f));
        }

        // -------------------------------------------------------------- manual

        string savedCombos;
        int savedWallet;

        [Test]
        public void DiscoveringARuleIsRememberedAndOnlyNewOnce()
        {
            savedCombos = PlayerPrefs.GetString("combos.learned", string.Empty);
            try
            {
                CombinationManual.ForgetAll();
                var rule = rules.Find(hot, electric);

                Assert.IsFalse(CombinationManual.IsKnown(rule));
                Assert.IsTrue(CombinationManual.Discover(rule));
                Assert.IsTrue(CombinationManual.IsKnown(rule));
                Assert.IsFalse(CombinationManual.Discover(rule), "second time is not new");

                CombinationManual.Reload();
                Assert.IsTrue(CombinationManual.IsKnown(rule), "survives a reload from PlayerPrefs");
            }
            finally
            {
                if (string.IsNullOrEmpty(savedCombos)) PlayerPrefs.DeleteKey("combos.learned");
                else PlayerPrefs.SetString("combos.learned", savedCombos);
                CombinationManual.Reload();
            }
        }

        [Test]
        public void RulesWithoutAnEntryAndDefaultKnownRulesNeedNoDiscovery()
        {
            Assert.IsTrue(CombinationManual.IsKnown(null));

            var known = R(hot, normal, CombinationOutcome.Safe);
            known.knownByDefault = true;
            Assert.IsTrue(CombinationManual.IsKnown(known));
            Assert.IsFalse(CombinationManual.Discover(known));
        }

        [Test]
        public void WalletAccumulatesAndSpends()
        {
            savedWallet = PlayerPrefs.GetInt("wallet.coins", 0);
            try
            {
                PlayerPrefs.SetInt("wallet.coins", 0);
                Wallet.Add(350);
                Wallet.Add(-20);
                Wallet.Add(0);
                Assert.AreEqual(350, Wallet.Coins);
                Assert.IsFalse(Wallet.TrySpend(400));
                Assert.IsTrue(Wallet.TrySpend(100));
                Assert.AreEqual(250, Wallet.Coins);
            }
            finally
            {
                PlayerPrefs.SetInt("wallet.coins", savedWallet);
            }
        }
    }
}
