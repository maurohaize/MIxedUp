using UnityEngine;
using UnityEngine.Rendering;

namespace MixedUp
{
    /// <summary>
    /// Turns the afternoon into a clear night: dark blue sky and ambient light, a faint moon instead of the sun, and lamps
    /// and fires shining brighter. Applied by the LevelDirector in the night mode.
    /// </summary>
    public class NightLighting : MonoBehaviour
    {
        public Light sun;
        public Color moonColour = new Color(0.52f, 0.62f, 1f);
        public float moonIntensity = 0.42f;
        public float lampBoost = 2.4f;

        public bool Applied { get; private set; }

        public void Apply()
        {
            if (Applied) return;
            Applied = true;

            if (sun == null)
            {
                foreach (var l in FindObjectsByType<Light>())
                    if (l.type == LightType.Directional) { sun = l; break; }
            }
            if (sun != null)
            {
                sun.color = moonColour;
                sun.intensity = moonIntensity;
                sun.shadowStrength = 0.55f;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.1f, 0.14f, 0.3f);
            RenderSettings.ambientEquatorColor = new Color(0.08f, 0.1f, 0.2f);
            RenderSettings.ambientGroundColor = new Color(0.04f, 0.05f, 0.09f);

            // A copy of the sky material with night colours, so the asset on disk is untouched.
            var sky = RenderSettings.skybox;
            if (sky != null)
            {
                sky = new Material(sky);
                sky.SetColor("_TopColor", new Color(0.02f, 0.04f, 0.12f));
                sky.SetColor("_HorizonColor", new Color(0.1f, 0.15f, 0.32f));
                sky.SetColor("_BottomColor", new Color(0.03f, 0.05f, 0.1f));
                RenderSettings.skybox = sky;
            }

            foreach (var flicker in FindObjectsByType<FlickerLight>()) flicker.Scale = lampBoost;
        }
    }
}
