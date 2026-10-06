using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>The cartoon death animations: each cause of death looks different, and reviving cleans everything up.</summary>
    public class DeathEffectsTests : SceneTestBase
    {
        DeathEffects Effects => player.GetComponent<DeathEffects>();

        [UnitySetUp]
        public IEnumerator KeepPlaying()
        {
            // The game-over screen would freeze time 1.4 s after a death; these tests watch the animation itself.
            GameManager.Instance.transitionDelay = 999f;
            yield break;
        }

        [UnityTest]
        public IEnumerator EveryCharacterGetsDeathEffectsWithoutSceneSetup()
        {
            Assert.IsNotNull(Effects);
            Assert.IsNotNull(Object.FindAnyObjectByType<TeammateDummy>().GetComponent<DeathEffects>());
            yield break;
        }

        [UnityTest]
        public IEnumerator ElectrocutionFlickersASkeletonAndZapsLightning()
        {
            status.Kill(DeathCause.ElectricWater);
            yield return null;
            Assert.AreEqual("Electric", Effects.KindName);
            Assert.GreaterOrEqual(Effects.BoltCount, 5, "lightning bolts crawl over the body");

            bool sawSkeleton = false, sawBody = false;
            var skin = player.GetComponentsInChildren<Transform>(true);
            Renderer head = null;
            foreach (var t in skin) if (t.name == "Head") head = t.GetComponent<Renderer>();
            for (float until = Time.time + 0.7f; Time.time < until;)
            {
                if (Effects.SkeletonVisible) sawSkeleton = true;
                if (head.enabled) sawBody = true;
                yield return null;
            }
            Assert.IsTrue(sawSkeleton, "the bones show through");
            Assert.IsTrue(sawBody, "and it flickers back to the body");

            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(Effects.SkeletonVisible, "after the shock only a charred body remains");
            Assert.IsTrue(head.enabled);
        }

        [UnityTest]
        public IEnumerator EachCauseGetsItsOwnAnimation()
        {
            var cases = new (DeathCause cause, string kind)[]
            {
                (DeathCause.Heat, "Burn"), (DeathCause.Burn, "Burn"), (DeathCause.Fall, "Squash"),
                (DeathCause.Sweeper, "Squash"), (DeathCause.Void, "Void")
            };
            foreach (var (cause, kind) in cases)
            {
                status.Revive();
                yield return null;
                status.Kill(cause);
                yield return null;
                Assert.AreEqual(kind, Effects.KindName, cause.Key);
                yield return new WaitForSeconds(0.3f);
                Assert.IsTrue(Effects.IsPlaying);
            }
        }

        [UnityTest]
        public IEnumerator FallingSquashesTheBodyFlat()
        {
            var body = player.GetComponent<PlayerAnimator>().body;
            status.Kill(DeathCause.Fall);
            yield return new WaitForSeconds(0.4f);
            Assert.Less(body.localScale.y, 0.6f, "flattened");
            Assert.Greater(body.localScale.x, 1.2f, "and spread out");
        }

        [UnityTest]
        public IEnumerator RevivingRestoresTheBodyAndRemovesTheEffects()
        {
            var body = player.GetComponent<PlayerAnimator>().body;
            status.Kill(DeathCause.ElectricWater);
            yield return new WaitForSeconds(0.5f);
            status.Revive();
            yield return null;
            yield return null;

            Assert.IsFalse(Effects.IsPlaying);
            Assert.IsNull(player.transform.Find("DeathFx"), "the effect objects are gone");
            Assert.That(body.localScale.y, Is.EqualTo(1f).Within(0.3f), "back to normal size");
            foreach (var r in body.GetComponentsInChildren<Renderer>()) Assert.IsTrue(r.enabled, r.name);
        }

        [UnityTest]
        public IEnumerator HurtingThePlayerShakesTheCamera()
        {
            var cam = Camera.main.transform;
            yield return new WaitForSeconds(0.3f);
            status.Damage(30f, DeathCause.Fall);
            float moved = 0f;
            var last = cam.position;
            for (int i = 0; i < 5; i++)
            {
                yield return null;
                moved = Mathf.Max(moved, Vector3.Distance(last, cam.position));
                last = cam.position;
            }
            Assert.Greater(moved, 0.01f, "the camera rumbles");
        }
    }
}
