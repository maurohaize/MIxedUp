using System.Text;
using TMPro;
using UnityEngine;

namespace MixedUp
{
    /// <summary>The little sign beside the spinning log: the best streaks of jumps over it and the current one. Just a detail.</summary>
    public class SweeperSign : MonoBehaviour
    {
        public Sweeper sweeper;
        public TMP_Text[] faces;

        public string CurrentText { get; private set; }

        void OnEnable()
        {
            Localization.LanguageChanged += Refresh;
            if (sweeper != null) sweeper.Board.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            Localization.LanguageChanged -= Refresh;
            if (sweeper != null) sweeper.Board.Changed -= Refresh;
        }

        void Refresh()
        {
            if (sweeper == null) return;

            var text = new StringBuilder();
            text.AppendLine(Localization.Get("sweeper.title"));
            var top = sweeper.Board.Top;
            for (int i = 0; i < JumpScoreboard.Places; i++)
            {
                if (i < top.Count) text.AppendLine((i + 1) + ". " + top[i].name + "  " + top[i].score);
                else text.AppendLine((i + 1) + ". ---");
            }

            var local = PlayerRegistry.Local;
            if (local != null) text.Append(Localization.Get("sweeper.now", sweeper.Board.Streak(Sweeper.NameOf(local))));

            CurrentText = text.ToString();
            if (faces == null) return;
            foreach (var face in faces)
                if (face != null) face.text = CurrentText;
        }
    }
}
