using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>Plays the whole delivery: 3D to 2D, arranging, the trip, and every kind of ending.</summary>
    public class TruckPuzzleFlowTests : SceneTestBase
    {
        string Symbol(int junction) => screen.junctions[junction].marker.symbol.text;

        // ---------------------------------------------------------- 3D to 2D

        [UnityTest]
        public IEnumerator CompletingTheOrderSwitchesFromTheWorldToThe2DTruck()
        {
            yield return DeliverEverything();

            Assert.AreEqual(GameState.TruckPuzzle, GameManager.Instance.State);
            Assert.AreEqual(0f, Time.timeScale, "the 3D action is frozen");
            Assert.IsFalse(ui.hudRoot.activeSelf);
            Assert.AreEqual(6, puzzle.State.Count);
            Assert.AreEqual(PuzzlePhase.Arranging, puzzle.State.Phase);

            for (int i = 0; i < 8; i++)
                Assert.AreEqual(i < 6, screen.slots[i].gameObject.activeSelf, "slot " + i);
            Assert.AreEqual(BoxOf("hot").DisplayName, screen.slots[0].label.text, "boxes appear in delivery order");
            Assert.IsTrue(screen.startButton.gameObject.activeSelf);
            Assert.IsFalse(screen.travelRoot.activeSelf);
        }

        [UnityTest]
        public IEnumerator UnknownCombinationsShowAQuestionMarkUntilLearned()
        {
            yield return DeliverEverything();

            Assert.AreEqual("?", Symbol(0), "hot + electric is undiscovered");
            Assert.AreEqual("?", Symbol(1), "electric + frozen is undiscovered");
            Assert.AreEqual("OK", Symbol(3), "toxic + normal is known from the start");
            Assert.AreEqual("OK", Symbol(4), "normal + normal is known from the start");

            CombinationManual.Discover(puzzle.rules.Find(BoxOf("hot"), BoxOf("electric")));
            yield return null;
            Assert.AreEqual("*", Symbol(0), "now it shows the explosion marker");
        }

        [UnityTest]
        public IEnumerator ClickingTwoBoxesSwapsThem()
        {
            yield return DeliverEverything();
            var first = puzzle.State[0];
            var third = puzzle.State[2];

            screen.slots[0].button.onClick.Invoke();
            Assert.IsTrue(screen.slots[0].selection.enabled, "the first click selects");
            screen.slots[2].button.onClick.Invoke();

            Assert.AreSame(third, puzzle.State[0]);
            Assert.AreSame(first, puzzle.State[2]);
            Assert.AreEqual(third.DisplayName, screen.slots[0].label.text);
            Assert.IsFalse(screen.slots[0].selection.enabled);
        }

        [UnityTest]
        public IEnumerator ClickingTheSameBoxTwiceCancelsTheSelection()
        {
            yield return DeliverEverything();
            var first = puzzle.State[0];

            screen.slots[0].button.onClick.Invoke();
            screen.slots[0].button.onClick.Invoke();

            Assert.AreSame(first, puzzle.State[0]);
            Assert.IsFalse(screen.slots[0].selection.enabled);
        }

        // ---------------------------------------------------------- discovery

        [UnityTest]
        public IEnumerator ReadingANoteTeachesACombination()
        {
            Resolve();
            var rule = puzzle.rules.Find(BoxOf("hot"), BoxOf("electric"));
            Assert.IsFalse(CombinationManual.IsKnown(rule));

            var note = GameObject.Find("Note_HotElectric");
            Assert.IsNotNull(note, "the level has a note about hot + electric");
            yield return GoTo(note.transform.position + new Vector3(1.2f, 0.05f, 0f));
            interactor.Scan();
            Assert.AreSame(note.GetComponent<LoreNote>(), interactor.Current);
            Assert.AreEqual("prompt.read_note", interactor.CurrentPrompt.Key);

            yield return Tap(Key.E);

            Assert.IsTrue(CombinationManual.IsKnown(rule));
            var toast = Object.FindAnyObjectByType<ToastHud>();
            Assert.AreEqual(Localization.Get(rule.hintKey), toast.label.text);
        }

        [UnityTest]
        public IEnumerator TheManualListsRulesAndRevealsThemWhenDiscovered()
        {
            Resolve();
            var panel = ui.manualPanel;
            panel.Show();
            yield return null;

            var rows = panel.rowContainer.GetComponentsInChildren<ManualRowView>();
            Assert.AreEqual(11, rows.Length, "10 cross pairs plus hot + hot");

            var hotElectric = puzzle.rules.Find(BoxOf("hot"), BoxOf("electric"));
            int index = System.Array.IndexOf(puzzle.rules.rules.Where(r => !r.IsTrivial).ToArray(), hotElectric);
            Assert.AreEqual(Localization.Get("manual.unknown"), rows[index].hintLabel.text);
            Assert.AreEqual("?", rows[index].marker.symbol.text);

            CombinationManual.Discover(hotElectric);
            yield return null;
            Assert.AreEqual("*", rows[index].marker.symbol.text);
            StringAssert.Contains(Localization.Get(hotElectric.hintKey), rows[index].hintLabel.text);
        }

        [UnityTest]
        public IEnumerator ThePauseMenuOpensTheManual()
        {
            yield return Tap(Key.Escape);
            Assert.IsTrue(ui.pausePanel.activeSelf);

            ui.manualButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(ui.manualPanel.gameObject.activeSelf);

            ui.manualPanel.closeButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(ui.manualPanel.gameObject.activeSelf);
            yield return Tap(Key.Escape);
        }

        // ------------------------------------------------------------ endings

        [UnityTest]
        public IEnumerator ASafeArrangementDeliversAndPaysTheFullReward()
        {
            Resolve();
            puzzle.travelSeconds = 1.2f;
            puzzle.baseReward = 400;
            int walletBefore = Wallet.Coins;

            yield return DeliverEverything();
            Arrange(SafeOrder);
            screen.startButton.onClick.Invoke();
            yield return null;

            Assert.AreEqual(PuzzlePhase.Traveling, puzzle.State.Phase);
            Assert.IsTrue(screen.travelRoot.activeSelf, "the trip bar is shown");
            Assert.IsFalse(screen.startButton.gameObject.activeSelf);

            yield return WaitForState(GameState.Results, 5f);

            var result = GameManager.Instance.LastResult;
            Assert.AreEqual(CombinationOutcome.Safe, result.Outcome);
            Assert.AreEqual(400, result.Reward);
            Assert.AreEqual(6, result.Delivered);
            Assert.AreEqual(walletBefore + 400, Wallet.Coins);
            Assert.IsTrue(ui.resultsPanel.activeSelf);
            Assert.AreEqual("400", ui.resultsScreen.rewardLabel.text);
            Assert.AreEqual(Localization.Get("result.title.safe"), ui.resultsScreen.titleLabel.text);
            Assert.IsFalse(status.IsDead);
        }

        [UnityTest]
        public IEnumerator ADangerousPairStillDeliversButPaysLess()
        {
            Resolve();
            puzzle.travelSeconds = 1f;
            puzzle.baseReward = 400;

            yield return DeliverEverything();
            Arrange("hot", "toxic", "normal", "electric", "frozen", "normal");
            screen.startButton.onClick.Invoke();
            yield return WaitForState(GameState.Results, 5f);

            var result = GameManager.Instance.LastResult;
            Assert.AreEqual(CombinationOutcome.Danger, result.Outcome);
            Assert.AreEqual(1, result.DangerCount);
            Assert.AreEqual(340, result.Reward, "15% off for the accident");
            Assert.AreEqual(Localization.Get("result.title.danger"), ui.resultsScreen.titleLabel.text);
        }

        [UnityTest]
        public IEnumerator AnExplosionDestroysTheOrderAndHurtsEveryoneButASturdyPlayerSurvives()
        {
            Resolve();
            puzzle.explosionFuseSeconds = 0.4f;
            puzzle.travelSeconds = 10f;
            var dummy = Object.FindAnyObjectByType<TeammateDummy>().GetComponent<PlayerStatus>();

            yield return DeliverEverything();
            Assert.AreEqual(CombinationOutcome.Explosion, puzzle.State.PairOutcome(0), "delivery order puts hot next to electric");
            float healthBefore = status.Health;

            screen.startButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(screen.alertLabel.gameObject.activeSelf, "a lit fuse raises the alert");
            Assert.IsTrue(screen.junctions[0].fuseRoot.activeSelf);
            Assert.AreEqual("*", Symbol(0), "the reaction is revealed once the truck starts moving");

            yield return WaitForState(GameState.Results, 4f);

            var result = GameManager.Instance.LastResult;
            Assert.AreEqual(CombinationOutcome.Explosion, result.Outcome);
            Assert.AreEqual(0, result.Reward, "the order is destroyed");
            Assert.IsFalse(status.IsDead);
            Assert.AreEqual(healthBefore - puzzle.explosionDamage, status.Health, 0.5f);
            Assert.AreEqual(dummy.maxHealth - puzzle.explosionDamage, dummy.Health, 0.5f, "teammates are hurt too");
            Assert.AreEqual(Localization.Get("result.title.explosion"), ui.resultsScreen.titleLabel.text);
            Assert.IsTrue(CombinationManual.IsKnown(puzzle.rules.Find(BoxOf("hot"), BoxOf("electric"))), "learned the hard way");
        }

        [UnityTest]
        public IEnumerator AnExplosionKillsAPlayerWhoArrivedHurtAndShowsTheCause()
        {
            Resolve();
            puzzle.explosionFuseSeconds = 0.3f;
            status.Damage(55f, DeathCause.Fall);

            yield return DeliverEverything();
            screen.startButton.onClick.Invoke();
            yield return WaitForState(GameState.GameOver, 4f);

            Assert.IsTrue(status.IsDead);
            Assert.IsTrue(ui.gameOverPanel.activeSelf);
            Assert.IsFalse(ui.resultsPanel.activeSelf);
            string expected = Localization.Get("death.explosion", BoxOf("hot").DisplayName, BoxOf("electric").DisplayName);
            Assert.AreEqual(expected, ui.deathCauseLabel.text);
            Assert.IsNotEmpty(ui.quipLabel.text);
        }

        [UnityTest]
        public IEnumerator ALethalMixKillsEveryoneAtOnce()
        {
            Resolve();
            puzzle.gameOverFuseSeconds = 0.3f;
            var dummy = Object.FindAnyObjectByType<TeammateDummy>().GetComponent<PlayerStatus>();

            yield return DeliverEverything();
            Arrange("toxic", "electric", "normal", "frozen", "normal", "hot");
            screen.startButton.onClick.Invoke();
            yield return WaitForState(GameState.GameOver, 4f);

            Assert.IsTrue(status.IsDead);
            Assert.IsTrue(dummy.IsDead);
            string expected = Localization.Get("death.deadly_mix", BoxOf("toxic").DisplayName, BoxOf("electric").DisplayName);
            Assert.AreEqual(expected, ui.deathCauseLabel.text);
        }

        [UnityTest]
        public IEnumerator SeparatingTheBadPairBeforeTheFuseEndsSavesTheDelivery()
        {
            Resolve();
            puzzle.explosionFuseSeconds = 2.5f;
            puzzle.travelSeconds = 3f;

            yield return DeliverEverything();
            screen.startButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual(PuzzlePhase.Traveling, puzzle.State.Phase);
            Assert.IsTrue(puzzle.State.IsFuseLit(0));

            screen.slots[1].button.onClick.Invoke();
            screen.slots[5].button.onClick.Invoke();
            Assert.AreEqual(CombinationOutcome.Safe, puzzle.State.PairOutcome(0), "electric moved away from hot");
            Assert.IsFalse(puzzle.State.IsFuseLit(0));
            Assert.IsFalse(puzzle.State.AnyFuseLit(), "the whole truck is now safe");

            yield return WaitForState(GameState.Results, 6f);

            Assert.AreEqual(CombinationOutcome.Safe, GameManager.Instance.LastResult.Outcome);
            Assert.IsFalse(status.IsDead);
            Assert.AreEqual(status.maxHealth, status.Health, 0.01f, "nothing exploded");
        }

        [UnityTest]
        public IEnumerator RetryAfterTheResultsReloadsAFreshLevel()
        {
            Resolve();
            puzzle.travelSeconds = 0.8f;
            yield return DeliverEverything();
            Arrange(SafeOrder);
            screen.startButton.onClick.Invoke();
            yield return WaitForState(GameState.Results, 5f);

            ui.resultsRetryButton.onClick.Invoke();
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.3f);

            Assert.AreEqual(GameState.Playing, GameManager.Instance.State);
            Assert.AreEqual(1f, Time.timeScale);
            var freshTruck = Object.FindAnyObjectByType<Truck>();
            Assert.AreEqual(0, freshTruck.TotalDelivered);
            Assert.AreEqual(6, Object.FindObjectsByType<BoxPickup>().Length, "all boxes are back");
        }
    }
}
