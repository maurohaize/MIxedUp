using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MixedUp.Tests
{
    /// <summary>Taking boxes from a teammate, resetting progress, the ice trail, river splashes and the frames of every effect.</summary>
    public class PendingFeaturesTests : SceneTestBase
    {
        TeammateDummy dummy;

        IEnumerator StandByTheDummy()
        {
            dummy = Object.FindAnyObjectByType<TeammateDummy>();
            var spot = new Vector3(-4f, 0.05f, -12f);
            dummy.transform.position = spot;
            yield return GoTo(spot + new Vector3(0f, 0f, -1.2f));
            yield return new WaitForSeconds(0.3f);
        }

        // ---------------------------------------------------------- take all boxes

        [UnityTest]
        public IEnumerator TheTakeKeyGrabsBothBoxesOfATeammateAtOnce()
        {
            yield return StandByTheDummy();
            var theirs = dummy.GetComponent<PlayerInventory>();
            theirs.TryAdd(BoxOf("hot"), out _);
            theirs.TryAdd(BoxOf("frozen"), out _);

            interactor.Scan();
            Assert.IsNotNull(interactor.TakeTarget, "the teammate can be emptied");
            yield return Tap(Key.F);

            Assert.AreEqual(2, status.Inventory.Count);
            Assert.AreEqual(0, theirs.Count);
        }

        [UnityTest]
        public IEnumerator TakingStopsWhenYourHandsAreFull()
        {
            yield return StandByTheDummy();
            var theirs = dummy.GetComponent<PlayerInventory>();
            theirs.TryAdd(BoxOf("hot"), out _);
            theirs.TryAdd(BoxOf("frozen"), out _);
            status.Inventory.TryAdd(BoxOf("normal"), out _);

            interactor.Scan();
            Assert.IsTrue(interactor.TryTake());
            Assert.AreEqual(2, status.Inventory.Count, "only one free hand");
            Assert.AreEqual(1, theirs.Count);

            interactor.Scan();
            Assert.IsNull(interactor.TakeTarget, "nothing more to take while full");
        }

        [UnityTest]
        public IEnumerator PassingBackAndForthIsNoLongerTheOnlyWay()
        {
            yield return StandByTheDummy();
            var theirs = dummy.GetComponent<PlayerInventory>();
            theirs.TryAdd(BoxOf("hot"), out _);
            theirs.TryAdd(BoxOf("toxic"), out _);

            interactor.Scan();
            interactor.TryTake();
            Assert.AreEqual(2, status.Inventory.Count);
            Assert.AreEqual(0, theirs.Count, "took both without the give-one-take-one loop");
        }

        // ------------------------------------------------------------ reset progress

        [UnityTest]
        public IEnumerator ResettingProgressNeedsConfirmationAndWipesMoneyAndCombinations()
        {
            Wallet.Add(500);
            var rule = puzzle.rules.Find(BoxOf("hot"), BoxOf("electric"));
            CombinationManual.Discover(rule);
            Assert.AreEqual(500, Wallet.Coins);
            Assert.IsTrue(CombinationManual.IsKnown(rule));

            yield return Tap(Key.Escape);
            ui.settingsButton.onClick.Invoke();
            yield return null;
            var button = ui.settingsPanel.resetProgressButton;
            var label = button.GetComponentInChildren<TMPro.TMP_Text>();
            Assert.AreEqual(Localization.Get("ui.reset_progress"), label.text);

            button.onClick.Invoke();
            Assert.AreEqual(500, Wallet.Coins, "one press only asks");
            Assert.AreEqual(Localization.Get("ui.reset_progress_confirm"), label.text);

            yield return new WaitForSecondsRealtime(3.4f);
            Assert.AreEqual(Localization.Get("ui.reset_progress"), label.text, "the question times out");
            Assert.AreEqual(500, Wallet.Coins);

            button.onClick.Invoke();
            button.onClick.Invoke();
            Assert.AreEqual(0, Wallet.Coins, "money back to zero");
            Assert.IsFalse(CombinationManual.IsKnown(rule), "combinations forgotten");
            Assert.AreEqual(Localization.Get("ui.reset_progress_done"), label.text);
        }

        // ------------------------------------------------------------- ice trail

        [UnityTest]
        public IEnumerator WadingWithAFrozenBoxLeavesAnIceTrailOthersCanCrossDry()
        {
            var emitter = player.GetComponent<IceTrailEmitter>();
            Assert.IsNotNull(emitter.slabPrefab);

            yield return GoTo(new Vector3(-2f, -0.35f, 5.6f));
            ClearLane(player.transform.position, Vector3.forward, 9f, 1.4f);
            status.Inventory.TryAdd(BoxOf("frozen"), out _);      // after clearing the lane, or it would disable the first slab
            yield return new WaitForSeconds(0.3f);
            Camera.main.GetComponent<ThirdPersonCamera>().enabled = false;
            Camera.main.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            Assert.IsTrue(status.Hazards.InWater, "standing in the river");

            SetKey(Key.W, true);
            yield return new WaitForSeconds(3.5f);
            SetKey(Key.W, false);

            Assert.GreaterOrEqual(emitter.PlacedCount, 2, "slabs of ice behind the wader");
            var slabs = Object.FindObjectsByType<IceSlab>();
            Assert.GreaterOrEqual(slabs.Length, 2);
            Assert.IsTrue(slabs.All(s => s.IsSolid), string.Join(", ", slabs.Select(s => s.name + " age " + s.Age.ToString("0.0") + " solid " + s.IsSolid)));

            // Now somebody carrying electricity walks over the ice.
            status.Inventory.Clear();
            status.Inventory.TryAdd(BoxOf("electric"), out _);
            var slab = slabs.OrderBy(s => s.transform.position.z).First();
            player.Teleport(slab.transform.position + new Vector3(0f, 0.25f, 0f));
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(player.IsGrounded, "the ice holds");
            Assert.IsFalse(status.Hazards.InWater, "dry on the ice");
            Assert.IsTrue(status.Hazards.OnIceSheet);
            Assert.AreEqual(status.maxHealth, status.Health, 0.01f, "no shock on the ice");
        }

        [UnityTest]
        public IEnumerator TheIceMeltsAwayAfterAWhile()
        {
            var emitter = player.GetComponent<IceTrailEmitter>();
            var slab = emitter.Place(new Vector3(-2f, 0f, 9f));
            slab.lifetime = 0.2f;
            slab.meltSeconds = 0.5f;
            yield return new WaitForSeconds(0.65f);
            Assert.IsFalse(slab.IsSolid, "mostly melted: no longer solid");
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(slab != null && slab.gameObject.activeSelf, "gone (recycled)");
        }

        // ---------------------------------------------------------------- water

        [UnityTest]
        public IEnumerator SteppingIntoTheRiverMakesASplashAndRipples()
        {
            var water = Object.FindAnyObjectByType<WaterEffects>();
            Assert.IsNotNull(water);
            yield return GoTo(new Vector3(-2f, 0.05f, 4.4f));
            yield return new WaitForSeconds(0.3f);
            int splashes = water.Splashes, ripples = water.Ripples;

            player.Teleport(new Vector3(-2f, -0.3f, 7f));
            yield return new WaitForSeconds(0.4f);
            Assert.Greater(water.Splashes, splashes, "a splash on entering");
            Assert.Greater(AudioManager.CountOf(SfxId.Splash), 0);

            Camera.main.GetComponent<ThirdPersonCamera>().enabled = false;
            Camera.main.transform.rotation = Quaternion.Euler(20f, 90f, 0f);
            SetKey(Key.W, true);
            yield return new WaitForSeconds(1.2f);
            SetKey(Key.W, false);
            Assert.Greater(water.Ripples, ripples + 2, "rings follow a wading player");
        }

        // ------------------------------------------------- frames of every effect

        [UnityTest]
        public IEnumerator EveryEffectHasItsOwnHandDrawnFrameFromTheOldProject()
        {
            foreach (var id in new[] { "hot", "electric", "frozen", "toxic" })
            {
                var effect = BoxOf(id).effects[0];
                Assert.IsNotNull(effect.screenOverlay, id + " has a frame");
                StringAssert.StartsWith("overlay_" + id, effect.screenOverlay.name.Replace("overlay_hot", "overlay_hot"));
            }

            status.Inventory.TryAdd(BoxOf("hot"), out _);
            status.Inventory.TryAdd(BoxOf("toxic"), out _);
            yield return new WaitForSeconds(0.4f);
            var hud = Object.FindAnyObjectByType<EffectHud>();
            var frames = hud.overlayContainer.GetComponentsInChildren<RawImage>(true).Select(r => r.texture.name).ToArray();
            CollectionAssert.Contains(frames, "overlay_hot");
            CollectionAssert.Contains(frames, "overlay_toxic");
        }

        // ------------------------------------------------------------------ fog

        [UnityTest]
        public IEnumerator TheLongestViewIsClearOfFogAndMist()
        {
            Assert.IsFalse(RenderSettings.fog);
            Assert.IsNull(GameObject.Find("EdgeMist"), "no wall of mist at the borders");
            yield break;
        }
    }
}
