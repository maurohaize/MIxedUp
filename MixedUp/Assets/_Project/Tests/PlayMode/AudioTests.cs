using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>The game makes noise: footsteps, jumps, hurt sounds, music and ambience.</summary>
    public class AudioTests : SceneTestBase
    {
        [UnityTest]
        public IEnumerator TheAudioManagerPlaysGameMusicInTheLevel()
        {
            Assert.IsNotNull(AudioManager.Instance);
            Assert.AreEqual(MusicId.Game, AudioManager.Instance.CurrentMusic);
            yield break;
        }

        [UnityTest]
        public IEnumerator WalkingMakesFootstepsThatMatchTheGround()
        {
            yield return GoTo(new Vector3(-5f, 0.05f, -12f));
            ClearLane(player.transform.position, Vector3.forward, 10f);
            int before = AudioManager.CountOf(SfxId.FootGrass) + AudioManager.CountOf(SfxId.FootDirt);
            SetKey(Key.W, true);
            yield return new WaitForSeconds(1.6f);
            SetKey(Key.W, false);
            int steps = AudioManager.CountOf(SfxId.FootGrass) + AudioManager.CountOf(SfxId.FootDirt) - before;
            Assert.GreaterOrEqual(steps, 3, "about one step every 1.3 m");

            var audio = player.GetComponent<PlayerAudio>();
            status.Hazards = new HazardState { InWater = true };
            Assert.AreEqual(SfxId.FootWater, audio.SurfaceSound());
            status.Hazards = new HazardState { InMud = true };
            Assert.AreEqual(SfxId.FootMud, audio.SurfaceSound());
        }

        [UnityTest]
        public IEnumerator JumpingAndLandingAreHeard()
        {
            yield return GoTo(new Vector3(-5f, 0.05f, -12f));
            yield return new WaitForSeconds(0.3f);
            int jumps = AudioManager.CountOf(SfxId.Jump);
            yield return Tap(Key.Space);
            yield return new WaitForSeconds(0.2f);
            Assert.Greater(AudioManager.CountOf(SfxId.Jump), jumps);
        }

        [UnityTest]
        public IEnumerator GettingHurtMakesAGrunt()
        {
            int before = AudioManager.CountOf(SfxId.Hurt);
            status.Damage(10f, DeathCause.Fall);
            yield return null;
            Assert.AreEqual(before + 1, AudioManager.CountOf(SfxId.Hurt));
        }

        [UnityTest]
        public IEnumerator DeliveringABoxRingsOut()
        {
            Resolve();
            int before = AudioManager.CountOf(SfxId.Deliver);
            truck.Deliver(truck.order.lines[0].box);
            yield return null;
            Assert.AreEqual(before + 1, AudioManager.CountOf(SfxId.Deliver));
        }

        [UnityTest]
        public IEnumerator PassingABoxToATeammateIsAudible()
        {
            status.Inventory.TryAdd(BoxOf("normal"), out _);
            var dummy = Object.FindAnyObjectByType<TeammateDummy>();
            int before = AudioManager.CountOf(SfxId.Pass);
            status.Inventory.TryTransfer(0, dummy.GetComponent<PlayerInventory>());
            yield return null;
            Assert.Greater(AudioManager.CountOf(SfxId.Pass), before);
        }

        [UnityTest]
        public IEnumerator SoundVolumeFollowsTheSettings()
        {
            float saved = GameSettings.SfxVolume;
            GameSettings.SfxVolume = 0f;
            AudioManager.Play(SfxId.UiClick);
            yield return null;
            Assert.AreEqual(0f, GameSettings.SfxVolume);
            GameSettings.SfxVolume = saved;
        }
    }
}
