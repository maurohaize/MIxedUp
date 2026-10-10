using System;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// A box lying in the world. The textured model is a child of the visual node
    /// (instantiated from BoxData.worldPrefab when the level is built). Interacting moves it into the inventory.
    /// </summary>
    public class BoxPickup : MonoBehaviour, IInteractable
    {
        public BoxData data;
        public Transform visual;
        public float bobHeight = 0.12f;
        public float spinSpeed = 40f;

        bool available = true;
        Vector3 visualBase;
        float phaseOffset;

        public static event Action<BoxPickup> Collected;

        /// <summary>
        /// Identifies this box on every machine of an online game (the same level gives the same numbers; boxes dropped by a
        /// dead player get theirs from the machine that dropped them). -1 = not numbered.
        /// </summary>
        [NonSerialized] public int NetId = -1;

        public bool IsAvailable => available;
        public Transform InteractionTransform => transform;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Collected = null;

        void Awake()
        {
            if (visual != null) visualBase = visual.localPosition;
            phaseOffset = UnityEngine.Random.value * 6.28f;
        }

        void Update()
        {
            if (visual == null) return;
            visual.localPosition = visualBase + Vector3.up * (Mathf.Sin(Time.time * 2f + phaseOffset) * bobHeight);
            visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        }

        public bool TryGetPrompt(PlayerInteractor who, out InteractionPrompt prompt)
        {
            prompt = default;
            if (!available || data == null) return false;

            prompt = who.Inventory.IsFull
                ? InteractionPrompt.Blocked("prompt.inventory_full")
                : InteractionPrompt.Allowed("prompt.pickup", data.DisplayName);
            return true;
        }

        public void Interact(PlayerInteractor who)
        {
            if (!available || data == null) return;
            if (!who.Inventory.TryAdd(data, out _)) return;

            available = false;
            Collected?.Invoke(this);
            gameObject.SetActive(false);
            NetWorld.PickedUp(this);
        }

        /// <summary>Somebody else (in an online game) took this box: it disappears without going into any local inventory.</summary>
        public void RemoteTake()
        {
            if (!available) return;
            available = false;
            gameObject.SetActive(false);
        }
    }
}
