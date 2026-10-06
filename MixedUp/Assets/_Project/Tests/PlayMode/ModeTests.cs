using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>Every game mode: the right order and boxes, sensible places, a way to win and ways to lose.</summary>
    public class ModeTests : SceneTestBase
    {
        IEnumerator Load(GameModeInfo mode)
        {
            yield return LoadLevel(mode);
            pickups = Object.FindObjectsByType<BoxPickup>();
        }

        List<BoxData> OrderBoxes()
        {
            var all = new List<BoxData>();
            foreach (var line in truck.order.lines)
                for (int i = 0; i < line.count; i++) all.Add(line.box);
            return all;
        }

        void AssertBoxesMatchTheOrder()
        {
            Resolve();
            var expected = new Dictionary<string, int>();
            foreach (var line in truck.order.lines) expected[line.box.id] = (expected.TryGetValue(line.box.id, out int n) ? n : 0) + line.count;

            var found = new Dictionary<string, int>();
            foreach (var pickup in pickups) found[pickup.data.id] = (found.TryGetValue(pickup.data.id, out int n) ? n : 0) + 1;

            CollectionAssert.AreEquivalent(expected, found, "one box in the world for every box of the order");
            Assert.AreEqual(truck.order.TotalBoxes, pickups.Length);
        }

        void AssertPlacesAreSane()
        {
            var seen = new List<Vector3>();
            foreach (var pickup in pickups)
            {
                var p = pickup.transform.position;
                foreach (var other in seen) Assert.Greater(Vector3.Distance(p, other), 2f, "two boxes on the same spot");
                seen.Add(p);

                var hits = Physics.RaycastAll(p + Vector3.up * 0.4f, Vector3.down, 1.2f, ~0, QueryTriggerInteraction.Ignore);
                Assert.IsTrue(hits.Any(h => !h.collider.transform.IsChildOf(pickup.transform)), pickup.name + " at " + p + " floats in the air");

                var inside = Physics.OverlapSphere(p + Vector3.up * 0.7f, 0.42f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(c => !c.transform.IsChildOf(pickup.transform) && !c.name.StartsWith("Ground")).ToArray();
                Assert.IsEmpty(inside.Select(c => c.name), pickup.name + " at " + p + " is buried in something");
            }
        }

        // ---------------------------------------------------------------- classic

        [UnityTest]
        public IEnumerator ClassicKeepsTheMapOrderAndTheDesignedSpots()
        {
            yield return Load(GameModes.Classic);
            AssertBoxesMatchTheOrder();
            Assert.AreEqual(6, truck.order.TotalBoxes);
            Assert.IsFalse(GameManager.Instance.HasTimeLimit);

            var hot = pickups.First(p => p.data.id == "hot").transform.position;
            Assert.Less(Vector3.Distance(hot, new Vector3(6f, 0f, 30f)), 1f, "the hot box waits in its ring of rocks");
            var frozen = pickups.First(p => p.data.id == "frozen").transform.position;
            Assert.Greater(frozen.y, 4.5f, "the frozen box is on the plateau");
        }

        // --------------------------------------------------- the other modes

        [UnityTest]
        public IEnumerator ExpressHasThreeBoxesAndAThreeMinuteClock()
        {
            yield return Load(GameModes.Express);
            AssertBoxesMatchTheOrder();
            Assert.AreEqual(3, truck.order.TotalBoxes);
            Assert.AreEqual(180f, GameManager.Instance.timeLimitSeconds);
            Assert.IsTrue(PuzzleSolver.HasSafeArrangement(OrderBoxes(), puzzle.rules));
            AssertPlacesAreSane();
        }

        [UnityTest]
        public IEnumerator GiantOrdersNineBoxes()
        {
            yield return Load(GameModes.Giant);
            AssertBoxesMatchTheOrder();
            Assert.AreEqual(9, truck.order.TotalBoxes);
            Assert.AreEqual(600f, GameManager.Instance.timeLimitSeconds);
            Assert.IsTrue(PuzzleSolver.HasSafeArrangement(OrderBoxes(), puzzle.rules));
            AssertPlacesAreSane();

            // The order card grows to fit.
            var hud = Object.FindAnyObjectByType<TruckOrderHud>();
            Assert.GreaterOrEqual(hud.panel.sizeDelta.y, 90f + truck.order.lines.Length * 70f);
        }

        [UnityTest]
        public IEnumerator SurpriseIsDifferentEveryTimeAndAlwaysSolvable()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < 4; i++)
            {
                yield return Load(GameModes.Surprise);
                AssertBoxesMatchTheOrder();
                Assert.That(truck.order.TotalBoxes, Is.InRange(5, 7));
                Assert.IsTrue(PuzzleSolver.HasSafeArrangement(OrderBoxes(), puzzle.rules));
                AssertPlacesAreSane();
                seen.Add(string.Join(",", pickups.OrderBy(p => p.transform.position.x).Select(p => p.data.id + p.transform.position.ToString("0"))));
            }
            Assert.GreaterOrEqual(seen.Count, 2, "the boxes are not always in the same places");
        }

        [UnityTest]
        public IEnumerator ChallengePutsBoxesWhereOnlyTheBraveCanGo()
        {
            yield return Load(GameModes.Challenge);
            AssertBoxesMatchTheOrder();
            Assert.AreEqual(5, truck.order.TotalBoxes);
            Assert.IsTrue(PuzzleSolver.HasSafeArrangement(OrderBoxes(), puzzle.rules));
            AssertPlacesAreSane();

            var hard = Object.FindObjectsByType<BoxSpawnPoint>().Where(s => s.hard).ToArray();
            Assert.GreaterOrEqual(hard.Length, 5);
            foreach (var pickup in pickups)
                Assert.IsTrue(hard.Any(h => Vector3.Distance(h.Position, pickup.transform.position) < 0.2f), pickup.name + " is on a hard spot");
        }

        [UnityTest]
        public IEnumerator NightIsDarkAndTheBoxesGlow()
        {
            yield return Load(GameModes.Night);
            AssertBoxesMatchTheOrder();
            Assert.AreEqual(6, truck.order.TotalBoxes, "the classic order");

            var sun = Object.FindObjectsByType<Light>().First(l => l.type == LightType.Directional);
            Assert.Less(sun.intensity, 0.6f, "the sun is gone");
            Assert.IsTrue(pickups.All(p => p.GetComponentInChildren<BoxBeacon>() != null), "every box has a glow");
        }

        // --------------------------------------------------------- every spot

        [UnityTest]
        public IEnumerator EverySpawnPointIsOnSolidGroundAndFreeOfScenery()
        {
            yield return Load(GameModes.Classic);
            foreach (var point in Object.FindObjectsByType<BoxSpawnPoint>())
            {
                var p = point.Position;
                var hits = Physics.RaycastAll(p + Vector3.up * 0.4f, Vector3.down, 1.2f, ~0, QueryTriggerInteraction.Ignore);
                Assert.IsTrue(hits.Length > 0, point.name + " floats in the air at " + p);
                var inside = Physics.OverlapSphere(p + Vector3.up * 0.7f, 0.42f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(c => !c.name.StartsWith("Ground") && !c.name.StartsWith("Box_")).ToArray();
                Assert.IsEmpty(inside.Select(c => c.name), point.name + " is buried in something at " + p);
            }
        }

        [UnityTest]
        public IEnumerator ABoxOnTopOfTheParkourTowerCanBePickedUpFromTheTop()
        {
            yield return Load(GameModes.Classic);
            var spot = Object.FindObjectsByType<BoxSpawnPoint>().First(s => s.name == "Spawn_Hard_Tower").Position;
            yield return GoTo(spot + new Vector3(1.3f, 0.1f, 0f));
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(player.IsGrounded, "the top of the tower is solid");
            Assert.Greater(player.transform.position.y, 6.5f);
        }

        // ------------------------------------------------------ winning and losing

        IEnumerator WinThisMode(GameModeInfo mode)
        {
            yield return Load(mode);
            Resolve();
            puzzle.travelSeconds = 1.2f;

            var lines = truck.order.lines;
            var all = new List<BoxData>();
            foreach (var line in lines) for (int i = 0; i < line.count; i++) all.Add(line.box);

            // Pick every box up for real, then hand them over at the truck.
            foreach (var pickup in pickups)
            {
                yield return GoTo(pickup.transform.position + new Vector3(0.9f, 0.1f, 0f));
                interactor.Scan();
                Assert.IsTrue(interactor.CurrentPrompt.Enabled || interactor.Current != null, mode.id + ": can reach " + pickup.name);
                pickup.Interact(interactor);
                status.Inventory.Clear();
                truck.Deliver(pickup.data);
            }

            float waited = 0f;
            while (!ui.puzzlePanel.activeSelf && waited < 6f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(ui.puzzlePanel.activeSelf, mode.id + ": the 2D screen opens");
            yield return null;

            var arrangement = PuzzleSolver.FindSafeArrangement(all, puzzle.rules);
            Assert.IsNotNull(arrangement, mode.id + " can be solved");
            Arrange(arrangement.Select(b => b.id).ToArray());
            screen.startButton.onClick.Invoke();
            yield return WaitForState(GameState.Results, 6f);

            var result = GameManager.Instance.LastResult;
            Assert.AreEqual(CombinationOutcome.Safe, result.Outcome);
            Assert.AreEqual(mode.id, result.ModeId);
            Assert.AreEqual(Mathf.RoundToInt(350 * mode.reward), result.BaseReward);
            Assert.GreaterOrEqual(result.Reward, result.BaseReward);
        }

        [UnityTest] public IEnumerator ClassicCanBeWon() { yield return WinThisMode(GameModes.Classic); }
        [UnityTest] public IEnumerator ExpressCanBeWon() { yield return WinThisMode(GameModes.Express); }
        [UnityTest] public IEnumerator GiantCanBeWon() { yield return WinThisMode(GameModes.Giant); }
        [UnityTest] public IEnumerator SurpriseCanBeWon() { yield return WinThisMode(GameModes.Surprise); }
        [UnityTest] public IEnumerator ChallengeCanBeWon() { yield return WinThisMode(GameModes.Challenge); }
        [UnityTest] public IEnumerator NightCanBeWon() { yield return WinThisMode(GameModes.Night); }

        [UnityTest]
        public IEnumerator RunningOutOfTimeLosesTheGame()
        {
            yield return Load(GameModes.Express);
            GameManager.Instance.timeLimitSeconds = 1.5f;
            yield return WaitForState(GameState.GameOver, 5f);
            Assert.AreEqual("death.timeout", GameManager.Instance.LastDeathCause.Key);
            Assert.IsTrue(ui.gameOverPanel.activeSelf);
            StringAssert.Contains(Localization.Get("death.timeout"), ui.deathCauseLabel.text);
        }

        [UnityTest]
        public IEnumerator DyingLosesEveryMode()
        {
            foreach (var mode in GameModes.All)
            {
                yield return Load(mode);
                GameManager.Instance.transitionDelay = 0.2f;
                status.Kill(DeathCause.Fall);
                yield return WaitForState(GameState.GameOver, 3f);
            }
        }

        [UnityTest]
        public IEnumerator TheClockCountsDownInTheHud()
        {
            yield return Load(GameModes.Express);
            var hud = Object.FindAnyObjectByType<TimerHud>();
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(hud.ShowsTimer);
            StringAssert.Contains("02:5", hud.timeLabel.text);
            StringAssert.Contains(GameModes.Express.DisplayName, hud.modeLabel.text);

            yield return Load(GameModes.Classic);
            hud = Object.FindAnyObjectByType<TimerHud>();
            yield return null;
            Assert.IsFalse(hud.ShowsTimer, "no clock without a time limit");
        }

        [UnityTest]
        public IEnumerator TheSelectedModeIsRememberedFromTheMenu()
        {
            var saved = GameModes.Selected;
            GameModes.Selected = GameModes.Giant;
            Assert.AreSame(GameModes.Surprise, GameModes.Step(1));
            GameModes.Selected = GameModes.Classic;
            Assert.AreSame(GameModes.Challenge, GameModes.Step(-1), "wraps round");
            Assert.AreEqual("challenge", PlayerPrefs.GetString("game.mode"));
            GameModes.Selected = saved;
            yield break;
        }
    }
}
