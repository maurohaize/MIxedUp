using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>Right click hugs, a shove costs a little health, and the prompt says that a hug heals.</summary>
    public class HugAndShoveTests : SceneTestBase
    {
        TeammateDummy dummy;

        IEnumerator StandNextToTheDummy(float distance = 1.3f)
        {
            dummy = Object.FindAnyObjectByType<TeammateDummy>();
            var spot = new Vector3(-4f, 0.05f, -12f);
            dummy.transform.position = spot;
            yield return GoTo(spot + new Vector3(0f, 0f, -distance));
            player.visual.rotation = Quaternion.LookRotation(Vector3.forward);
            yield return null;
        }

        [Test]
        public void TheRightMouseButtonIsTheHugButton()
        {
            Assert.AreEqual("<Mouse>/rightButton", GameInput.Hug.bindings[0].effectivePath);
            Assert.AreEqual(Localization.Get("input.right_click"), GameInput.Label(GameInput.Hug));
            Assert.AreEqual("<Mouse>/leftButton", GameInput.Push.bindings[1].effectivePath, "left click still shoves");
        }

        [UnityTest]
        public IEnumerator AShoveCostsBetweenTwoAndFiveHealth()
        {
            yield return StandNextToTheDummy();
            var victim = dummy.GetComponent<PlayerStatus>();
            float before = victim.Health;

            Assert.IsNotNull(player.GetComponent<PlayerPush>().TryPush());

            float lost = before - victim.Health;
            Assert.GreaterOrEqual(lost, 2f - 0.001f);
            Assert.LessOrEqual(lost, 5f + 0.001f);
        }

        [UnityTest]
        public IEnumerator AShoveNeverKillsAnAlreadyDeadPlayer()
        {
            yield return StandNextToTheDummy();
            var victim = dummy.GetComponent<PlayerStatus>();
            victim.Kill(DeathCause.Fall);
            float health = victim.Health;
            player.GetComponent<PlayerPush>().TryPush();
            Assert.AreEqual(health, victim.Health, 0.001f);
        }

        [UnityTest]
        public IEnumerator ThePromptOffersAHugNearATeammateAndSaysItHeals()
        {
            yield return StandNextToTheDummy();
            var hug = player.GetComponent<PlayerHug>();
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(hug.HugAvailable, "a teammate is close enough to hug");
            Assert.IsFalse(hug.WantsHealing, "at full health a hug is just a hug");

            status.Damage(30f, DeathCause.Fall);
            Assert.IsTrue(hug.WantsHealing, "hurt: the prompt says that the hug heals");
        }

        [UnityTest]
        public IEnumerator FarFromEveryoneThereIsNoHugToOffer()
        {
            yield return StandNextToTheDummy(8f);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(player.GetComponent<PlayerHug>().HugAvailable);
        }

        [UnityTest]
        public IEnumerator AHugRaisesTheOtherCharactersArmsToo()
        {
            yield return StandNextToTheDummy();
            Assert.IsTrue(player.GetComponent<PlayerHug>().TryHug());
            var animator = dummy.GetComponent<PlayerAnimator>();
            Assert.IsNotNull(animator);
            yield return new WaitForSeconds(0.6f);
            // The embrace is running on the teammate's animator (it has no hug component of its own).
            Assert.IsTrue(player.GetComponent<PlayerHug>().IsHugging);
        }
    }
}
