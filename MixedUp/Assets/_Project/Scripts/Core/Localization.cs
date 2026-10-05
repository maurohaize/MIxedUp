using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MixedUp
{
    public enum Language
    {
        Basque = 0,
        Spanish = 1,
        English = 2
    }

    /// <summary>
    /// Runtime localization backed by Resources/Localization/strings.csv.
    /// Header: key,eu,es,en. Values may be quoted; a literal \n becomes a line break.
    /// </summary>
    public static class Localization
    {
        public const string ResourcePath = "Localization/strings";
        const string PrefKey = "settings.language";
        static readonly string[] Codes = { "eu", "es", "en" };

        static Dictionary<string, string[]> table = new Dictionary<string, string[]>();
        static bool loaded;
        static Language current = Language.Spanish;

        public static event Action LanguageChanged;

        public static Language Current
        {
            get { EnsureLoaded(); return current; }
        }

        public static IEnumerable<string> Keys
        {
            get { EnsureLoaded(); return table.Keys; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            loaded = false;
            table = new Dictionary<string, string[]>();
            LanguageChanged = null;
        }

        public static void SetLanguage(Language language)
        {
            EnsureLoaded();
            if (current == language) return;
            current = language;
            PlayerPrefs.SetInt(PrefKey, (int)language);
            LanguageChanged?.Invoke();
        }

        public static bool Has(string key, Language language)
        {
            EnsureLoaded();
            return table.TryGetValue(key, out var row) && !string.IsNullOrEmpty(row[(int)language]);
        }

        public static string Get(string key, params object[] args)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (!table.TryGetValue(key, out var row)) return "[" + key + "]";

            string text = row[(int)current];
            if (string.IsNullOrEmpty(text)) text = row[(int)Language.English];
            if (string.IsNullOrEmpty(text)) return "[" + key + "]";

            if (args != null && args.Length > 0)
            {
                try { text = string.Format(text, args); }
                catch (FormatException) { /* keep unformatted text */ }
            }
            return text;
        }

        /// <summary>Replaces the table with the given CSV text (used by tests and tools).</summary>
        public static void LoadFromText(string csv)
        {
            table = new Dictionary<string, string[]>();
            Parse(csv);
            loaded = true;
            current = ReadInitialLanguage();
        }

        static void EnsureLoaded()
        {
            if (loaded) return;
            table = new Dictionary<string, string[]>();
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset != null) Parse(asset.text);
            else Debug.LogWarning("Localization table not found at Resources/" + ResourcePath);
            loaded = true;
            current = ReadInitialLanguage();
        }

        static Language ReadInitialLanguage()
        {
            if (PlayerPrefs.HasKey(PrefKey))
                return (Language)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey), 0, Codes.Length - 1);

            switch (Application.systemLanguage)
            {
                case SystemLanguage.Basque: return Language.Basque;
                case SystemLanguage.Spanish: return Language.Spanish;
                default: return Language.English;
            }
        }

        static void Parse(string csv)
        {
            var rows = ParseCsv(csv);
            if (rows.Count == 0) return;

            var header = rows[0];
            int keyCol = header.FindIndex(h => h.Trim().Equals("key", StringComparison.OrdinalIgnoreCase));
            if (keyCol < 0) return;

            var langCols = new int[Codes.Length];
            for (int i = 0; i < Codes.Length; i++)
                langCols[i] = header.FindIndex(h => h.Trim().Equals(Codes[i], StringComparison.OrdinalIgnoreCase));

            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (keyCol >= row.Count) continue;
                string key = row[keyCol].Trim();
                if (key.Length == 0 || key[0] == '#') continue;

                var values = new string[Codes.Length];
                for (int i = 0; i < Codes.Length; i++)
                {
                    int col = langCols[i];
                    values[i] = col >= 0 && col < row.Count ? row[col].Replace("\\n", "\n") : string.Empty;
                }
                table[key] = values;
            }
        }

        static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            if (string.IsNullOrEmpty(text)) return rows;
            if (text[0] == '﻿') text = text.Substring(1);

            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else field.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '"': inQuotes = true; break;
                    case ',': row.Add(field.ToString()); field.Clear(); break;
                    case '\r': break;
                    case '\n':
                        row.Add(field.ToString()); field.Clear();
                        rows.Add(row); row = new List<string>();
                        break;
                    default: field.Append(c); break;
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }
            return rows;
        }
    }
}
