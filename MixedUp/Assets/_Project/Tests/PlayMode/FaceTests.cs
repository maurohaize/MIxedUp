using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>The face changes with what happens to the character.</summary>
    public class FaceTests : SceneTestBase
    {
        CharacterFace Face => player.GetComponent<CharacterFace>();

        static Vector4 CellOf(CharacterFace face)
        {
            var block = new MaterialPropertyBlock();
            face.faceRenderer.GetPropertyBlock(block);
            return block.GetVector("_Cell");
        }

        [UnityTest]
        public IEnumerator EveryCharacterHasAFaceThatStartsNeutral()
        {
            Assert.IsNotNull(Face);
            Assert.IsNotNull(Face.faceRenderer);
            Assert.AreEqual("MixedUp/FaceAtlas", Face.faceRenderer.sharedMaterial.shader.name);
            Assert.IsNotNull(Object.FindAnyObjectByType<TeammateDummy>().GetComponent<CharacterFace>());
            yield return new WaitForSeconds(0.2f);
            Assert.That(Face.Current, Is.EqualTo(FaceExpression.Neutral).Or.EqualTo(FaceExpression.Blink));
        }

        [UnityTest]
        public IEnumerator PainMakesAGrimaceAndThenItPasses()
        {
            status.Damage(20f, DeathCause.Fall);
            yield return null;
            Assert.AreEqual(FaceExpression.Hurt, Face.Current);
            Assert.AreEqual((float)FaceExpression.Hurt % 4, CellOf(Face).x, "the right cell of the atlas");

            yield return new WaitForSeconds(1.6f);
            Assert.AreNotEqual(FaceExpression.Hurt, Face.Current);
        }

        [UnityTest]
        public IEnumerator ShocksShowStarsInTheEyes()
        {
            status.RaiseShock();
            yield return null;
            Assert.AreEqual(FaceExpression.Shocked, Face.Current);
        }

        [UnityTest]
        public IEnumerator CarryingSomethingHotFrozenOrToxicShowsOnTheFace()
        {
            status.Inventory.TryAdd(BoxOf("hot"), out _);
            yield return null;
            Assert.AreEqual(FaceExpression.Hot, Face.Current);

            status.Inventory.Clear();
            status.Inventory.TryAdd(BoxOf("frozen"), out _);
            yield return null;
            Assert.AreEqual(FaceExpression.Cold, Face.Current);

            status.Inventory.Clear();
            status.Inventory.TryAdd(BoxOf("toxic"), out _);
            status.Inventory.Tick(40f);           // long enough for the fumes to get to you
            yield return null;
            Assert.AreEqual(FaceExpression.Sick, Face.Current);
        }

        [UnityTest]
        public IEnumerator HuggingMeansHeartsInTheEyes()
        {
            var dummy = Object.FindAnyObjectByType<TeammateDummy>();
            dummy.transform.position = new Vector3(-4f, 0.05f, -12f);
            yield return GoTo(new Vector3(-4f, 0.05f, -13.2f));
            Assert.IsTrue(player.GetComponent<PlayerHug>().TryHug());
            yield return null;
            Assert.AreEqual(FaceExpression.Love, Face.Current);
            Assert.AreEqual(FaceExpression.Love, dummy.GetComponent<CharacterFace>().Current, "the hugged one too");
        }

        [UnityTest]
        public IEnumerator ShovingGivesAnEffortFaceAndASurprisedOne()
        {
            var dummy = Object.FindAnyObjectByType<TeammateDummy>();
            dummy.transform.position = new Vector3(-4f, 0.05f, -12f);
            yield return GoTo(new Vector3(-4f, 0.05f, -13.2f));
            player.visual.rotation = Quaternion.LookRotation(Vector3.forward);
            Assert.IsNotNull(player.GetComponent<PlayerPush>().TryPush());
            yield return null;
            Assert.AreEqual(FaceExpression.Effort, Face.Current);
            Assert.AreEqual(FaceExpression.Surprised, dummy.GetComponent<CharacterFace>().Current);
        }

        [UnityTest]
        public IEnumerator DeathShowsCrossedEyes()
        {
            status.Kill(DeathCause.Fall);
            yield return null;
            Assert.AreEqual(FaceExpression.Dead, Face.Current);
        }

        [UnityTest]
        public IEnumerator TheCharacterBlinksNowAndThen()
        {
            bool blinked = false;
            for (float until = Time.time + 7f; Time.time < until && !blinked;)
            {
                blinked = Face.Current == FaceExpression.Blink;
                yield return null;
            }
            Assert.IsTrue(blinked);
        }

        [UnityTest]
        public IEnumerator DeliveringOrPassingABoxMakesThemSmile()
        {
            var dummy = Object.FindAnyObjectByType<TeammateDummy>();
            status.Inventory.TryAdd(BoxOf("normal"), out _);
            status.Inventory.TryTransfer(0, dummy.GetComponent<PlayerInventory>());
            yield return null;
            Assert.AreEqual(FaceExpression.Happy, Face.Current);
            Assert.AreEqual(FaceExpression.Happy, dummy.GetComponent<CharacterFace>().Current);
        }
    }
}
