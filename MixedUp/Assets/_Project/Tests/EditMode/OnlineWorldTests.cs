using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MixedUp.Tests
{
    /// <summary>The parts of the online world that need no network to check: the networked puzzle state and ghost players.</summary>
    public class OnlineWorldTests
    {
        TestFactory f;
        BoxData normal, hot, electric;
        CombinationRules rules;

        [SetUp]
        public void SetUp()
        {
            f = new TestFactory();
            normal = f.Box("normal");
            hot = f.Box("hot");
            electric = f.Box("electric");
            rules = f.Make<CombinationRules>();
            rules.rules = new[]
            {
                new CombinationRule { a = hot, b = electric, outcome = CombinationOutcome.Explosion, hintKey = "hint.he" }
            };
        }

        [TearDown] public void TearDown() => f.Cleanup();

        TruckPuzzleState State(params BoxData[] boxes) =>
            new TruckPuzzleState(rules, new List<BoxData>(boxes), 10f, 3f, 2f);

        // ------------------------------------------------------------ the networked puzzle

        [Test]
        public void ANetworkedSwapOnlyAsksAndTheHostsAnswerApplies()
        {
            var s = State(hot, electric, normal);
            s.Networked = true;
            int asked = 0, a = -1, b = -1;
            s.SwapRequested += (i, j) => { asked++; a = i; b = j; };

            Assert.IsTrue(s.Swap(0, 2));
            Assert.AreEqual(1, asked);
            Assert.AreEqual(0, a);
            Assert.AreEqual(2, b);
            Assert.AreSame(hot, s[0], "nothing moved yet");

            Assert.IsTrue(s.ApplySwap(0, 2));
            Assert.AreSame(normal, s[0]);
            Assert.AreSame(hot, s[2]);
        }

        [Test]
        public void AnOfflineSwapStillAppliesAtOnce()
        {
            var s = State(hot, electric);
            Assert.IsTrue(s.Swap(0, 1));
            Assert.AreSame(electric, s[0]);
        }

        [Test]
        public void SetArrangementPutsTheBoxesInTheHostsOrderAndNotifies()
        {
            var s = State(hot, electric, normal);
            int changes = 0;
            s.Changed += () => changes++;

            s.SetArrangement(new List<BoxData> { normal, hot, electric });

            Assert.AreSame(normal, s[0]);
            Assert.AreSame(hot, s[1]);
            Assert.AreSame(electric, s[2]);
            Assert.AreEqual(CombinationOutcome.Explosion, s.PairOutcome(1));
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void SetArrangementIgnoresAListOfTheWrongSize()
        {
            var s = State(hot, electric, normal);
            s.SetArrangement(new List<BoxData> { normal });
            Assert.AreSame(hot, s[0]);
        }

        [Test]
        public void AFollowerDoesNotEndTheTripByItself()
        {
            var s = State(hot, electric);
            s.Authoritative = false;
            PuzzleResolution resolution = null;
            s.Resolved += r => resolution = r;

            s.StartTravel();
            s.Tick(20f);   // far beyond the fuse and the trip length

            Assert.IsNull(resolution, "only the host decides");
            Assert.AreEqual(PuzzlePhase.Traveling, s.Phase);
        }

        [Test]
        public void TheHostsVerdictEndsTheTripOnAFollower()
        {
            var s = State(hot, electric);
            s.Authoritative = false;
            PuzzleResolution resolution = null;
            s.Resolved += r => resolution = r;
            s.StartTravel();

            s.ForceResolve(0, true);

            Assert.IsNotNull(resolution);
            Assert.AreEqual(CombinationOutcome.Explosion, resolution.Outcome);
            Assert.IsTrue(resolution.FuseExpired);
            Assert.AreEqual(0, resolution.Pair);
            Assert.AreEqual(PuzzlePhase.Resolved, s.Phase);
        }

        [Test]
        public void AHostResolutionSaysWhichPairDecidedIt()
        {
            var s = State(normal, hot, electric);
            PuzzleResolution resolution = null;
            s.Resolved += r => resolution = r;
            s.StartTravel();

            s.Tick(5f);   // the fuse of the hot + electric pair (slots 1 and 2) runs out after 3 s

            Assert.IsNotNull(resolution);
            Assert.AreEqual(1, resolution.Pair);
            Assert.IsTrue(resolution.FuseExpired);
        }

        [Test]
        public void ASafeTripReportsNoPair()
        {
            var s = State(normal, hot);
            PuzzleResolution resolution = null;
            s.Resolved += r => resolution = r;
            s.StartTravel();
            s.Tick(11f);

            Assert.IsNotNull(resolution);
            Assert.AreEqual(CombinationOutcome.Safe, resolution.Outcome);
            Assert.AreEqual(-1, resolution.Pair);
        }

        // ------------------------------------------------------------ ghosts

        [Test]
        public void AGhostIsNeitherHurtNorHealedNorKilledByItsOwnMachine()
        {
            var status = f.Player("Ghost");
            status.IsMirror = true;

            status.Damage(30f, DeathCause.Fall);
            Assert.AreEqual(100f, status.Health, 0.001f);

            status.Kill(DeathCause.Burn);
            Assert.IsFalse(status.IsDead);

            status.MirrorDie(DeathCause.Void);
            Assert.IsTrue(status.IsDead);
        }

        [Test]
        public void ANormalPlayerCannotBeMirrorKilled()
        {
            var status = f.Player("Real");
            status.MirrorDie(DeathCause.Void);
            Assert.IsFalse(status.IsDead);
        }

        [Test]
        public void ABoxHeldByAGhostDoesNotHurtIt()
        {
            var burning = f.Box("burning", f.Make<HeatEffect>());
            var status = f.Player("Ghost");
            status.IsMirror = true;
            status.Inventory.TryAdd(burning, out _);

            for (int i = 0; i < 100; i++) status.Tick(0.5f);

            Assert.IsFalse(status.IsDead);
            Assert.AreEqual(100f, status.Health, 0.001f);
        }

        // ------------------------------------------------------------ the shared clock

        [Test]
        public void OfflineThereIsNoSharedClockAndNoWaiting()
        {
            Assert.IsFalse(NetWorld.Active);
            Assert.IsFalse(NetWorld.SharedClock);
            Assert.IsFalse(NetWorld.Waiting);
        }

        [Test]
        public void DropIdsAreAboveTheStartingBoxes()
        {
            Assert.GreaterOrEqual(NetWorld.DropIdBase, 1000);
        }
    }
}
