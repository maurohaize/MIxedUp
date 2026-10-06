using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace MixedUp.Tests
{
    /// <summary>Saved preferences (GameSettings, CharacterCustomization) and the assets of the hand-made interface.</summary>
    public class SettingsDataTests
    {
        const string HandDir = "Assets/_Project/Art/UI/Hand/";

        float master, music, sfx, mouse, gamepad;
        bool invert;
        int skin, clothes;

        [SetUp]
        public void Remember()
        {
            master = GameSettings.MasterVolume;
            music = GameSettings.MusicVolume;
            sfx = GameSettings.SfxVolume;
            mouse = GameSettings.MouseSensitivity;
            gamepad = GameSettings.GamepadSensitivity;
            invert = GameSettings.InvertY;
            skin = CharacterCustomization.SkinIndex;
            clothes = CharacterCustomization.ClothesIndex;
        }

        [TearDown]
        public void Restore()
        {
            GameSettings.MasterVolume = master;
            GameSettings.MusicVolume = music;
            GameSettings.SfxVolume = sfx;
            GameSettings.MouseSensitivity = mouse;
            GameSettings.GamepadSensitivity = gamepad;
            GameSettings.InvertY = invert;
            CharacterCustomization.SkinIndex = skin;
            CharacterCustomization.ClothesIndex = clothes;
        }

        // ---------------------------------------------------------- GameSettings

        [Test]
        public void VolumesStayBetweenZeroAndOne()
        {
            GameSettings.MasterVolume = 5f;
            Assert.AreEqual(1f, GameSettings.MasterVolume);
            GameSettings.MasterVolume = -2f;
            Assert.AreEqual(0f, GameSettings.MasterVolume);
            GameSettings.MusicVolume = 0.4f;
            Assert.AreEqual(0.4f, GameSettings.MusicVolume, 1e-5f);
        }

        [Test]
        public void SensitivityIsClampedToItsRange()
        {
            GameSettings.MouseSensitivity = 99f;
            Assert.AreEqual(GameSettings.MaxSensitivity, GameSettings.MouseSensitivity);
            GameSettings.MouseSensitivity = 0f;
            Assert.AreEqual(GameSettings.MinSensitivity, GameSettings.MouseSensitivity);
            GameSettings.GamepadSensitivity = 1.5f;
            Assert.AreEqual(1.5f, GameSettings.GamepadSensitivity, 1e-5f);
        }

        [Test]
        public void ChangesRaiseOneEventAndRepeatsDoNot()
        {
            GameSettings.InvertY = false;
            int events = 0;
            void Count() => events++;
            GameSettings.Changed += Count;
            try
            {
                GameSettings.InvertY = true;
                GameSettings.InvertY = true;
                GameSettings.MusicVolume = 0.123f;
                GameSettings.MusicVolume = 0.123f;
            }
            finally { GameSettings.Changed -= Count; }
            Assert.AreEqual(2, events);
        }

        [Test]
        public void ResetToDefaultsRestoresEverythingButTheScreenMode()
        {
            GameSettings.MasterVolume = 0.2f;
            GameSettings.MusicVolume = 0.1f;
            GameSettings.SfxVolume = 0.3f;
            GameSettings.MouseSensitivity = 2.5f;
            GameSettings.GamepadSensitivity = 0.5f;
            GameSettings.InvertY = true;

            GameSettings.ResetToDefaults();
            Assert.AreEqual(1f, GameSettings.MasterVolume);
            Assert.AreEqual(0.8f, GameSettings.MusicVolume, 1e-5f);
            Assert.AreEqual(0.8f, GameSettings.SfxVolume, 1e-5f);
            Assert.AreEqual(1f, GameSettings.MouseSensitivity);
            Assert.AreEqual(1f, GameSettings.GamepadSensitivity);
            Assert.IsFalse(GameSettings.InvertY);
        }

        // ------------------------------------------------- CharacterCustomization

        [Test]
        public void CustomisationIsSavedAndAnnounced()
        {
            CharacterCustomization.SkinIndex = 0;
            CharacterCustomization.ClothesIndex = 0;

            int events = 0;
            void Count() => events++;
            CharacterCustomization.Changed += Count;
            try
            {
                CharacterCustomization.SkinIndex = 3;
                CharacterCustomization.SkinIndex = 3;
                CharacterCustomization.ClothesIndex = 7;
            }
            finally { CharacterCustomization.Changed -= Count; }

            Assert.AreEqual(2, events, "only real changes are announced");
            Assert.AreEqual(3, PlayerPrefs.GetInt("character.skin"));
            Assert.AreEqual(7, PlayerPrefs.GetInt("character.clothes"));
        }

        [Test]
        public void CustomisationResetGoesBackToTheDrawingsCharacter()
        {
            CharacterCustomization.SkinIndex = 4;
            CharacterCustomization.ClothesIndex = 9;
            CharacterCustomization.Reset();
            Assert.AreEqual(CharacterCustomization.DefaultSkin, CharacterCustomization.SkinIndex);
            Assert.AreEqual(CharacterCustomization.DefaultClothes, CharacterCustomization.ClothesIndex);
        }

        [Test]
        public void PaletteIndicesWrapAround()
        {
            var palette = ScriptableObject.CreateInstance<PlayerPalette>();
            palette.skinTones = new[] { Color.red, Color.green, Color.blue };
            Assert.AreEqual(Color.red, palette.Skin(3));
            Assert.AreEqual(Color.blue, palette.Skin(-1));
            Object.DestroyImmediate(palette);
        }

        // ------------------------------------------------------------ the UI kit

        [Test]
        public void BothScenesAreInTheBuildAndTheMenuComesFirst()
        {
            var scenes = EditorBuildSettings.scenes;
            Assume.That(scenes.Length, Is.GreaterThan(0), "build the prototype first");
            Assert.AreEqual("Assets/Scenes/MainMenu.unity", scenes[0].path, "the game starts in the main menu");
            Assert.AreEqual("Assets/Scenes/Level_Prototype.unity", scenes[1].path);
            Assert.IsTrue(scenes[0].enabled && scenes[1].enabled);
        }

        [Test]
        public void TheCardsAndSignsAreNineSlicedSprites()
        {
            foreach (var name in new[] { "paper", "plank_right", "plank_left", "plank_plain", "slider_track", "highlight" })
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HandDir + name + ".png");
                Assume.That(sprite, Is.Not.Null, "missing " + name + ": run MixedUp > Build Prototype Scene");
                Assert.Greater(sprite.border.x + sprite.border.z, 0f, name + " must have horizontal borders so it can stretch");
            }
            var paper = AssetDatabase.LoadAssetAtPath<Sprite>(HandDir + "paper.png");
            Assert.Greater(paper.border.y + paper.border.w, 0f, "paper also stretches vertically");
        }

        [Test]
        public void TheAuthorsOwnDrawingsAreUsed()
        {
            foreach (var name in new[] { "logo", "bar_brick", "art_gameover", "art_win" })
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(HandDir + name + ".png"), name);
        }

        [Test]
        public void TheBrushFontCoversSpanishAndBasqueLetters()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Art/Fonts/Bangers SDF.asset");
            Assume.That(font, Is.Not.Null, "build the prototype first");
            Assert.IsNotNull(font.sourceFontFile, "dynamic font assets need their font file");
            foreach (var letter in "ÁÉÍÓÚÑáéíóúñ¡¿ÜüAZaz09")
                Assert.IsTrue(font.HasCharacter(letter, false, true), "glyph " + letter);
        }

        [Test]
        public void NewMenuStringsAreTranslatedIntoEveryLanguage()
        {
            foreach (var key in new[]
            {
                "ui.settings", "ui.play", "ui.main_menu", "ui.back", "ui.reset", "ui.sound", "ui.volume_master", "ui.volume_music",
                "ui.volume_sfx", "ui.controls", "ui.sens_mouse", "ui.sens_gamepad", "ui.invert_y", "ui.screen", "ui.fullscreen",
                "ui.character", "ui.skin", "ui.clothes", "ui.drag_to_turn"
            })
            {
                foreach (var language in new[] { Language.Basque, Language.Spanish, Language.English })
                    Assert.IsTrue(Localization.Has(key, language), key + " " + language);
            }
        }
    }
}
