using TMPro;

namespace MixedUp
{
    public static class UiUtil
    {
        /// <summary>Assigns text only when it changed, avoiding needless TMP mesh rebuilds.</summary>
        public static void SetText(TMP_Text label, string value)
        {
            if (label != null && label.text != value) label.text = value;
        }
    }
}
