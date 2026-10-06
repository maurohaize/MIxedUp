using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>
    /// The settings menu, shared by the main menu and the pause menu: volumes, look sensitivity, screen mode,
    /// language and the character's skin tone and clothes colour (with a live preview). Every control edits
    /// GameSettings / Localization / CharacterCustomization directly, which save themselves.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Sound")]
        public Slider masterSlider;
        public Slider musicSlider;
        public Slider sfxSlider;
        public TMP_Text masterValue, musicValue, sfxValue;

        [Header("Controls")]
        public Slider mouseSlider;
        public Slider gamepadSlider;
        public TMP_Text mouseValue, gamepadValue;
        public Toggle invertToggle;

        [Header("Screen")]
        public Toggle fullscreenToggle;

        [Header("Language")]
        public Button basqueButton;
        public Button spanishButton;
        public Button englishButton;
        public GameObject basqueMark, spanishMark, englishMark;

        [Header("Character")]
        public PlayerPalette palette;
        public Button[] skinSwatches = Array.Empty<Button>();
        public Image[] skinRings = Array.Empty<Image>();
        public Button[] clothesSwatches = Array.Empty<Button>();
        public Image[] clothesRings = Array.Empty<Image>();

        [Header("Buttons")]
        public Button backButton;
        public Button resetButton;

        public event Action Closed;

        bool wired;
        bool syncing;

        public bool IsOpen => gameObject.activeSelf;

        void Awake() => Wire();

        void OnEnable()
        {
            Wire();
            Localization.LanguageChanged += RefreshLanguage;
            CharacterCustomization.Changed += RefreshSwatches;
            Sync();
        }

        void OnDisable()
        {
            Localization.LanguageChanged -= RefreshLanguage;
            CharacterCustomization.Changed -= RefreshSwatches;
        }

        public void Open() => gameObject.SetActive(true);

        public void Close()
        {
            if (!IsOpen) return;
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        void Wire()
        {
            if (wired) return;
            wired = true;

            // Ranges first: changing them clamps the current value and would otherwise overwrite the saved setting.
            syncing = true;
            masterSlider.minValue = musicSlider.minValue = sfxSlider.minValue = 0f;
            masterSlider.maxValue = musicSlider.maxValue = sfxSlider.maxValue = 1f;
            mouseSlider.minValue = gamepadSlider.minValue = GameSettings.MinSensitivity;
            mouseSlider.maxValue = gamepadSlider.maxValue = GameSettings.MaxSensitivity;
            syncing = false;

            masterSlider.onValueChanged.AddListener(v => { if (!syncing) GameSettings.MasterVolume = v; ShowPercent(masterValue, v); });
            musicSlider.onValueChanged.AddListener(v => { if (!syncing) GameSettings.MusicVolume = v; ShowPercent(musicValue, v); });
            sfxSlider.onValueChanged.AddListener(v => { if (!syncing) GameSettings.SfxVolume = v; ShowPercent(sfxValue, v); });
            mouseSlider.onValueChanged.AddListener(v => { if (!syncing) GameSettings.MouseSensitivity = v; ShowMultiplier(mouseValue, v); });
            gamepadSlider.onValueChanged.AddListener(v => { if (!syncing) GameSettings.GamepadSensitivity = v; ShowMultiplier(gamepadValue, v); });
            invertToggle.onValueChanged.AddListener(v => { if (!syncing) GameSettings.InvertY = v; });
            fullscreenToggle.onValueChanged.AddListener(v => { if (!syncing) GameSettings.Fullscreen = v; });

            basqueButton.onClick.AddListener(() => Localization.SetLanguage(Language.Basque));
            spanishButton.onClick.AddListener(() => Localization.SetLanguage(Language.Spanish));
            englishButton.onClick.AddListener(() => Localization.SetLanguage(Language.English));

            for (int i = 0; i < skinSwatches.Length; i++)
            {
                int index = i;
                skinSwatches[i].onClick.AddListener(() => CharacterCustomization.SkinIndex = index);
            }
            for (int i = 0; i < clothesSwatches.Length; i++)
            {
                int index = i;
                clothesSwatches[i].onClick.AddListener(() => CharacterCustomization.ClothesIndex = index);
            }

            backButton.onClick.AddListener(Close);
            resetButton.onClick.AddListener(() =>
            {
                GameSettings.ResetToDefaults();
                Sync();
            });
        }

        /// <summary>Shows the saved values without writing them back.</summary>
        void Sync()
        {
            syncing = true;
            masterSlider.value = GameSettings.MasterVolume;
            musicSlider.value = GameSettings.MusicVolume;
            sfxSlider.value = GameSettings.SfxVolume;
            mouseSlider.value = GameSettings.MouseSensitivity;
            gamepadSlider.value = GameSettings.GamepadSensitivity;
            invertToggle.isOn = GameSettings.InvertY;
            fullscreenToggle.isOn = GameSettings.Fullscreen;
            syncing = false;

            ShowPercent(masterValue, masterSlider.value);
            ShowPercent(musicValue, musicSlider.value);
            ShowPercent(sfxValue, sfxSlider.value);
            ShowMultiplier(mouseValue, mouseSlider.value);
            ShowMultiplier(gamepadValue, gamepadSlider.value);
            RefreshLanguage();
            RefreshSwatches();
        }

        static void ShowPercent(TMP_Text label, float value) => UiUtil.SetText(label, Mathf.RoundToInt(value * 100f) + "%");
        static void ShowMultiplier(TMP_Text label, float value) => UiUtil.SetText(label, "x" + value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));

        void RefreshLanguage()
        {
            var language = Localization.Current;
            if (basqueMark != null) basqueMark.SetActive(language == Language.Basque);
            if (spanishMark != null) spanishMark.SetActive(language == Language.Spanish);
            if (englishMark != null) englishMark.SetActive(language == Language.English);
        }

        void RefreshSwatches()
        {
            Mark(skinRings, CharacterCustomization.SkinIndex);
            Mark(clothesRings, CharacterCustomization.ClothesIndex);
        }

        static void Mark(Image[] rings, int selected)
        {
            for (int i = 0; i < rings.Length; i++)
                if (rings[i] != null) rings[i].enabled = i == selected;
        }
    }
}
