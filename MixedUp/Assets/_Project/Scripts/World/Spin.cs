using UnityEngine;

namespace MixedUp
{
    /// <summary>Turns something slowly and forever: windmill blades, a weathervane.</summary>
    public class Spin : MonoBehaviour
    {
        public Vector3 axis = Vector3.forward;
        public float degreesPerSecond = 20f;

        void Update() => transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
