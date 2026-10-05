using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>One line of the truck order panel: icon, box name and delivered/required count.</summary>
    public class OrderRowView : MonoBehaviour
    {
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text countLabel;
        public Image strikeThrough;
    }
}
