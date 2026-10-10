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
    }

    public class HarbourGallery : MapGalleryBase
    {
        protected override string SceneToLoad => "Assets/Scenes/Level_Harbour.unity";

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator Harbour() => Gallery("harbour");
    }
}
