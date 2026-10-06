using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>
    /// The multiplayer lobby opened from the main menu: put in your name, create a room or join one with its code; inside a
    /// room see who is there, get ready, and (as host) pick the game mode and start. Talks to IRoomService only.
    /// </summary>
    public class LobbyPanel : MonoBehaviour
    {
        [Header("Entry view")]
        public GameObject entryView;
        public TMP_InputField nameInput;
        public TMP_InputField codeInput;
        public Button createButton;
        public Button joinButton;
        public Button backButton;
        public TMP_Text messageLabel;

        [Header("Room view")]
        public GameObject roomView;
        public TMP_Text codeLabel;
        public TMP_Text[] memberNames = Array.Empty<TMP_Text>();
        public TMP_Text[] memberStates = Array.Empty<TMP_Text>();
        public Image[] memberSkin = Array.Empty<Image>();
        public Image[] memberClothes = Array.Empty<Image>();
        public PlayerPalette palette;
        public TMP_Text modeLabel;
        public TMP_Text modeDescription;
        public Button modePrevious, modeNext;
        public Button readyButton;
        public TMP_Text readyButtonLabel;
        public Button startButton;
        public Button leaveButton;
        public TMP_Text waitingLabel;

        public event Action Closed;

        public bool IsOpen => gameObject.activeSelf;
        IRoomService Rooms => RoomServices.Current;

        bool wired;
        string lastMessageKey;

        void Awake() => Wire();

        void Wire()
        {
            if (wired) return;
            wired = true;

            nameInput.characterLimit = PlayerProfile.MaxNameLength;
            nameInput.onEndEdit.AddListener(text => PlayerProfile.Name = text);
            codeInput.characterLimit = RoomCode.Length;
            codeInput.onValueChanged.AddListener(text =>
            {
                string upper = text.ToUpperInvariant();
                if (upper != text) codeInput.SetTextWithoutNotify(upper);
            });

            createButton.onClick.AddListener(Create);
            joinButton.onClick.AddListener(Join);
            backButton.onClick.AddListener(Close);
            leaveButton.onClick.AddListener(Leave);
            readyButton.onClick.AddListener(ToggleReady);
            startButton.onClick.AddListener(() => Rooms.StartGame());
            modePrevious.onClick.AddListener(() => StepMode(-1));
            modeNext.onClick.AddListener(() => StepMode(1));
        }

        void OnEnable()
        {
            Wire();
            Rooms.RoomChanged += Refresh;
            Rooms.Started += OnStarted;
            Rooms.Closed += OnClosed;
            Localization.LanguageChanged += Refresh;
            nameInput.SetTextWithoutNotify(PlayerProfile.Name);
            ShowMessage(null);
            Refresh();
        }

        void OnDisable()
        {
            Rooms.RoomChanged -= Refresh;
            Rooms.Started -= OnStarted;
            Rooms.Closed -= OnClosed;
            Localization.LanguageChanged -= Refresh;
        }

        void Update()
        {
            if (Rooms is LocalRoomService local) local.Tick(Time.unscaledDeltaTime);
        }

        public void Open() => gameObject.SetActive(true);

        /// <summary>Closes the lobby; leaving the room if you were in one.</summary>
        public void Close()
        {
            if (!IsOpen) return;
            if (Rooms.Current != null) Rooms.Leave();
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        // ------------------------------------------------------------------ actions

        string PlayerName()
        {
            string name = PlayerProfile.Sanitize(nameInput.text, true);
            PlayerProfile.Name = name;
            return name;
        }

        void Create()
        {
            Rooms.CreateRoom(PlayerName(), RoomInfo.MaxPlayersLimit, CharacterCustomization.SkinIndex, CharacterCustomization.ClothesIndex);
            ShowMessage(null);
        }

        void Join()
        {
            var result = Rooms.JoinRoom(codeInput.text, PlayerName(), CharacterCustomization.SkinIndex, CharacterCustomization.ClothesIndex);
            switch (result)
            {
                case JoinResult.Ok: ShowMessage(null); break;
                case JoinResult.BadCode: ShowMessage("lobby.err.bad"); break;
                case JoinResult.NotFound: ShowMessage("lobby.err.notfound"); break;
                case JoinResult.Full: ShowMessage("lobby.err.full"); break;
                case JoinResult.AlreadyStarted: ShowMessage("lobby.err.started"); break;
            }
        }

        void Leave()
        {
            Rooms.Leave();
            ShowMessage(null);
            Refresh();
        }

        void ToggleReady()
        {
            var me = Rooms.Current?.Local;
            if (me != null) Rooms.SetReady(!me.ready);
        }

        void StepMode(int direction)
        {
            var room = Rooms.Current;
            if (room == null) return;
            int index = GameModes.IndexOf(GameModes.Find(room.modeId) ?? GameModes.Classic);
            Rooms.SetMode(GameModes.All[(index + direction + GameModes.All.Length) % GameModes.All.Length].id);
        }

        void OnStarted() => SceneManager.LoadScene(MainMenu.LevelScene);

        void OnClosed()
        {
            ShowMessage("lobby.closed");
            Refresh();
        }

        void ShowMessage(string key)
        {
            lastMessageKey = key;
            if (messageLabel != null) messageLabel.text = key == null ? string.Empty : Localization.Get(key);
        }

        // ------------------------------------------------------------------ drawing

        void Refresh()
        {
            if (!wired) return;
            var room = Rooms.Current;
            entryView.SetActive(room == null);
            roomView.SetActive(room != null);
            if (messageLabel != null) messageLabel.text = lastMessageKey == null ? string.Empty : Localization.Get(lastMessageKey);
            if (room == null) return;

            codeLabel.text = Localization.Get("lobby.code") + ": " + room.code;
            var me = room.Local;
            bool host = me != null && me.isHost;

            for (int i = 0; i < memberNames.Length; i++)
            {
                bool present = i < room.members.Count;
                memberNames[i].transform.parent.gameObject.SetActive(true);
                if (!present)
                {
                    memberNames[i].text = "-";
                    memberStates[i].text = Localization.Get("lobby.waiting_slot");
                    SetColour(memberSkin, i, new Color(1f, 1f, 1f, 0.15f));
                    SetColour(memberClothes, i, new Color(1f, 1f, 1f, 0.15f));
                    continue;
                }

                var member = room.members[i];
                memberNames[i].text = member.name + (member.isLocal ? " (" + Localization.Get("ui.you") + ")" : string.Empty);
                memberStates[i].text = member.isHost ? Localization.Get("lobby.host") : member.ready ? Localization.Get("lobby.ready") : Localization.Get("lobby.not_ready");
                if (palette != null)
                {
                    SetColour(memberSkin, i, palette.Skin(member.skin));
                    SetColour(memberClothes, i, palette.Clothes(member.clothes));
                }
            }

            var mode = GameModes.Find(room.modeId) ?? GameModes.Classic;
            modeLabel.text = mode.DisplayName;
            modeDescription.text = mode.Description;
            modePrevious.gameObject.SetActive(host);
            modeNext.gameObject.SetActive(host);

            readyButton.gameObject.SetActive(!host);
            startButton.gameObject.SetActive(host);
            startButton.interactable = Rooms.CanStart;
            if (readyButtonLabel != null)
            {
                var localized = readyButtonLabel.GetComponent<LocalizedText>();
                string key = me != null && me.ready ? "lobby.not_ready_action" : "lobby.ready_action";
                if (localized != null) localized.SetKey(key);
                else readyButtonLabel.text = Localization.Get(key);
            }
            waitingLabel.text = host
                ? (Rooms.CanStart ? string.Empty : Localization.Get("lobby.need_ready"))
                : Localization.Get("lobby.waiting_host");
        }

        static void SetColour(Image[] images, int index, Color colour)
        {
            if (index < images.Length && images[index] != null) images[index].color = colour;
        }
    }
}
