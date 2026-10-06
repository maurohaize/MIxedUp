using System;
using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Boxes a player carries. Boxes are data only: picking one up removes the world object.
    /// This component is the single source of truth for everything that happens to the carrier.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [Serializable]
        public class Slot
        {
            public BoxData box;
            public float heldTime;
            public bool IsEmpty => box == null;
        }

        [SerializeField, Range(1, GameInput.MaxSlots)] int capacity = 2;

        readonly List<Slot> slots = new List<Slot>();
        int selected;

        public event Action Changed;
        /// <summary>Raised on both inventories when a box changes hands between players (true = this one received it).</summary>
        public event Action<bool> Transferred;

        public int Capacity { get { EnsureSlots(); return capacity; } }
        public IReadOnlyList<Slot> Slots { get { EnsureSlots(); return slots; } }
        public int SelectedIndex { get { EnsureSlots(); return selected; } }
        public Slot Selected => Slots[SelectedIndex];

        public int Count
        {
            get
            {
                EnsureSlots();
                int count = 0;
                for (int i = 0; i < capacity; i++)
                    if (!slots[i].IsEmpty) count++;
                return count;
            }
        }

        public bool HasSpace => Count < Capacity;
        public bool IsFull => !HasSpace;

        void EnsureSlots()
        {
            while (slots.Count < capacity) slots.Add(new Slot());
        }

        public bool TryAdd(BoxData box, out int index)
        {
            EnsureSlots();
            index = -1;
            if (box == null) return false;

            for (int i = 0; i < capacity; i++)
            {
                if (!slots[i].IsEmpty) continue;

                slots[i].box = box;
                slots[i].heldTime = 0f;
                index = i;
                if (slots[selected].IsEmpty || selected == i) selected = i;
                Changed?.Invoke();
                return true;
            }
            return false;
        }

        public BoxData RemoveAt(int index)
        {
            EnsureSlots();
            if (index < 0 || index >= capacity || slots[index].IsEmpty) return null;

            var box = slots[index].box;
            slots[index].box = null;
            slots[index].heldTime = 0f;
            AdjustSelection();
            Changed?.Invoke();
            return box;
        }

        /// <summary>Moves a box to another inventory. Its carry timer restarts for the receiver.</summary>
        public bool TryTransfer(int index, PlayerInventory target)
        {
            EnsureSlots();
            if (target == null || target == this) return false;
            if (index < 0 || index >= capacity || slots[index].IsEmpty) return false;
            if (!target.HasSpace) return false;

            var box = slots[index].box;
            if (!target.TryAdd(box, out _)) return false;
            RemoveAt(index);
            Transferred?.Invoke(false);
            target.Transferred?.Invoke(true);
            return true;
        }

        public void Select(int index)
        {
            EnsureSlots();
            if (index < 0 || index >= capacity || index == selected) return;
            selected = index;
            Changed?.Invoke();
        }

        /// <summary>Moves the selection to the next occupied slot in the given direction.</summary>
        public void CycleSelection(int direction)
        {
            EnsureSlots();
            if (Count == 0) return;

            int dir = direction >= 0 ? 1 : -1;
            for (int step = 1; step <= capacity; step++)
            {
                int idx = (((selected + dir * step) % capacity) + capacity) % capacity;
                if (slots[idx].IsEmpty) continue;
                if (idx != selected)
                {
                    selected = idx;
                    Changed?.Invoke();
                }
                return;
            }
        }

        public void Tick(float deltaTime)
        {
            EnsureSlots();
            for (int i = 0; i < capacity; i++)
                if (!slots[i].IsEmpty) slots[i].heldTime += deltaTime;
        }

        public bool Contains(BoxData box)
        {
            EnsureSlots();
            for (int i = 0; i < capacity; i++)
                if (slots[i].box == box && box != null) return true;
            return false;
        }

        public int FirstOccupiedIndex()
        {
            EnsureSlots();
            for (int i = 0; i < capacity; i++)
                if (!slots[i].IsEmpty) return i;
            return -1;
        }

        public void Clear()
        {
            EnsureSlots();
            for (int i = 0; i < capacity; i++)
            {
                slots[i].box = null;
                slots[i].heldTime = 0f;
            }
            Changed?.Invoke();
        }

        void AdjustSelection()
        {
            if (!slots[selected].IsEmpty) return;
            for (int i = 1; i < capacity; i++)
            {
                int idx = (selected + i) % capacity;
                if (slots[idx].IsEmpty) continue;
                selected = idx;
                return;
            }
        }
    }
}
