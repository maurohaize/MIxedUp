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
        /// <summary>Set by a screen with its own mouse interface (the online lobby) so the character and camera stop reading input.</summary>
        public static bool UiBlocksInput;
        public static bool InputBlocked => UiBlocksInput || NetWorld.Waiting || (Instance != null && Instance.State != GameState.Playing);

        public Truck truck;
        [Tooltip("Seconds between dying / completing the order and the screen appearing.")]
        public float transitionDelay = 1.4f;

        [Tooltip("Seconds of play before time runs out (0 = no limit). Set by the LevelDirector from the game mode.")]
        public float timeLimitSeconds;

        PlayerStatus watched;
        bool transitioning;
        bool spectating;

        public GameState State { get; private set; } = GameState.Playing;
        public DeathCause LastDeathCause { get; private set; }
        public DeliveryResult LastResult { get; private set; }
        /// <summary>Seconds of actual play (excludes pauses and screens) since the level started.</summary>
        public float ElapsedPlaySeconds { get; private set; }
        public event Action<GameState> StateChanged;
        public bool HasTimeLimit => timeLimitSeconds > 0f;
        public float TimeLeft => HasTimeLimit ? Mathf.Max(0f, timeLimitSeconds - ElapsedPlaySeconds) : float.PositiveInfinity;
        public bool TimeIsUp { get; private set; }
        /// <summary>True while the local player is dead but a teammate is still alive: the game goes on and the dead one watches.</summary>
        public bool IsSpectating => spectating;

        void Awake()
        {
            Instance = this;
            UiBlocksInput = false;
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
            // Online, the clock starts when every player has loaded the level (the host says so).
            if (State == GameState.Playing && !NetWorld.Waiting)
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
            if (spectating && State == GameState.Playing && !AnotherPlayerAlive())
            {
                // The last teammate fell as well.
                spectating = false;
                LastDeathCause = watched != null ? watched.LastCause : DeathCause.Burn;
                StartCoroutine(TransitionAfterDelay(GameState.GameOver));
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
            if (AnotherPlayerAlive())
            {
                spectating = true;
                return;
            }
            StartCoroutine(TransitionAfterDelay(GameState.GameOver));
        }

        /// <summary>Is there another real player (not the local one) still alive?</summary>
        public static bool AnotherPlayerAlive()
        {
            foreach (var player in PlayerRegistry.All)
            {
                if (player == null || player == PlayerRegistry.Local) continue;
                var status = player.GetComponent<PlayerStatus>();
                if (status != null && !status.IsDead) return true;
            }
            foreach (var avatar in NetAvatar.All)
                if (avatar != null && avatar.IsSpawned && !avatar.IsOwner && !avatar.CurrentPose.Has(AvatarPose.Dead)) return true;
            return false;
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
            if (OnlineSession.IsOnline)
            {
                // Everybody has to be in the same level: only the host can start it again, for all.
                if (OnlineSession.IsHost) OnlineSession.RestartMatch();
                else GameEvents.RaiseToast("toast.only_host");
                return;
            }
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
            OnlineSession.Leave();
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
            // Online, the pause menu does not stop the world: the others keep playing.
            bool worldGoesOn = next == GameState.Playing || (next == GameState.Paused && OnlineSession.IsOnline);
            Time.timeScale = worldGoesOn ? 1f : 0f;
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
