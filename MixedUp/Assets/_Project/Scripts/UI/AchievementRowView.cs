using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>One line of the achievements card: name, what to do, progress and a tick once done.</summary>
    public class AchievementRowView : MonoBehaviour
    {
        public TMP_Text nameLabel;
        public TMP_Text descriptionLabel;
        public TMP_Text progressLabel;
        public Image tick;
        public CanvasGroup group;
    }
}
