using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>Crouching, shoving and hugging.</summary>
    public class SocialTests : SceneTestBase
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

        // ---------------------------------------------------------------- crouch

        [UnityTest]
        public IEnumerator CrouchingMakesYouShorterAndSlower()
        {
            yield return GoTo(new Vector3(-5f, 0.05f, -12f));
            ClearLane(player.transform.position, Vector3.forward, 10f);
            yield return new WaitForSeconds(0.3f);
            var cc = player.GetComponent<CharacterController>();
            float standing = cc.height;

            var from = player.transform.position;
            SetKey(Key.W, true);
            yield return new WaitForSeconds(1f);
            float walked = Vector3.Distance(from, player.transform.position);
            SetKey(Key.W, false);
            yield return new WaitForSeconds(0.4f);

            yield return GoTo(new Vector3(-5f, 0.05f, -12f));
            SetKey(Key.C, true);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(player.IsCrouching);
            Assert.Less(cc.height, standing - 0.4f, "the capsule shrinks");

            from = player.transform.position;
            SetKey(Key.W, true);
            yield return new WaitForSeconds(1f);
            float crouchWalked = Vector3.Distance(from, player.transform.position);
            SetKey(Key.W, false);

            Assert.Less(crouchWalked, walked * 0.7f, "walked " + walked + " m upright but " + crouchWalked + " m crouched");
            SetKey(Key.C, false);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(player.IsCrouching, "stands up again");
            Assert.AreEqual(standing, cc.height, 0.05f);
        }

        [UnityTest]
        public IEnumerator YouStayCrouchedUnderALowCeiling()
        {
            yield return GoTo(new Vector3(-5f, 0.05f, -12f));
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.transform.position = new Vector3(-5f, 1.75f, -12f);
            ceiling.transform.localScale = new Vector3(3f, 1f, 3f);

            SetKey(Key.C, true);
            yield return new WaitForSeconds(0.5f);
            SetKey(Key.C, false);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(player.IsCrouching, "there is no room to stand up");

            Object.Destroy(ceiling);
            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(player.IsCrouching, "out from under the ceiling");
        }

        // ------------------------------------------------------------------ push

        [UnityTest]
        public IEnumerator PushingSendsATeammateFlying()
        {
            yield return StandNextToTheDummy();
            var before = dummy.transform.position;

            var target = player.GetComponent<PlayerPush>().TryPush();
            Assert.IsNotNull(target, "the dummy is right in front");
            yield return new WaitForSeconds(0.8f);

            Assert.Greater(dummy.transform.position.z, before.z + 1.2f, "shoved away from the pusher");
        }

        [UnityTest]
        public IEnumerator PushingNobodyDoesNothing()
        {
            yield return StandNextToTheDummy(6f);
            Assert.IsNull(player.GetComponent<PlayerPush>().TryPush());
        }

        [UnityTest]
        public IEnumerator PlayersCanShoveEachOther()
        {
            yield return StandNextToTheDummy();
            // A shove between real players goes through IPushable too.
            Assert.IsTrue(player is IPushable);
            var pushable = (IPushable)player;
            var before = player.transform.position;
            pushable.ReceivePush(Vector3.back * 8f, 3f);
            yield return new WaitForSeconds(0.5f);
            Assert.Less(player.transform.position.z, before.z - 0.8f);
        }

        // ------------------------------------------------------------------- hug

        [UnityTest]
        public IEnumerator HuggingHealsBothPlayers()
        {
            yield return StandNextToTheDummy();
            var other = dummy.GetComponent<PlayerStatus>();
            status.Damage(40f, DeathCause.Fall);
            other.Damage(40f, DeathCause.Fall);
            float mine = status.Health, theirs = other.Health;

            var hug = player.GetComponent<PlayerHug>();
            Assert.IsTrue(hug.TryHug());
            Assert.IsTrue(hug.IsHugging);
            Assert.IsTrue(player.MovementLocked, "held still while hugging");

            yield return new WaitForSeconds(hug.duration + 0.4f);
            Assert.IsFalse(hug.IsHugging);
            Assert.Greater(status.Health, mine + 15f);
            Assert.Greater(other.Health, theirs + 15f);
        }

        [UnityTest]
        public IEnumerator HurtPlayersNoLongerHealQuickly()
        {
            status.Damage(40f, DeathCause.Fall);
            float hurt = status.Health;
            yield return new WaitForSeconds(5f);
            Assert.AreEqual(hurt, status.Health, 0.01f, "nothing for the first seconds");
        }

        [UnityTest]
        public IEnumerator YouCannotHugAnEmptyField()
        {
            yield return StandNextToTheDummy(8f);
            Assert.IsFalse(player.GetComponent<PlayerHug>().TryHug());
        }

        [UnityTest]
        public IEnumerator ADeadPlayerCannotBeHealed()
        {
            var other = Object.FindAnyObjectByType<TeammateDummy>().GetComponent<PlayerStatus>();
            other.Kill(DeathCause.Fall);
            other.Heal(30f);
            Assert.AreEqual(0f, other.Health);
            yield break;
        }
    }
}
