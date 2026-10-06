using TMPro;
using UnityEngine;

namespace MixedUp
{
    /// <summary>Top-centre card with the name of the game mode and, in timed modes, the time left (red and pulsing at the end).</summary>
    public class TimerHud : MonoBehaviour
    {
        public TMP_Text timeLabel;
        public TMP_Text modeLabel;
        [Tooltip("The card; it shrinks to just the mode name when there is no clock.")]
        public RectTransform panel;
        public float heightWithClock = 112f, heightWithoutClock = 64f;
        public Color normalColour = new Color(0.3f, 0.2f, 0.08f);
        public Color urgentColour = new Color(0.78f, 0.12f, 0.08f);
        public float urgentSeconds = 30f;

        public bool ShowsTimer => timeLabel != null && timeLabel.gameObject.activeSelf;

        void Update()
        {
            var game = GameManager.Instance;
            var director = LevelDirector.Instance;
            if (game == null) return;

            if (modeLabel != null && director != null)
            {
                string name = director.Mode.DisplayName;
                if (modeLabel.text != name) modeLabel.text = name;
            }

            bool timed = game.HasTimeLimit;
            if (timeLabel == null) return;
            if (timeLabel.gameObject.activeSelf != timed) timeLabel.gameObject.SetActive(timed);
            if (panel != null)
            {
                float height = timed ? heightWithClock : heightWithoutClock;
                if (!Mathf.Approximately(panel.sizeDelta.y, height)) panel.sizeDelta = new Vector2(panel.sizeDelta.x, height);
            }
            if (!timed) return;

            float left = game.TimeLeft;
            timeLabel.text = ResultsScreen.FormatTime(Mathf.Ceil(left));
            bool urgent = left <= urgentSeconds;
            timeLabel.color = urgent ? urgentColour : normalColour;
            float pulse = urgent ? 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 8f) : 1f;
            timeLabel.rectTransform.localScale = Vector3.one * pulse;
        }
    }
}
