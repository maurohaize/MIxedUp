using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Applies skin and clothes colours to the character renderers without touching materials.
    /// The local player (and the preview in the settings menu) follows the saved customisation live;
    /// other characters keep the colours they were given.
    /// </summary>
    public class PlayerAppearance : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public Renderer[] skinRenderers;
        public Renderer[] clothesRenderers;
        public Color skinColor = new Color(0.667f, 0.357f, 0.212f);
        public Color clothesColor = new Color(0.165f, 0.482f, 0.608f);

        [Tooltip("Use the colours chosen in the settings menu instead of the fixed ones above.")]
        public bool followSavedChoice;
        public PlayerPalette palette;

        MaterialPropertyBlock block;

        void OnEnable()
        {
            if (followSavedChoice)
            {
                CharacterCustomization.Changed += ApplySaved;
                ApplySaved();
            }
            else
            {
                Apply();
            }
        }

        void OnDisable()
        {
            if (followSavedChoice) CharacterCustomization.Changed -= ApplySaved;
        }

        public void SetColors(Color skin, Color clothes)
        {
            skinColor = skin;
            clothesColor = clothes;
            Apply();
        }

        void ApplySaved()
        {
            if (palette != null)
            {
                skinColor = palette.Skin(CharacterCustomization.SkinIndex);
                clothesColor = palette.Clothes(CharacterCustomization.ClothesIndex);
            }
            Apply();
        }

        public void Apply()
        {
            if (block == null) block = new MaterialPropertyBlock();
            Tint(skinRenderers, skinColor);
            Tint(clothesRenderers, clothesColor);
        }

        void Tint(Renderer[] renderers, Color color)
        {
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                r.SetPropertyBlock(block);
            }
        }
    }
}
