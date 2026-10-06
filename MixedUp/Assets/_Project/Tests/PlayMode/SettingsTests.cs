using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MixedUp.Tests
{
    /// <summary>The pause menu and the settings menu it opens, played in the level scene.</summary>
    public class SettingsTests : SceneTestBase
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        float master, music, sfx, mouse, gamepad;
        bool invert;
        int skin, clothes;

        [UnitySetUp]
        public IEnumerator RememberSettings()
        {
            master = GameSettings.MasterVolume;
            music = GameSettings.MusicVolume;
            sfx = GameSettings.SfxVolume;
            mouse = GameSettings.MouseSensitivity;
            gamepad = GameSettings.GamepadSensitivity;
            invert = GameSettings.InvertY;
            skin = CharacterCustomization.SkinIndex;
            clothes = CharacterCustomization.ClothesIndex;
            yield break;
        }

        [UnityTearDown]
        public IEnumerator RestoreSettings()
        {
            GameSettings.MasterVolume = master;
            GameSettings.MusicVolume = music;
            GameSettings.SfxVolume = sfx;
            GameSettings.MouseSensitivity = mouse;
            GameSettings.GamepadSensitivity = gamepad;
            GameSettings.InvertY = invert;
            CharacterCustomization.SkinIndex = skin;
            CharacterCustomization.ClothesIndex = clothes;
            yield break;
        }

        IEnumerator OpenSettingsFromPause()
        {
            yield return Tap(Key.Escape);
            Assert.AreEqual(GameState.Paused, GameManager.Instance.State);
            ui.settingsButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(ui.settingsPanel.IsOpen, "the settings card opens");
        }

        static int HexOf(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Color32 c = block.GetColor(BaseColorId);
            return (c.r << 16) | (c.g << 8) | c.b;
        }

        // ------------------------------------------------------------ the pause menu

        [UnityTest]
        public IEnumerator PauseMenuOffersEveryOption()
        {
            yield return Tap(Key.Escape);
            foreach (var button in new[] { ui.resumeButton, ui.settingsButton, ui.manualButton, ui.menuButton, ui.pauseQuitButton })
            {
                Assert.IsNotNull(button);
                Assert.IsTrue(button.gameObject.activeInHierarchy, button.name);
            }
            Assert.IsFalse(ui.settingsPanel.IsOpen, "settings stay closed until asked for");
            yield return Tap(Key.Escape);
        }

        [UnityTest]
        public IEnumerator EscapeClosesSettingsBeforeItResumesTheGame()
        {
            yield return OpenSettingsFromPause();

            yield return Tap(Key.Escape);
            Assert.IsFalse(ui.settingsPanel.IsOpen, "first Escape closes the settings");
            Assert.AreEqual(GameState.Paused, GameManager.Instance.State, "and the game is still paused");
            Assert.IsTrue(ui.pausePanel.activeSelf, "back on the pause menu");

            yield return Tap(Key.Escape);
            Assert.AreEqual(GameState.Playing, GameManager.Instance.State, "second Escape resumes");
        }

        [UnityTest]
        public IEnumerator BackButtonReturnsToThePauseMenu()
        {
            yield return OpenSettingsFromPause();
            ui.settingsPanel.backButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(ui.settingsPanel.IsOpen);
            Assert.IsTrue(ui.pausePanel.activeSelf);
        }

        // --------------------------------------------------------------- the controls

        [UnityTest]
        public IEnumerator SlidersAndTogglesChangeTheSavedSettings()
        {
            yield return OpenSettingsFromPause();
            var panel = ui.settingsPanel;

            panel.masterSlider.value = 0.35f;
            panel.musicSlider.value = 0.2f;
            panel.sfxSlider.value = 0.9f;
            panel.mouseSlider.value = 2.2f;
            panel.gamepadSlider.value = 0.6f;
            panel.invertToggle.isOn = true;
            yield return null;

            Assert.That(GameSettings.MasterVolume, Is.EqualTo(0.35f).Within(0.001f));
            Assert.That(AudioListener.volume, Is.EqualTo(0.35f).Within(0.001f), "master volume drives the audio listener");
            Assert.That(GameSettings.MusicVolume, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(GameSettings.SfxVolume, Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(GameSettings.MouseSensitivity, Is.EqualTo(2.2f).Within(0.001f));
            Assert.That(GameSettings.GamepadSensitivity, Is.EqualTo(0.6f).Within(0.001f));
            Assert.IsTrue(GameSettings.InvertY);

            StringAssert.Contains("35", panel.masterValue.text);
            StringAssert.Contains("2.2", panel.mouseValue.text);

            // Reopening shows what was saved rather than the defaults.
            panel.Close();
            yield return null;
            panel.Open();
            yield return null;
            Assert.That(panel.masterSlider.value, Is.EqualTo(0.35f).Within(0.001f));
            Assert.IsTrue(panel.invertToggle.isOn);
        }

        [UnityTest]
        public IEnumerator OpeningTheSettingsNeverOverwritesWhatWasSaved()
        {
            GameSettings.MouseSensitivity = 2.5f;
            GameSettings.GamepadSensitivity = 0.7f;
            GameSettings.MasterVolume = 0.45f;

            yield return OpenSettingsFromPause();
            var panel = ui.settingsPanel;
            Assert.That(GameSettings.MouseSensitivity, Is.EqualTo(2.5f).Within(0.001f), "the first opening must not reset it");
            Assert.That(GameSettings.GamepadSensitivity, Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(GameSettings.MasterVolume, Is.EqualTo(0.45f).Within(0.001f));
            Assert.That(panel.mouseSlider.value, Is.EqualTo(2.5f).Within(0.001f), "and the slider shows it");
            Assert.That(panel.mouseSlider.minValue, Is.EqualTo(GameSettings.MinSensitivity));
            Assert.That(panel.mouseSlider.maxValue, Is.EqualTo(GameSettings.MaxSensitivity));
        }

        [UnityTest]
        public IEnumerator ResetRestoresTheDefaultSettings()
        {
            yield return OpenSettingsFromPause();
            var panel = ui.settingsPanel;
            panel.masterSlider.value = 0.1f;
            panel.mouseSlider.value = 3f;
            panel.invertToggle.isOn = true;
            yield return null;

            panel.resetButton.onClick.Invoke();
            yield return null;
            Assert.That(GameSettings.MasterVolume, Is.EqualTo(1f).Within(0.001f));
            Assert.That(GameSettings.MouseSensitivity, Is.EqualTo(1f).Within(0.001f));
            Assert.IsFalse(GameSettings.InvertY);
            Assert.That(panel.masterSlider.value, Is.EqualTo(1f).Within(0.001f), "the sliders move too");
        }

        [UnityTest]
        public IEnumerator InvertingTheYAxisTurnsTheCameraTheOtherWay()
        {
            var camera = Camera.main.GetComponent<ThirdPersonCamera>();
            var mouseDevice = InputSystem.GetDevice<Mouse>() ?? InputSystem.AddDevice<Mouse>();

            GameSettings.InvertY = false;
            float start = camera.transform.eulerAngles.x;
            InputSystem.QueueStateEvent(mouseDevice, new UnityEngine.InputSystem.LowLevel.MouseState { delta = new Vector2(0f, 60f) });
            yield return null;
            yield return null;
            float normal = Mathf.DeltaAngle(start, camera.transform.eulerAngles.x);

            GameSettings.InvertY = true;
            start = camera.transform.eulerAngles.x;
            InputSystem.QueueStateEvent(mouseDevice, new UnityEngine.InputSystem.LowLevel.MouseState { delta = new Vector2(0f, 60f) });
            yield return null;
            yield return null;
            float inverted = Mathf.DeltaAngle(start, camera.transform.eulerAngles.x);

            Assert.AreNotEqual(0f, normal, "moving the mouse turns the camera");
            Assert.AreEqual(Mathf.Sign(normal), -Mathf.Sign(inverted), "inverted turns the opposite way");
            InputSystem.RemoveDevice(mouseDevice);
        }

        // --------------------------------------------------------------- the character

        [UnityTest]
        public IEnumerator SwatchesRecolourThePlayerAndMarkTheChoice()
        {
            yield return OpenSettingsFromPause();
            var panel = ui.settingsPanel;
            Assert.AreEqual(5, panel.skinSwatches.Length, "five skin tones");
            Assert.AreEqual(10, panel.clothesSwatches.Length, "ten clothes colours");

            panel.skinSwatches[3].onClick.Invoke();
            panel.clothesSwatches[7].onClick.Invoke();
            yield return null;

            var dress = player.GetComponentsInChildren<Transform>(true).First(t => t.name == "Dress").GetComponent<Renderer>();
            var head = player.GetComponentsInChildren<Transform>(true).First(t => t.name == "Head").GetComponent<Renderer>();
            Assert.AreEqual(0xedb07e, HexOf(head), "4th skin tone");
            Assert.AreEqual(0x910b3f, HexOf(dress), "8th clothes colour");

            Assert.IsTrue(panel.skinRings[3].enabled);
            Assert.IsFalse(panel.skinRings[1].enabled);
            Assert.IsTrue(panel.clothesRings[7].enabled);
            Assert.AreEqual(1, panel.skinRings.Count(r => r.enabled), "exactly one skin tone is marked");
            Assert.AreEqual(1, panel.clothesRings.Count(r => r.enabled), "exactly one clothes colour is marked");
        }

        [UnityTest]
        public IEnumerator ThePreviewShowsTheCharacterAndFollowsTheChoice()
        {
            yield return OpenSettingsFromPause();
            var preview = ui.settingsPanel.GetComponentInChildren<CharacterPreview>(true);
            yield return null;

            Assert.IsNotNull(preview.Character, "a character stands in the studio");
            Assert.IsNotNull(preview.StudioCamera, "with its own camera");
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.IsNotNull(preview.target.texture, "rendered into the picture frame");
            Assert.Greater(preview.Character.transform.position.y, 1000f, "far away from the level");

            ui.settingsPanel.clothesSwatches[9].onClick.Invoke();
            yield return null;
            var dress = preview.Character.GetComponentsInChildren<Transform>(true).First(t => t.name == "Dress").GetComponent<Renderer>();
            Assert.AreEqual(0xfe5732, HexOf(dress), "the preview wears the chosen colour");

            ui.settingsPanel.Close();
            yield return null;
            Assert.IsNull(preview.Character, "the studio disappears with the panel");
        }

        // ------------------------------------------------------------ no clipped text

        [UnityTest]
        public IEnumerator LabelsInButtonsFitTheirBoxes()
        {
            yield return OpenSettingsFromPause();
            foreach (var language in new[] { Language.Basque, Language.Spanish, Language.English })
            {
                Localization.SetLanguage(language);
                yield return null;
                Canvas.ForceUpdateCanvases();

                foreach (var label in ui.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (string.IsNullOrEmpty(label.text)) continue;
                    var box = label.rectTransform.rect;
                    label.ForceMeshUpdate();
                    Vector2 size = label.GetPreferredValues(label.text, box.width, box.height);
                    // With auto-sizing the preferred size at the chosen font size must not exceed the box.
                    Assert.LessOrEqual(label.textBounds.size.x, box.width + 2f, language + " '" + label.text + "' too wide in " + label.name);
                    Assert.LessOrEqual(label.textBounds.size.y, box.height + 2f, language + " '" + label.text + "' too tall in " + label.name);
                    Assert.IsFalse(label.isTextOverflowing, language + " '" + label.text + "' overflows in " + label.name);
                }
            }
        }
    }
}
