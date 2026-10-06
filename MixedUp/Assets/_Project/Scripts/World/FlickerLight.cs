using UnityEngine;

namespace MixedUp
{
    /// <summary>Makes a point light dance like a flame (campfires) or breathe softly (lanterns).</summary>
    [RequireComponent(typeof(Light))]
    public class FlickerLight : MonoBehaviour
    {
        public float baseIntensity = 2f;
        [Range(0f, 1f)] public float amount = 0.25f;
        public float speed = 9f;

        Light lamp;
        float seed;

        /// <summary>Multiplier set by the lighting mood (lamps shine brighter at night).</summary>
        public float Scale { get; set; } = 1f;

        void Awake()
        {
            lamp = GetComponent<Light>();
            seed = Random.value * 100f;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * speed) * 2f - 1f;
            lamp.intensity = baseIntensity * (1f + n * amount) * Scale;
        }
    }
}
