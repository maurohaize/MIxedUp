using TMPro;
using UnityEngine;

namespace MixedUp
{
    /// <summary>Keeps a TMP label in sync with the current language for a fixed localization key.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        public string key;
        [Tooltip("-1 shows the whole text; 0 only its first line (a sign's title); 1 everything after the first line.")]
        public int line = -1;

        TMP_Text label;

        void OnEnable()
        {
            Localization.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDisable() => Localization.LanguageChanged -= Refresh;

        public void SetKey(string newKey)
        {
            key = newKey;
            Refresh();
        }

        public void Refresh()
        {
            if (label == null) label = GetComponent<TMP_Text>();
            if (label != null && !string.IsNullOrEmpty(key)) label.text = Pick(Localization.Get(key), line);
        }

        /// <summary>The part of a text a label shows: all of it, its first line or the rest.</summary>
        public static string Pick(string text, int line)
        {
            if (line < 0 || text == null) return text;
            int cut = text.IndexOf('\n');
            if (cut < 0) return line == 0 ? text : string.Empty;
            return line == 0 ? text.Substring(0, cut).Trim() : text.Substring(cut + 1).Trim();
        }
    }
}
