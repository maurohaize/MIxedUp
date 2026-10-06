using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MixedUp
{
    public enum GameState
    {
        Playing,
        Paused,
        GameOver,
        TruckPuzzle,
        Results
    }

    /// <summary>Owns the high-level game state: pause, the truck puzzle, results and game over.</summary>
    [DefaultExecutionOrder(-200)]
    public class GameManager : MonoBehaviour
    {
        public const string MainMenuScene = "MainMenu";

        public static GameManager Instance { get; private set; }
        /// <summary>
        /// Lets the UI consume the Escape key (to close a sub-menu) before it pauses or resumes the game.
        /// Return true when the key was used.
        /// </summary>
        public static Func<bool> EscapeInterceptor;
        public static bool InputBlocked => Instance != null && Instance.State != GameState.Playing;

        public Truck truck;
        [Tooltip("Seconds between dying / completing the order and the screen appearing.")]
        public float transitionDelay = 1.4f;

        [Tooltip("Seconds of play before time runs out (0 = no limit). Set by the LevelDirector from the game mode.")]
        public float timeLimitSeconds;

        PlayerStatus watched;
        bool transitioning;

        public GameState State { get; private set; } = GameState.Playing;
        public DeathCause LastDeathCause { get; private set; }
        public DeliveryResult LastResult { get; private set; }
        /// <summary>Seconds of actual play (excludes pauses and screens) since the level started.</summary>
        public float ElapsedPlaySeconds { get; private set; }
        public event Action<GameState> StateChanged;
        public bool HasTimeLimit => timeLimitSeconds > 0f;
        public float TimeLeft => HasTimeLimit ? Mathf.Max(0f, timeLimitSeconds - ElapsedPlaySeconds) : float.PositiveInfinity;
        public bool TimeIsUp { get; private set; }

        void Awake()
        {
            Instance = this;
            GameInput.Enable();
            Time.timeScale = 1f;
            ApplyCursor();
        }

        void OnEnable() => PlayerRegistry.LocalChanged += OnLocalChanged;

        void Start()
        {
            OnLocalChanged(PlayerRegistry.Local);
            if (truck != null) truck.OrderCompleted += OnOrderCompleted;
        }

        void OnDisable() => PlayerRegistry.LocalChanged -= OnLocalChanged;

        void OnDestroy()
        {
            if (watched != null) watched.Died -= OnLocalPlayerDied;
            if (truck != null) truck.OrderCompleted -= OnOrderCompleted;
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (State == GameState.Playing)
            {
                ElapsedPlaySeconds += Time.deltaTime;
                if (HasTimeLimit && ElapsedPlaySeconds >= timeLimitSeconds && !TimeIsUp)
                {
                    // Out of time: the delivery failed.
                    TimeIsUp = true;
                    LastDeathCause = DeathCause.Timeout;
                    SetState(GameState.GameOver);
                }
            }
            if (GameInput.Pause.WasPressedThisFrame() && !(EscapeInterceptor?.Invoke() ?? false)) TogglePause();
        }

        void OnLocalChanged(PlayerController player)
        {
            if (watched != null) watched.Died -= OnLocalPlayerDied;
            watched = player != null ? player.GetComponent<PlayerStatus>() : null;
            if (watched != null) watched.Died += OnLocalPlayerDied;
        }

        void OnLocalPlayerDied(DeathCause cause)
        {
            // Deaths during the truck puzzle are reported through CompleteDelivery instead.
            if (State != GameState.Playing && State != GameState.Paused) return;

            LastDeathCause = cause;
            StartCoroutine(TransitionAfterDelay(GameState.GameOver));
        }

        void OnOrderCompleted() => StartCoroutine(TransitionAfterDelay(GameState.TruckPuzzle));

        IEnumerator TransitionAfterDelay(GameState next)
        {
            if (transitioning) yield break;
            transitioning = true;
            yield return new WaitForSeconds(transitionDelay);
            transitioning = false;
            if (State == GameState.Playing || State == GameState.Paused) SetState(next);
        }

        /// <summary>Called once the truck puzzle is resolved: shows results, or game over if the local player died.</summary>
        public void CompleteDelivery(DeliveryResult result)
        {
            LastResult = result;
            if (result.LocalPlayerDied && result.Cause.HasValue)
            {
                LastDeathCause = result.Cause.Value;
                SetState(GameState.GameOver);
            }
            else
            {
                SetState(GameState.Results);
            }
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
            else if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void Resume()
        {
            if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex);
                return;
            }

            // A scene opened by path (for example by tests) has no build index.
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scene.name);
#endif
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            // Going back to the menu also leaves the room.
            RoomSession.End();
            RoomServices.Current.Leave();
            if (Application.CanStreamedLevelBeLoaded(MainMenuScene)) SceneManager.LoadScene(MainMenuScene);
            else Quit();
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void SetState(GameState next)
        {
            if (State == next) return;
            State = next;
            Time.timeScale = next == GameState.Playing ? 1f : 0f;
            ApplyCursor();
            StateChanged?.Invoke(next);
        }

        void ApplyCursor()
        {
            bool playing = State == GameState.Playing;
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;
        }
    }
}
