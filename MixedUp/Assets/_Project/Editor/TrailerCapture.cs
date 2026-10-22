using System;
using System.IO;
using MixedUp.Trailer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// Films the trailer shots in play mode (MixedUp > Grabar trailer) and exports the game's synthesised sounds for the edit.
    /// Batch mode: Unity -batchmode -projectPath P -executeMethod MixedUp.EditorTools.TrailerCapture.Batch
    ///             -trailerOut /out [-trailerShots S01,S02] [-trailerFormats h,v] [-trailerFps 30] [-trailerScale 0.5]
    /// </summary>
    public static class TrailerCapture
    {
        static string DefaultOutput => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Trailer", "capture"));

        [MenuItem("MixedUp/Grabar trailer")]
        public static void Menu()
        {
            if (!EditorUtility.DisplayDialog("Grabar trailer",
                    "Se van a grabar todos los planos del trailer (horizontal y vertical) en:\n" + DefaultOutput +
                    "\n\nTarda un rato y no hace falta tocar nada. ¿Empezar?", "Grabar", "Cancelar")) return;
            Start(new TrailerRequest { output = DefaultOutput, formats = "h,v" });
        }

        [MenuItem("MixedUp/Grabar trailer (prueba rapida)")]
        public static void Preview() => Start(new TrailerRequest { output = DefaultOutput + "_preview", formats = "h", scale = 0.4f });

        /// <summary>Entry point for -executeMethod. Unity keeps running (no -quit) until the director exits it.</summary>
        public static void Batch()
        {
            var request = TrailerRequest.Load() ?? new TrailerRequest();
            if (string.IsNullOrEmpty(request.output)) request.output = DefaultOutput;
            ExportSfx(Path.Combine(request.output, "sfx"));
            Start(request);
        }

        static void Start(TrailerRequest request)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TrailerRequest.FilePath));
            File.WriteAllText(TrailerRequest.FilePath, JsonUtility.ToJson(request));
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("MixedUp/Exportar sonidos (wav)")]
        public static void ExportSfxMenu() => ExportSfx(Path.Combine(DefaultOutput, "sfx"));

        /// <summary>Writes every synthesised effect (first take) as a 16-bit WAV so the edit can use the game's own sounds.</summary>
        public static void ExportSfx(string folder)
        {
            Directory.CreateDirectory(folder);
            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            {
                for (int variant = 0; variant < 3; variant++)
                {
                    var clip = ProceduralAudio.Get(id, variant);
                    if (clip == null) continue;
                    WriteWav(Path.Combine(folder, $"{id}_{variant}.wav"), clip);
                }
            }
            Debug.Log("[Trailer] Sounds exported to " + folder);
        }

        static void WriteWav(string path, AudioClip clip)
        {
            var data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);
            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                int bytes = data.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + bytes);
                writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)clip.channels);
                writer.Write(clip.frequency);
                writer.Write(clip.frequency * clip.channels * 2);
                writer.Write((short)(clip.channels * 2));
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(bytes);
                foreach (var sample in data) writer.Write((short)Mathf.Clamp(Mathf.RoundToInt(sample * 32767f), -32768, 32767));
            }
        }
    }
}
