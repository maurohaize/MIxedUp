using UnityEngine;

namespace MixedUp.Trailer
{
    /// <summary>
    /// The four friends of the trailer: colours (skin and clothes are indices of the game's palette) and voice. They babble
    /// instead of talking, so the voice is just a pitch (1 = neutral) and a pace (seconds per syllable).
    /// </summary>
    public sealed class Friend
    {
        public string name;
        public int skin, clothes;
        public float voice = 1f, pace = 0.085f;
    }

    public static class Friends
    {
        // Mauro: the calm one who plans. Ane: quick and sharp. Iker: talks too fast, causes everything. Maite: high and dramatic.
        public static readonly Friend Mauro = new Friend { name = "Mauro", skin = 1, clothes = 9, voice = 0.86f, pace = 0.095f };
        public static readonly Friend Ane = new Friend { name = "Ane", skin = 3, clothes = 3, voice = 1.14f, pace = 0.078f };
        public static readonly Friend Iker = new Friend { name = "Iker", skin = 0, clothes = 0, voice = 1.0f, pace = 0.066f };
        public static readonly Friend Maite = new Friend { name = "Maite", skin = 4, clothes = 8, voice = 1.3f, pace = 0.085f };

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
