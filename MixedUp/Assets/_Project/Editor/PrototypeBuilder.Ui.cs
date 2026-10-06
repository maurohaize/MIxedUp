using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp.EditorTools
{
    /// <summary>The hand-made interface kit: font, sprite import and the settings menu shared by the main menu and the pause menu.</summary>
    public static partial class PrototypeBuilder
    {
        const string FontsDir = ArtDir + "/Fonts";

        static readonly Color Brick = new Color(0.62f, 0.27f, 0.16f);

        // ---------------------------------------------------------------- import

        /// <summary>Slice borders in sprite pixels: left, bottom, right, top.</summary>
        static Vector4 BorderOf(string sprite)
        {
            switch (sprite)
            {
                case "paper": return new Vector4(100f, 100f, 100f, 100f);
                case "plank_right": return new Vector4(50f, 32f, 120f, 32f);
                case "plank_left": return new Vector4(120f, 32f, 50f, 32f);
                case "plank_plain": return new Vector4(50f, 30f, 50f, 30f);
                case "slider_track": return new Vector4(24f, 0f, 24f, 0f);
                case "brush_white": return new Vector4(24f, 0f, 24f, 0f);
                case "bar_brick": return new Vector4(40f, 0f, 40f, 0f);
                case "highlight": return new Vector4(80f, 80f, 80f, 80f);
                default: return Vector4.zero;
            }
        }

        static void ImportUi()
        {
            AssetDatabase.Refresh();
            if (!Directory.Exists(UiFactory.HandDir)) return;

            foreach (var file in Directory.GetFiles(UiFactory.HandDir, "*.png"))
            {
                string path = file.Replace('\\', '/');
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;

                var border = BorderOf(Path.GetFileNameWithoutExtension(path));
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                bool upToDate = importer.textureType == TextureImporterType.Sprite && importer.spriteBorder == border
                                && settings.spriteMeshType == SpriteMeshType.FullRect && !importer.mipmapEnabled
                                && importer.textureCompression == TextureImporterCompression.Uncompressed;
                if (upToDate) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.spriteBorder = border;
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            AssetDatabase.Refresh();
        }

        /// <summary>The brush font as a dynamic TextMeshPro asset (glyphs are added as they are needed), with Liberation Sans as fallback.</summary>
        static TMP_FontAsset CreateUiFont()
        {
            string assetPath = FontsDir + "/Bangers SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontsDir + "/Bangers.ttf");
            if (font == null)
            {
                Debug.LogWarning("[MixedUp] Bangers.ttf not found: the default font will be used.");
                return null;
            }

            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            asset.name = "Bangers SDF";
            AssetDatabase.CreateAsset(asset, assetPath);

            asset.material.name = "Bangers SDF Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            for (int i = 0; i < asset.atlasTextures.Length; i++)
            {
                asset.atlasTextures[i].name = "Bangers SDF Atlas " + i;
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[i], asset);
            }

            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (fallback != null) asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        // ------------------------------------------------------------- helpers

        /// <summary>Places a child by its top-left corner (x to the right, yDown downwards); its pivot is the centre.</summary>
        static RectTransform At(RectTransform rect, float x, float yDown, float width, float height)
        {
            UiFactory.Place(rect, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(x + width * 0.5f, -(yDown + height * 0.5f)), new Vector2(width, height));
            return rect;
        }

        static TextMeshProUGUI Header(Transform parent, string key, float x, float yDown, float width = 760f)
        {
            var label = UiFactory.NewText("Header_" + key, parent, "", 46f, Brick, TextAlignmentOptions.MidlineLeft, key);
            At(label.rectTransform, x, yDown, width, 54f);
            return label;
        }

        static void SliderRow(Transform parent, string key, float yDown, out Slider slider, out TMP_Text value, float min = 0f, float max = 1f)
        {
            var label = UiFactory.NewText("Label_" + key, parent, "", 36f, UiFactory.Ink, TextAlignmentOptions.MidlineLeft, key);
            At(label.rectTransform, 90f, yDown, 380f, 50f);

            slider = UiFactory.NewSlider("Slider_" + key, parent, new Vector2(270f, 34f), min, max);
            At((RectTransform)slider.transform, 480f, yDown + 8f, 270f, 34f);

            var number = UiFactory.NewText("Value_" + key, parent, "", 34f, UiFactory.Ink, TextAlignmentOptions.MidlineRight);
            At(number.rectTransform, 770f, yDown, 110f, 50f);
            value = number;
        }

        static Toggle ToggleRow(Transform parent, string key, float yDown)
        {
            var label = UiFactory.NewText("Label_" + key, parent, "", 36f, UiFactory.Ink, TextAlignmentOptions.MidlineLeft, key);
            At(label.rectTransform, 90f, yDown, 380f, 50f);

            var toggle = UiFactory.NewToggle("Toggle_" + key, parent, 50f);
            At((RectTransform)toggle.transform, 480f, yDown, 50f, 50f);
            return toggle;
        }

        static Button LanguageButton(Transform parent, string text, float x, float yDown, out GameObject mark)
        {
            var button = UiFactory.NewButton("Language_" + text, parent, text, null, new Vector2(250f, 74f), UiFactory.ButtonStyle.Plank);
            At((RectTransform)button.transform, x, yDown, 250f, 74f);

            var highlight = UiFactory.NewSprite("Selected", button.transform, "highlight", sliced: true, slicedScale: 3f);
            UiFactory.Stretch(highlight.rectTransform, -8f);
            highlight.transform.SetAsFirstSibling();
            mark = highlight.gameObject;
            return button;
        }

        // ------------------------------------------------------ settings panel

        static SettingsPanel BuildSettingsPanel(Transform canvas, GameAssets a, GameObject previewPrefab)
        {
            var dim = FullScreenDim("SettingsPanel", canvas, new Color(0f, 0f, 0f, 0.62f));
            var panel = dim.gameObject.AddComponent<SettingsPanel>();
            panel.palette = a.palette;

            const float cardWidth = 1760f;
            var card = UiFactory.Panel("Card", dim, new Vector2(cardWidth, 1000f), out var cardRoot, tilt: false, tape: true);
            UiFactory.Place(cardRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cardWidth, 1000f));

            var title = UiFactory.NewText("Title", card, "", 90f, UiFactory.Ink, TextAlignmentOptions.Center, "ui.settings");
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(900f, 110f));
            var underline = UiFactory.NewSprite("Underline", card, "underline");
            UiFactory.Place(underline.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(520f, 32f));

            // --- left column: sound, controls, screen, language
            Header(card, "ui.sound", 90f, 150f);
            SliderRow(card, "ui.volume_master", 204f, out panel.masterSlider, out panel.masterValue);
            SliderRow(card, "ui.volume_music", 258f, out panel.musicSlider, out panel.musicValue);
            SliderRow(card, "ui.volume_sfx", 312f, out panel.sfxSlider, out panel.sfxValue);

            Header(card, "ui.controls", 90f, 372f);
            SliderRow(card, "ui.sens_mouse", 426f, out panel.mouseSlider, out panel.mouseValue, GameSettings.MinSensitivity, GameSettings.MaxSensitivity);
            SliderRow(card, "ui.sens_gamepad", 480f, out panel.gamepadSlider, out panel.gamepadValue, GameSettings.MinSensitivity, GameSettings.MaxSensitivity);
            panel.invertToggle = ToggleRow(card, "ui.invert_y", 534f);

            Header(card, "ui.screen", 90f, 596f);
            panel.fullscreenToggle = ToggleRow(card, "ui.fullscreen", 650f);

            Header(card, "ui.language", 90f, 708f);
            panel.basqueButton = LanguageButton(card, "EUSKARA", 100f, 780f, out panel.basqueMark);
            panel.spanishButton = LanguageButton(card, "ESPAÑOL", 370f, 780f, out panel.spanishMark);
            panel.englishButton = LanguageButton(card, "ENGLISH", 640f, 780f, out panel.englishMark);

            // --- right column: the character (a big preview, then the two colour rows)
            const float rx = 990f;
            var header = Header(card, "ui.character", rx, 150f, 700f);
            header.alignment = TextAlignmentOptions.Center;

            var frame = UiFactory.NewSprite("PreviewFrame", card, "paper", sliced: true, slicedScale: 4f);
            At(frame.rectTransform, rx + 120f, 190f, 460f, 480f);
            frame.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -1.2f);
            var halftone = UiFactory.NewSprite("Halftone", frame.transform, "halftone");
            halftone.preserveAspect = false;
            UiFactory.Stretch(halftone.rectTransform, 22f);
            var viewRect = UiFactory.NewRect("View", frame.transform);
            var raw = viewRect.gameObject.AddComponent<RawImage>();
            raw.color = Color.white;
            raw.raycastTarget = true;
            UiFactory.Stretch(raw.rectTransform, 14f);

            var preview = raw.gameObject.AddComponent<CharacterPreview>();
            preview.characterPrefab = previewPrefab;
            preview.target = raw;
            preview.width = 760;
            preview.height = 800;
            preview.cameraDistance = 3.7f;

            var hint = UiFactory.NewText("DragHint", card, "", 28f, new Color(UiFactory.Ink.r, UiFactory.Ink.g, UiFactory.Ink.b, 0.6f), TextAlignmentOptions.Center, "ui.drag_to_turn");
            At(hint.rectTransform, rx + 120f, 672f, 460f, 38f);

            // Each colour group sits in its own boxed grid, so it is clear which colours are for the skin and which for the clothes.
            const float swatch = 54f;
            var gridTint = new Color(0.9f, 0.82f, 0.68f, 0.9f);
            int skinCount = a.palette.skinTones.Length;
            int count = a.palette.clothesColors.Length;
            int clothesRows = (count + 4) / 5;
            float skinTop = 716f, skinHeight = 86f;
            float clothesTop = skinTop + skinHeight + 10f, clothesHeight = 22f + clothesRows * 62f;

            var skinBox = UiFactory.NewSprite("SkinGrid", card, "paper", sliced: true, slicedScale: 4f);
            skinBox.color = gridTint;
            At(skinBox.rectTransform, rx - 10f, skinTop, 710f, skinHeight);
            var clothesBox = UiFactory.NewSprite("ClothesGrid", card, "paper", sliced: true, slicedScale: 4f);
            clothesBox.color = gridTint;
            At(clothesBox.rectTransform, rx - 10f, clothesTop, 710f, clothesHeight);

            var skinLabel = UiFactory.NewText("SkinLabel", card, "", 38f, Brick, TextAlignmentOptions.MidlineLeft, "ui.skin");
            At(skinLabel.rectTransform, rx + 14f, skinTop + (skinHeight - swatch) * 0.5f, 150f, swatch);
            panel.skinSwatches = new Button[skinCount];
            panel.skinRings = new Image[skinCount];
            for (int i = 0; i < skinCount; i++)
            {
                panel.skinSwatches[i] = UiFactory.NewSwatch("Skin" + (i + 1), card, a.palette.skinTones[i], swatch, out panel.skinRings[i]);
                At((RectTransform)panel.skinSwatches[i].transform, rx + 180f + i * 76f, skinTop + (skinHeight - swatch) * 0.5f, swatch, swatch);
            }

            var clothesLabel = UiFactory.NewText("ClothesLabel", card, "", 38f, Brick, TextAlignmentOptions.MidlineLeft, "ui.clothes");
            At(clothesLabel.rectTransform, rx + 14f, clothesTop + 12f, 150f, swatch);
            panel.clothesSwatches = new Button[count];
            panel.clothesRings = new Image[count];
            for (int i = 0; i < count; i++)
            {
                panel.clothesSwatches[i] = UiFactory.NewSwatch("Clothes" + (i + 1), card, a.palette.clothesColors[i], swatch, out panel.clothesRings[i]);
                At((RectTransform)panel.clothesSwatches[i].transform, rx + 180f + (i % 5) * 76f, clothesTop + 11f + (i / 5) * 62f, swatch, swatch);
            }

            // --- bottom: back / reset settings / reset progress
            panel.backButton = UiFactory.NewButton("Back", card, "", "ui.back", new Vector2(340f, 92f), UiFactory.ButtonStyle.SignLeft);
            At((RectTransform)panel.backButton.transform, 70f, 884f, 340f, 92f);
            panel.resetButton = UiFactory.NewButton("Reset", card, "", "ui.reset", new Vector2(250f, 74f), UiFactory.ButtonStyle.Plank);
            At((RectTransform)panel.resetButton.transform, 450f, 892f, 250f, 74f);
            panel.resetProgressButton = UiFactory.NewButton("ResetProgress", card, "", "ui.reset_progress", new Vector2(260f, 74f), UiFactory.ButtonStyle.Plank);
            At((RectTransform)panel.resetProgressButton.transform, 715f, 892f, 260f, 74f);

            dim.gameObject.SetActive(false);
            return panel;
        }
    }
}
