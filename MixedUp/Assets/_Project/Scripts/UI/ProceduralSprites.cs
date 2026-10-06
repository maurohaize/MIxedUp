using UnityEngine;

namespace MixedUp
{
    /// <summary>Placeholder full-screen overlay sprites generated at runtime (replaceable by hand-drawn art).</summary>
    public static class ProceduralSprites
    {
        static Sprite vignette;
        static Sprite fog;

        static Sprite disc;

        public static Sprite Vignette => vignette != null ? vignette : vignette = Build(false);
        public static Sprite Fog => fog != null ? fog : fog = Build(true);

        /// <summary>A slightly wobbly white disc, tinted by the UI for markers and coins.</summary>
        public static Sprite Disc => disc != null ? disc : disc = BuildDisc();

        static Sprite BuildDisc()
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float angle = Mathf.Atan2(ny, nx);
                    float radius = 0.92f + 0.025f * Mathf.Sin(angle * 3f + 1f) + 0.015f * Mathf.Sin(angle * 7f);
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = Mathf.Clamp01((radius - d) * size * 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static Sprite Build(bool fogStyle)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Clamp01(Mathf.Sqrt(nx * nx + ny * ny) / 1.4142f);
                    float a = fogStyle
                        ? Mathf.Lerp(0.55f, 1f, Mathf.SmoothStep(0f, 1f, d))
                        : Mathf.SmoothStep(0.3f, 1f, d);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
