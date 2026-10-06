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
        public SettingsPanel settingsPanel;
        public PaperWipe wipe;
        public ResultsScreen resultsScreen;
        public TMP_Text deathCauseLabel;
        public TMP_Text quipLabel;

        [Header("Buttons")]
        public Button resumeButton;
        public Button settingsButton;
        public Button manualButton;
        public Button menuButton;
        public Button pauseQuitButton;
        public Button retryButton;
        public Button gameOverMenuButton;
        public Button resultsRetryButton;
        public Button resultsMenuButton;

        GameManager game;

        void Awake()
        {
            pausePanel.SetActive(false);
            gameOverPanel.SetActive(false);
            puzzlePanel.SetActive(false);
            resultsPanel.SetActive(false);
            manualPanel.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(false);
            wipe.sheet.gameObject.SetActive(false);

            manualButton.onClick.AddListener(manualPanel.Show);
            settingsButton.onClick.AddListener(settingsPanel.Open);
        }

        void OnEnable()
        {
            PlayerRegistry.LocalChanged += Bind;
            GameManager.EscapeInterceptor = HandleEscape;
        }

        void Start()
        {
            game = GameManager.Instance;
            if (game != null)
            {
                game.StateChanged += OnStateChanged;
                resumeButton.onClick.AddListener(game.Resume);
                menuButton.onClick.AddListener(game.GoToMainMenu);
                pauseQuitButton.onClick.AddListener(game.Quit);
                retryButton.onClick.AddListener(game.Restart);
                gameOverMenuButton.onClick.AddListener(game.GoToMainMenu);
                resultsRetryButton.onClick.AddListener(game.Restart);
                resultsMenuButton.onClick.AddListener(game.GoToMainMenu);
            }
            Bind(PlayerRegistry.Local);
        }

        void OnDisable()
        {
            PlayerRegistry.LocalChanged -= Bind;
            if (GameManager.EscapeInterceptor == (System.Func<bool>)HandleEscape) GameManager.EscapeInterceptor = null;
        }

        void OnDestroy()
        {
            if (game != null) game.StateChanged -= OnStateChanged;
        }

        /// <summary>Escape closes the topmost sub-menu first; only when there is none does it pause or resume.</summary>
        bool HandleEscape()
        {
            if (settingsPanel.IsOpen)
            {
                settingsPanel.Close();
                return true;
            }
            if (manualPanel.gameObject.activeSelf)
            {
                manualPanel.Hide();
                return true;
            }
            return false;
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
            if (state != GameState.Paused) settingsPanel.Close();

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
