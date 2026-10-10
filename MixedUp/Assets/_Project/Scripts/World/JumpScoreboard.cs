using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Counts how many times in a row each player has jumped over the spinning log without being hit, and keeps the best
    /// three scores (in PlayerPrefs). It is only a small easter egg, shown on the sign beside the log.
    /// </summary>
    public sealed class JumpScoreboard
    {
        public const string PrefKey = "sweeper.top";
        public const int Places = 3;

        public struct Entry
        {
            public string name;
            public int score;
        }

        readonly Dictionary<string, int> streaks = new Dictionary<string, int>();
        readonly List<Entry> top = new List<Entry>();
        readonly bool persist;

        public JumpScoreboard(bool persist = true)
        {
            this.persist = persist;
            if (persist) Load();
        }

        public IReadOnlyList<Entry> Top => top;
        public event System.Action Changed;

        public int Streak(string player) => streaks.TryGetValue(player, out int n) ? n : 0;

        /// <summary>The player cleared the log: one more in their streak, and maybe a place on the board.</summary>
        public int Clean(string player)
        {
            int streak = Streak(player) + 1;
            streaks[player] = streak;
            if (Submit(player, streak)) Save();
            Changed?.Invoke();
            return streak;
        }

        /// <summary>The log hit the player: their streak is over. Returns true when there was a streak to end.</summary>
        public bool Hit(string player)
        {
            if (Streak(player) == 0) return false;
            streaks[player] = 0;
            Changed?.Invoke();
            return true;
        }

        /// <summary>A streak reported by another machine of an online game (0 = it ended).</summary>
        public void SetStreak(string player, int streak)
        {
            if (streak <= 0)
            {
                Hit(player);
                return;
            }
            streaks[player] = streak;
            if (Submit(player, streak)) Save();
            Changed?.Invoke();
        }

        bool Submit(string player, int score)
        {
            int index = top.FindIndex(e => e.name == player);
            if (index >= 0)
            {
                if (top[index].score >= score) return false;
                top.RemoveAt(index);
            }
            else if (top.Count >= Places && top[top.Count - 1].score >= score)
            {
                return false;
            }

            top.Add(new Entry { name = player, score = score });
            top.Sort((a, b) => b.score.CompareTo(a.score));
            if (top.Count > Places) top.RemoveRange(Places, top.Count - Places);
            return true;
        }

        public static void ClearSaved() => PlayerPrefs.DeleteKey(PrefKey);

        void Save()
        {
            if (!persist) return;
            var parts = new List<string>();
            foreach (var e in top) parts.Add(e.name.Replace(":", " ").Replace(";", " ") + ":" + e.score.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.SetString(PrefKey, string.Join(";", parts));
        }

        void Load()
        {
            top.Clear();
            foreach (var part in PlayerPrefs.GetString(PrefKey, string.Empty).Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                int colon = part.LastIndexOf(':');
                if (colon <= 0 || !int.TryParse(part.Substring(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int score)) continue;
                top.Add(new Entry { name = part.Substring(0, colon), score = score });
            }
            top.Sort((a, b) => b.score.CompareTo(a.score));
        }
    }
}
