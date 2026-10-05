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
        TruckPuzzle
    }

    /// <summary>Owns the high-level game state: pause, game over, and the hand-off to the truck puzzle.</summary>
    [DefaultExecutionOrder(-200)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public static bool InputBlocked => Instance != null && Instance.State != GameState.Playing;

        public Truck truck;
        [Tooltip("Seconds between dying / completing the order and the screen appearing.")]
        public float transitionDelay = 1.4f;

        PlayerStatus watched;
        bool transitioning;

        public GameState State { get; private set; } = GameState.Playing;
        public DeathCause LastDeathCause { get; private set; }
        public event Action<GameState> StateChanged;

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
            if (GameInput.Pause.WasPressedThisFrame()) TogglePause();
        }

        void OnLocalChanged(PlayerController player)
        {
            if (watched != null) watched.Died -= OnLocalPlayerDied;
            watched = player != null ? player.GetComponent<PlayerStatus>() : null;
            if (watched != null) watched.Died += OnLocalPlayerDied;
        }

        void OnLocalPlayerDied(DeathCause cause)
        {
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
