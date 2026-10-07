using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>The main menu: play, settings, quit. Escape closes the settings first.</summary>
    public class MainMenu : MonoBehaviour
    {
        public const string LevelScene = "Level_Prototype";

        public Button playButton;
        public Button multiplayerButton;
        public LobbyPanel lobby;
        public Button settingsButton;
        public Button quitButton;
        public SettingsPanel settings;
        public Button achievementsButton;
        public AchievementsPanel achievements;
        [Tooltip("The signpost with the buttons; slides away while the settings card is open.")]
        public GameObject signpost;

        void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameInput.Enable();

            playButton.onClick.AddListener(Play);
            if (multiplayerButton != null) multiplayerButton.onClick.AddListener(lobby.Open);
            if (lobby != null)
            {
                lobby.gameObject.SetActive(false);
                lobby.Closed += OnSettingsClosed;
            }
            if (achievementsButton != null && achievements != null)
            {
                achievementsButton.onClick.AddListener(achievements.Open);
                achievements.gameObject.SetActive(false);
                achievements.Closed += OnSettingsClosed;
            }
            settingsButton.onClick.AddListener(settings.Open);
            quitButton.onClick.AddListener(Quit);
            settings.gameObject.SetActive(false);
            settings.Closed += OnSettingsClosed;

            // Back from an online game that ended on its own (the host left, the connection dropped).
            string notice = OnlineSession.TakeNotice();
            if (notice != null && lobby != null)
            {
                lobby.Open();
                lobby.ShowNotice(notice);
            }
        }

        void OnDestroy()
        {
            if (settings != null) settings.Closed -= OnSettingsClosed;
            if (lobby != null) lobby.Closed -= OnSettingsClosed;
            if (achievements != null) achievements.Closed -= OnSettingsClosed;
        }

        void Update()
        {
            bool overlay = settings.IsOpen || (lobby != null && lobby.IsOpen) || (achievements != null && achievements.IsOpen);
            if (signpost != null && signpost.activeSelf == overlay) signpost.SetActive(!overlay);
            if (GameInput.Pause.WasPressedThisFrame())
            {
                if (settings.IsOpen) settings.Close();
                else if (lobby != null && lobby.IsOpen) lobby.Close();
                else if (achievements != null && achievements.IsOpen) achievements.Close();
            }
        }

        void OnSettingsClosed()
        {
            if (signpost != null) signpost.SetActive(true);
        }

        public void Play() => SceneManager.LoadScene(LevelCatalog.Selected.scene);

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
