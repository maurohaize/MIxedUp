using UnityEngine;

namespace MixedUp
{
    /// <summary>Colour options for the cosmetic character customisation (no gameplay effect).</summary>
    [CreateAssetMenu(fileName = "PlayerPalette", menuName = "MixedUp/Player Palette")]
    public class PlayerPalette : ScriptableObject
    {
        public Color[] skinTones = System.Array.Empty<Color>();
        public Color[] clothesColors = System.Array.Empty<Color>();

        public Color Skin(int index) => Pick(skinTones, index, Color.gray);
        public Color Clothes(int index) => Pick(clothesColors, index, Color.white);

        static Color Pick(Color[] colors, int index, Color fallback) =>
            colors == null || colors.Length == 0 ? fallback : colors[((index % colors.Length) + colors.Length) % colors.Length];
    }
}
