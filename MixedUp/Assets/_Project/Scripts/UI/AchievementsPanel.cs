using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>The list of achievements opened from the main menu: what each one asks for and how far along you are.</summary>
    public class AchievementsPanel : MonoBehaviour
    {
        public RectTransform rowContainer;
        public AchievementRowView rowTemplate;
        public TMP_Text summaryLabel;
        public Button closeButton;

        readonly List<AchievementRowView> rows = new List<AchievementRowView>();
        bool built;

        public event Action Closed;
        public bool IsOpen => gameObject.activeSelf;

        void Awake() => closeButton.onClick.AddListener(Close);

        void OnEnable()
        {
            Build();
            Achievements.Changed += Refresh;
            Localization.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            Achievements.Changed -= Refresh;
            Localization.LanguageChanged -= Refresh;
        }

        public void Open() => gameObject.SetActive(true);

        public void Close()
        {
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        void Build()
        {
            if (built) return;
            built = true;

            rowTemplate.gameObject.SetActive(false);
            foreach (var def in Achievements.All)
            {
                var row = Instantiate(rowTemplate, rowContainer);
                row.gameObject.SetActive(true);
                rows.Add(row);
            }
        }

        void Refresh()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var def = Achievements.All[i];
                bool done = Achievements.IsUnlocked(def);
                var row = rows[i];
                UiUtil.SetText(row.nameLabel, Localization.Get(def.NameKey));
                UiUtil.SetText(row.descriptionLabel, Localization.Get(def.DescriptionKey));
                UiUtil.SetText(row.progressLabel, def.goal > 1 ? Achievements.Progress(def) + " / " + def.goal : string.Empty);
                row.tick.enabled = done;
                row.group.alpha = done ? 1f : 0.72f;
            }
            UiUtil.SetText(summaryLabel, Localization.Get("ach.summary", Achievements.UnlockedCount, Achievements.All.Length));
        }
    }
}
