using UnityEngine;

namespace MixedUp
{
    /// <summary>Floats gently up and down (and sways a little): lily pads, drifting leaves.</summary>
    public class Bob : MonoBehaviour
    {
        public float height = 0.03f;
        public float speed = 1.4f;
        public float swayDegrees = 4f;

        Vector3 basePosition;
        Quaternion baseRotation;
        float phase;

        void Awake()
        {
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
            phase = Random.value * 6.28f;
        }

        void Update()
        {
            float t = Time.time * speed + phase;
            transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(t) * height);
            transform.localRotation = baseRotation * Quaternion.Euler(Mathf.Sin(t * 0.8f) * swayDegrees, 0f, Mathf.Cos(t * 0.7f) * swayDegrees);
        }
    }
}
