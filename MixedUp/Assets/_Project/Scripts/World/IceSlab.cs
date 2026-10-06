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
        /// <summary>Raised when the slab has melted away completely; the owner may reuse it (pooling) instead of destroying it.</summary>
        public System.Action<IceSlab> Released;
        public bool IsSolid => !dead && solid != null && solid.enabled;

        void Awake()
        {
            if (visual != null)
            {
                visualScale = visual.localScale;
                visualPosition = visual.localPosition;
            }
        }

        /// <summary>Puts the slab (new or recycled) at a spot, solid and fresh again.</summary>
        public void Begin(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            age = 0f;
            dead = false;
            if (visual != null)
            {
                visual.localScale = visualScale;
                visual.localPosition = visualPosition;
            }
            if (solid != null) solid.enabled = true;
            if (sheet != null) sheet.enabled = true;
            gameObject.SetActive(true);
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
                // It shrinks and sinks, a little faster at the end.
                float shrink = melt * melt * 0.3f + melt * 0.2f;
                visual.localScale = new Vector3(visualScale.x * (1f - shrink), visualScale.y * (1f - 0.9f * melt), visualScale.z * (1f - shrink));
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
                if (Released != null) Released(this);
                else Destroy(gameObject);
            }
        }
    }
}
