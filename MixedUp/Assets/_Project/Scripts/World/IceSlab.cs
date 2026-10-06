using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// One slab of the ice a frozen box leaves in the river. It is solid, keeps feet out of the water (a HazardZone of type
    /// IceSheet on its trigger child), and melts away after a while: it shrinks, sinks, and finally disappears.
    /// </summary>
    public class IceSlab : MonoBehaviour
    {
        public Transform visual;
        public Collider solid;
        public Collider sheet;
        [Tooltip("Seconds the slab lasts before it starts to melt.")]
        public float lifetime = 26f;
        public float meltSeconds = 5f;

        float age;
        bool dead;
        Vector3 visualScale = Vector3.one;
        Vector3 visualPosition;

        public float Age => age;
        public bool IsMelting => age > lifetime;
        public bool IsSolid => !dead && solid != null && solid.enabled;

        void Awake()
        {
            if (visual != null)
            {
                visualScale = visual.localScale;
                visualPosition = visual.localPosition;
            }
        }

        /// <summary>The player who made the ice walks through it for a moment, so the slab never shoves them around.</summary>
        public void IgnoreFor(Collider owner, float seconds)
        {
            if (owner == null || solid == null) return;
            Physics.IgnoreCollision(solid, owner, true);
            StartCoroutine(Restore(owner, seconds));
        }

        System.Collections.IEnumerator Restore(Collider owner, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (owner != null && solid != null) Physics.IgnoreCollision(solid, owner, false);
        }

        /// <summary>Starts melting right now (the oldest slabs go first when there are too many).</summary>
        public void MeltNow()
        {
            if (age < lifetime) age = lifetime;
        }

        void Update()
        {
            if (dead) return;
            age += Time.deltaTime;
            if (age <= lifetime) return;

            float melt = Mathf.Clamp01((age - lifetime) / meltSeconds);
            if (visual != null)
            {
                visual.localScale = new Vector3(visualScale.x * (1f - 0.35f * melt), visualScale.y * (1f - 0.9f * melt), visualScale.z * (1f - 0.35f * melt));
                visual.localPosition = visualPosition + Vector3.down * (0.25f * melt);
            }

            // Stop being something to stand on once it is mostly gone.
            if (melt > 0.7f)
            {
                if (solid != null) solid.enabled = false;
                if (sheet != null) sheet.enabled = false;
            }
            if (melt >= 1f)
            {
                dead = true;
                Destroy(gameObject);
            }
        }
    }
}
