using System;
using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// The delivery truck. Players hand over boxes here until the order is complete,
    /// at which point the 2D combination puzzle starts (phase 2).
    /// </summary>
    public class Truck : MonoBehaviour, IInteractable
    {
        public OrderData order;
        public Transform deliveryPoint;
        [Tooltip("Where delivered boxes are stacked (the loading dock behind the truck).")]
        public Transform bedAnchor;
        public float stackScale = 0.85f;

        readonly Dictionary<BoxData, int> delivered = new Dictionary<BoxData, int>();
        readonly List<BoxData> deliveredOrder = new List<BoxData>();

        public event Action Changed;
        public event Action<BoxData> BoxDelivered;
        public event Action OrderCompleted;

        public Transform InteractionTransform => deliveryPoint != null ? deliveryPoint : transform;
        public IReadOnlyList<BoxData> DeliveredBoxes => deliveredOrder;
        public int TotalDelivered => deliveredOrder.Count;
        public bool IsComplete => order != null && TotalDelivered >= order.TotalBoxes;

        public int DeliveredCount(BoxData box) => delivered.TryGetValue(box, out int n) ? n : 0;

        public bool Accepts(BoxData box) =>
            order != null && box != null && !IsComplete && DeliveredCount(box) < order.RequiredCount(box);

        public bool TryGetPrompt(PlayerInteractor who, out InteractionPrompt prompt)
        {
            prompt = default;
            if (IsComplete) return false;

            var selected = who.Inventory.Selected;
            if (selected.IsEmpty)
                prompt = InteractionPrompt.Blocked("prompt.truck_empty");
            else if (!Accepts(selected.box))
                prompt = InteractionPrompt.Blocked("prompt.truck_reject");
            else
                prompt = InteractionPrompt.Allowed("prompt.deliver", selected.box.DisplayName);
            return true;
        }

        public void Interact(PlayerInteractor who)
        {
            var selected = who.Inventory.Selected;
            if (selected.IsEmpty || !Accepts(selected.box)) return;

            var box = who.Inventory.RemoveAt(who.Inventory.SelectedIndex);
            Deliver(box);
        }

        public void Deliver(BoxData box)
        {
            if (!Accepts(box)) return;

            delivered[box] = DeliveredCount(box) + 1;
            deliveredOrder.Add(box);
            AddBedCube(box);

            GameEvents.RaiseToast("toast.delivered", box.DisplayName);
            BoxDelivered?.Invoke(box);
            Changed?.Invoke();
            if (IsComplete) OrderCompleted?.Invoke();
        }

        void AddBedCube(BoxData box)
        {
            if (box.worldPrefab == null || bedAnchor == null) return;

            int index = deliveredOrder.Count - 1;
            float step = 1.05f * stackScale;
            var stacked = Instantiate(box.worldPrefab, bedAnchor);
            stacked.transform.localPosition = new Vector3((index % 3 - 1) * step, (index / 6) * step, ((index / 3) % 2) * step);
            stacked.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(-8f, 8f), 0f);
            stacked.transform.localScale = Vector3.one * stackScale;
        }
    }
}
