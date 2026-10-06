using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>Renders the new landmarks and hazards to PNG files. Explicit: needs a GPU. Output folder: env MIXEDUP_SHOTS.</summary>
    public class WorldScreenshots : SceneTestBase
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

        IEnumerator Shot(string name, Vector3 position, Vector3 lookAt)
        {
            var cam = Camera.main;
            cam.GetComponent<ThirdPersonCamera>().enabled = false;
            cam.transform.position = position;
            cam.transform.rotation = Quaternion.LookRotation(lookAt - position);
            ui.hudRoot.SetActive(false);

            var rt = new RenderTexture(Width, Height, 24);
            cam.targetTexture = rt;
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
        public IEnumerator WorldGallery()
        {
            yield return Shot("60_overview_south", new Vector3(0f, 48f, -66f), new Vector3(0f, 0f, 14f));
            yield return Shot("61_overview_north", new Vector3(0f, 60f, 4f), new Vector3(0f, 0f, 38f));
            yield return Shot("62_start_camp", new Vector3(24f, 5f, -36f), new Vector3(14f, 1f, -29f));
            yield return Shot("63_swamp", new Vector3(-30f, 7f, 8f), new Vector3(-17f, 0f, 22f));
            yield return Shot("64_bonfire_camp", new Vector3(3f, 6f, 24f), new Vector3(12f, 1f, 32f));
            yield return Shot("65_sweeper", new Vector3(14f, 6f, 16f), new Vector3(22f, 0.5f, 26f));
            yield return Shot("66_windmill", new Vector3(10f, 4f, 2f), new Vector3(22f, 4f, 19f));
            yield return Shot("67_mushroom_platform", new Vector3(-8f, 6f, 33f), new Vector3(-18f, 2f, 42f));
            yield return Shot("68_farm", new Vector3(-2f, 7f, 31f), new Vector3(0f, 2f, 47f));
            yield return Shot("69_stepping_stones", new Vector3(14f, 5f, 0f), new Vector3(21f, 0f, 9f));
            yield return Shot("70_trail_to_bridge", new Vector3(-6f, 4f, -14f), new Vector3(-20f, 1f, 2f));
            yield return Shot("71_bridge_north_bank", new Vector3(-32f, 6f, 6f), new Vector3(-22f, 1f, 22f));
        }
    }
}
