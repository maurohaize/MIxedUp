using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>Plays the real prototype scene headlessly with simulated keyboard input.</summary>
    public class PrototypeSceneTests : SceneTestBase
    {
        float measured;

        // ------------------------------------------------------------ wiring

        [UnityTest]
        public IEnumerator SceneIsWiredTogether()
        {
            Assert.IsNotNull(GameManager.Instance, "GameManager");
            Assert.IsNotNull(ui, "UIManager");
            Assert.IsNotNull(truck, "Truck");
            Assert.IsNotNull(EventSystem.current, "EventSystem");

            var module = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            Assert.IsNotNull(module, "InputSystemUIInputModule");
            Assert.IsNotNull(module.actionsAsset, "UI input actions asset: buttons would not respond");

            Assert.AreEqual(6, pickups.Length);
            foreach (var pickup in pickups) Assert.IsNotNull(pickup.data, pickup.name + " has no BoxData");
            Assert.AreEqual(5, pickups.Select(p => p.data.id).Distinct().Count());
            Assert.AreEqual(6, truck.order.TotalBoxes);

            foreach (var pickup in pickups)
            {
                Assert.IsNotNull(pickup.visual.GetComponentInChildren<MeshRenderer>(), pickup.name + " shows its textured model");
                Assert.IsNull(pickup.GetComponentInChildren<SpriteRenderer>(true), pickup.name + " has no floating icon");
            }
            Assert.IsNotNull(RenderSettings.skybox, "custom sky");
            Assert.IsTrue(RenderSettings.fog, "distance fog");
            Assert.IsNotNull(Object.FindAnyObjectByType<UnityEngine.Rendering.Volume>(), "post-processing volume");

            Assert.AreEqual(1, PlayerRegistry.All.Count, "only the local player has a controller; the teammate is a dummy");
            Assert.IsNotNull(Object.FindAnyObjectByType<TeammateDummy>());

            var cam = Camera.main.GetComponent<ThirdPersonCamera>();
            Assert.IsNotNull(cam);
            Assert.AreSame(player.transform, cam.target);

            var inventoryHud = Object.FindAnyObjectByType<InventoryHud>();
            Assert.IsTrue(inventoryHud.slots[0].root.activeSelf);
            Assert.IsTrue(inventoryHud.slots[1].root.activeSelf);
            Assert.IsFalse(inventoryHud.slots[2].root.activeSelf, "capacity is 2, so only 2 slots are shown");

            Assert.IsFalse(ui.pausePanel.activeSelf);
            Assert.IsFalse(ui.gameOverPanel.activeSelf);
            Assert.IsFalse(ui.puzzlePanel.activeSelf);
            Assert.AreEqual(GameState.Playing, GameManager.Instance.State);
            yield break;
        }

        // ----------------------------------------------------------- movement

        [UnityTest]
        public IEnumerator WalkingForwardMovesThePlayerNorth()
        {
            var start = player.transform.position;
            SetKey(Key.W, true);
            yield return new WaitForSeconds(1f);
            SetKey(Key.W, false);

            var delta = player.transform.position - start;
            Assert.Greater(delta.z, 3f, "moved " + delta);
            Assert.Less(delta.z, 6f, "walked too fast: " + delta);
            Assert.Less(Mathf.Abs(delta.x), 1f);
            Assert.Less(Mathf.Abs(delta.y), 0.3f, "should stay on the ground");
        }

        [UnityTest]
        public IEnumerator SprintingIsFasterThanWalking()
        {
            yield return GoTo(new Vector3(-5f, 0.05f, -12f));
            var start = player.transform.position;
            SetKey(Key.W, true);
            yield return new WaitForSeconds(1f);
            SetKey(Key.W, false);
            float walked = player.transform.position.z - start.z;

            yield return new WaitForSeconds(0.5f);
            yield return GoTo(new Vector3(-5f, 0.05f, -12f));
            start = player.transform.position;
            SetKey(Key.W, true);
            SetKey(Key.LeftShift, true);
            yield return new WaitForSeconds(1f);
            SetKey(Key.W, false);
            SetKey(Key.LeftShift, false);
            float ran = player.transform.position.z - start.z;

            Assert.Greater(ran, walked * 1.3f, "walked " + walked + " m, ran " + ran + " m");
        }

        [UnityTest]
        public IEnumerator JumpingLiftsThePlayerAndTheyLandAgain()
        {
            yield return GoTo(new Vector3(-5f, 0.05f, -12f));
            float groundY = player.transform.position.y;
            float peak = groundY;
            bool wasAirborne = false;

            yield return Tap(Key.Space);
            for (float t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                peak = Mathf.Max(peak, player.transform.position.y);
                wasAirborne |= !player.IsGrounded;
                yield return null;
            }

            Assert.Greater(peak - groundY, 0.8f, "peak height " + (peak - groundY));
            Assert.Less(peak - groundY, 2f);
            Assert.IsTrue(wasAirborne);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(player.IsGrounded);
            Assert.Less(Mathf.Abs(player.transform.position.y - groundY), 0.2f);
        }

        // -------------------------------------------------- the box core loop

        [UnityTest]
        public IEnumerator PickingUpTheHotBoxPutsItInTheHudAndStartsHurting()
        {
            var hot = pickups.First(p => p.data.id == "hot");
            yield return GoTo(hot.transform.position + new Vector3(1f, 0.05f, 0f));
            interactor.Scan();

            Assert.AreSame(hot, interactor.Current as BoxPickup, "the hot box should be the interaction target");
            Assert.AreEqual("prompt.pickup", interactor.CurrentPrompt.Key);

            yield return Tap(Key.E);
            yield return null;

            Assert.AreEqual(1, status.Inventory.Count);
            Assert.AreSame(hot.data, status.Inventory.Slots[0].box);
            Assert.IsFalse(hot.gameObject.activeSelf, "the physical box disappears");

            yield return new WaitForSeconds(1.5f);
            Assert.Less(status.Health, status.maxHealth, "HOT must start burning");

            var inventoryHud = Object.FindAnyObjectByType<InventoryHud>();
            Assert.IsTrue(inventoryHud.slots[0].icon.enabled, "inventory icon shown");
            Assert.AreEqual(hot.data.DisplayName, inventoryHud.slots[0].label.text);

            var effectHud = Object.FindAnyObjectByType<EffectHud>();
            Assert.IsTrue(effectHud.banners[0].root.activeSelf, "effect banner shown");
            Assert.AreEqual(Localization.Get("effect.heat"), effectHud.banners[0].label.text);
        }

        [UnityTest]
        public IEnumerator AHotBoxCanBePassedToTheTeammateWhoThenSuffersIt()
        {
            status.Inventory.TryAdd(BoxOf("hot"), out _);
            var dummy = Object.FindAnyObjectByType<TeammateDummy>();
            var dummyStatus = dummy.GetComponent<PlayerStatus>();

            yield return GoTo(dummy.transform.position + new Vector3(1.6f, 0.05f, 0f));
            interactor.Scan();

            Assert.AreSame(dummy.GetComponent<PlayerPassTarget>(), interactor.Current);
            Assert.AreEqual("prompt.pass", interactor.CurrentPrompt.Key);
            Assert.IsTrue(interactor.CurrentPrompt.Enabled);

            yield return Tap(Key.E);
            yield return null;

            Assert.AreEqual(0, status.Inventory.Count, "giver is empty-handed");
            Assert.AreEqual(1, dummyStatus.Inventory.Count, "teammate carries it now");

            float giverHealthAfterPass = status.Health;
            yield return new WaitForSeconds(1.5f);
            Assert.Less(dummyStatus.Health, dummyStatus.maxHealth, "the teammate takes the heat");
            Assert.GreaterOrEqual(status.Health, giverHealthAfterPass, "the giver stops taking damage once the box is gone");
            Assert.Greater(status.Health, status.maxHealth - 2f, "the giver only burned for the moment before passing");

            var effectHud = Object.FindAnyObjectByType<EffectHud>();
            Assert.IsFalse(effectHud.banners[0].root.activeSelf, "the giver's banner is gone");
        }

        [UnityTest]
        public IEnumerator DeliveringEveryBoxCompletesTheOrderAndOpensThePuzzleScreen()
        {
            var lines = truck.order.lines;
            var queue = lines.SelectMany(l => Enumerable.Repeat(l.box, l.count)).ToList();
            yield return GoTo(new Vector3(0f, 0.05f, -31.5f));

            int safety = 0;
            while (!truck.IsComplete && safety++ < 40)
            {
                while (status.Inventory.HasSpace && queue.Count > 0)
                {
                    status.Inventory.TryAdd(queue[0], out _);
                    queue.RemoveAt(0);
                }

                interactor.Scan();
                Assert.AreSame(truck, interactor.Current, "the truck should be the interaction target");
                Assert.AreEqual("prompt.deliver", interactor.CurrentPrompt.Key);
                yield return Tap(Key.E);
            }

            Assert.IsTrue(truck.IsComplete);
            Assert.AreEqual(6, truck.TotalDelivered);
            Assert.AreEqual(0, status.Inventory.Count);
            Assert.AreEqual(6, truck.bedAnchor.childCount, "one cube per delivered box on the truck bed");

            yield return new WaitForSecondsRealtime(GameManager.Instance.transitionDelay + 1.6f);
            Assert.AreEqual(GameState.TruckPuzzle, GameManager.Instance.State);
            Assert.IsTrue(ui.puzzlePanel.activeSelf, "the 2D truck screen opens after the paper wipe");
            Assert.IsFalse(ui.hudRoot.activeSelf, "the 3D HUD is hidden while the puzzle is open");
            Assert.AreEqual(0f, Time.timeScale);
        }

        // ------------------------------------------------------------ hazards

        [UnityTest]
        public IEnumerator ElectricBoxIsSafeOnTheBridgeButDeadlyInTheRiver()
        {
            status.Inventory.TryAdd(BoxOf("electric"), out _);

            yield return GoTo(new Vector3(-27f, 0.5f, 9f));
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(status.Hazards.InWater, "the bridge deck is above the water");
            Assert.IsFalse(status.IsDead);
            Assert.AreEqual(status.maxHealth, status.Health, 0.001f);

            yield return GoTo(new Vector3(0f, 0.3f, 9f));
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(status.Hazards.InWater, "standing in the river");

            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(status.IsDead, "electric box + water = death");
            Assert.AreEqual("death.electric_water", status.LastCause.Key);

            yield return new WaitForSecondsRealtime(GameManager.Instance.transitionDelay + 0.6f);
            Assert.AreEqual(GameState.GameOver, GameManager.Instance.State);
            Assert.IsTrue(ui.gameOverPanel.activeSelf);
            Assert.AreEqual(Localization.Get("death.electric_water"), ui.deathCauseLabel.text);
        }

        [UnityTest]
        public IEnumerator WadingWithoutAnElectricBoxIsHarmlessButSlow()
        {
            yield return GoTo(new Vector3(0f, 0.3f, 9f));
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(status.Hazards.InWater);

            var start = player.transform.position;
            SetKey(Key.W, true);
            yield return new WaitForSeconds(1f);
            SetKey(Key.W, false);

            Assert.IsFalse(status.IsDead);
            Assert.AreEqual(status.maxHealth, status.Health, 0.001f);
            Assert.Less(player.transform.position.z - start.z, 3.6f, "water slows the player down");
        }

        [UnityTest]
        public IEnumerator FallingFromAHeightHurtsAndFromTooHighKills()
        {
            yield return GoTo(new Vector3(-5f, 8f, -12f));
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(status.IsDead);
            Assert.Less(status.Health, status.maxHealth, "an 8 m fall should hurt");

            yield return GoTo(new Vector3(-5f, 20f, -12f));
            yield return new WaitForSeconds(2f);
            Assert.IsTrue(status.IsDead);
            Assert.AreEqual("death.fall", status.LastCause.Key);
        }

        [UnityTest]
        public IEnumerator FrozenBoxMakesThePlayerSlideFarPastTheirStopPoint()
        {
            yield return SlideDistance(withFrozenBox: false);
            float normal = measured;

            status.Inventory.Clear();
            yield return SlideDistance(withFrozenBox: true);
            float icy = measured;

            Assert.Greater(icy, 1f, "frozen player barely slid: " + icy);
            Assert.Greater(icy, normal * 3f, "normal slide " + normal + " m vs frozen " + icy + " m");
        }

        IEnumerator SlideDistance(bool withFrozenBox)
        {
            status.Inventory.Clear();
            if (withFrozenBox) status.Inventory.TryAdd(BoxOf("frozen"), out _);

            yield return GoTo(new Vector3(-5f, 0.05f, -12f));
            ClearLane(player.transform.position, Vector3.forward, 14f);
            yield return new WaitForSeconds(0.4f);

            SetKey(Key.W, true);
            yield return new WaitForSeconds(1f);
            var atRelease = player.transform.position;
            SetKey(Key.W, false);
            yield return new WaitForSeconds(2.5f);
            measured = Vector3.Distance(atRelease, player.transform.position);
        }

        [UnityTest]
        public IEnumerator TheIceRampMakesAnyoneSlideButFlatGroundDoesNot()
        {
            yield return GoTo(new Vector3(-5f, 0.05f, -12f));
            yield return new WaitForSeconds(0.8f);
            var flatStart = player.transform.position;
            yield return new WaitForSeconds(1f);
            Assert.Less(Vector3.Distance(flatStart, player.transform.position), 0.2f, "idle on flat ground must not move");

            yield return GoTo(new Vector3(-32f, 2.9f, -18f));
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(status.Hazards.OnSlippery, "the ramp is a slippery zone");
            float startZ = player.transform.position.z;
            yield return new WaitForSeconds(1.5f);

            Assert.Less(player.transform.position.z, startZ - 1f, "should slide down the ramp");
        }

        // ------------------------------------------------------ pause and UI

        [UnityTest]
        public IEnumerator EscapeTogglesPauseAndFreezesTime()
        {
            yield return Tap(Key.Escape);
            Assert.AreEqual(GameState.Paused, GameManager.Instance.State);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(ui.pausePanel.activeSelf);

            yield return Tap(Key.Escape);
            Assert.AreEqual(GameState.Playing, GameManager.Instance.State);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(ui.pausePanel.activeSelf);
        }

        [UnityTest]
        public IEnumerator LanguageButtonsRetranslateTheHud()
        {
            var orderTitle = Object.FindObjectsByType<LocalizedText>().First(t => t.key == "ui.order");
            var label = orderTitle.GetComponent<TMP_Text>();

            yield return Tap(Key.Escape);
            ui.settingsButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(ui.settingsPanel.IsOpen);

            ui.settingsPanel.basqueButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(Language.Basque, Localization.Current);
            Assert.AreEqual("ESKAERA", label.text);

            ui.settingsPanel.englishButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual("ORDER", label.text);

            ui.settingsPanel.spanishButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual("PEDIDO", label.text);
        }

        [UnityTest]
        public IEnumerator PausedGameIgnoresMovementInput()
        {
            yield return Tap(Key.Escape);
            var start = player.transform.position;
            SetKey(Key.W, true);
            yield return new WaitForSecondsRealtime(0.5f);
            SetKey(Key.W, false);

            Assert.Less(Vector3.Distance(start, player.transform.position), 0.1f);
            yield return Tap(Key.Escape);
        }
    }
}
