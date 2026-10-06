using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>What happens between teammates: dropping boxes on death, watching the others, health only coming back with hugs.</summary>
    public class TeamTests : SceneTestBase
    {
        /// <summary>A second real player next to the first one (a copy of the local character that this machine does not control).</summary>
        PlayerController AddSecondPlayer(Vector3 position)
        {
            var copy = Object.Instantiate(player.gameObject, position, Quaternion.identity);
            copy.name = "SecondPlayer";
            var other = copy.GetComponent<PlayerController>();
            other.isLocal = false;
            PlayerRegistry.Register(player);       // the camera and the game manager go back to following the original
            Assert.AreEqual(player, PlayerRegistry.Local);
            return other;
        }

        [UnityTest]
        public IEnumerator TheBoxesOfADeadPlayerFallToTheGroundForTheTeam()
        {
            yield return GoTo(new Vector3(-6f, 0.05f, -14f));
            status.Inventory.TryAdd(BoxOf("hot"), out _);
            status.Inventory.TryAdd(BoxOf("normal"), out _);
            int before = Object.FindObjectsByType<BoxPickup>().Count(p => p.IsAvailable);

            status.Kill(DeathCause.Burn);
            yield return null;

            Assert.AreEqual(0, status.Inventory.Count, "the dead carry nothing");
            var lying = Object.FindObjectsByType<BoxPickup>().Where(p => p.IsAvailable).ToArray();
            Assert.AreEqual(before + 2, lying.Length, "both boxes are back in the world");
            var near = lying.Count(p => Vector3.Distance(p.transform.position, player.transform.position) < 2.5f);
            Assert.GreaterOrEqual(near, 2, "they lie right where the player fell");
        }

        [UnityTest]
        public IEnumerator HealthRegeneratesAloneButOnlyHugsHealWithTeammates()
        {
            yield return GoTo(new Vector3(-6f, 0.05f, -14f));
            status.regenDelay = 0.2f;
            status.regenPerSecond = 20f;

            // alone: it comes back by itself
            status.Damage(40f, DeathCause.Burn);
            yield return new WaitForSeconds(1.2f);
            Assert.Greater(status.Health, 70f, "regenerates when playing alone");

            // with a teammate: it does not
            var other = AddSecondPlayer(player.transform.position + new Vector3(1.2f, 0f, 0f));
            yield return null;
            Assert.IsTrue(PlayerStatus.HugsRequired);
            float hurt = status.Health;
            status.Damage(30f, DeathCause.Burn);
            float afterHit = status.Health;
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(afterHit, status.Health, 0.01f, "no automatic regeneration with a teammate around");

            // a hug does
            player.visual.rotation = Quaternion.LookRotation(Vector3.right);
            Assert.IsTrue(player.GetComponent<PlayerHug>().TryHug());
            yield return new WaitForSeconds(1.2f);
            Assert.Greater(status.Health, afterHit + 5f, "the hug healed");
            Assert.IsNotNull(other);
        }

        [UnityTest]
        public IEnumerator ADeadPlayerWatchesTheTeamAndTheGameGoesOn()
        {
            yield return GoTo(new Vector3(-6f, 0.05f, -14f));
            var other = AddSecondPlayer(player.transform.position + new Vector3(3f, 0f, 0f));
            var otherStatus = other.GetComponent<PlayerStatus>();
            yield return null;

            status.Kill(DeathCause.Burn);
            yield return new WaitForSeconds(2.2f);
            Assert.AreEqual(GameState.Playing, GameManager.Instance.State, "a teammate is alive: no game over");
            Assert.IsTrue(GameManager.Instance.IsSpectating);
            var camera = Camera.main.GetComponent<ThirdPersonCamera>();
            Assert.IsNotNull(camera.Spectated, "the camera follows somebody else");
            Assert.AreNotEqual(player.transform, camera.Spectated);
            Assert.IsNotNull(ThirdPersonCamera.SpectatedName);

            // the last one standing falls: now it is over
            otherStatus.Kill(DeathCause.Burn);
            yield return WaitForState(GameState.GameOver, 5f);
        }

        [UnityTest]
        public IEnumerator ADeathAloneStillEndsTheGame()
        {
            yield return GoTo(new Vector3(-6f, 0.05f, -14f));
            status.Kill(DeathCause.Burn);
            yield return WaitForState(GameState.GameOver, 5f);
            Assert.IsFalse(GameManager.Instance.IsSpectating);
        }

        // ------------------------------------------------------------ area lights

        [UnityTest]
        public IEnumerator TheTunnelLightsOnlyBurnWhenABoxLiesInTheTunnel()
        {
            var tunnel = GameObject.Find("CrawlTunnel");
            var area = tunnel.GetComponent<SpawnAreaLights>();
            Assert.IsNotNull(area);
            Assert.IsFalse(area.Wanted, "the classic mode puts no box in the tunnel");
            Assert.IsFalse(area.dressing.activeSelf, "so its lanterns are off");

            yield return LoadLevel(GameModes.Challenge);
            tunnel = GameObject.Find("CrawlTunnel");
            area = tunnel.GetComponent<SpawnAreaLights>();
            Assert.AreEqual(area.Wanted, area.dressing.activeSelf, "the lanterns follow the boxes of the mode");
        }
    }
}
