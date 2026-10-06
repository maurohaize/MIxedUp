using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>The new map: rolling ground, mud, fire, the spinning log, bounce mushrooms, windmill, stepping stones.</summary>
    public class WorldTests : SceneTestBase
    {
        IEnumerator Walk(Vector3 start, Key key, float seconds)
        {
            player.Teleport(start);
            ClearLane(start, Vector3.forward, 8f);
            yield return Settle();
            yield return new WaitForSeconds(0.3f);
            var from = player.transform.position;
            SetKey(key, true);
            yield return new WaitForSeconds(seconds);
            SetKey(key, false);
            lastWalk = Vector3.Distance(from, player.transform.position);
        }

        float lastWalk;

        // ------------------------------------------------------------------ mud

        [UnityTest]
        public IEnumerator MudSlowsTheWalkAndCanBeFoundOnTheTrail()
        {
            var zones = Object.FindObjectsByType<HazardZone>().Where(z => z.type == HazardType.Mud).ToArray();
            Assert.GreaterOrEqual(zones.Length, 2, "a swamp is more than one puddle");

            yield return Walk(new Vector3(-5f, 0.05f, -12f), Key.W, 1f);
            float dry = lastWalk;

            var mud = zones[0].transform.position;
            yield return Walk(new Vector3(mud.x, mud.y + 0.05f, mud.z - 1f), Key.W, 1f);
            float wet = lastWalk;

            Assert.IsTrue(status.Hazards.InMud || wet < dry, "the player is wading");
            Assert.Less(wet, dry * 0.7f, "mud slows you down: dry " + dry + " m vs mud " + wet + " m");
        }

        [UnityTest]
        public IEnumerator MudAlsoWeakensTheJump()
        {
            var mud = Object.FindObjectsByType<HazardZone>().First(z => z.type == HazardType.Mud).transform.position;
            yield return GoTo(new Vector3(mud.x, mud.y + 0.05f, mud.z));
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(status.Hazards.InMud);

            float start = player.transform.position.y, peak = start;
            yield return Tap(Key.Space);
            for (float until = Time.time + 0.6f; Time.time < until;)
            {
                peak = Mathf.Max(peak, player.transform.position.y);
                yield return null;
            }
            Assert.Less(peak - start, 0.6f, "a jump out of mud barely leaves the ground: " + (peak - start));
        }

        // ----------------------------------------------------------------- fire

        [UnityTest]
        public IEnumerator FireBurnsAnyoneStandingInIt()
        {
            var fire = Object.FindObjectsByType<HazardZone>().First(z => z.type == HazardType.Fire).transform.position;
            float before = status.Health;
            yield return GoTo(new Vector3(fire.x, fire.y - 0.3f, fire.z));
            yield return new WaitForSeconds(1.2f);

            Assert.IsTrue(status.Hazards.OnFire);
            Assert.Less(status.Health, before - 8f, "about 14 damage a second: " + status.Health);
        }

        [UnityTest]
        public IEnumerator DyingInTheFireSaysSo()
        {
            status.Damage(95f, DeathCause.Fall);   // leave 5 HP
            var fire = Object.FindObjectsByType<HazardZone>().First(z => z.type == HazardType.Fire).transform.position;
            yield return GoTo(new Vector3(fire.x, fire.y - 0.3f, fire.z));
            yield return new WaitForSeconds(1f);

            Assert.IsTrue(status.IsDead);
            Assert.AreEqual("death.burn", status.LastCause.Key);
            Assert.IsTrue(Localization.Has("death.burn", Language.Basque));
            yield break;
        }

        // ------------------------------------------------------------- sweeper

        [UnityTest]
        public IEnumerator TheSpinningLogHurtsAndThrowsPlayersBack()
        {
            var sweeper = Object.FindAnyObjectByType<Sweeper>();
            Assert.IsNotNull(sweeper);

            sweeper.degreesPerSecond = 0f;
            Vector3 onTheLog = sweeper.transform.position + sweeper.arm.right * 2.4f;
            float health = status.Health;
            Assert.IsTrue(sweeper.Touches(onTheLog + Vector3.up * 0.8f, 0.38f), "the geometry test sees the log");
            Assert.IsFalse(sweeper.Touches(sweeper.transform.position + sweeper.arm.forward * 2.5f + Vector3.up * 0.8f, 0.38f), "but not the empty side");
            Assert.IsFalse(sweeper.Touches(onTheLog + Vector3.up * 3f, 0.38f), "and not someone high above it");

            yield return GoTo(onTheLog + Vector3.up * 0.05f);
            yield return new WaitForSeconds(0.25f);

            Assert.Less(status.Health, health - sweeper.damage + 0.5f, "hit for " + sweeper.damage);
            Assert.Greater(Vector3.Distance(player.transform.position, onTheLog), 0.5f, "knocked away from where it stood");
        }

        [UnityTest]
        public IEnumerator TheLogCannotHitEveryFrame()
        {
            var sweeper = Object.FindAnyObjectByType<Sweeper>();
            sweeper.degreesPerSecond = 0f;
            Vector3 onTheLog = sweeper.transform.position + sweeper.arm.right * 2.4f;
            yield return GoTo(onTheLog + Vector3.up * 0.05f);
            yield return new WaitForSeconds(0.2f);
            float afterFirst = status.Health;

            // Stay in reach: nothing more happens until the cooldown has passed.
            player.Teleport(onTheLog + Vector3.up * 0.05f);
            yield return new WaitForSeconds(0.3f);
            Assert.That(status.Health, Is.EqualTo(afterFirst).Within(1.5f), "only one hit within the cooldown (regeneration aside)");
        }

        [UnityTest]
        public IEnumerator TheLogKeepsTurning()
        {
            var sweeper = Object.FindAnyObjectByType<Sweeper>();
            float a = sweeper.arm.eulerAngles.y;
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(a, sweeper.arm.eulerAngles.y)), 10f);
        }

        // ------------------------------------------------------ bounce mushroom

        [UnityTest]
        public IEnumerator SteppingOnTheMushroomLaunchesYouUpAndOntoThePlatform()
        {
            var pad = Object.FindObjectsByType<BouncePad>().First(b => Vector2.Distance(new Vector2(b.transform.position.x, b.transform.position.z), new Vector2(-18f, 38.6f)) < 2f);
            Assert.IsNotNull(pad);
            var platform = GameObject.Find("Platform");
            Assert.IsNotNull(platform);
            float platformTop = platform.transform.position.y + platform.transform.localScale.y * 0.5f;

            var start = pad.transform.position + new Vector3(0f, 0.05f, -1.2f);
            player.Teleport(start);
            ClearLane(start, Vector3.forward, 9f, 1.6f, "Platform", "PlatformCap");
            yield return Settle();
            yield return new WaitForSeconds(0.3f);

            float peak = 0f;
            SetKey(Key.W, true);
            for (float t = 0f; t < 2.4f; t += Time.deltaTime)
            {
                peak = Mathf.Max(peak, player.transform.position.y);
                // Stop as soon as the player is standing up there, or they would simply walk off the far edge.
                if (peak > 3f && player.IsGrounded && player.transform.position.y > platformTop - 0.3f) break;
                yield return null;
            }
            SetKey(Key.W, false);
            yield return new WaitForSeconds(0.3f);

            Assert.Greater(peak, 3f, "launched well above head height: " + peak);
            Assert.That(player.transform.position.y, Is.GreaterThan(platformTop - 0.3f), "standing on the platform");
            Assert.IsTrue(player.IsGrounded);
            Assert.That(player.transform.position.z, Is.InRange(40.4f, 45.6f), "within the platform's footprint");
            Assert.Greater(status.Health, 90f, "a landing on the platform is safe");
        }

        [UnityTest]
        public IEnumerator TheToxicBoxSitsOnThePlatformAndARampLeadsUp()
        {
            var toxic = pickups.First(p => p.data.id == "toxic");
            Assert.Greater(toxic.transform.position.y, 2.2f, "the toxic box is up on the platform");

            var start = new Vector3(-18f, 0.05f, 52.5f);
            player.Teleport(start);
            ClearLane(start, Vector3.back, 9f, 1.6f, "Platform", "PlatformCap", "Ramp");
            yield return Settle();
            yield return new WaitForSeconds(0.3f);

            SetKey(Key.S, true);
            for (float t = 0f; t < 4f; t += Time.deltaTime)
            {
                if (player.IsGrounded && player.transform.position.y > 2.2f) break;   // up on the platform: stop there
                yield return null;
            }
            SetKey(Key.S, false);
            yield return new WaitForSeconds(0.3f);

            Assert.Greater(player.transform.position.y, 2.0f, "walked up the ramp");
            var flat = new Vector2(player.transform.position.x - toxic.transform.position.x, player.transform.position.z - toxic.transform.position.z);
            Assert.Less(flat.magnitude, 4f, "on top of the platform, within reach of the box: " + flat.magnitude);
        }

        // -------------------------------------------------------------- windmill

        [UnityTest]
        public IEnumerator TheWindmillSailsTurn()
        {
            var sails = Object.FindObjectsByType<Spin>().First(s => s.name == "Blades");
            float a = sails.transform.localEulerAngles.z;
            yield return new WaitForSeconds(0.6f);
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(a, sails.transform.localEulerAngles.z)), 3f);
            Assert.IsNotNull(GameObject.Find("Windmill").GetComponent<Collider>(), "the tower is solid");
        }

        // ---------------------------------------------------------- stepping stones

        [UnityTest]
        public IEnumerator SteppingStonesRiseAboveTheWater()
        {
            var stones = Object.FindObjectsByType<Transform>().Where(t => t.name == "StepStone").ToArray();
            Assert.AreEqual(3, stones.Length);
            foreach (var stone in stones)
            {
                Assert.IsTrue(Physics.Raycast(stone.position + Vector3.up * 5f, Vector3.down, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore));
                Assert.That(hit.point.y, Is.InRange(0f, 0.3f), "the stone's top is above the water, near ground level");
                Assert.AreEqual(stone, hit.collider.transform, "and it is solid");
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator TheStonesAreCloseEnoughToJumpBetween()
        {
            var stones = Object.FindObjectsByType<Transform>().Where(t => t.name == "StepStone").OrderBy(t => t.position.z).ToArray();
            Assert.Less(stones[0].position.z - 5f, 2.0f, "the first stone is a short hop from the south bank");
            for (int i = 1; i < stones.Length; i++)
                Assert.Less(Vector3.Distance(stones[i - 1].position, stones[i].position), 3f, "a jump of at most ~1.5 m between stone edges");
            Assert.Less(13f - stones[stones.Length - 1].position.z, 2.4f, "and a short hop to the north bank");

            // The player's jump carries well over that distance when running.
            var controller = player;
            float airTime = 2f * Mathf.Sqrt(2f * controller.jumpHeight / controller.gravity);
            Assert.Greater(airTime * controller.walkSpeed, 2.5f, "even a walking jump spans the gap");
            yield break;
        }

        // ------------------------------------------------------------ the ground

        [UnityTest]
        public IEnumerator NoHolesInTheGroundAnywhereOnTheMap()
        {
            int holes = 0, samples = 0;
            var where = new System.Text.StringBuilder();
            // Off the 1 m grid of the ground tiles: a ray through an exact vertex or edge is a poor test.
            for (float x = -41.63f; x <= 42f; x += 3f)
            {
                for (float z = -31.63f; z <= 52f; z += 3f)
                {
                    if (z > 4.5f && z < 13.5f) continue;   // the river
                    samples++;
                    bool hole = !Physics.Raycast(new Vector3(x, 30f, z), Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore) || hit.point.y < -1.2f;
                    if (hole) { holes++; where.Append(" (").Append(x).Append(",").Append(z).Append(")"); }
                }
            }
            Assert.AreEqual(0, holes, holes + " of " + samples + " sample points have no ground:" + where);
            yield break;
        }

        [UnityTest]
        public IEnumerator GroundNorthOfTheRiverRollsGently()
        {
            var ground = GameObject.Find("GroundNorth").GetComponent<Collider>();
            Assert.IsInstanceOf<MeshCollider>(ground, "the rolling ground needs a mesh collider");

            float min = float.MaxValue, max = float.MinValue;
            for (float x = -40f; x <= 40f; x += 4f)
            {
                for (float z = 20f; z <= 50f; z += 4f)
                {
                    if (!ground.Raycast(new Ray(new Vector3(x, 30f, z), Vector3.down), out var hit, 60f)) continue;
                    min = Mathf.Min(min, hit.point.y);
                    max = Mathf.Max(max, hit.point.y);
                }
            }
            Assert.Greater(max - min, 0.3f, "there are mounds and dips: " + (max - min));
            Assert.Less(max, 1.2f, "but no mountains in the play area: " + max);
            Assert.Greater(min, -1.2f);
            yield break;
        }

        [UnityTest]
        public IEnumerator EveryBoxSitsOnSolidGround()
        {
            foreach (var pickup in pickups)
            {
                var from = pickup.transform.position + Vector3.up * 0.4f;
                Assert.IsTrue(Physics.Raycast(from, Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore), pickup.name + " has nothing under it");
                Assert.Less(from.y - 0.4f - hit.point.y, 0.2f, pickup.name + " floats above its ground: " + (from.y - 0.4f - hit.point.y));
                Assert.Greater(from.y - 0.4f - hit.point.y, -0.2f, pickup.name + " is buried: " + (from.y - 0.4f - hit.point.y));
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator TheWholeRouteStaysFlatAroundTheStartSoThePhysicsTestsStayHonest()
        {
            var south = GameObject.Find("GroundSouth").GetComponent<Collider>();
            Assert.IsTrue(south.Raycast(new Ray(new Vector3(-5f, 20f, -12f), Vector3.down), out var hit, 40f));
            Assert.That(hit.point.y, Is.InRange(-0.01f, 0.01f), "the start meadow is flat");
            Assert.IsInstanceOf<BoxCollider>(south, "and its collider is the simple box the movement tests were tuned on");
            yield break;
        }
    }
}
