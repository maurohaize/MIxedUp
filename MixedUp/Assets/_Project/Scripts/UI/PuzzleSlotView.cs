using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>One box position in the truck's cargo hold.</summary>
    public class PuzzleSlotView : MonoBehaviour
    {
        public Button button;
        public Image icon;
        public TMP_Text label;
        public Image selection;

        public void Show(BoxData box, bool selected)
        {
            icon.enabled = box != null;
            if (box != null)
            {
                icon.sprite = box.icon;
                icon.color = box.icon != null ? Color.white : box.color;
            }
            UiUtil.SetText(label, box != null ? box.DisplayName : string.Empty);
            selection.enabled = selected;
        }
    }
}
