using UnityEngine;

namespace MixedUp
{
    /// <summary>Applies skin and clothes colours to the character renderers without touching materials.</summary>
    public class PlayerAppearance : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public Renderer[] skinRenderers;
        public Renderer[] clothesRenderers;
        public Color skinColor = new Color(0.72f, 0.72f, 0.74f);
        public Color clothesColor = new Color(0.55f, 0.65f, 0.85f);

        MaterialPropertyBlock block;

        void Awake() => Apply();

        public void SetColors(Color skin, Color clothes)
        {
            skinColor = skin;
            clothesColor = clothes;
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
