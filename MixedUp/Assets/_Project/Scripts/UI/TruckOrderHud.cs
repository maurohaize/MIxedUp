using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>Top-right panel listing what the truck still needs.</summary>
    public class TruckOrderHud : MonoBehaviour
    {
        public Truck truck;
        public RectTransform rowContainer;
        public OrderRowView rowTemplate;

        readonly List<OrderRowView> rows = new List<OrderRowView>();

        void Start()
        {
            if (truck == null || truck.order == null) return;

            rowTemplate.gameObject.SetActive(false);
            foreach (var line in truck.order.lines)
            {
                var row = Instantiate(rowTemplate, rowContainer);
                row.gameObject.SetActive(true);
                row.icon.sprite = line.box.icon;
                row.icon.color = line.box.icon != null ? Color.white : line.box.color;
                rows.Add(row);
            }

            truck.Changed += Refresh;
            Localization.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (truck != null) truck.Changed -= Refresh;
            Localization.LanguageChanged -= Refresh;
        }

        void Refresh()
        {
            var lines = truck.order.lines;
            for (int i = 0; i < rows.Count; i++)
            {
                var line = lines[i];
                int done = truck.DeliveredCount(line.box);
                bool complete = done >= line.count;

                rows[i].nameLabel.text = line.box.DisplayName;
                rows[i].countLabel.text = Mathf.Min(done, line.count) + "/" + line.count;
                rows[i].strikeThrough.enabled = complete;
            }
        }
    }
}
