using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>Wires the HUD to the local player and shows the right panel for each game state.</summary>
    public class UIManager : MonoBehaviour
    {
        const int QuipCount = 4;

        [Header("HUD")]
        public GameObject hudRoot;
        public InventoryHud inventoryHud;
        public HealthHud healthHud;
        public PromptHud promptHud;
        public EffectHud effectHud;

        [Header("Panels")]
        public GameObject pausePanel;
        public GameObject gameOverPanel;
        public GameObject puzzlePanel;
        public GameObject resultsPanel;
        public ManualPanel manualPanel;
        public PaperWipe wipe;
        public ResultsScreen resultsScreen;
        public TMP_Text deathCauseLabel;
        public TMP_Text quipLabel;

        [Header("Buttons")]
        public Button resumeButton;
        public Button manualButton;
        public Button pauseQuitButton;
        public Button retryButton;
        public Button gameOverQuitButton;
        public Button resultsRetryButton;
        public Button resultsQuitButton;
        public Button basqueButton;
        public Button spanishButton;
        public Button englishButton;

        GameManager game;

        void Awake()
        {
            pausePanel.SetActive(false);
            gameOverPanel.SetActive(false);
            puzzlePanel.SetActive(false);
            resultsPanel.SetActive(false);
            manualPanel.gameObject.SetActive(false);
            wipe.sheet.gameObject.SetActive(false);

            basqueButton.onClick.AddListener(() => Localization.SetLanguage(Language.Basque));
            spanishButton.onClick.AddListener(() => Localization.SetLanguage(Language.Spanish));
            englishButton.onClick.AddListener(() => Localization.SetLanguage(Language.English));
            manualButton.onClick.AddListener(manualPanel.Show);
        }

        void OnEnable() => PlayerRegistry.LocalChanged += Bind;

        void Start()
        {
            game = GameManager.Instance;
            if (game != null)
            {
                game.StateChanged += OnStateChanged;
                resumeButton.onClick.AddListener(game.Resume);
                pauseQuitButton.onClick.AddListener(game.Quit);
                retryButton.onClick.AddListener(game.Restart);
                gameOverQuitButton.onClick.AddListener(game.Quit);
                resultsRetryButton.onClick.AddListener(game.Restart);
                resultsQuitButton.onClick.AddListener(game.Quit);
            }
            Bind(PlayerRegistry.Local);
        }

        void OnDisable() => PlayerRegistry.LocalChanged -= Bind;

        void OnDestroy()
        {
            if (game != null) game.StateChanged -= OnStateChanged;
        }

        void Bind(PlayerController player)
        {
            if (player == null) return;

            var status = player.GetComponent<PlayerStatus>();
            inventoryHud.Bind(status);
            healthHud.Bind(status);
            effectHud.Bind(status);
            promptHud.Bind(player.GetComponent<PlayerInteractor>());
        }

        void OnStateChanged(GameState state)
        {
            pausePanel.SetActive(state == GameState.Paused);
            gameOverPanel.SetActive(state == GameState.GameOver);
            resultsPanel.SetActive(state == GameState.Results);
            if (state != GameState.Paused && state != GameState.TruckPuzzle) manualPanel.Hide();

            if (state == GameState.TruckPuzzle)
            {
                StartCoroutine(OpenPuzzleThroughWipe());
            }
            else
            {
                puzzlePanel.SetActive(false);
                hudRoot.SetActive(state == GameState.Playing || state == GameState.Paused);
            }

            if (state == GameState.GameOver)
            {
                deathCauseLabel.text = game.LastDeathCause.Localized;
                quipLabel.text = Localization.Get("quip." + (1 + Random.Range(0, QuipCount)));
            }
            if (state == GameState.Results) resultsScreen.Show(game.LastResult);
        }

        IEnumerator OpenPuzzleThroughWipe()
        {
            yield return wipe.Play(() =>
            {
                hudRoot.SetActive(false);
                puzzlePanel.SetActive(true);
            });
        }
    }
}
