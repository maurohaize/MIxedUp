using System;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Player preferences edited in the settings menu: volumes, look sensitivity, screen mode. Everything is saved in
    /// PlayerPrefs and applied immediately. Music and effects volumes are stored for the audio that comes later;
    /// the master volume already drives AudioListener.volume.
    /// </summary>
    public static class GameSettings
    {
        const string MasterKey = "settings.volume.master";
        const string MusicKey = "settings.volume.music";
        const string SfxKey = "settings.volume.sfx";
        const string MouseKey = "settings.look.mouse";
        const string GamepadKey = "settings.look.gamepad";
        const string InvertKey = "settings.look.invertY";
        const string FullscreenKey = "settings.screen.fullscreen";

        public const float MinSensitivity = 0.2f;
        public const float MaxSensitivity = 3f;

        public static event Action Changed;

        static bool loaded;
        static float master = 1f, music = 0.8f, sfx = 0.8f, mouse = 1f, gamepad = 1f;
        static bool invertY, fullscreen = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            loaded = false;
            Changed = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplyOnStart()
        {
            Load();
            AudioListener.volume = master;
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            master = PlayerPrefs.GetFloat(MasterKey, 1f);
            music = PlayerPrefs.GetFloat(MusicKey, 0.8f);
            sfx = PlayerPrefs.GetFloat(SfxKey, 0.8f);
            mouse = PlayerPrefs.GetFloat(MouseKey, 1f);
            gamepad = PlayerPrefs.GetFloat(GamepadKey, 1f);
            invertY = PlayerPrefs.GetInt(InvertKey, 0) == 1;
            fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        }

        public static float MasterVolume { get { Load(); return master; } set { Set(ref master, Mathf.Clamp01(value), MasterKey); AudioListener.volume = master; } }
        public static float MusicVolume { get { Load(); return music; } set => Set(ref music, Mathf.Clamp01(value), MusicKey); }
        public static float SfxVolume { get { Load(); return sfx; } set => Set(ref sfx, Mathf.Clamp01(value), SfxKey); }

        /// <summary>Multiplier on the camera's mouse turn speed (1 = default).</summary>
        public static float MouseSensitivity { get { Load(); return mouse; } set => Set(ref mouse, Mathf.Clamp(value, MinSensitivity, MaxSensitivity), MouseKey); }
        public static float GamepadSensitivity { get { Load(); return gamepad; } set => Set(ref gamepad, Mathf.Clamp(value, MinSensitivity, MaxSensitivity), GamepadKey); }

        public static bool InvertY
        {
            get { Load(); return invertY; }
            set
            {
                Load();
                if (invertY == value) return;
                invertY = value;
                PlayerPrefs.SetInt(InvertKey, value ? 1 : 0);
                Changed?.Invoke();
            }
        }

        public static bool Fullscreen
        {
            get { Load(); return fullscreen; }
            set
            {
                Load();
                if (fullscreen == value) return;
                fullscreen = value;
                PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
                Screen.fullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                Changed?.Invoke();
            }
        }

        static void Set(ref float field, float value, string key)
        {
            Load();
            if (Mathf.Approximately(field, value)) return;
            field = value;
            PlayerPrefs.SetFloat(key, value);
            Changed?.Invoke();
        }

        public static void ResetToDefaults()
        {
            MasterVolume = 1f;
            MusicVolume = 0.8f;
            SfxVolume = 0.8f;
            MouseSensitivity = 1f;
            GamepadSensitivity = 1f;
            InvertY = false;
        }
    }
}
