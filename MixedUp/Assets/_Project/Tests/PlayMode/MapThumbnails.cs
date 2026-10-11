using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>
    /// Takes the pictures the map selectors show (Resources/MapThumbs/id.png): what a player sees standing in the most telling
    /// spot of each map, not a map seen from the sky. Explicit: needs a GPU. Run it again whenever a map changes, then commit
    /// the PNGs. `Candidates` renders a few other viewpoints (to MIXEDUP_SHOTS) to choose from.
    /// </summary>
    public abstract class MapThumbnailBase : SceneTestBase
    {
        const int Width = 640, Height = 360;
        const float FieldOfView = 52f;

        protected abstract string MapId { get; }
        protected abstract Vector3 CameraAt { get; }
        protected abstract Vector3 LookAt { get; }
        protected virtual (Vector3 at, Vector3 look)[] Others => System.Array.Empty<(Vector3, Vector3)>();

        protected IEnumerator Render(Vector3 at, Vector3 look, string file)
        {
            var cam = Camera.main;
            cam.GetComponent<ThirdPersonCamera>().enabled = false;
            cam.fieldOfView = FieldOfView;
            cam.transform.position = at;
            cam.transform.rotation = Quaternion.LookRotation(look - at);
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
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllBytes(file, tex.EncodeToPNG());

            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }

        protected IEnumerator Take()
        {
            string dir = System.Environment.GetEnvironmentVariable("MIXEDUP_THUMBS");
            if (string.IsNullOrEmpty(dir)) dir = "Assets/_Project/Resources/MapThumbs";
            yield return Render(CameraAt, LookAt, Path.Combine(dir, MapId + ".png"));
        }

        protected IEnumerator TakeCandidates()
        {
            string dir = System.Environment.GetEnvironmentVariable("MIXEDUP_SHOTS");
            if (string.IsNullOrEmpty(dir)) dir = "Temp/Shots";
            yield return Render(CameraAt, LookAt, Path.Combine(dir, "thumb_" + MapId + "_0.png"));
            for (int i = 0; i < Others.Length; i++)
                yield return Render(Others[i].at, Others[i].look, Path.Combine(dir, "thumb_" + MapId + "_" + (i + 1) + ".png"));
        }
    }

    public class MeadowThumbnail : MapThumbnailBase
    {
        protected override string MapId => "meadow";
        // On the trail north of the river: the farm, the barn and the scarecrow, a camp fire and the autumn forest on the hills.
        protected override Vector3 CameraAt => new Vector3(-2f, 3.6f, 16f);
        protected override Vector3 LookAt => new Vector3(2f, 2f, 44f);
        protected override (Vector3 at, Vector3 look)[] Others => new[]
        {
            (new Vector3(9f, 3.4f, -24f), new Vector3(-6f, 1.6f, 4f)),
            (new Vector3(-6f, 3.6f, -14f), new Vector3(-22f, 1.5f, 8f)),
            (new Vector3(4f, 3.2f, 20f), new Vector3(16f, 3f, 30f))
        };

        [UnityTest, Explicit("Needs a GPU; writes a PNG into the project")]
        public IEnumerator Meadow() => Take();

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator Candidates() => TakeCandidates();
    }

    public class SummitThumbnail : MapThumbnailBase
    {
        protected override string SceneToLoad => "Assets/Scenes/Level_Summit.unity";
        protected override string MapId => "summit";
        // From the west shore, over the frozen lake to its islet, the pine woods, the pylon and the snowy mountains.
        protected override Vector3 CameraAt => new Vector3(-12f, 3.4f, -16f);
        protected override Vector3 LookAt => new Vector3(10f, 1.5f, -2f);
        protected override (Vector3 at, Vector3 look)[] Others => new[]
        {
            (new Vector3(-2f, 3.2f, -25f), new Vector3(6f, 1.4f, 2f)),
            (new Vector3(-21f, 3.2f, 6f), new Vector3(-28f, 1.4f, 18f)),
            (new Vector3(2f, 3.2f, 22f), new Vector3(10f, 4f, 52f))
        };

        [UnityTest, Explicit("Needs a GPU; writes a PNG into the project")]
        public IEnumerator Summit() => Take();

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator Candidates() => TakeCandidates();
    }

    public class HarbourThumbnail : MapThumbnailBase
    {
        protected override string SceneToLoad => "Assets/Scenes/Level_Harbour.unity";
        protected override string MapId => "harbour";
        // On the quay at dusk: the crane over the water, the fish market and the old lighthouse with its scaffolding.
        protected override Vector3 CameraAt => new Vector3(14f, 3.4f, -22f);
        protected override Vector3 LookAt => new Vector3(36f, 4f, -14f);
        protected override (Vector3 at, Vector3 look)[] Others => new[]
        {
            (new Vector3(-10f, 3.4f, -17f), new Vector3(14f, 2f, 0f)),
            (new Vector3(-2f, 3.6f, -4f), new Vector3(10f, 1.5f, 24f)),
            (new Vector3(-14f, 3.2f, -22f), new Vector3(-26f, 1.6f, -27f))
        };

        [UnityTest, Explicit("Needs a GPU; writes a PNG into the project")]
        public IEnumerator Harbour() => Take();

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator Candidates() => TakeCandidates();
    }
}
