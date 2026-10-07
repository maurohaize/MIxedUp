using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Lets the lamps, fires and beacons nearest to the camera cast real shadows, so their light is projected onto the scenery
    /// instead of just tinting it. Shadow-casting point lights are expensive, so only a few at a time get them.
    /// </summary>
    public class LightShadowBudget : MonoBehaviour
    {
        [Min(0)] public int maxShadowLights = 4;
        public float maxDistance = 28f;
        public float refreshSeconds = 0.3f;

        readonly List<Light> lights = new List<Light>();
        readonly HashSet<Light> casting = new HashSet<Light>();
        float nextRefresh;

        void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + refreshSeconds;

            var cam = Camera.main;
            if (cam == null) return;

            lights.Clear();
            foreach (var flicker in FindObjectsByType<FlickerLight>())
            {
                var l = flicker.GetComponent<Light>();
                if (l != null && l.isActiveAndEnabled && l.type == LightType.Point) lights.Add(l);
            }

            Vector3 eye = cam.transform.position;
            lights.Sort((a, b) =>
                (a.transform.position - eye).sqrMagnitude.CompareTo((b.transform.position - eye).sqrMagnitude));

            casting.Clear();
            for (int i = 0; i < lights.Count; i++)
            {
                var l = lights[i];
                bool wanted = i < maxShadowLights && (l.transform.position - eye).magnitude <= maxDistance;
                var mode = wanted ? LightShadows.Soft : LightShadows.None;
                if (l.shadows != mode) l.shadows = mode;
                if (wanted) casting.Add(l);
            }
        }
    }
}
