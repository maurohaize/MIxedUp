using TMPro;
using UnityEngine;

namespace MixedUp
{
    /// <summary>"DELIVERY COMPLETE": boxes delivered, time and the money earned.</summary>
    public class ResultsScreen : MonoBehaviour
    {
        public TMP_Text titleLabel;
        public TMP_Text boxesLabel;
        public TMP_Text timeLabel;
        public TMP_Text penaltyLabel;
        public TMP_Text rewardLabel;
        public TMP_Text walletLabel;

        DeliveryResult shown;

        void OnEnable() => Localization.LanguageChanged += Redraw;
        void OnDisable() => Localization.LanguageChanged -= Redraw;

        public void Show(DeliveryResult result)
        {
            shown = result;
            Redraw();
        }

        void Redraw()
        {
            if (shown == null) return;

            string titleKey;
            Color titleColor;
            switch (shown.Outcome)
            {
                case CombinationOutcome.Safe:
                    titleKey = "result.title.safe";
                    titleColor = new Color(0.35f, 0.55f, 0.3f);
                    break;
                case CombinationOutcome.Danger:
                    titleKey = "result.title.danger";
                    titleColor = new Color(0.75f, 0.55f, 0.1f);
                    break;
                default:
                    titleKey = "result.title.explosion";
                    titleColor = new Color(0.75f, 0.25f, 0.1f);
                    break;
            }
            titleLabel.text = Localization.Get(titleKey);
            titleLabel.color = titleColor;

            var mode = GameModes.Find(shown.ModeId);
            string modeName = mode != null && mode != GameModes.Classic ? "   (" + mode.DisplayName + ")" : string.Empty;
            boxesLabel.text = Localization.Get("result.boxes", shown.Delivered, shown.Total) + modeName;
            timeLabel.text = Localization.Get("result.time", FormatTime(shown.Seconds))
                             + (shown.TimeBonus > 0 ? "   " + Localization.Get("result.time_bonus", shown.TimeBonus) : string.Empty);

            bool showPenalty = shown.Outcome == CombinationOutcome.Danger && shown.DangerCount > 0;
            penaltyLabel.gameObject.SetActive(showPenalty || shown.Outcome >= CombinationOutcome.Explosion);
            penaltyLabel.text = shown.Outcome >= CombinationOutcome.Explosion
                ? Localization.Get("result.destroyed")
                : Localization.Get("result.accidents", shown.DangerCount);

            rewardLabel.text = shown.Reward.ToString();
            walletLabel.text = Localization.Get("result.wallet", Wallet.Coins);
        }

        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }
    }
}
