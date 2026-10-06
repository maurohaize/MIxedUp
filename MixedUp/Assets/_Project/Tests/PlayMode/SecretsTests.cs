using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>The easter eggs and small mechanics: log ranking, snowman, rubber ducks, the ferry raft and the wind.</summary>
    public class SecretsTests : SceneTestBase
    {
        // ----------------------------------------------------------- spinning log

        [UnityTest]
        public IEnumerator CleanJumpsOverTheLogEndUpOnItsSign()
        {
            JumpScoreboard.ClearSaved();
            var sweeper = Object.FindAnyObjectByType<Sweeper>();
            var sign = Object.FindAnyObjectByType<SweeperSign>();
            Assert.IsNotNull(sign, "the sign stands beside the log");

            yield return GoTo(sweeper.transform.position + new Vector3(0f, 0.05f, 5.5f));   // near the log, out of its reach
            for (int i = 0; i < 3; i++) sweeper.RegisterCleanJump(player);
            yield return null;

            StringAssert.Contains("3", sign.CurrentText);
            Assert.AreEqual(3, sweeper.Board.Top[0].score);
            Assert.AreEqual(3, sweeper.Board.Streak(Sweeper.NameOf(player)));
            JumpScoreboard.ClearSaved();
        }

        [UnityTest]
        public IEnumerator BeingHitByTheLogEndsTheStreak()
        {
            var sweeper = Object.FindAnyObjectByType<Sweeper>();
            sweeper.RegisterCleanJump(player);
            sweeper.RegisterCleanJump(player);

            var pos = sweeper.transform.position;
            yield return GoTo(new Vector3(pos.x + 2f, pos.y + 0.05f, pos.z));
            sweeper.arm.localRotation = Quaternion.identity;
            yield return new WaitForSeconds(2.2f);

            Assert.AreEqual(0, sweeper.Board.Streak(Sweeper.NameOf(player)), "the log must have hit the player standing on it");
            JumpScoreboard.ClearSaved();
        }

        // ------------------------------------------------------------ snowman

        [UnityTest]
        public IEnumerator RunningIntoTheSnowmanKnocksItDown()
        {
            var snowman = Object.FindAnyObjectByType<Snowman>();
            Assert.IsNotNull(snowman);
            Assert.IsFalse(snowman.IsCollapsed);

            var p = snowman.transform.position;
            var start = p + new Vector3(0f, 0.05f, 0f) + Vector3.forward * 4f;
            yield return GoTo(start);
            ClearLane(start, Vector3.back, 5f, 0.8f, "Plateau");
            SetKey(Key.W, true);
            // Face the snowman: walk towards -z.
            Camera.main.GetComponent<ThirdPersonCamera>().enabled = false;
            Camera.main.transform.rotation = Quaternion.Euler(20f, 180f, 0f);
            SetKey(Key.LeftShift, true);
            yield return new WaitForSeconds(1.5f);
            SetKey(Key.W, false);
            SetKey(Key.LeftShift, false);

            Assert.IsTrue(snowman.IsCollapsed, "bumped into it at a run");
            Assert.IsNotNull(snowman.head.GetComponent<Rigidbody>(), "the head rolls away");
        }

        [UnityTest]
        public IEnumerator TheSnowmanBuildsItselfUpAgain()
        {
            var snowman = Object.FindAnyObjectByType<Snowman>();
            snowman.respawnSeconds = 0.5f;
            snowman.Collapse(snowman.transform.position + Vector3.forward);
            Assert.IsTrue(snowman.IsCollapsed);
            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(snowman.IsCollapsed);
            Assert.IsNull(snowman.head.GetComponent<Rigidbody>());
            Assert.AreEqual(1f, snowman.body.localScale.y, 0.01f);
        }

        // ------------------------------------------------------------- the duck

        [UnityTest]
        public IEnumerator SqueezingARubberDuckMakesItQuack()
        {
            var duck = Object.FindObjectsByType<RubberDuck>().First(d => d.transform.position.z < 7f);
            yield return GoTo(new Vector3(duck.transform.position.x, 0.05f, 4.2f));
            yield return new WaitForSeconds(0.3f);
            interactor.Scan();
            Assert.AreSame(duck, interactor.Current);
            interactor.TryInteract();
            Assert.AreEqual(1, duck.Squeezes);
        }

        // -------------------------------------------------------------- the raft

        [UnityTest]
        public IEnumerator TheRaftCarriesAPlayerAcrossAndKeepsTheirFeetDry()
        {
            var raft = Object.FindAnyObjectByType<MovingRaft>();
            raft.pause = 0.1f;
            raft.transform.position = raft.pointA;
            var start = raft.pointA;
            yield return GoTo(new Vector3(start.x, 0.2f, start.z));
            yield return new WaitForSeconds(0.4f);
            status.Inventory.TryAdd(BoxOf("electric"), out _);

            float z0 = player.transform.position.z;
            float wait = 0f;
            while (wait < 6f && raft.transform.position.z < raft.pointB.z - 0.2f)
            {
                Assert.IsFalse(status.Hazards.InWater, "on the deck, not in the water");
                wait += Time.deltaTime;
                yield return null;
            }
            Assert.Greater(player.transform.position.z, z0 + 3f, "carried over the river by the raft");
            Assert.Greater(status.Health, 99f, "no electric shock on the way");
        }

        // --------------------------------------------------------------- the wind

        [UnityTest]
        public IEnumerator GustsPushPlayersStandingInThem()
        {
            var gust = Object.FindAnyObjectByType<GustZone>();
            gust.calmSeconds = 0.2f;
            gust.warningSeconds = 0.1f;
            gust.gustSeconds = 1.5f;
            yield return GoTo(gust.transform.position + new Vector3(-2f, -1.4f, 0f));
            ClearLane(player.transform.position, Vector3.right, 6f, 1.5f);
            var from = player.transform.position;
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(player.transform.position.x, from.x + 0.8f, "blown along");
        }
    }
}
