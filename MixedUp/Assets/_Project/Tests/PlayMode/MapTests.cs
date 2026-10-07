using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>The checks every map must pass: a safe start, boxes on solid ground, a full classic order and enough hard spots.</summary>
    public abstract class MapTestsBase : SceneTestBase
    {
        [UnityTest]
        public IEnumerator ThePlayerStartsOnSolidGround()
        {
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(player.IsGrounded, "the player does not stand on the ground at the start");
            Assert.IsFalse(status.IsDead);
        }

        [UnityTest]
        public IEnumerator EveryBoxSpotRestsOnSolidGroundInsideTheWalls()
        {
            var spots = Object.FindObjectsByType<BoxSpawnPoint>();
            Assert.GreaterOrEqual(spots.Length, 15, "too few spots for the random modes");
            foreach (var spot in spots)
            {
                var p = spot.transform.position;
                Assert.IsTrue(p.x > -44f && p.x < 44f && p.z > -35f && p.z < 55f, spot.name + " is outside the walls");
                Assert.IsTrue(Physics.Raycast(p + Vector3.up * 0.4f, Vector3.down, out var hit, 1.2f, ~0, QueryTriggerInteraction.Ignore),
                    spot.name + " has no ground under it");
                Assert.Less(Mathf.Abs(hit.point.y - p.y), 0.35f, spot.name + " floats above or sinks into its ground");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheClassicOrderHasAllItsBoxes()
        {
            Assert.AreEqual(truck.order.TotalBoxes, pickups.Length);
            foreach (var line in truck.order.lines)
                Assert.AreEqual(line.count, pickups.Count(p => p.data == line.box), line.box.id);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ThereAreEnoughHardSpotsForTheChallengeMode()
        {
            var hard = Object.FindObjectsByType<BoxSpawnPoint>().Count(s => s.hard);
            Assert.GreaterOrEqual(hard, 4);
            yield return null;
        }
    }

    public class SummitMapTests : MapTestsBase
    {
        protected override string SceneToLoad => "Assets/Scenes/Level_Summit.unity";
    }

    public class HarbourMapTests : MapTestsBase
    {
        protected override string SceneToLoad => "Assets/Scenes/Level_Harbour.unity";
    }
}
