using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>The card of the main menu where the game mode is chosen with two arrows.</summary>
    public class ModeSelector : MonoBehaviour
    {
        public Button previous, next;
        public TMP_Text nameLabel, descriptionLabel, detailLabel;

        void Awake()
        {
            previous.onClick.AddListener(() => { GameModes.Step(-1); Refresh(); });
            next.onClick.AddListener(() => { GameModes.Step(1); Refresh(); });
        }

        void OnEnable()
        {
            Localization.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDisable() => Localization.LanguageChanged -= Refresh;

        public void Refresh()
        {
            var mode = GameModes.Selected;
            if (nameLabel != null) nameLabel.text = mode.DisplayName;
            if (descriptionLabel != null) descriptionLabel.text = mode.Description;
            if (detailLabel != null)
            {
                string time = mode.HasTimer ? Localization.Get("mode.minutes", Mathf.RoundToInt(mode.timeLimit / 60f)) : Localization.Get("mode.no_limit");
                detailLabel.text = time + "   x" + mode.reward.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
