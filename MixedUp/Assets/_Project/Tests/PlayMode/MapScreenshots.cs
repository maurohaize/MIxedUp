using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>Renders the two extra maps from several viewpoints to PNG files. Explicit: needs a GPU. Output folder: env MIXEDUP_SHOTS.</summary>
    public abstract class MapGalleryBase : SceneTestBase
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

        protected IEnumerator Shot(string name, Vector3 position, Vector3 lookAt)
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

        protected IEnumerator Gallery(string prefix)
        {
            yield return Shot(prefix + "_01_overview_south", new Vector3(0f, 52f, -70f), new Vector3(0f, 0f, 12f));
            yield return Shot(prefix + "_02_overview_north", new Vector3(0f, 62f, 6f), new Vector3(0f, 0f, 40f));
            yield return Shot(prefix + "_03_overview_west", new Vector3(-70f, 40f, 10f), new Vector3(0f, 0f, 10f));
            yield return Shot(prefix + "_04_overview_east", new Vector3(70f, 40f, 10f), new Vector3(0f, 0f, 10f));
            yield return Shot(prefix + "_05_start", new Vector3(14f, 5f, -38f), new Vector3(0f, 1f, -26f));
            yield return Shot(prefix + "_06_middle", new Vector3(0f, 14f, -4f), new Vector3(0f, 0f, 14f));
            yield return Shot(prefix + "_07_far", new Vector3(0f, 12f, 30f), new Vector3(0f, 1f, 50f));
            yield return Shot(prefix + "_08_low_south", new Vector3(-20f, 3f, -34f), new Vector3(10f, 2f, 0f));
        }
    }

    public class SummitGallery : MapGalleryBase
    {
        protected override string SceneToLoad => "Assets/Scenes/Level_Summit.unity";

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator Summit() => Gallery("summit");

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator SummitDetails()
        {
            yield return Shot("summit_d01_hut", new Vector3(-14f, 3.2f, 36f), new Vector3(-20f, 1.6f, 46f));
            yield return Shot("summit_d02_camp", new Vector3(-21f, 3.4f, 7f), new Vector3(-30f, 1f, 15f));
            yield return Shot("summit_d03_yeti", new Vector3(-31f, 4f, 42f), new Vector3(-40.5f, 1.8f, 52.5f));
            yield return Shot("summit_d04_lake", new Vector3(-10f, 3.5f, -22f), new Vector3(8f, 0f, -6f));
            yield return Shot("summit_d05_signpost", new Vector3(-12f, 2.8f, -15f), new Vector3(-18.6f, 2.4f, -9.6f));
            yield return Shot("summit_d06_mine", new Vector3(-33f, 3f, 21f), new Vector3(-37f, 1.5f, 30f));
            yield return Shot("summit_d07_pylon", new Vector3(19f, 3.5f, 6f), new Vector3(31f, 4f, 16f));
            yield return Shot("summit_d08_dump", new Vector3(6f, 3f, 33f), new Vector3(0f, 1f, 43f));
            yield return Shot("summit_d09_tower", new Vector3(31f, 4.5f, 2f), new Vector3(38f, 4f, -7f));
            yield return Shot("summit_d10_lift", new Vector3(8f, 3f, 40f), new Vector3(12f, 5f, 58f));
            yield return Shot("summit_d11_igloo", new Vector3(-32f, 3f, -3f), new Vector3(-40.5f, 1f, -7f));
            yield return Shot("summit_d12_arch", new Vector3(-26f, 3f, -17f), new Vector3(-26f, 2f, -40f));
            yield return Shot("summit_d13_glacier", new Vector3(-22f, 3.4f, -16f), new Vector3(-30f, 2f, -2f));
            yield return Shot("summit_d14_cliff", new Vector3(22f, 4f, 38f), new Vector3(33f, 3f, 50f));
        }
    }

    public class HarbourGallery : MapGalleryBase
    {
        protected override string SceneToLoad => "Assets/Scenes/Level_Harbour.unity";

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator Harbour() => Gallery("harbour");

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator HarbourDetails()
        {
            yield return Shot("harbour_d01_warehouse", new Vector3(30f, 4f, -22f), new Vector3(40f, 1.5f, -30f));
            yield return Shot("harbour_d02_crane_market", new Vector3(18f, 4f, -21f), new Vector3(30f, 2f, -12f));
            yield return Shot("harbour_d03_kraken", new Vector3(24f, 4f, 34f), new Vector3(33f, 1f, 45f));
            yield return Shot("harbour_d04_lighthouse", new Vector3(26f, 4.5f, -27f), new Vector3(38f, 6f, -19f));
            yield return Shot("harbour_d05_office", new Vector3(-6f, 3.5f, -20f), new Vector3(-14f, 1.5f, -13f));
            yield return Shot("harbour_d06_gull_pier", new Vector3(-1f, 3.2f, 11.5f), new Vector3(2.5f, 1f, 17.5f));
            yield return Shot("harbour_d07_tavern", new Vector3(-16f, 3.2f, -20f), new Vector3(-24f, 1.5f, -28f));
            yield return Shot("harbour_d08_containers", new Vector3(-26f, 4f, -21f), new Vector3(-38f, 3f, -12f));
            yield return Shot("harbour_d09_promenade", new Vector3(-22f, 3f, -15f), new Vector3(10f, 2f, -9f));
            yield return Shot("harbour_d10_bay", new Vector3(0f, 4f, 20f), new Vector3(0f, 2f, 60f));
            yield return Shot("harbour_d11_barge", new Vector3(-6f, 4f, 22f), new Vector3(-18f, 1f, 36f));
            yield return Shot("harbour_d12_drydock", new Vector3(-33f, 3f, -23f), new Vector3(-40f, 1.5f, -30f));
            yield return Shot("harbour_d13_island", new Vector3(22f, 4f, 14f), new Vector3(32f, 1f, 22f));
            yield return Shot("harbour_d14_arch", new Vector3(0f, 3f, -17f), new Vector3(0f, 2f, -40f));
        }
    }

    public class MeadowGallery : MapGalleryBase
    {
        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator MeadowChallenges()
        {
            yield return Shot("meadow_c1_tower", new Vector3(28f, 4f, -14f), new Vector3(38f, 4f, -7f));
            yield return Shot("meadow_c2_cliff", new Vector3(22f, 4f, 38f), new Vector3(33f, 3f, 50f));
            yield return Shot("meadow_c3_stump", new Vector3(-10f, 4f, 34f), new Vector3(-18f, 1.5f, 43f));
            yield return Shot("meadow_c4_tunnel", new Vector3(-31f, 3f, 23f), new Vector3(-37f, 1.5f, 32f));
            yield return Shot("meadow_c5_swamp", new Vector3(-8f, 4f, 14f), new Vector3(-17f, 0.5f, 23f));
            yield return Shot("meadow_c6_walls", new Vector3(20f, 3.5f, 24f), new Vector3(30f, 1f, 33f));
        }
    }
}
