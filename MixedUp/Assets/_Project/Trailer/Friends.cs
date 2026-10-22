using UnityEngine;

namespace MixedUp.Trailer
{
    /// <summary>The four friends of the trailer and their colours (skin and clothes are indices of the game's palette).</summary>
    public sealed class Friend
    {
        public string name;
        public int skin, clothes;
    }

    public static class Friends
    {
        public static readonly Friend Mauro = new Friend { name = "Mauro", skin = 1, clothes = 9 };
        public static readonly Friend Ane = new Friend { name = "Ane", skin = 3, clothes = 3 };
        public static readonly Friend Iker = new Friend { name = "Iker", skin = 0, clothes = 0 };
        public static readonly Friend Maite = new Friend { name = "Maite", skin = 4, clothes = 8 };

        static PlayerPalette palette;

        static PlayerPalette Palette
        {
            get
            {
                if (palette == null && LevelDirector.Instance != null) palette = LevelDirector.Instance.palette;
                if (palette == null)
                {
                    var appearance = Object.FindAnyObjectByType<PlayerAppearance>();
                    if (appearance != null) palette = appearance.palette;
                }
                return palette;
            }
        }

        public static Color Skin(Friend f) => Palette != null ? Palette.Skin(f.skin) : Color.white;
        public static Color Clothes(Friend f) => Palette != null ? Palette.Clothes(f.clothes) : Color.white;
    }
}
