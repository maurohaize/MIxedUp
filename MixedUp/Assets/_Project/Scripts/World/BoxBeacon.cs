using UnityEngine;

namespace MixedUp
{
    /// <summary>A soft glow over a box so it can be found in the dark. Takes the colour of its box.</summary>
    public class BoxBeacon : MonoBehaviour
    {
        public Light glow;
        public Renderer halo;

        public void Tint(Color colour)
        {
            // Pale versions of the box colours so every beacon reads as light, not paint.
            var light = Color.Lerp(colour, Color.white, 0.45f);
            if (glow != null) glow.color = light;
            if (halo != null)
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_Tint", new Color(light.r, light.g, light.b, 0.6f));
                halo.SetPropertyBlock(block);
            }
        }
    }
}
