using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Shows the boxes a player carries as real models held in their hands, the second one stacked on the first.
    /// Purely visual and derived from PlayerInventory (the single source of truth), so in multiplayer every client
    /// shows the same thing from the synchronised inventory. The inventory itself is unchanged.
    /// </summary>
    public class CarriedBoxesView : MonoBehaviour
    {
        [Tooltip("The player whose inventory is shown.")]
        public PlayerStatus status;
        [Tooltip("Bottom-centre of the stack, in the character's body space. Held to the front-right so the big head never hides it from a camera behind.")]
        public Transform anchor;
        [Tooltip("Size of a held box compared with the box lying on the ground.")]
        public float boxScale = 0.6f;
        public float stackGap = 0.03f;
        [Tooltip("Seconds of the little pop when a box appears in the hands.")]
        public float popSeconds = 0.2f;
        [Tooltip("Hands rest on the sides of the lowest box, this far from its faces.")]
        public float handInset = 0.04f;

        /// <summary>Extra offset (shake, bob) applied on top of the anchor's rest position by the animator.</summary>
        public Vector3 AnimationOffset { get; set; }

        sealed class Held
        {
            public BoxData box;
            public Transform root;
            public float age;
        }

        readonly List<Held> held = new List<Held>();
        PlayerInventory inventory;
        Vector3 restPosition;
        bool hasRest;

        public int VisibleCount => held.Count;
        public Transform BoxRoot(int index) => index >= 0 && index < held.Count ? held[index].root : null;

        void Awake() => CaptureRest();

        void CaptureRest()
        {
            if (hasRest || anchor == null) return;
            restPosition = anchor.localPosition;
            hasRest = true;
        }

        void OnEnable()
        {
            if (status == null) status = GetComponentInParent<PlayerStatus>();
            if (status == null) return;

            inventory = status.Inventory;
            inventory.Changed += Sync;
            Sync();
        }

        void OnDisable()
        {
            if (inventory != null) inventory.Changed -= Sync;
            Clear(0);
        }

        /// <summary>Where the given hand should be, in the space of the anchor's parent. side: -1 left, +1 right.</summary>
        public Vector3 HandPosition(int side)
        {
            CaptureRest();
            float half = boxScale * 0.5f + handInset;
            return restPosition + AnimationOffset + new Vector3(side * half, boxScale * 0.5f, 0f);
        }

        void Sync()
        {
            if (inventory == null || anchor == null) return;

            var wanted = new List<BoxData>(inventory.Capacity);
            var slots = inventory.Slots;
            for (int i = 0; i < slots.Count; i++)
                if (!slots[i].IsEmpty) wanted.Add(slots[i].box);

            // Keep the boxes that are already in the right place in the stack, rebuild from the first difference.
            int keep = 0;
            while (keep < held.Count && keep < wanted.Count && held[keep].box == wanted[keep]) keep++;
            Clear(keep);

            for (int i = keep; i < wanted.Count; i++) Add(wanted[i], i);
        }

        void Add(BoxData box, int level)
        {
            if (box.worldPrefab == null) return;

            var instance = Instantiate(box.worldPrefab, anchor);
            instance.name = "Carried_" + box.id;
            foreach (var collider in instance.GetComponentsInChildren<Collider>()) Destroy(collider);

            var t = instance.transform;
            t.localPosition = new Vector3(0f, level * (boxScale + stackGap), 0f);
            // A slight twist per level so a stack reads as hand-placed rather than machine-perfect.
            t.localRotation = Quaternion.Euler(0f, level == 0 ? 0f : 9f, 0f);
            t.localScale = Vector3.one * 0.01f;

            held.Add(new Held { box = box, root = t });
        }

        void Clear(int from)
        {
            for (int i = held.Count - 1; i >= from; i--)
            {
                if (held[i].root != null) Destroy(held[i].root.gameObject);
                held.RemoveAt(i);
            }
        }

        void LateUpdate()
        {
            if (anchor != null && hasRest) anchor.localPosition = restPosition + AnimationOffset;

            for (int i = 0; i < held.Count; i++)
            {
                var item = held[i];
                if (item.root == null) continue;

                item.age += Time.deltaTime;
                float t = popSeconds <= 0f ? 1f : Mathf.Clamp01(item.age / popSeconds);
                item.root.localScale = Vector3.one * (boxScale * EaseOutBack(t));
            }
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.9f, c3 = c1 + 1f;
            float u = t - 1f;
            return Mathf.Max(0.01f, 1f + c3 * u * u * u + c1 * u * u);
        }
    }
}
