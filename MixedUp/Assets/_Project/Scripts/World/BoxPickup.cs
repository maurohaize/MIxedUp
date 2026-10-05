using System;
using UnityEngine;

namespace MixedUp
{
    /// <summary>A box lying in the world. Interacting with it moves it into the player's inventory.</summary>
    public class BoxPickup : MonoBehaviour, IInteractable
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public BoxData data;
        public Renderer[] tintRenderers;
        public SpriteRenderer iconRenderer;
        public Transform visual;
        public float bobHeight = 0.12f;
        public float spinSpeed = 40f;

        bool available = true;
        Vector3 visualBase;
        float phaseOffset;

        public static event Action<BoxPickup> Collected;

        public bool IsAvailable => available;
        public Transform InteractionTransform => transform;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Collected = null;

        void Awake()
        {
            if (visual != null) visualBase = visual.localPosition;
            phaseOffset = UnityEngine.Random.value * 6.28f;
            ApplyVisuals();
        }

        public void Setup(BoxData boxData)
        {
            data = boxData;
            ApplyVisuals();
        }

        void ApplyVisuals()
        {
            if (data == null) return;

            var block = new MaterialPropertyBlock();
            if (tintRenderers != null)
            {
                foreach (var r in tintRenderers)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(block);
                    block.SetColor(BaseColorId, data.color);
                    r.SetPropertyBlock(block);
                }
            }

            if (iconRenderer != null)
            {
                iconRenderer.sprite = data.icon;
                iconRenderer.enabled = data.icon != null;
            }
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
        }
    }
}
