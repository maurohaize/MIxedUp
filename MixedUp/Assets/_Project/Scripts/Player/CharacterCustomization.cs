using System;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// The local player's chosen skin tone and clothes colour, stored as palette indices so they are tiny to sync in
    /// multiplayer. The settings menu writes them, PlayerAppearance reads them. Defaults match the reference drawing.
    /// </summary>
    public static class CharacterCustomization
    {
        const string SkinKey = "character.skin";
        const string ClothesKey = "character.clothes";

        public const int DefaultSkin = 1;
        public const int DefaultClothes = 4;

        public static event Action Changed;

        static int? skin, clothes;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            skin = clothes = null;
            Changed = null;
        }

        public static int SkinIndex
        {
            get => (skin ?? (skin = PlayerPrefs.GetInt(SkinKey, DefaultSkin))).Value;
            set => Set(ref skin, SkinKey, value);
        }

        public static int ClothesIndex
        {
            get => (clothes ?? (clothes = PlayerPrefs.GetInt(ClothesKey, DefaultClothes))).Value;
            set => Set(ref clothes, ClothesKey, value);
        }

        static void Set(ref int? cache, string key, int value)
        {
            if (cache == value) return;
            cache = value;
            PlayerPrefs.SetInt(key, value);
            Changed?.Invoke();
        }

        public static void Reset()
        {
            skin = DefaultSkin;
            clothes = DefaultClothes;
            PlayerPrefs.SetInt(SkinKey, DefaultSkin);
            PlayerPrefs.SetInt(ClothesKey, DefaultClothes);
            Changed?.Invoke();
        }
    }
}
