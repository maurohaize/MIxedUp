using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>The main menu scene: the diorama, the signpost buttons and the settings card.</summary>
    public class MenuTests : SceneTestBase
    {
        protected override string SceneToLoad => MenuScenePath;
        protected override bool NeedsPlayer => false;

        MainMenu menu;

        [UnitySetUp]
        public IEnumerator FindMenu()
        {
            menu = Object.FindAnyObjectByType<MainMenu>();
            yield break;
        }

        [UnityTest]
        public IEnumerator MenuIsWiredTogether()
        {
            Assert.IsNotNull(menu, "MainMenu component");
            Assert.IsNotNull(menu.playButton);
            Assert.IsNotNull(menu.settingsButton);
            Assert.IsNotNull(menu.quitButton);
            Assert.IsNotNull(menu.settings, "the settings card is part of the menu");
            Assert.IsFalse(menu.settings.IsOpen, "closed at the start");
            Assert.IsTrue(menu.signpost.activeSelf);

            var module = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            Assert.IsNotNull(module);
            Assert.IsNotNull(module.actionsAsset, "buttons need the UI input actions");

            Assert.IsTrue(Cursor.visible, "the mouse is free in a menu");
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
            Assert.AreEqual(1f, Time.timeScale);
            yield break;
        }

        [UnityTest]
        public IEnumerator TheDioramaHasTheTruckTheCharacterAndAMovingCamera()
        {
            Assert.IsNotNull(GameObject.Find("Truck"), "the truck");
            var hero = GameObject.Find("Character");
            Assert.IsNotNull(hero, "the character");
            Assert.IsNotNull(hero.GetComponent<PlayerAppearance>());
            Assert.IsNotNull(hero.GetComponent<CharacterIdle>(), "it is idling, not frozen");
            Assert.IsNull(hero.GetComponent<PlayerController>(), "no gameplay components in a menu");

            var orbit = Camera.main.GetComponent<MenuCameraOrbit>();
            Assert.IsNotNull(orbit);
            var before = Camera.main.transform.position;
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.Greater(Vector3.Distance(before, Camera.main.transform.position), 0.01f, "the camera sways");
            Assert.IsNotNull(RenderSettings.skybox);
        }

        [UnityTest]
        public IEnumerator SettingsOpenFromTheSignpostAndEscapeClosesThem()
        {
            menu.settingsButton.onClick.Invoke();
            yield return null;
            yield return null;
            Assert.IsTrue(menu.settings.IsOpen);
            Assert.IsFalse(menu.signpost.activeSelf, "the signpost makes room for the settings card");

            yield return Tap(Key.Escape);
            Assert.IsFalse(menu.settings.IsOpen);
            Assert.IsTrue(menu.signpost.activeSelf);
        }

        [UnityTest]
        public IEnumerator SettingsBackButtonBringsTheSignpostBack()
        {
            menu.settingsButton.onClick.Invoke();
            yield return null;
            menu.settings.backButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(menu.settings.IsOpen);
            Assert.IsTrue(menu.signpost.activeSelf);
        }

        [UnityTest]
        public IEnumerator PlayLoadsTheLevel()
        {
            menu.playButton.onClick.Invoke();
            float waited = 0f;
            while (SceneManager.GetActiveScene().name != "Level_Prototype" && waited < 10f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.AreEqual("Level_Prototype", SceneManager.GetActiveScene().name);
            yield return null;
            yield return null;
            Assert.IsNotNull(PlayerRegistry.Local, "the player spawned");
            Assert.AreEqual(GameState.Playing, GameManager.Instance.State);
        }

        [UnityTest]
        public IEnumerator ChangingTheLanguageRetranslatesTheSignpost()
        {
            // The signpost is hidden while the settings are open, and refreshes itself when it comes back.
            foreach (var (button, play, quit) in new[]
            {
                (menu.settings.basqueButton, "HASI", "IRTEN"),
                (menu.settings.englishButton, "PLAY", "QUIT"),
                (menu.settings.spanishButton, "JUGAR", "SALIR")
            }.Select(t => (t.Item1, t.Item2, t.Item3)))
            {
                menu.settingsButton.onClick.Invoke();
                yield return null;
                button.onClick.Invoke();
                menu.settings.backButton.onClick.Invoke();
                yield return null;
                Assert.AreEqual(play, LabelOf(menu.playButton));
                Assert.AreEqual(quit, LabelOf(menu.quitButton));
            }
        }

        [UnityTest]
        public IEnumerator CustomisingTheCharacterRecoloursTheDioramaToo()
        {
            int savedSkin = CharacterCustomization.SkinIndex, savedClothes = CharacterCustomization.ClothesIndex;
            try
            {
                menu.settingsButton.onClick.Invoke();
                yield return null;
                menu.settings.skinSwatches[0].onClick.Invoke();
                menu.settings.clothesSwatches[0].onClick.Invoke();
                yield return null;

                var hero = GameObject.Find("Character");
                var dress = hero.GetComponentsInChildren<Transform>(true).First(t => t.name == "Dress").GetComponent<Renderer>();
                var block = new MaterialPropertyBlock();
                dress.GetPropertyBlock(block);
                Color32 c = block.GetColor(Shader.PropertyToID("_BaseColor"));
                Assert.AreEqual(0xeddc52, (c.r << 16) | (c.g << 8) | c.b, "the character on stage wears the first yellow");
            }
            finally
            {
                CharacterCustomization.SkinIndex = savedSkin;
                CharacterCustomization.ClothesIndex = savedClothes;
            }
        }

        [UnityTest]
        public IEnumerator EveryMenuLabelFitsItsBoxInAllThreeLanguages()
        {
            menu.settingsButton.onClick.Invoke();
            yield return null;
            foreach (var language in new[] { Language.Basque, Language.Spanish, Language.English })
            {
                Localization.SetLanguage(language);
                yield return null;
                Canvas.ForceUpdateCanvases();
                foreach (var label in menu.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (string.IsNullOrEmpty(label.text)) continue;
                    label.ForceMeshUpdate();
                    var box = label.rectTransform.rect;
                    Assert.LessOrEqual(label.textBounds.size.x, box.width + 2f, language + " '" + label.text + "' too wide in " + label.name);
                    Assert.IsFalse(label.isTextOverflowing, language + " '" + label.text + "' overflows in " + label.name);
                }
            }
            Localization.SetLanguage(Language.Spanish);
        }

        static string LabelOf(UnityEngine.UI.Button button) => button.GetComponentInChildren<TMP_Text>().text;
    }
}
