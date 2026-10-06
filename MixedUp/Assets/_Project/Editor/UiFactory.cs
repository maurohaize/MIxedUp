using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// Builds the hand-made look of the interface: paper cards with a wobbly sketched outline, wooden sign planks as
    /// buttons, brush sliders and a brush font. All pieces are 9-sliced sprites drawn by Tools/generate_ui_art.py.
    /// </summary>
    public static class UiFactory
    {
        public const string HandDir = "Assets/_Project/Art/UI/Hand";

        public static readonly Color Paper = new Color(0.98f, 0.945f, 0.86f);
        /// <summary>Dark brush brown used for text and lines, like the lettering of the old Mixed_Up signs.</summary>
        public static readonly Color Ink = new Color(0.30f, 0.20f, 0.08f);
        public static readonly Color Sand = new Color(1f, 0.82f, 0.56f);
        public static readonly Color Highlight = new Color(1f, 0.8f, 0.25f, 0.9f);
        public static readonly Color Danger = new Color(0.66f, 0.14f, 0.08f);

        /// <summary>Brush font used by every label. Assigned by the builder before the interface is created.</summary>
        public static TMP_FontAsset Font;

        static System.Random jitter = new System.Random(7);

        public enum ButtonStyle { SignRight, SignLeft, Plank }

        // ----------------------------------------------------------- sprites

        public static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(HandDir + "/" + name + ".png");

        // ------------------------------------------------------------- basics

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image NewImage(string name, Transform parent, Color color)
        {
            var rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Image NewSprite(string name, Transform parent, string sprite, bool sliced = false, float slicedScale = 1f)
        {
            var image = NewImage(name, parent, Color.white);
            image.sprite = Sprite(sprite);
            if (sliced)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = slicedScale;
            }
            else
            {
                image.preserveAspect = true;
            }
            return image;
        }

        public static void Place(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = pivot;
            r.anchoredPosition = position;
            r.sizeDelta = size;
        }

        public static void Stretch(RectTransform r, float inset = 0f)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = new Vector2(inset, inset);
            r.offsetMax = new Vector2(-inset, -inset);
        }

        // ------------------------------------------------------------- panels

        /// <summary>
        /// Paper card with a sketched outline and a slight random tilt. Returns the paper area for children.
        /// The 9-slice border is scaled to the card, so small cards keep their proportions.
        /// </summary>
        public static RectTransform Panel(string name, Transform parent, Vector2 size, out RectTransform root, bool tilt = true, bool tape = false)
        {
            root = NewRect(name, parent);
            root.sizeDelta = size;
            if (tilt) root.localRotation = Quaternion.Euler(0f, 0f, (float)(jitter.NextDouble() * 1.6 - 0.8));

            var paper = NewSprite("Paper", root, "paper", sliced: true, slicedScale: SliceScale(100f, size, 0.34f));
            Stretch(paper.rectTransform);
            paper.raycastTarget = true;

            if (tape)
            {
                var strip = NewSprite("Tape", root, "tape");
                Place(strip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(220f, 70f));
                strip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (float)(jitter.NextDouble() * 8 - 4));
            }
            return paper.rectTransform;
        }

        /// <summary>Pixels-per-unit multiplier that keeps a sprite border at most `fraction` of the smaller side.</summary>
        static float SliceScale(float borderPixels, Vector2 size, float fraction)
        {
            float allowed = Mathf.Min(size.x, size.y) * fraction;
            return Mathf.Max(1f, borderPixels / allowed);
        }

        // --------------------------------------------------------------- text

        public static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, string locKey = null, bool autoSize = true)
        {
            var rect = NewRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null) label.font = Font;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.characterSpacing = 1.5f;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.text = text;

            if (autoSize)
            {
                // Text must always fit its box: shrink instead of overflowing or breaking words in the middle.
                label.enableAutoSizing = true;
                label.fontSizeMax = size;
                label.fontSizeMin = Mathf.Max(10f, size * 0.4f);
            }

            if (!string.IsNullOrEmpty(locKey))
            {
                var localized = rect.gameObject.AddComponent<LocalizedText>();
                localized.key = locKey;
                label.text = Localization.Get(locKey);
            }
            return label;
        }

        // ------------------------------------------------------------ buttons

        /// <summary>A wooden sign (or plank) that tips and grows when hovered. The label always fits inside it.</summary>
        public static Button NewButton(string name, Transform parent, string text, string locKey, Vector2 size, ButtonStyle style = ButtonStyle.SignRight)
        {
            var root = NewRect(name, parent);
            root.sizeDelta = size;

            string sprite = style == ButtonStyle.SignRight ? "plank_right" : style == ButtonStyle.SignLeft ? "plank_left" : "plank_plain";
            float spriteHeight = style == ButtonStyle.Plank ? 144f : 168f;
            float scale = Mathf.Max(0.5f, spriteHeight / size.y);

            var image = NewSprite("Plank", root, sprite, sliced: true, slicedScale: scale);
            Stretch(image.rectTransform);
            image.raycastTarget = true;

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.02f, 0.9f);
            colors.pressedColor = new Color(0.82f, 0.74f, 0.62f);
            colors.selectedColor = new Color(1.08f, 1.02f, 0.9f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            var hand = root.gameObject.AddComponent<HandDrawnButton>();
            hand.restTilt = (float)(jitter.NextDouble() * 1.6 - 0.8);

            // The visible wood is inset from the sprite edge; the arrow tip takes room on the pointing side.
            float vertical = 14f / scale + 3f;
            float body = 40f / scale;
            float tip = (style == ButtonStyle.Plank ? 40f : 118f) / scale;
            float left = style == ButtonStyle.SignLeft ? tip : body;
            float right = style == ButtonStyle.SignLeft ? body : tip;

            var label = NewText("Label", root, text, size.y * 0.55f, Ink, TextAlignmentOptions.Center, locKey);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(left, vertical);
            label.rectTransform.offsetMax = new Vector2(-right, -vertical);
            return button;
        }

        // ------------------------------------------------------ settings pieces

        /// <summary>A brush slider with a paper knob. Value range 0..1 unless told otherwise.</summary>
        public static Slider NewSlider(string name, Transform parent, Vector2 size, float min, float max)
        {
            var root = NewRect(name, parent);
            root.sizeDelta = size;

            var track = NewSprite("Track", root, "slider_track", sliced: true, slicedScale: Mathf.Max(0.5f, 48f / size.y));
            Stretch(track.rectTransform);
            track.color = new Color(1f, 1f, 1f, 0.55f);

            var fillArea = NewRect("FillArea", root);
            fillArea.anchorMin = new Vector2(0f, 0.12f);
            fillArea.anchorMax = new Vector2(1f, 0.88f);
            fillArea.offsetMin = new Vector2(size.y * 0.3f, 0f);
            fillArea.offsetMax = new Vector2(-size.y * 0.3f, 0f);
            var fill = NewSprite("Fill", fillArea, "bar_brick", sliced: true, slicedScale: 1.4f);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

            var handleArea = NewRect("HandleArea", root);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(size.y * 0.5f, 0f);
            handleArea.offsetMax = new Vector2(-size.y * 0.5f, 0f);
            var knob = NewSprite("Knob", handleArea, "slider_knob");
            knob.raycastTarget = true;
            knob.rectTransform.sizeDelta = new Vector2(size.y * 1.5f, size.y * 1.5f);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = knob.rectTransform;
            slider.targetGraphic = knob;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = min;
            return slider;
        }

        public static Toggle NewToggle(string name, Transform parent, float size)
        {
            var root = NewRect(name, parent);
            root.sizeDelta = new Vector2(size, size);

            var box = NewSprite("Box", root, "checkbox");
            Stretch(box.rectTransform);
            box.raycastTarget = true;
            var mark = NewSprite("Check", root, "checkmark");
            Stretch(mark.rectTransform, size * 0.05f);

            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = mark;
            toggle.isOn = false;
            var hand = root.gameObject.AddComponent<HandDrawnButton>();
            hand.hoverTilt = 3f;
            return toggle;
        }

        /// <summary>A round colour blob that can be picked; `ring` is shown around the selected one.</summary>
        public static Button NewSwatch(string name, Transform parent, Color color, float size, out Image ring)
        {
            var root = NewRect(name, parent);
            root.sizeDelta = new Vector2(size, size);

            ring = NewSprite("Ring", root, "swatch_ring");
            Place(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 1.3f, size * 1.3f));
            ring.enabled = false;

            var blob = NewSprite("Color", root, "swatch");
            Stretch(blob.rectTransform);
            blob.color = color;
            blob.raycastTarget = true;

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = blob;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            var hand = root.gameObject.AddComponent<HandDrawnButton>();
            hand.hoverTilt = 6f;
            hand.hoverScale = 1.14f;
            return button;
        }
    }
}
