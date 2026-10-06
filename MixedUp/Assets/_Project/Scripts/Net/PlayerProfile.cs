using UnityEngine;

namespace MixedUp
{
    /// <summary>The player's name, saved between sessions and shown over their head and in rooms.</summary>
    public static class PlayerProfile
    {
        const string NameKey = "player.name";
        public const int MaxNameLength = 14;

        public static string Name
        {
            get => Sanitize(PlayerPrefs.GetString(NameKey, string.Empty), true);
            set => PlayerPrefs.SetString(NameKey, Sanitize(value, false));
        }

        /// <summary>Trims, limits the length and drops control characters. An empty name becomes the localized "Player".</summary>
        public static string Sanitize(string raw, bool fallback)
        {
            var text = new System.Text.StringBuilder();
            foreach (char c in raw ?? string.Empty)
                if (!char.IsControl(c) && c != ';' && c != ':') text.Append(c);
            string result = text.ToString().Trim();
            if (result.Length > MaxNameLength) result = result.Substring(0, MaxNameLength).TrimEnd();
            return result.Length == 0 && fallback ? Localization.Get("ui.player") : result;
        }
    }
}
