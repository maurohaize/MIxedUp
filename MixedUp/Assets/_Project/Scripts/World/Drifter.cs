using UnityEngine;

namespace MixedUp
{
    /// <summary>Carries something along with the current of the river: moves along a line, wrapping round at the ends.</summary>
    public class Drifter : MonoBehaviour
    {
        public Vector3 velocity = new Vector3(0.55f, 0f, 0f);
        [Tooltip("Where it comes back to once it has left the river (x range).")]
        public float minX = -44f, maxX = 44f;
        public float spinDegrees = 12f;
        public float bobHeight = 0.02f;

        float baseY;
        float phase;

        void Awake()
        {
            baseY = transform.position.y;
            phase = Random.value * 6.28f;
        }

        void Update()
        {
            var p = transform.position;
            p += velocity * Time.deltaTime;
            if (p.x > maxX) p.x = minX;
            else if (p.x < minX) p.x = maxX;
            p.y = baseY + Mathf.Sin(Time.time * 1.6f + phase) * bobHeight;
            transform.position = p;
            transform.Rotate(0f, spinDegrees * Time.deltaTime, 0f, Space.World);
        }
    }
}
