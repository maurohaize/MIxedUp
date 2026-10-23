using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MixedUp
{
    public enum ConnectionKind { Relay, Direct }

    /// <summary>
    /// The real multiplayer connection, built on Netcode for GameObjects. A player hosts either through the internet relay
    /// (a short room code, needs the Unity Gaming Services packages) or directly (IP address and port, works on a LAN or with
    /// an open port), and the others join with the code or the address. Everyone then meets in the 3D lobby scene.
    /// The NetworkManager is created from code the first time it is needed and lives for the whole run of the game.
    /// </summary>
    public static class OnlineSession
    {
        public const ushort DefaultPort = 7777;
        public const int MaxPlayers = RoomInfo.MaxPlayersLimit;
        public const string LobbyScene = "Lobby";
        public const string AvatarResource = "Net/NetAvatar";
        const string KindPref = "net.kind";
        const float ConnectTimeout = 12f;

        static NetworkManager manager;
        static OnlineRunner runner;
        static bool busy;
        static bool connectFailed;
        static string pendingNotice;

        /// <summary>Turned off by tests so the lobby panel keeps using the offline rooms.</summary>
        public static bool Enabled = true;

        public static ConnectionKind Kind { get; private set; }
        /// <summary>What a friend needs to join: the room code or "address:port".</summary>
        public static string JoinCode { get; private set; }
        public static bool IsBusy => busy;
        public static bool IsOnline => manager != null && manager.IsListening;
        public static bool IsHost => IsOnline && manager.IsHost;
        public static bool IsClient => IsOnline && !manager.IsServer;
        public static NetworkManager Manager => manager;

        /// <summary>Raised with a localization key when hosting or joining did not work.</summary>
        public static event Action<string> Failed;
        /// <summary>Raised once the host is running, or the client has connected.</summary>
        public static event Action Connected;
        /// <summary>Raised when the connection ends, whoever ended it.</summary>
        public static event Action Disconnected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            manager = null;
            runner = null;
            busy = false;
            connectFailed = false;
            pendingNotice = null;
            Enabled = true;
            JoinCode = null;
            Failed = null;
            Connected = null;
            Disconnected = null;
        }

        /// <summary>The connection kind the player last chose (saved).</summary>
        public static ConnectionKind PreferredKind
        {
            get => (ConnectionKind)PlayerPrefs.GetInt(KindPref, RelayProviders.Available ? 0 : 1);
            set => PlayerPrefs.SetInt(KindPref, (int)value);
        }

        /// <summary>A message left for the main menu (for example "the host closed the room"). Returns it once.</summary>
        public static string TakeNotice()
        {
            string notice = pendingNotice;
            pendingNotice = null;
            return notice;
        }

        sealed class OnlineRunner : MonoBehaviour { }

        static OnlineRunner Runner
        {
            get
            {
                if (runner == null)
                {
                    var go = new GameObject("OnlineRunner");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    runner = go.AddComponent<OnlineRunner>();
                }
                return runner;
            }
        }

        // ------------------------------------------------------------- manager

        static NetworkManager EnsureManager()
        {
            if (manager != null) return manager;

            var avatar = Resources.Load<GameObject>(AvatarResource);
            if (avatar == null || avatar.GetComponent<NetworkObject>() == null) return null;

            manager = NetworkManager.Singleton;
            if (manager == null)
            {
                // Built inactive so the NetworkManager wakes up with its configuration already in place.
                var go = new GameObject("OnlineNetwork");
                go.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(go);
                var transport = go.AddComponent<UnityTransport>();
                var created = go.AddComponent<NetworkManager>();
                created.NetworkConfig = new NetworkConfig
                {
                    NetworkTransport = transport,
                    PlayerPrefab = avatar,
                    ConnectionApproval = true,
                    EnableSceneManagement = true,
                    TickRate = 30
                };
                created.AddNetworkPrefab(avatar);
                created.ConnectionApprovalCallback = Approve;
                go.SetActive(true);
                manager = created;
            }

            manager.OnClientDisconnectCallback += OnClientDisconnected;
            return manager;
        }

        static void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Pending = false;
            response.CreatePlayerObject = true;

            bool isHostItself = request.ClientNetworkId == NetworkManager.ServerClientId;
            if (!isHostItself && OnlineMatch.Started)
            {
                response.Approved = false;
                response.Reason = "started";
                return;
            }
            if (!isHostItself && manager.ConnectedClientsIds.Count >= MaxPlayers)
            {
                response.Approved = false;
                response.Reason = "full";
                return;
            }
            response.Approved = true;
        }

        static UnityTransport Transport => manager.GetComponent<UnityTransport>();

        // ---------------------------------------------------------------- host

        /// <summary>Starts a room: relay (with a short code) or direct (address and port). On success the lobby scene loads.</summary>
        public static void Host(ConnectionKind kind, ushort port = DefaultPort)
        {
            if (busy || IsOnline) return;
            Runner.StartCoroutine(HostRoutine(kind, port));
        }

        static IEnumerator HostRoutine(ConnectionKind kind, ushort port)
        {
            busy = true;
            var nm = EnsureManager();
            if (nm == null) { Fail("lobby.err.avatar"); yield break; }

            string code;
            if (kind == ConnectionKind.Relay)
            {
                if (!RelayProviders.Available) { Fail("lobby.err.relay"); yield break; }
                var task = RelayProviders.Current.CreateAsync(Transport, MaxPlayers);
                while (!task.IsCompleted) yield return null;
                if (task.IsFaulted || task.IsCanceled || string.IsNullOrEmpty(task.Result))
                {
                    if (task.Exception != null) Debug.LogException(task.Exception);
                    Fail("lobby.err.relay_failed");
                    yield break;
                }
                code = task.Result;
            }
            else
            {
                Transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
                code = LocalAddress() + ":" + port;
            }

            if (!nm.StartHost()) { Fail("lobby.err.host"); yield break; }

            Kind = kind;
            JoinCode = code;
            busy = false;
            nm.SceneManager.LoadScene(LobbyScene, LoadSceneMode.Single);
            Connected?.Invoke();
        }

        // ---------------------------------------------------------------- join

        /// <summary>What kind of address this is: "address[:port]" goes direct, six letters and digits is a relay code.</summary>
        public static bool TryParse(string text, out ConnectionKind kind, out string address, out ushort port)
        {
            kind = ConnectionKind.Direct;
            address = null;
            port = DefaultPort;
            text = (text ?? string.Empty).Trim();
            if (text.Length == 0) return false;

            if (text.IndexOf('.') < 0 && text.IndexOf(':') < 0)
            {
                string code = text.ToUpperInvariant();
                if (code.Length != RoomCode.Length) return false;
                foreach (char c in code)
                    if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))) return false;
                kind = ConnectionKind.Relay;
                address = code;
                return true;
            }

            string host = text;
            int colon = text.LastIndexOf(':');
            if (colon >= 0)
            {
                host = text.Substring(0, colon);
                if (!ushort.TryParse(text.Substring(colon + 1), out port)) return false;
            }
            if (host.Length == 0 || host.IndexOf(' ') >= 0) return false;
            address = host;
            return true;
        }

        /// <summary>Joins a room with its code or "address:port". Returns false when the text is not a code or an address.</summary>
        public static bool Join(string text)
        {
            if (busy || IsOnline) return false;
            if (!TryParse(text, out var kind, out var address, out var port)) return false;
            Runner.StartCoroutine(JoinRoutine(kind, address, port));
            return true;
        }

        static IEnumerator JoinRoutine(ConnectionKind kind, string address, ushort port)
        {
            busy = true;
            connectFailed = false;
            var nm = EnsureManager();
            if (nm == null) { Fail("lobby.err.avatar"); yield break; }

            if (kind == ConnectionKind.Relay)
            {
                if (!RelayProviders.Available) { Fail("lobby.err.relay"); yield break; }
                var task = RelayProviders.Current.JoinAsync(Transport, address);
                while (!task.IsCompleted) yield return null;
                if (task.IsFaulted || task.IsCanceled)
                {
                    if (task.Exception != null) Debug.LogException(task.Exception);
                    Fail("lobby.err.notfound");
                    yield break;
                }
            }
            else
            {
                Transport.SetConnectionData(address, port);
            }

            if (!nm.StartClient()) { Fail("lobby.err.connect"); yield break; }

            float waited = 0f;
            while (!nm.IsConnectedClient && !connectFailed && waited < ConnectTimeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!nm.IsConnectedClient)
            {
                string reason = nm.DisconnectReason;
                nm.Shutdown();
                Fail(reason == "full" ? "lobby.err.full" : reason == "started" ? "lobby.err.started" : "lobby.err.connect");
                yield break;
            }

            Kind = kind;
            JoinCode = kind == ConnectionKind.Relay ? address : address + ":" + port;
            busy = false;
            Connected?.Invoke();
        }

        // --------------------------------------------------------------- leave

        /// <summary>Ends the connection (a host closes the room for everybody). Safe to call when offline.</summary>
        public static void Leave()
        {
            if (manager != null && manager.IsListening) manager.Shutdown();
            busy = false;
            JoinCode = null;
            Disconnected?.Invoke();
        }

        static void OnClientDisconnected(ulong clientId)
        {
            if (manager == null) return;
            if (busy) { connectFailed = true; return; }
            if (manager.IsServer) return;
            if (clientId != manager.LocalClientId && manager.IsConnectedClient) return;

            // The host closed the room or the connection dropped: back to the main menu.
            pendingNotice = "lobby.closed";
            string reason = manager.DisconnectReason;
            if (reason == "full") pendingNotice = "lobby.err.full";
            Leave();
            GoToMenu();
        }

        static void GoToMenu()
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded(GameManager.MainMenuScene)) SceneManager.LoadScene(GameManager.MainMenuScene);
        }

        static void Fail(string key)
        {
            busy = false;
            Failed?.Invoke(key);
        }

        // ------------------------------------------------------- scene changes

        /// <summary>The host moves everybody to another scene (the chosen map, or back to the lobby) after a short pause that lets the match settings reach everyone.</summary>
        public static void LoadForAll(string scene, float delay = 0.35f)
        {
            if (!IsHost) return;
            Runner.StartCoroutine(LoadRoutine(scene, delay));
        }

        static IEnumerator LoadRoutine(string scene, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (IsHost) manager.SceneManager.LoadScene(scene, LoadSceneMode.Single);
        }

        /// <summary>Host only: starts the level again for everybody with a new random seed.</summary>
        public static void RestartMatch()
        {
            if (!IsHost) return;
            NetAvatar.Host?.BeginMatch(UnityEngine.Random.Range(1, int.MaxValue));
            LoadForAll(SceneManager.GetActiveScene().name);
        }

        /// <summary>Host only: everybody goes back to the lobby to choose again.</summary>
        public static void ReturnToLobby()
        {
            if (!IsHost) return;
            NetAvatar.Host?.EndMatch();
            LoadForAll(LobbyScene, 0.1f);
        }

        // ------------------------------------------------------------- helpers

        /// <summary>The address of this machine on the local network, to give to friends on the same network.</summary>
        public static string LocalAddress()
        {
            try
            {
                foreach (var address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address)) continue;
                    return address.ToString();
                }
            }
            catch (Exception)
            {
                // No network information: fall through to the loopback address.
            }
            return "127.0.0.1";
        }
    }
}
