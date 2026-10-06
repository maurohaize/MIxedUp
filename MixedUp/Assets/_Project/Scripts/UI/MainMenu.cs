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
            settingsButton.onClick.AddListener(settings.Open);
            quitButton.onClick.AddListener(Quit);
            settings.gameObject.SetActive(false);
            settings.Closed += OnSettingsClosed;
        }

        void OnDestroy()
        {
            if (settings != null) settings.Closed -= OnSettingsClosed;
            if (lobby != null) lobby.Closed -= OnSettingsClosed;
        }

        void Update()
        {
            bool overlay = settings.IsOpen || (lobby != null && lobby.IsOpen);
            if (signpost != null && signpost.activeSelf == overlay) signpost.SetActive(!overlay);
            if (GameInput.Pause.WasPressedThisFrame())
            {
                if (settings.IsOpen) settings.Close();
                else if (lobby != null && lobby.IsOpen) lobby.Close();
            }
        }

        void OnSettingsClosed()
        {
            if (signpost != null) signpost.SetActive(true);
        }

        public void Play() => SceneManager.LoadScene(LevelScene);

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
