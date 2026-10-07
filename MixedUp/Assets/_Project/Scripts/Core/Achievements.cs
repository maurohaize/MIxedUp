using System;
using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>One achievement: an id (also the base of its localization keys), and how much progress it needs.</summary>
    public sealed class AchievementDef
    {
        public readonly string id;
        public readonly int goal;

        public AchievementDef(string id, int goal = 1)
        {
            this.id = id;
            this.goal = goal;
        }

        public string NameKey => "ach." + id + ".name";
        public string DescriptionKey => "ach." + id + ".desc";
    }

    /// <summary>
    /// The achievements and what the player has done towards each one, saved in PlayerPrefs. Counters go up with Add; "find them
    /// all" kinds of achievements remember which items were found (AddToSet). The AchievementTracker feeds it from game events.
    /// </summary>
    public static class Achievements
    {
        const string Prefix = "ach.";

        public static readonly AchievementDef SpinningLog = new AchievementDef("log", 67);
        public static readonly AchievementDef Ducks = new AchievementDef("ducks", 3);
        public static readonly AchievementDef Combinations = new AchievementDef("combos", 1);
        public static readonly AchievementDef Snowman = new AchievementDef("snowman");
        public static readonly AchievementDef Mushrooms = new AchievementDef("mushrooms", 30);
        public static readonly AchievementDef FirstBox = new AchievementDef("first_box");
        public static readonly AchievementDef Deliveries = new AchievementDef("deliveries", 50);
        public static readonly AchievementDef Hugs = new AchievementDef("hugs", 10);
        public static readonly AchievementDef Shoves = new AchievementDef("shoves", 5);
        public static readonly AchievementDef Splashes = new AchievementDef("splashes", 15);
        public static readonly AchievementDef Victory = new AchievementDef("victory");
        public static readonly AchievementDef AllModes = new AchievementDef("all_modes", 6);
        public static readonly AchievementDef Deaths = new AchievementDef("deaths", 6);
        public static readonly AchievementDef Boom = new AchievementDef("boom");
        public static readonly AchievementDef Rich = new AchievementDef("rich", 1000);
        public static readonly AchievementDef Veteran = new AchievementDef("veteran", 10);
        public static readonly AchievementDef Cavers = new AchievementDef("cave");

        public static readonly AchievementDef[] All =
        {
            SpinningLog, Ducks, Combinations, Snowman, Mushrooms, FirstBox, Deliveries, Hugs, Shoves, Splashes,
            Victory, AllModes, Deaths, Boom, Rich, Veteran, Cavers
        };

        public static event Action<AchievementDef> Unlocked;
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Unlocked = null; Changed = null; }

        public static int Progress(AchievementDef def) => Mathf.Min(PlayerPrefs.GetInt(Prefix + def.id, 0), def.goal);

        public static bool IsUnlocked(AchievementDef def) => Progress(def) >= def.goal;

        public static int UnlockedCount
        {
            get
            {
                int n = 0;
                foreach (var def in All) if (IsUnlocked(def)) n++;
                return n;
            }
        }

        public static AchievementDef Find(string id)
        {
            foreach (var def in All) if (def.id == id) return def;
            return null;
        }

        /// <summary>Adds to a counter. Returns true when this unlocked the achievement.</summary>
        public static bool Add(AchievementDef def, int amount = 1) => SetAtLeast(def, PlayerPrefs.GetInt(Prefix + def.id, 0) + amount);

        /// <summary>Raises a counter to `value` (never lowers it). Returns true when this unlocked the achievement.</summary>
        public static bool SetAtLeast(AchievementDef def, int value)
        {
            if (IsUnlocked(def)) return false;
            int old = PlayerPrefs.GetInt(Prefix + def.id, 0);
            if (value <= old) return false;

            PlayerPrefs.SetInt(Prefix + def.id, Mathf.Min(value, def.goal));
            Changed?.Invoke();
            if (value < def.goal) return false;

            Unlocked?.Invoke(def);
            return true;
        }

        /// <summary>Remembers one more distinct item (a duck, a game mode...). Progress is the number of different ones.</summary>
        public static bool AddToSet(AchievementDef def, string item)
        {
            if (IsUnlocked(def) || string.IsNullOrEmpty(item)) return false;

            var items = new HashSet<string>(PlayerPrefs.GetString(Prefix + def.id + ".set", string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            if (!items.Add(item)) return false;

            PlayerPrefs.SetString(Prefix + def.id + ".set", string.Join(";", items));
            return SetAtLeast(def, items.Count);
        }

        public static void ResetAll()
        {
            foreach (var def in All)
            {
                PlayerPrefs.DeleteKey(Prefix + def.id);
                PlayerPrefs.DeleteKey(Prefix + def.id + ".set");
            }
            Changed?.Invoke();
        }
    }
}
