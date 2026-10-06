using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>One line of the combination manual: the two boxes, the outcome marker and the hint.</summary>
    public class ManualRowView : MonoBehaviour
    {
        public Image iconA;
        public Image iconB;
        public OutcomeMarker marker;
        public TMP_Text hintLabel;
    }
}
