using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>The card of the main menu where the map of a solo game is chosen with two arrows.</summary>
    public class MapSelector : MonoBehaviour
    {
        public Button previous, next;
        public TMP_Text nameLabel;

        void Awake()
        {
            previous.onClick.AddListener(() => Step(-1));
            next.onClick.AddListener(() => Step(1));
        }

        void OnEnable()
        {
            Localization.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDisable() => Localization.LanguageChanged -= Refresh;

        void Step(int direction)
        {
            LevelCatalog.Selected = LevelCatalog.Step(LevelCatalog.Selected, direction);
            Refresh();
        }

        public void Refresh()
        {
            if (nameLabel != null) nameLabel.text = LevelCatalog.Selected.DisplayName;
        }
    }
}
