using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>Renders the dressing of the map (ice hill, river, outskirts, road, mist, lights) to PNG files. Explicit: needs a GPU.</summary>
    public class DressingScreenshots : SceneTestBase
    {
        const int Width = 1600, Height = 900;

        static string OutDir
        {
            get
            {
                string dir = System.Environment.GetEnvironmentVariable("MIXEDUP_SHOTS");
                if (string.IsNullOrEmpty(dir)) dir = "Temp/Shots";
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        IEnumerator Shot(string name, Vector3 position, Vector3 lookAt, float seconds = 0f)
        {
            var cam = Camera.main;
            cam.GetComponent<ThirdPersonCamera>().enabled = false;
            cam.transform.position = position;
            cam.transform.rotation = Quaternion.LookRotation(lookAt - position);
            ui.hudRoot.SetActive(false);

            var rt = new RenderTexture(Width, Height, 24);
            cam.targetTexture = rt;
            if (seconds > 0f) yield return new WaitForSeconds(seconds);
            yield return null;
            yield return null;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator IceHillGallery()
        {
            yield return Shot("80_ice_hill_from_south", new Vector3(-32f, 9f, -40f), new Vector3(-30f, 3f, -8f), 1.5f);
            yield return Shot("81_ice_stairs", new Vector3(-8f, 5f, -10f), new Vector3(-24f, 2.5f, -6f));
            yield return Shot("82_plateau_top", new Vector3(-26f, 8f, -12f), new Vector3(-34f, 5f, -5f));
            yield return Shot("83_ramp_close", new Vector3(-32f, 3f, -30f), new Vector3(-32f, 3f, -14f));
        }

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator RiverGallery()
        {
            yield return Shot("84_river_wide", new Vector3(8f, 4f, 0f), new Vector3(24f, 0f, 9f), 1.0f);
            yield return Shot("85_river_low", new Vector3(-8f, 1.2f, 4.5f), new Vector3(12f, 0f, 9f), 0.4f);
            yield return Shot("86_bridge", new Vector3(-34f, 3f, 2f), new Vector3(-26f, 0f, 10f));
        }

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator OutskirtsGallery()
        {
            yield return Shot("87_road_from_truck", new Vector3(0f, 3.5f, -31f), new Vector3(0f, 1f, -90f));
            yield return Shot("88_road_overview", new Vector3(0f, 30f, -50f), new Vector3(8f, 0f, -110f));
            yield return Shot("89_edge_east", new Vector3(36f, 2.2f, 20f), new Vector3(70f, 5f, 24f));
            yield return Shot("90_edge_north", new Vector3(10f, 2.5f, 46f), new Vector3(14f, 5f, 80f));
            yield return Shot("91_outskirts_aerial", new Vector3(-90f, 60f, -60f), new Vector3(10f, 0f, 10f));
        }

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator TreeCloseups()
        {
            var outside = GameObject.Find("Outside").transform;
            foreach (var name in new[] { "Tree1", "Tree2", "Tree3", "Tree4" })
            {
                Transform best = null;
                float bestDistance = float.MaxValue;
                foreach (Transform t in outside)
                {
                    if (t.name != name) continue;
                    float d = Vector3.Distance(t.position, new Vector3(0f, 0f, -60f));
                    if (d < bestDistance) { best = t; bestDistance = d; }
                }
                if (best == null) continue;
                var p = best.position;
                yield return Shot("95_" + name, p + new Vector3(7f, 3.5f, -9f), p + Vector3.up * 3f);
            }
        }

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator TreeWithoutInk()
        {
            var outside = GameObject.Find("Outside").transform;
            foreach (var kind in new[] { "Tree1", "Tree2", "Tree3", "Tree4" })
            {
                Transform best = null;
                float bestDistance = float.MaxValue;
                foreach (Transform t in outside)
                {
                    if (t.name != kind) continue;
                    float d = Vector3.Distance(t.position, new Vector3(0f, 0f, -60f));
                    if (d < bestDistance) { best = t; bestDistance = d; }
                }
                foreach (Transform t in outside) t.gameObject.SetActive(t == best);
                var p = best.position;
                // From the side at a distance, looking at the crown; nothing else around.
                yield return Shot("96_" + kind + "_alone", p + new Vector3(9f, 4f, -9f), p + Vector3.up * 4f);
            }
        }

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator SecretsGallery()
        {
            JumpScoreboard.ClearSaved();
            var sweeper = Object.FindAnyObjectByType<Sweeper>();
            for (int i = 0; i < 4; i++) sweeper.RegisterCleanJump(player);
            yield return Shot("98_sweeper_sign", new Vector3(21.5f, 2.2f, 26.5f), new Vector3(18.6f, 1.75f, 28.8f), 0.3f);
            yield return Shot("99_raft", new Vector3(-3f, 3f, 2f), new Vector3(-8f, 0f, 8f));
            yield return Shot("100_duck", new Vector3(7f, 1.6f, 3.5f), new Vector3(4.5f, 0.2f, 6.4f));
            yield return Shot("101_tower", new Vector3(30f, 6f, -14f), new Vector3(38f, 4f, -7f));
            yield return Shot("102_tunnel", new Vector3(-31f, 2.5f, 26f), new Vector3(-37f, 1f, 33f));
            yield return Shot("103_cliff", new Vector3(24f, 5f, 42f), new Vector3(32f, 4f, 50f));
            yield return Shot("104_island", new Vector3(9f, 2.2f, 3.2f), new Vector3(3f, 0f, 9f));
            yield return Shot("105_lake_and_pier", new Vector3(60f, 9f, 24f), new Vector3(92f, 0f, 8f));
            yield return Shot("106_cave", new Vector3(-92f, 4f, 20f), new Vector3(-114f, 2f, 8f));
            JumpScoreboard.ClearSaved();
        }

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator LightsGallery()
        {
            yield return Shot("92_camp_fire", new Vector3(16f, 3f, -34f), new Vector3(12.2f, 0.8f, -29.6f));
            yield return Shot("93_lamps_trail", new Vector3(-2f, 3.5f, -14f), new Vector3(-12f, 2.2f, -4f));
            yield return Shot("94_bonfire", new Vector3(5f, 4f, 24f), new Vector3(12.2f, 1f, 32.2f));
        }
    }
}
