using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>Shows the local player's carried boxes as icons at the bottom of the screen.</summary>
    public class InventoryHud : MonoBehaviour
    {
        [Serializable]
        public class SlotView
        {
            public GameObject root;
            public Image icon;
            public TMP_Text label;
            public Image selection;
            public RectTransform severityFill;
            public Image severityFillImage;
        }

        public SlotView[] slots;

        PlayerStatus status;

        public void Bind(PlayerStatus playerStatus) => status = playerStatus;

        void Update()
        {
            if (status == null) return;

            var inventory = status.Inventory;
            for (int i = 0; i < slots.Length; i++)
            {
                var view = slots[i];
                bool exists = i < inventory.Capacity;
                if (view.root.activeSelf != exists) view.root.SetActive(exists);
                if (!exists) continue;

                var slot = inventory.Slots[i];
                bool hasBox = !slot.IsEmpty;

                view.icon.enabled = hasBox;
                if (hasBox)
                {
                    view.icon.sprite = slot.box.icon;
                    view.icon.color = slot.box.icon != null ? Color.white : slot.box.color;
                }

                UiUtil.SetText(view.label, hasBox ? slot.box.DisplayName : Localization.Get("ui.empty_slot"));
                view.selection.enabled = i == inventory.SelectedIndex && hasBox;

                float severity = hasBox ? slot.box.MaxSeverity(status, slot.heldTime) : 0f;
                view.severityFill.anchorMax = new Vector2(severity, 1f);
                if (hasBox && view.severityFillImage != null)
                    view.severityFillImage.color = Color.Lerp(new Color(0.55f, 0.75f, 0.45f), new Color(0.85f, 0.25f, 0.2f), severity);
            }
        }
    }
}
