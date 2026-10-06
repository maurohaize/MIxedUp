using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// Renders scenes and models to PNG from batch mode, with a GPU (no -nographics), for visual review.
    /// Self-contained on purpose so it can be dropped into any Unity project.
    ///   Unity.exe -batchmode -projectPath P -executeMethod MixedUp.EditorTools.ScreenshotTool.CaptureScene
    ///             -shotScene Assets/Scenes/X.unity -shotOut C:/out [-shotNoUi] [-shotW 1280 -shotH 720]
    ///   ... CaptureModels -shotModels "Assets/a.fbx;Assets/b.prefab" -shotOut C:/out
    /// </summary>
    public static class ScreenshotTool
    {
        static string Arg(string name, string fallback = null)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }

        static bool Flag(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;

        static int Size(string name, int fallback) => int.TryParse(Arg(name), out int v) ? v : fallback;

        public static void CaptureScene()
        {
            string scenePath = Arg("-shotScene");
            string outDir = Arg("-shotOut", "Temp/Shots");
            Directory.CreateDirectory(outDir);

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int w = Size("-shotW", 1280), h = Size("-shotH", 720);
            string prefix = Arg("-shotPrefix", Path.GetFileNameWithoutExtension(scenePath));

            if (Flag("-shotNoUi"))
                foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
                    canvas.gameObject.SetActive(false);

            if (Flag("-shotNoVolume"))
                foreach (var behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>())
                    if (behaviour != null && behaviour.GetType().Name == "Volume") behaviour.enabled = false;

            var main = Camera.main != null ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (main != null && Flag("-shotNoPostCam"))
            {
                var data = main.GetComponent("UniversalAdditionalCameraData");
                var property = data != null ? data.GetType().GetProperty("renderPostProcessing") : null;
                if (property != null) property.SetValue(data, false);
            }

            if (main != null)
            {
                BindCanvases(main);
                // The first frames after opening a scene render with uninitialised ambient lighting, so warm up first.
                for (int i = 0; i < 3; i++) Render(main, w, h, Path.Combine(outDir, prefix + "_warmup.png"));
                Render(main, w, h, Path.Combine(outDir, prefix + "_camera.png"));
            }

            var bounds = SceneBounds();
            var rig = new GameObject("ShotCamera").AddComponent<Camera>();
            rig.nearClipPlane = 0.3f;
            rig.farClipPlane = Mathf.Max(500f, bounds.size.magnitude * 3f);
            rig.clearFlags = main != null ? main.clearFlags : CameraClearFlags.Skybox;
            rig.backgroundColor = main != null ? main.backgroundColor : Color.gray;

            float d = Mathf.Max(8f, bounds.size.magnitude * 0.55f);
            Frame(rig, bounds.center, new Vector3(0.0f, 0.8f, -1.0f), d);
            Render(rig, w, h, Path.Combine(outDir, prefix + "_overview.png"));
            Frame(rig, bounds.center, new Vector3(1.0f, 0.55f, -1.0f), d * 0.7f);
            Render(rig, w, h, Path.Combine(outDir, prefix + "_overview2.png"));

            string extra = Arg("-shotViews");
            if (!string.IsNullOrEmpty(extra))
            {
                // "name:px,py,pz:lx,ly,lz;name2:..."
                foreach (var view in extra.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = view.Split(':');
                    var p = ParseVector(parts[1]);
                    var l = ParseVector(parts[2]);
                    rig.transform.position = p;
                    rig.transform.rotation = Quaternion.LookRotation(l - p);
                    rig.fieldOfView = 60f;
                    Render(rig, w, h, Path.Combine(outDir, prefix + "_" + parts[0] + ".png"));
                }
            }

            Debug.Log("[ScreenshotTool] Saved " + prefix + " views to " + outDir);
            EditorApplication.Exit(0);
        }

        public static void CaptureModels()
        {
            string outDir = Arg("-shotOut", "Temp/Shots");
            Directory.CreateDirectory(outDir);
            int w = Size("-shotW", 800), h = Size("-shotH", 600);

            foreach (var path in Arg("-shotModels", string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null)
                {
                    Debug.LogWarning("[ScreenshotTool] Not a model: " + path);
                    continue;
                }

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var light = new GameObject("Light").AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
                light.intensity = 1.2f;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.6f, 0.62f, 0.68f);

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                var bounds = SceneBounds();

                var cam = new GameObject("Cam").AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.78f, 0.82f, 0.86f);
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 1000f;
                float d = Mathf.Max(0.5f, bounds.size.magnitude * 1.1f);

                string name = Path.GetFileNameWithoutExtension(path);
                Frame(cam, bounds.center, new Vector3(1f, 0.7f, -1f), d);
                Render(cam, w, h, Path.Combine(outDir, name + "_iso.png"));
                Frame(cam, bounds.center, new Vector3(0f, 0.3f, -1f), d);
                Render(cam, w, h, Path.Combine(outDir, name + "_front.png"));

                Debug.Log("[ScreenshotTool] " + name + " size " + bounds.size + " renderers " + instance.GetComponentsInChildren<Renderer>().Length);
            }
            EditorApplication.Exit(0);
        }

        // ----------------------------------------------------------- helpers

        static Vector3 ParseVector(string text)
        {
            var c = text.Split(',').Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            return new Vector3(c[0], c[1], c[2]);
        }

        static void BindCanvases(Camera cam)
        {
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }
        }

        static Bounds SceneBounds()
        {
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude)
                .Where(r => r.enabled && !(r is ParticleSystemRenderer) && r.bounds.size.magnitude < 400f).ToArray();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one * 10f);

            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        static void Frame(Camera cam, Vector3 center, Vector3 direction, float distance)
        {
            cam.transform.position = center + direction.normalized * distance;
            cam.transform.rotation = Quaternion.LookRotation(center - cam.transform.position);
        }

        static void Render(Camera cam, int w, int h, string file)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var previousTarget = cam.targetTexture;
            var previousActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(file, tex.EncodeToPNG());

            cam.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
