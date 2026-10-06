using System;
using UnityEngine;

namespace MixedUp
{
    /// <summary>Where the boxes of an order are placed.</summary>
    public enum SpawnStyle
    {
        /// <summary>Each box where the level designer put it (the original puzzle of the map).</summary>
        Classic,
        /// <summary>Any ordinary spot of the map, picked at random every game.</summary>
        Random,
        /// <summary>Only the hard-to-reach spots: towers, tunnels, a ledge above a bounce pad, the spinning log.</summary>
        Hard
    }

    /// <summary>One way to play: what is ordered, where the boxes are, how long you have and how the night looks.</summary>
    public sealed class GameModeInfo
    {
        public string id;
        public string nameKey;
        public string descriptionKey;
        public SpawnStyle spawn;
        /// <summary>Boxes in a random order; 0 uses the map's own order.</summary>
        public int minBoxes, maxBoxes;
        /// <summary>Seconds you have to deliver everything; 0 = no limit.</summary>
        public float timeLimit;
        public bool night;
        /// <summary>Multiplier on the money earned.</summary>
        public float reward = 1f;

        public bool HasTimer => timeLimit > 0f;
        public bool UsesMapOrder => minBoxes <= 0;
        public string DisplayName => Localization.Get(nameKey);
        public string Description => Localization.Get(descriptionKey);
    }

    /// <summary>The list of game modes and the one chosen in the main menu (saved between sessions).</summary>
    public static class GameModes
    {
        const string PrefKey = "game.mode";

        public static readonly GameModeInfo Classic = new GameModeInfo
        {
            id = "classic", nameKey = "mode.classic.name", descriptionKey = "mode.classic.desc", spawn = SpawnStyle.Classic
        };

        public static readonly GameModeInfo Express = new GameModeInfo
        {
            id = "express", nameKey = "mode.express.name", descriptionKey = "mode.express.desc", spawn = SpawnStyle.Random,
            minBoxes = 3, maxBoxes = 3, timeLimit = 180f, reward = 1.2f
        };

        public static readonly GameModeInfo Giant = new GameModeInfo
        {
            id = "giant", nameKey = "mode.giant.name", descriptionKey = "mode.giant.desc", spawn = SpawnStyle.Random,
            minBoxes = 9, maxBoxes = 9, timeLimit = 600f, reward = 2f
        };

        public static readonly GameModeInfo Surprise = new GameModeInfo
        {
            id = "surprise", nameKey = "mode.surprise.name", descriptionKey = "mode.surprise.desc", spawn = SpawnStyle.Random,
            minBoxes = 5, maxBoxes = 7, timeLimit = 420f, reward = 1.5f
        };

        public static readonly GameModeInfo Night = new GameModeInfo
        {
            id = "night", nameKey = "mode.night.name", descriptionKey = "mode.night.desc", spawn = SpawnStyle.Classic,
            night = true, reward = 1.5f
        };

        public static readonly GameModeInfo Challenge = new GameModeInfo
        {
            id = "challenge", nameKey = "mode.challenge.name", descriptionKey = "mode.challenge.desc", spawn = SpawnStyle.Hard,
            minBoxes = 5, maxBoxes = 5, timeLimit = 480f, reward = 2.5f
        };

        public static readonly GameModeInfo[] All = { Classic, Express, Giant, Surprise, Night, Challenge };

        static GameModeInfo selected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => selected = null;

        public static GameModeInfo Find(string id)
        {
            foreach (var mode in All)
                if (mode.id == id) return mode;
            return null;
        }

        public static int IndexOf(GameModeInfo mode) => Array.IndexOf(All, mode);

        public static GameModeInfo Selected
        {
            get
            {
                if (selected == null) selected = Find(PlayerPrefs.GetString(PrefKey, Classic.id)) ?? Classic;
                return selected;
            }
            set
            {
                selected = value ?? Classic;
                PlayerPrefs.SetString(PrefKey, selected.id);
            }
        }

        /// <summary>Moves the selection to the next (+1) or previous (-1) mode, wrapping round.</summary>
        public static GameModeInfo Step(int direction)
        {
            int index = (IndexOf(Selected) + direction + All.Length) % All.Length;
            Selected = All[index];
            return Selected;
        }
    }
}
