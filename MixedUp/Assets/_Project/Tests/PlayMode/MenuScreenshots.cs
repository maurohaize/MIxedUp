using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>
    /// Renders the menus to PNG files so they can be inspected without opening the editor. Explicit: needs a GPU.
    /// Output folder: env MIXEDUP_SHOTS, or Temp/Shots.
    /// </summary>
    public class MenuScreenshots : SceneTestBase
    {
        const int Width = 1920, Height = 1080;

        protected override string SceneToLoad => MenuScenePath;
        protected override bool NeedsPlayer => false;

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

        public static IEnumerator Capture(string name)
        {
            var cam = Camera.main;
            var canvas = Object.FindAnyObjectByType<Canvas>();
            var rt = new RenderTexture(Width, Height, 24);
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;

            cam.targetTexture = rt;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
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
            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCamera;
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator MainMenuGallery()
        {
            var menu = Object.FindAnyObjectByType<MainMenu>();
            Localization.SetLanguage(Language.Spanish);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture("50_main_menu");

            // Hover over a plank.
            menu.settingsButton.Select();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Capture("51_main_menu_selected");

            menu.settingsButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Capture("52_settings");

            menu.settings.clothesSwatches[9].onClick.Invoke();
            menu.settings.skinSwatches[3].onClick.Invoke();
            menu.settings.basqueButton.onClick.Invoke();
            menu.settings.masterSlider.value = 0.6f;
            menu.settings.mouseSlider.value = 1.8f;
            menu.settings.invertToggle.isOn = true;
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Capture("53_settings_basque_orange");

            // Put the preferences back.
            CharacterCustomization.Reset();
            menu.settings.basqueButton.onClick.Invoke();
            Localization.SetLanguage(Language.Spanish);
            menu.settings.resetButton.onClick.Invoke();
        }
    }
}
