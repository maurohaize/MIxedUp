using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>The spinning log notices a player who jumps over it, by itself (no test shortcut).</summary>
    public class SweeperJumpTests : SceneTestBase
    {
        /// <summary>Signed side of the log's line the player stands on (0 = in line with it).</summary>
        public static float SideOf(Sweeper sweeper, Vector3 playerPosition)
        {
            Vector3 offset = playerPosition - sweeper.transform.position;
            offset.y = 0f;
            Vector3 along = sweeper.arm.right;
            along.y = 0f;
            return Vector3.Dot(Vector3.Cross(along.normalized, offset.normalized), Vector3.up);
        }

        /// <summary>
        /// Stands still at `fromPost` and jumps so that the log passes under the player at the top of the jump, `attempts`
        /// times. Reports the clean jumps counted and the times the log hit the player.
        /// </summary>
        public static IEnumerator JumpOverTheLog(Sweeper sweeper, PlayerController who, Vector3 fromPost, int attempts, System.Action<int, int> done)
        {
            int clean = 0, hits = 0;
            sweeper.Board.Hit(Sweeper.NameOf(who));
            Sweeper.CleanJump += OnClean;
            Sweeper.Hit += OnHit;
            void OnClean(Sweeper s, PlayerController p, int streak) { if (p == who) clean++; }
            void OnHit(Sweeper s, PlayerController p) { if (p == who) hits++; }

            float apex = Mathf.Sqrt(2f * who.jumpHeight / who.gravity);   // seconds from take-off to the top of the jump
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                who.Teleport(sweeper.transform.position + fromPost + new Vector3(0f, 0.05f, 0f));
                yield return new WaitForSeconds(0.4f);

                // Wait until the log is about to arrive, and jump so that its arrival is the top of the jump.
                float previous = Mathf.Abs(SideOf(sweeper, who.transform.position));
                bool jumped = false;
                float waited = 0f;
                while (waited < 12f)
                {
                    yield return null;
                    waited += Time.deltaTime;
                    float side = Mathf.Abs(SideOf(sweeper, who.transform.position));
                    float degreesAway = Mathf.Asin(Mathf.Clamp01(side)) * Mathf.Rad2Deg;
                    float eta = degreesAway / sweeper.degreesPerSecond;
                    if (!jumped && side < previous && eta <= apex)
                    {
                        who.AddImpulse(Vector3.zero, Mathf.Sqrt(2f * who.gravity * who.jumpHeight));
                        jumped = true;
                    }
                    previous = side;
                    if (jumped && waited > apex * 2f + 0.3f && who.IsGrounded && side > 0.9f) break;
                }
                yield return new WaitForSeconds(0.5f);
            }

            Sweeper.CleanJump -= OnClean;
            Sweeper.Hit -= OnHit;
            done(clean, hits);
        }

        static readonly Vector3 NearTheEnd = new Vector3(0f, 0f, 3.2f);

        [UnityTest]
        public IEnumerator JumpingOverTheLogIsRegistered()
        {
            JumpScoreboard.ClearSaved();
            var sweeper = Object.FindAnyObjectByType<Sweeper>();
            Assert.IsNotNull(sweeper);

            int clean = -1, hits = -1;
            yield return JumpOverTheLog(sweeper, player, NearTheEnd, 5, (c, h) => { clean = c; hits = h; });

            Assert.GreaterOrEqual(clean, 3, "well-timed jumps count as clean (hit " + hits + " times)");
            JumpScoreboard.ClearSaved();
        }

        [UnityTest]
        public IEnumerator JumpingOverTheLogIsRegisteredOnASlowComputer()
        {
            JumpScoreboard.ClearSaved();
            var sweeper = Object.FindAnyObjectByType<Sweeper>();
            int savedRate = Application.targetFrameRate;
            int savedVsync = QualitySettings.vSyncCount;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 15;
            yield return null;

            int clean = -1, hits = -1;
            yield return JumpOverTheLog(sweeper, player, NearTheEnd, 6, (c, h) => { clean = c; hits = h; });

            Application.targetFrameRate = savedRate;
            QualitySettings.vSyncCount = savedVsync;
            Assert.GreaterOrEqual(clean, 3, "at 15 frames per second the jumps over the log still count (hit " + hits + " times)");
            JumpScoreboard.ClearSaved();
        }

        [UnityTest]
        public IEnumerator StandingStillOnTheGroundIsAHitAndNotAJump()
        {
            JumpScoreboard.ClearSaved();
            var sweeper = Object.FindAnyObjectByType<Sweeper>();
            int clean = 0, hits = 0;
            Sweeper.CleanJump += (s, p, n) => clean++;
            Sweeper.Hit += (s, p) => hits++;

            player.Teleport(sweeper.transform.position + NearTheEnd + new Vector3(0f, 0.05f, 0f));
            yield return new WaitForSeconds(5.5f);   // the log comes round at least twice

            Assert.GreaterOrEqual(hits, 1, "the log hits somebody standing on the ground");
            Assert.AreEqual(0, clean, "being hit is never a clean jump");
            JumpScoreboard.ClearSaved();
        }
    }
}
