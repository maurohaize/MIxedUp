using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>
    /// The 2D view of the truck's cargo hold. Click two boxes to swap them, press DRIVE to start the trip,
    /// and keep rearranging while fuses burn. All game state lives in TruckPuzzleState.
    /// </summary>
    public class TruckPuzzleScreen : MonoBehaviour
    {
        public TruckPuzzleController controller;
        public PuzzleSlotView[] slots;
        public PuzzleJunctionView[] junctions;
        public TMP_Text titleLabel;
        public TMP_Text hintLabel;
        public TMP_Text alertLabel;
        public Button startButton;
        public Button manualButton;
        public ManualPanel manualPanel;

        [Header("Trip progress")]
        public GameObject travelRoot;
        public RectTransform travelFill;
        public RectTransform truckIcon;
        public RectTransform travelTrack;
        public TMP_Text timeLabel;

        TruckPuzzleState state;
        int selected = -1;

        void Awake()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                slots[i].button.onClick.AddListener(() => OnSlotClicked(index));
            }
            startButton.onClick.AddListener(() => controller.StartTravel());
            manualButton.onClick.AddListener(() => manualPanel.Show());
        }

        void OnEnable()
        {
            controller.Opened += OnOpened;
            Localization.LanguageChanged += Refresh;
            CombinationManual.Changed += Refresh;
            if (controller.State != null) Bind(controller.State);
        }

        void OnDisable()
        {
            controller.Opened -= OnOpened;
            Localization.LanguageChanged -= Refresh;
            CombinationManual.Changed -= Refresh;
            Unbind();
        }

        void OnOpened(TruckPuzzleState newState) => Bind(newState);

        void Bind(TruckPuzzleState newState)
        {
            Unbind();
            state = newState;
            selected = -1;
            state.Changed += Refresh;
            Refresh();
        }

        void Unbind()
        {
            if (state != null) state.Changed -= Refresh;
            state = null;
        }

        void OnSlotClicked(int index)
        {
            if (state == null || state.Phase == PuzzlePhase.Resolved || index >= state.Count) return;

            if (selected < 0)
            {
                selected = index;
            }
            else if (selected == index)
            {
                selected = -1;
            }
            else
            {
                state.Swap(selected, index);
                selected = -1;
            }
            Refresh();
        }

        void Update()
        {
            if (state == null || state.Phase != PuzzlePhase.Traveling) return;
            UpdateTravelBar();
        }

        void Refresh()
        {
            if (state == null) return;

            bool traveling = state.Phase == PuzzlePhase.Traveling;

            for (int i = 0; i < slots.Length; i++)
            {
                bool exists = i < state.Count;
                if (slots[i].gameObject.activeSelf != exists) slots[i].gameObject.SetActive(exists);
                if (exists) slots[i].Show(state[i], i == selected);
            }

            for (int i = 0; i < junctions.Length; i++)
            {
                bool exists = i < state.PairCount;
                if (junctions[i].gameObject.activeSelf != exists) junctions[i].gameObject.SetActive(exists);
                if (!exists) continue;

                var rule = state.PairRule(i);
                var outcome = state.PairOutcome(i);
                bool known = CombinationManual.IsKnown(rule);
                bool lit = state.IsFuseLit(i);
                junctions[i].Show(outcome, known || traveling, lit, state.FuseRemaining01(i));
            }

            UiUtil.SetText(titleLabel, Localization.Get(traveling ? "puzzle.title_travel" : "puzzle.title_arrange"));
            UiUtil.SetText(hintLabel, Localization.Get(selected >= 0 ? "puzzle.hint_second" : "puzzle.hint_swap"));

            bool alert = traveling && state.AnyFuseLit();
            if (alertLabel.gameObject.activeSelf != alert) alertLabel.gameObject.SetActive(alert);
            if (alert) UiUtil.SetText(alertLabel, Localization.Get("puzzle.reaction_alert"));

            startButton.gameObject.SetActive(state.Phase == PuzzlePhase.Arranging);
            manualButton.gameObject.SetActive(state.Phase != PuzzlePhase.Resolved);
            travelRoot.SetActive(traveling);
            if (traveling) UpdateTravelBar();
        }

        void UpdateTravelBar()
        {
            float progress = state.TravelProgress01;
            travelFill.anchorMax = new Vector2(progress, 1f);

            float width = travelTrack.rect.width;
            truckIcon.anchoredPosition = new Vector2(progress * width, truckIcon.anchoredPosition.y);
            UiUtil.SetText(timeLabel, Mathf.CeilToInt(state.TimeLeft).ToString());
        }
    }
}
