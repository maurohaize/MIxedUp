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
        public IEnumerator LightsGallery()
        {
            yield return Shot("92_camp_fire", new Vector3(16f, 3f, -34f), new Vector3(12.2f, 0.8f, -29.6f));
            yield return Shot("93_lamps_trail", new Vector3(-2f, 3.5f, -14f), new Vector3(-12f, 2.2f, -4f));
            yield return Shot("94_bonfire", new Vector3(5f, 4f, 24f), new Vector3(12.2f, 1f, 32.2f));
        }
    }
}
