using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>The combination manual: every rule with its outcome and hint once discovered, "???" before.</summary>
    public class ManualPanel : MonoBehaviour
    {
        public CombinationRules rules;
        public RectTransform rowContainer;
        public ManualRowView rowTemplate;
        public Button closeButton;

        readonly List<ManualRowView> rows = new List<ManualRowView>();
        readonly List<CombinationRule> shown = new List<CombinationRule>();
        bool built;

        void Awake() => closeButton.onClick.AddListener(Hide);

        void OnEnable()
        {
            Build();
            CombinationManual.Changed += Refresh;
            Localization.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            CombinationManual.Changed -= Refresh;
            Localization.LanguageChanged -= Refresh;
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        void Build()
        {
            if (built || rules == null) return;
            built = true;

            rowTemplate.gameObject.SetActive(false);
            foreach (var rule in rules.rules)
            {
                if (rule == null || rule.IsTrivial) continue;
                var row = Instantiate(rowTemplate, rowContainer);
                row.gameObject.SetActive(true);
                SetIcon(row.iconA, rule.a);
                SetIcon(row.iconB, rule.b);
                rows.Add(row);
                shown.Add(rule);
            }
        }

        static void SetIcon(Image image, BoxData box)
        {
            image.sprite = box.icon;
            image.color = box.icon != null ? Color.white : box.color;
        }

        void Refresh()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var rule = shown[i];
                bool known = CombinationManual.IsKnown(rule);
                rows[i].marker.Show(rule.outcome, known);
                string hint = known
                    ? Localization.Get(OutcomeStyle.NameKey(rule.outcome, true)) + ": " + Localization.Get(rule.hintKey)
                    : Localization.Get("manual.unknown");
                UiUtil.SetText(rows[i].hintLabel, hint);
            }
        }
    }
}
