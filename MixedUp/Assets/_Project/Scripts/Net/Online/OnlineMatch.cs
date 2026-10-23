using System;

namespace MixedUp
{
    /// <summary>A map players can pick in the lobby. Every map is its own scene.</summary>
    public sealed class LevelInfo
    {
        public string id;
        public string scene;
        public string nameKey;
        public string descriptionKey;

        public string DisplayName => Localization.Get(nameKey);
        public string Description => Localization.Get(descriptionKey);
    }

    /// <summary>The maps of the game, in the order the lobby shows them.</summary>
    public static class LevelCatalog
    {
        public static readonly LevelInfo Prototype = new LevelInfo
        {
            id = "meadow", scene = MainMenu.LevelScene, nameKey = "map.meadow.name", descriptionKey = "map.meadow.desc"
        };

        public static readonly LevelInfo Summit = new LevelInfo
        {
            id = "summit", scene = "Level_Summit", nameKey = "map.summit.name", descriptionKey = "map.summit.desc"
        };

        public static readonly LevelInfo Harbour = new LevelInfo
        {
            id = "harbour", scene = "Level_Harbour", nameKey = "map.harbour.name", descriptionKey = "map.harbour.desc"
        };

        public static readonly LevelInfo[] All = { Prototype, Summit, Harbour };

        public static LevelInfo Find(string id)
        {
            foreach (var level in All)
                if (level.id == id) return level;
            return null;
        }

        public static int IndexOf(LevelInfo level) => Array.IndexOf(All, level);

        public static LevelInfo Step(LevelInfo from, int direction)
        {
            int index = Math.Max(0, IndexOf(from));
            return All[(index + direction + All.Length) % All.Length];
        }

        const string SelectedPref = "game.map";

        /// <summary>The map chosen in the main menu for a solo game (saved between sessions).</summary>
        public static LevelInfo Selected
        {
            get => Find(UnityEngine.PlayerPrefs.GetString(SelectedPref, Prototype.id)) ?? Prototype;
            set => UnityEngine.PlayerPrefs.SetString(SelectedPref, (value ?? Prototype).id);
        }
    }

    /// <summary>
    /// What the host chose in the lobby (map, game mode and the random seed), as seen on this machine. The values travel with
    /// the host's NetAvatar, so every player builds the same level.
    /// </summary>
    public static class OnlineMatch
    {
        public static bool IsOnline => OnlineSession.IsOnline && NetAvatar.Host != null;

        public static LevelInfo Level => IsOnline ? LevelCatalog.Find(NetAvatar.Host.MapId) ?? LevelCatalog.Prototype : null;
        public static GameModeInfo Mode => IsOnline ? GameModes.Find(NetAvatar.Host.ModeId) ?? GameModes.Classic : null;
        public static int Seed => IsOnline ? NetAvatar.Host.Seed : 0;
        public static bool Started => IsOnline && NetAvatar.Host.Started;
    }
}
