using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp.EditorTools
{
    /// <summary>Builds the hand-drawn-paper style uGUI pieces used by the prototype HUD.</summary>
    public static class UiFactory
    {
        public static readonly Color Paper = new Color(0.96f, 0.92f, 0.82f);
        public static readonly Color Ink = new Color(0.18f, 0.13f, 0.10f);
        public static readonly Color Highlight = new Color(1f, 0.8f, 0.25f, 0.9f);

        static System.Random jitter = new System.Random(7);

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

        /// <summary>Paper card with an ink border and a slight random tilt. Returns the inner area for children.</summary>
        public static RectTransform Panel(string name, Transform parent, Vector2 size, out RectTransform root, bool tilt = true)
        {
            root = NewRect(name, parent);
            root.sizeDelta = size;
            if (tilt) root.localRotation = Quaternion.Euler(0f, 0f, (float)(jitter.NextDouble() * 1.6 - 0.8));

            var border = NewImage("Border", root, Ink);
            Stretch(border.rectTransform);
            border.raycastTarget = true;
            var paper = NewImage("Paper", root, Paper);
            Stretch(paper.rectTransform, 5f);
            paper.raycastTarget = true;
            return paper.rectTransform;
        }

        public static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, string locKey = null)
        {
            var rect = NewRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
            label.text = text;

            if (!string.IsNullOrEmpty(locKey))
            {
                var localized = rect.gameObject.AddComponent<LocalizedText>();
                localized.key = locKey;
                label.text = Localization.Get(locKey);
            }
            return label;
        }

        public static Button NewButton(string name, Transform parent, string text, string locKey, Vector2 size)
        {
            var inner = Panel(name, parent, size, out var root);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = inner.GetComponent<Image>();

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.93f, 0.65f);
            colors.pressedColor = new Color(0.85f, 0.78f, 0.6f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            var label = NewText("Label", inner, text, size.y * 0.42f, Ink, TextAlignmentOptions.Center, locKey);
            Stretch(label.rectTransform, 6f);
            return button;
        }
    }
}
