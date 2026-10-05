using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>Wires the HUD to the local player and shows the right panel for each game state.</summary>
    public class UIManager : MonoBehaviour
    {
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
        public TMP_Text deathCauseLabel;

        [Header("Buttons")]
        public Button resumeButton;
        public Button pauseQuitButton;
        public Button retryButton;
        public Button gameOverQuitButton;
        public Button puzzleRetryButton;
        public Button puzzleQuitButton;
        public Button basqueButton;
        public Button spanishButton;
        public Button englishButton;

        GameManager game;

        void Awake()
        {
            pausePanel.SetActive(false);
            gameOverPanel.SetActive(false);
            puzzlePanel.SetActive(false);

            basqueButton.onClick.AddListener(() => Localization.SetLanguage(Language.Basque));
            spanishButton.onClick.AddListener(() => Localization.SetLanguage(Language.Spanish));
            englishButton.onClick.AddListener(() => Localization.SetLanguage(Language.English));
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
                puzzleRetryButton.onClick.AddListener(game.Restart);
                puzzleQuitButton.onClick.AddListener(game.Quit);
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
            puzzlePanel.SetActive(state == GameState.TruckPuzzle);
            hudRoot.SetActive(state == GameState.Playing || state == GameState.Paused);

            if (state == GameState.GameOver) deathCauseLabel.text = game.LastDeathCause.Localized;
        }
    }
}
