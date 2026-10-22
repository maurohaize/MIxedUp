using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MixedUp.Trailer
{
    /// <summary>What to film: written by the editor menu (or read from the command line in batch mode).</summary>
    [Serializable]
    public class TrailerRequest
    {
        public string shots = "";          // comma separated ids; empty = all
        public string output = "";
        public int width = 1920, height = 1080;
        public int fps = 30;
        public int quality = 93;
        public string formats = "h";      // "h", "v" or "h,v"
        public float scale = 1f;          // preview renders at a fraction of the size
        public int maxFrames = 0;         // > 0 cuts every shot short (quick checks)

        public static string FilePath => Path.Combine(Application.dataPath, "..", "Temp", "TrailerRequest.json");

        public static TrailerRequest Load()
        {
            TrailerRequest request = null;
            if (File.Exists(FilePath)) request = JsonUtility.FromJson<TrailerRequest>(File.ReadAllText(FilePath));
            var args = Environment.GetCommandLineArgs();
            string Arg(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
            if (Arg("-trailerOut") != null)
            {
                request ??= new TrailerRequest();
                request.output = Arg("-trailerOut");
                request.shots = Arg("-trailerShots") ?? request.shots;
                request.formats = Arg("-trailerFormats") ?? request.formats;
                if (int.TryParse(Arg("-trailerFps"), out int fps)) request.fps = fps;
                if (float.TryParse(Arg("-trailerScale"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float scale)) request.scale = scale;
                if (int.TryParse(Arg("-trailerMaxFrames"), out int max)) request.maxFrames = max;
            }
            return request;
        }
    }

    [Serializable] public class SubtitleLine { public float t; public float dur; public string who; public string colour; public string text; }
    [Serializable] public class SfxCue { public float t; public string id; public float volume; public float pitch; public float distance; public float pan; }
    [Serializable] public class ShotMarker { public float t; public string label; }

    [Serializable]
    public class ShotRecord
    {
        public string id;
        public string format;
        public int width, height, fps, frames;
        public float duration;
        public List<SubtitleLine> subtitles = new List<SubtitleLine>();
        public List<SfxCue> sfx = new List<SfxCue>();
        public List<ShotMarker> markers = new List<ShotMarker>();
    }

    /// <summary>
    /// Films the trailer shots one after another inside the real game: loads each map, casts the four friends, runs the
    /// shot's script and saves every frame at a fixed frame rate (game time advances exactly one frame per picture, so a
    /// slow machine renders slower but the result is smooth). Also writes what is said and which sounds play, for the edit.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public class TrailerDirector : MonoBehaviour
    {
        public static TrailerDirector Instance { get; private set; }
        public TrailerRequest request;

        ShotContext current;
        bool recording;
        int frame;
        RenderTexture target;
        Texture2D readback;
        ShotRecord record;
        string frameDir;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var request = TrailerRequest.Load();
            if (request == null || string.IsNullOrEmpty(request.output)) return;
            var go = new GameObject("TrailerDirector");
            DontDestroyOnLoad(go);
            var director = go.AddComponent<TrailerDirector>();
            director.request = request;
        }

        void Start()
        {
            Instance = this;
            Application.runInBackground = true;
            StartCoroutine(Run());
        }

        static void Log(string message) => Debug.Log("[Trailer] " + message);

        IEnumerator Run()
        {
            Directory.CreateDirectory(request.output);
            var wanted = new HashSet<string>(request.shots.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            var formats = request.formats.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var shot in TrailerShots.All)
            {
                if (wanted.Count > 0 && !wanted.Contains(shot.id)) continue;
                foreach (var format in formats)
                {
                    bool vertical = format.Trim() == "v";
                    if (vertical && !shot.vertical) continue;
                    yield return Film(shot, vertical);
                }
            }

            Log("All shots done.");
            Finish();
        }

        IEnumerator Film(Shot shot, bool vertical)
        {
            int w = Mathf.RoundToInt((vertical ? request.height : request.width) * request.scale);
            int h = Mathf.RoundToInt((vertical ? request.width : request.height) * request.scale);
            Log($"Shot {shot.id} {(vertical ? "vertical" : "horizontal")} {w}x{h} in {shot.scene}");

            Time.captureFramerate = 0;
            LevelDirector.Override = shot.mode ?? GameModes.Classic;
            var load = SceneManager.LoadSceneAsync(shot.scene, LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;
            yield return null;

            Time.captureFramerate = request.fps;
            UnityEngine.Random.InitState(1234);
            current = new ShotContext(this, shot, vertical, w, h);
            current.Prepare();
            shot.setup?.Invoke(current);
            if (shot.action != null) current.Run(shot.action(current));

            // Settle (physics, boxes landing, cameras catching up) before the first frame that counts.
            current.T = -shot.preroll;
            while (current.T < 0f)
            {
                yield return null;
                current.T += Time.deltaTime;
                current.Camera.time = current.T;
            }

            target = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 1 };
            readback = new Texture2D(w, h, TextureFormat.RGB24, false);
            record = new ShotRecord { id = shot.id, format = vertical ? "v" : "h", width = w, height = h, fps = request.fps, duration = shot.duration };
            frameDir = Path.Combine(request.output, $"{shot.id}_{(vertical ? "v" : "h")}");
            if (Directory.Exists(frameDir)) Directory.Delete(frameDir, true);
            Directory.CreateDirectory(frameDir);
            AudioManager.Played += OnSfx;

            int total = Mathf.RoundToInt(shot.duration * request.fps);
            if (request.maxFrames > 0) total = Mathf.Min(total, request.maxFrames);
            frame = 0;
            recording = true;
            float started = Time.realtimeSinceStartup;
            while (frame < total)
            {
                yield return new WaitForEndOfFrame();
                Capture();
                frame++;
                current.T += 1f / request.fps;
                current.Camera.time = current.T;
                if (frame % 60 == 0) Log($"  {shot.id}: {frame}/{total} frames, {(Time.realtimeSinceStartup - started) / frame:0.00} s per frame");
            }
            recording = false;
            AudioManager.Played -= OnSfx;
            current.StopAll();

            record.frames = frame;
            record.subtitles = current.subtitles;
            record.markers = current.markers;
            File.WriteAllText(Path.Combine(request.output, $"{shot.id}_{record.format}.json"), JsonUtility.ToJson(record, true));
            target.Release();
            Destroy(target);
            Destroy(readback);
            Time.captureFramerate = 0;
            current = null;
        }

        /// <summary>Runs a shot coroutine and logs (instead of killing the whole recording) if it throws.</summary>
        public IEnumerator Guard(IEnumerator action)
        {
            while (true)
            {
                object step;
                try
                {
                    if (!action.MoveNext()) yield break;
                    step = action.Current;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    yield break;
                }
                yield return step;
            }
        }

        void Capture()
        {
            var cam = current.Camera.GetComponent<Camera>();
            var previous = cam.targetTexture;
            var request = new RenderPipeline.StandardRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(cam, request))
            {
                RenderPipeline.SubmitRenderRequest(cam, request);
            }
            else
            {
                cam.targetTexture = target;
                cam.Render();
                cam.targetTexture = previous;
            }

            var active = RenderTexture.active;
            RenderTexture.active = target;
            readback.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0, false);
            readback.Apply(false);
            RenderTexture.active = active;
            File.WriteAllBytes(Path.Combine(frameDir, frame.ToString("00000") + ".jpg"), readback.EncodeToJPG(this.request.quality));
        }

        void OnSfx(SfxId id, Vector3? position, float volume, float pitch)
        {
            if (!recording || current == null) return;
            var camT = current.Camera.transform;
            float distance = position.HasValue ? Vector3.Distance(position.Value, camT.position) : 0f;
            float pan = position.HasValue ? Mathf.Clamp(Vector3.Dot((position.Value - camT.position).normalized, camT.right), -1f, 1f) : 0f;
            record.sfx.Add(new SfxCue { t = current.T, id = id.ToString(), volume = volume, pitch = pitch, distance = distance, pan = pan });
        }

        void Finish()
        {
            Time.captureFramerate = 0;
            if (File.Exists(TrailerRequest.FilePath)) File.Delete(TrailerRequest.FilePath);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            if (Application.isBatchMode) UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit();
#endif
        }
    }
}
