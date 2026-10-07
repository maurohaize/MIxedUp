using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// A tip that pops up as a toast when the local player gets near a hard-to-reach box ("crouch to fit through"). It is
    /// only shown when the game mode actually puts a box there, and not again for a while after it was shown.
    /// </summary>
    public class AreaHint : MonoBehaviour
    {
        public string hintKey;
        public float radius = 9f;
        public float cooldownSeconds = 90f;
        [Tooltip("Optional: the hint only appears while this spawn point holds a box.")]
        public BoxSpawnPoint point;

        bool inside;
        float nextAllowed;

        void Update()
        {
            var local = PlayerRegistry.Local;
            if (local == null || local.Status.IsDead) return;

            Vector3 offset = local.transform.position - transform.position;
            bool near = offset.magnitude <= radius;
            if (near && !inside && Time.time >= nextAllowed && (point == null || point.InUse))
            {
                GameEvents.RaiseToast(hintKey);
                nextAllowed = Time.time + cooldownSeconds;
            }
            inside = near;
        }
    }
}
