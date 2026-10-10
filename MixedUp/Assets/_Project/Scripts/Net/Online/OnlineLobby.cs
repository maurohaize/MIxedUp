using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>
    /// The 3D meeting place of an online game. Players appear in the plaza as they connect and can walk about; the interface
    /// lists them with their ready state, shows the room code or address, and lets the host pick the map and the game mode and
    /// start, which sends everybody to the chosen level. Press Tab to use the mouse on the interface.
    /// </summary>
    public class OnlineLobby : MonoBehaviour
    {
        [Header("Where players stand when they arrive")]
        public Transform[] slots = System.Array.Empty<Transform>();

        [Header("Room")]
        public TMP_Text codeLabel;
        public TMP_Text hintLabel;
        public TMP_Text[] memberNames = System.Array.Empty<TMP_Text>();
        public TMP_Text[] memberStates = System.Array.Empty<TMP_Text>();
        public Image[] memberSkin = System.Array.Empty<Image>();
        public Image[] memberClothes = System.Array.Empty<Image>();
        public PlayerPalette palette;

        [Header("Host choices")]
        public TMP_Text mapName;
        public TMP_Text mapDescription;
        public Button mapPrevious, mapNext;
        public TMP_Text modeName;
        public TMP_Text modeDescription;
        public Button modePrevious, modeNext;

        [Header("Buttons")]
        public Button readyButton;
        public TMP_Text readyLabel;
        public Button startButton;
        public Button leaveButton;
        public TMP_Text waitingLabel;
        public GameObject mousePanel;

        bool uiMode;
        bool placed;
        bool wired;
        bool dirty = true;
        float refreshTimer;

        public bool UiMode => uiMode;

        void Awake()
        {
            GameInput.Enable();
            Time.timeScale = 1f;
            // The lobby is all buttons, so it opens with the mouse free; Tab lets you walk about the plaza.
            SetUiMode(true);
        }

        void OnEnable()
        {
            Wire();
            NetAvatar.Changed += MarkDirty;
            Localization.LanguageChanged += MarkDirty;
            OnlineSession.Disconnected += OnDisconnected;
        }

        void OnDisable()
        {
            NetAvatar.Changed -= MarkDirty;
            Localization.LanguageChanged -= MarkDirty;
            OnlineSession.Disconnected -= OnDisconnected;
            GameManager.UiBlocksInput = false;
        }

        void Wire()
        {
            if (wired) return;
            wired = true;
            readyButton.onClick.AddListener(ToggleReady);
            startButton.onClick.AddListener(StartMatch);
            leaveButton.onClick.AddListener(Leave);
            mapPrevious.onClick.AddListener(() => StepMap(-1));
            mapNext.onClick.AddListener(() => StepMap(1));
            modePrevious.onClick.AddListener(() => StepMode(-1));
            modeNext.onClick.AddListener(() => StepMode(1));
        }

        void MarkDirty() => dirty = true;

        void OnDisconnected()
        {
            if (this != null && gameObject.scene.isLoaded && SceneManager.GetActiveScene().name == OnlineSession.LobbyScene)
                GoToMenu();
        }

        // ---------------------------------------------------------------- input

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.tabKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
                SetUiMode(!uiMode);

            // Something else (the editor after Esc, a focus change) may have locked the cursor again: the buttons must stay usable.
            if (uiMode && (Cursor.lockState != CursorLockMode.None || !Cursor.visible))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            PlaceLocalPlayer();

            refreshTimer -= Time.unscaledDeltaTime;
            if (dirty || refreshTimer <= 0f)
            {
                dirty = false;
                refreshTimer = 0.25f;
                Refresh();
            }
        }

        void SetUiMode(bool value)
        {
            uiMode = value;
            GameManager.UiBlocksInput = value;
            Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = value;
            if (mousePanel != null) mousePanel.SetActive(value);
        }

        /// <summary>Moves the local character to its own spot of the plaza the first time the network knows who we are.</summary>
        void PlaceLocalPlayer()
        {
            if (placed || NetAvatar.Local == null || PlayerRegistry.Local == null || slots.Length == 0) return;
            placed = true;

            int index = Mathf.Max(0, NetAvatar.Sorted().IndexOf(NetAvatar.Local)) % slots.Length;
            var slot = slots[index];
            PlayerRegistry.Local.Teleport(slot.position);
            PlayerRegistry.Local.visual.rotation = slot.rotation;
        }

        // -------------------------------------------------------------- actions

        void ToggleReady()
        {
            var me = NetAvatar.Local;
            if (me != null && !me.IsHostAvatar) me.SetReady(!me.Ready);
        }

        void StepMap(int direction)
        {
            var host = NetAvatar.Host;
            if (host == null || !OnlineSession.IsHost) return;
            host.SetMap(LevelCatalog.Step(LevelCatalog.Find(host.MapId) ?? LevelCatalog.Prototype, direction).id);
        }

        void StepMode(int direction)
        {
            var host = NetAvatar.Host;
            if (host == null || !OnlineSession.IsHost) return;
            int index = GameModes.IndexOf(GameModes.Find(host.ModeId) ?? GameModes.Classic);
            host.SetMode(GameModes.All[(index + direction + GameModes.All.Length) % GameModes.All.Length].id);
        }

        bool CanStart
        {
            get
            {
                if (!OnlineSession.IsHost || NetAvatar.Host == null || NetAvatar.Host.Started) return false;
                foreach (var avatar in NetAvatar.Sorted())
                    if (!avatar.IsHostAvatar && !avatar.Ready) return false;
                return true;
            }
        }

        void StartMatch()
        {
            if (!CanStart) return;
            var level = LevelCatalog.Find(NetAvatar.Host.MapId) ?? LevelCatalog.Prototype;
            NetAvatar.Host.BeginMatch(Random.Range(1, int.MaxValue));
            OnlineSession.LoadForAll(level.scene);
        }

        void Leave()
        {
            OnlineSession.Leave();
            GoToMenu();
        }

        static void GoToMenu()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameManager.UiBlocksInput = false;
            if (Application.CanStreamedLevelBeLoaded(GameManager.MainMenuScene)) SceneManager.LoadScene(GameManager.MainMenuScene);
        }

        // -------------------------------------------------------------- drawing

        void Refresh()
        {
            if (!wired) return;

            var members = NetAvatar.Sorted();
            var me = NetAvatar.Local;
            bool host = OnlineSession.IsHost;

            string codeKey = OnlineSession.Kind == ConnectionKind.Relay ? "lobby.code" : "lobby.address";
            if (codeLabel != null)
                codeLabel.text = OnlineSession.IsOnline && !string.IsNullOrEmpty(OnlineSession.JoinCode)
                    ? Localization.Get(codeKey) + ": " + OnlineSession.JoinCode
                    : Localization.Get("lobby.not_connected");
            if (hintLabel != null)
                hintLabel.text = host && OnlineSession.Kind == ConnectionKind.Direct ? Localization.Get("lobby.hint_direct") : string.Empty;

            for (int i = 0; i < memberNames.Length; i++)
            {
                bool present = i < members.Count;
                if (!present)
                {
                    memberNames[i].text = "-";
                    memberStates[i].text = Localization.Get("lobby.waiting_slot");
                    SetColour(memberSkin, i, new Color(1f, 1f, 1f, 0.15f));
                    SetColour(memberClothes, i, new Color(1f, 1f, 1f, 0.15f));
                    continue;
                }

                var member = members[i];
                memberNames[i].text = member.DisplayName + (member == me ? " (" + Localization.Get("ui.you") + ")" : string.Empty);
                memberStates[i].text = member.IsHostAvatar ? Localization.Get("lobby.host")
                    : member.Ready ? Localization.Get("lobby.ready") : Localization.Get("lobby.not_ready");
                if (palette != null)
                {
                    SetColour(memberSkin, i, palette.Skin(member.Skin));
                    SetColour(memberClothes, i, palette.Clothes(member.Clothes));
                }
            }

            var hostAvatar = NetAvatar.Host;
            var level = (hostAvatar != null ? LevelCatalog.Find(hostAvatar.MapId) : null) ?? LevelCatalog.Prototype;
            var mode = (hostAvatar != null ? GameModes.Find(hostAvatar.ModeId) : null) ?? GameModes.Classic;
            mapName.text = level.DisplayName;
            mapDescription.text = level.Description;
            modeName.text = mode.DisplayName;
            modeDescription.text = mode.Description;

            bool manyMaps = LevelCatalog.All.Length > 1;
            mapPrevious.gameObject.SetActive(host && manyMaps);
            mapNext.gameObject.SetActive(host && manyMaps);
            modePrevious.gameObject.SetActive(host);
            modeNext.gameObject.SetActive(host);

            readyButton.gameObject.SetActive(!host);
            startButton.gameObject.SetActive(host);
            startButton.interactable = CanStart;
            if (readyLabel != null)
            {
                var localized = readyLabel.GetComponent<LocalizedText>();
                string key = me != null && me.Ready ? "lobby.not_ready_action" : "lobby.ready_action";
                if (localized != null) localized.SetKey(key);
                else readyLabel.text = Localization.Get(key);
            }
            waitingLabel.text = host
                ? (CanStart ? string.Empty : Localization.Get("lobby.need_ready"))
                : Localization.Get("lobby.waiting_host");
        }

        static void SetColour(Image[] images, int index, Color colour)
        {
            if (index < images.Length && images[index] != null) images[index].color = colour;
        }
    }
}
