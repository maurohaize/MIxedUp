using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>The cartoon character from the reference drawing and the boxes it holds in its hands.</summary>
    public class CharacterTests : SceneTestBase
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        int savedSkin, savedClothes;

        [UnitySetUp]
        public IEnumerator RememberCustomisation()
        {
            savedSkin = CharacterCustomization.SkinIndex;
            savedClothes = CharacterCustomization.ClothesIndex;
            yield break;
        }

        [UnityTearDown]
        public IEnumerator RestoreCustomisation()
        {
            CharacterCustomization.SkinIndex = savedSkin;
            CharacterCustomization.ClothesIndex = savedClothes;
            yield break;
        }

        static Transform Part(Component character, string name) =>
            character.GetComponentsInChildren<Transform>(true).First(t => t.name == name);

        /// <summary>The renderer's tint as 0xRRGGBB.</summary>
        static int HexOf(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Color32 c = block.GetColor(BaseColorId);
            return (c.r << 16) | (c.g << 8) | c.b;
        }

        // ------------------------------------------------------------ the model

        [UnityTest]
        public IEnumerator CharacterHasTheParts_OfTheDrawing()
        {
            foreach (var part in new[] { "Dress", "Head", "Face", "HandLeft", "HandRight", "BootLeft", "BootRight", "CarryAnchor" })
                Assert.IsNotNull(Part(player, part), part);

            var face = Part(player, "Face").GetComponent<MeshRenderer>();
            Assert.IsNotNull(face.sharedMaterial.mainTexture, "the face texture (eyes, eyebrows) must be assigned");

            // Thick ink outline: an inverted-hull copy on every body part except the face.
            foreach (var part in new[] { "Dress", "Head", "HandLeft", "HandRight", "BootLeft", "BootRight" })
            {
                var hull = Part(player, part).Find("Outline");
                Assert.IsNotNull(hull, part + " outline");
                Assert.AreEqual("MixedUp/Outline", hull.GetComponent<MeshRenderer>().sharedMaterial.shader.name);
            }

            // Big head, small body: the head is about as tall as the dress.
            var head = Part(player, "Head").GetComponent<Renderer>().bounds;
            var dress = Part(player, "Dress").GetComponent<Renderer>().bounds;
            Assert.Greater(head.size.y, dress.size.y * 0.95f, "the head is as big as the body, like in the drawing");
            Assert.Less(head.min.y, dress.max.y + 0.05f, "the head sits right on the dress");
            yield break;
        }

        [UnityTest]
        public IEnumerator LocalPlayerStartsWithTheColoursOfTheDrawing()
        {
            CharacterCustomization.SkinIndex = CharacterCustomization.DefaultSkin;
            CharacterCustomization.ClothesIndex = CharacterCustomization.DefaultClothes;
            yield return null;

            Assert.AreEqual(0xaa5b36, HexOf(Part(player, "Head").GetComponent<Renderer>()), "skin");
            Assert.AreEqual(0x2a7b9b, HexOf(Part(player, "Dress").GetComponent<Renderer>()), "dress");
            yield break;
        }

        [UnityTest]
        public IEnumerator ChangingTheCustomisationRecoloursTheCharacterAtOnce()
        {
            CharacterCustomization.SkinIndex = 4;
            CharacterCustomization.ClothesIndex = 8;
            yield return null;

            Assert.AreEqual(0xfad7b1, HexOf(Part(player, "HandLeft").GetComponent<Renderer>()), "skin tone 5 is the palest");
            Assert.AreEqual(0xc6041f, HexOf(Part(player, "Dress").GetComponent<Renderer>()), "clothes colour 9 is red");
            Assert.AreEqual(0xfad7b1, HexOf(Part(player, "Head").GetComponent<Renderer>()), "the head follows the hands");

            var teammate = Object.FindAnyObjectByType<TeammateDummy>();
            Assert.AreNotEqual(0xc6041f, HexOf(Part(teammate, "Dress").GetComponent<Renderer>()), "other players keep their own colours");
            yield break;
        }

        // ---------------------------------------------------------- carrying

        [UnityTest]
        public IEnumerator PickedUpBoxIsVisibleInTheHands()
        {
            var carry = player.GetComponent<CarriedBoxesView>();
            Assert.AreEqual(0, carry.VisibleCount);

            status.Inventory.TryAdd(BoxOf("hot"), out _);
            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(1, carry.VisibleCount);
            var held = carry.BoxRoot(0);
            var renderer = held.GetComponentInChildren<MeshRenderer>();
            Assert.IsNotNull(renderer, "the carried box is the textured model");
            Assert.IsNotNull(renderer.sharedMaterial.GetTexture("_BaseMap"), "with its painted texture");
            Assert.IsNull(held.GetComponentInChildren<Collider>(), "a carried box must not collide with its carrier");

            Assert.IsTrue(renderer.isVisible || renderer.enabled);
            Assert.That(held.localScale.x, Is.EqualTo(carry.boxScale).Within(0.02f), "popped in to its held size");

            // Both hands move to the sides of the box.
            var left = Part(player, "HandLeft");
            var right = Part(player, "HandRight");
            Assert.Less(Vector3.Distance(left.position, held.position), 0.75f, "left hand next to the box");
            Assert.Less(Vector3.Distance(right.position, held.position), 0.75f, "right hand next to the box");
            Assert.Greater(Vector3.Distance(left.position, right.position), carry.boxScale, "hands are on opposite sides of it");
            yield break;
        }

        [UnityTest]
        public IEnumerator SecondBoxIsStackedOnTopOfTheFirst()
        {
            var carry = player.GetComponent<CarriedBoxesView>();
            status.Inventory.TryAdd(BoxOf("normal"), out _);
            status.Inventory.TryAdd(BoxOf("frozen"), out _);
            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(2, carry.VisibleCount);
            var lower = carry.BoxRoot(0);
            var upper = carry.BoxRoot(1);
            Assert.AreEqual("Carried_normal", lower.name);
            Assert.AreEqual("Carried_frozen", upper.name);

            float step = upper.position.y - lower.position.y;
            Assert.That(step, Is.InRange(carry.boxScale * 0.95f, carry.boxScale * 1.2f), "one box height above the other");
            Assert.That(Vector2.Distance(new Vector2(upper.position.x, upper.position.z), new Vector2(lower.position.x, lower.position.z)),
                Is.LessThan(0.1f), "directly on top");

            // Releasing the first one drops the second to the bottom of the stack.
            status.Inventory.RemoveAt(0);
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(1, carry.VisibleCount);
            Assert.AreEqual("Carried_frozen", carry.BoxRoot(0).name);
            yield break;
        }

        [UnityTest]
        public IEnumerator PassingABoxMovesItFromOnePairOfHandsToTheOther()
        {
            var mine = player.GetComponent<CarriedBoxesView>();
            var teammate = Object.FindAnyObjectByType<TeammateDummy>();
            var theirs = teammate.GetComponent<CarriedBoxesView>();

            status.Inventory.TryAdd(BoxOf("toxic"), out _);
            yield return null;
            Assert.AreEqual(1, mine.VisibleCount);
            Assert.AreEqual(0, theirs.VisibleCount);

            Assert.IsTrue(status.Inventory.TryTransfer(0, teammate.GetComponent<PlayerInventory>()));
            yield return null;
            Assert.AreEqual(0, mine.VisibleCount);
            Assert.AreEqual(1, theirs.VisibleCount);
            yield break;
        }

        [UnityTest]
        public IEnumerator DeliveringTheBoxesEmptiesTheHands()
        {
            var carry = player.GetComponent<CarriedBoxesView>();
            status.Inventory.TryAdd(BoxOf("normal"), out _);
            yield return null;
            Assert.AreEqual(1, carry.VisibleCount);

            status.Inventory.Clear();
            yield return null;
            Assert.AreEqual(0, carry.VisibleCount);
            Assert.AreEqual(0, carry.anchor.childCount, "no leftover models");
        }
    }
}
