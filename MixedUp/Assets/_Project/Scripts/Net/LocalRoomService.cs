using System;
using System.Collections.Generic;

namespace MixedUp
{
    /// <summary>
    /// The offline room service: rooms live in memory and the other players are computer-controlled stand-ins that join and
    /// get ready by themselves. It lets the whole lobby (and the game with several characters) be built and tested without
    /// a network. The code AMETSA always opens a demo room hosted by a stand-in friend.
    /// </summary>
    public sealed class LocalRoomService : IRoomService
    {
        public const string DemoCode = "AMETSA";
        static readonly string[] BotNames = { "Ane", "Iker", "Maite", "Unai", "Nerea" };

        readonly Dictionary<string, RoomInfo> rooms = new Dictionary<string, RoomInfo>();
        readonly Random rng = new Random();
        RoomInfo current;
        float botTimer;
        float startTimer = -1f;

        public RoomInfo Current => current;
        /// <summary>How many stand-in players join a room the local player creates.</summary>
        public int BotsToAdd = 1;
        /// <summary>Seconds between a stand-in joining and getting ready, and between joins.</summary>
        public float BotDelay = 1.2f;

        public event Action RoomChanged;
        public event Action Started;
        public event Action Closed;

        public LocalRoomService()
        {
            SeedDemoRoom();
        }

        void SeedDemoRoom()
        {
            var room = new RoomInfo { code = DemoCode, maxPlayers = 4, modeId = "classic" };
            room.members.Add(new RoomMember { id = "bot-ane", name = BotNames[0], isHost = true, isBot = true, ready = true, skin = 2, clothes = 8 });
            room.members.Add(new RoomMember { id = "bot-iker", name = BotNames[1], isBot = true, ready = true, skin = 0, clothes = 3 });
            rooms[room.code] = room;
        }

        string NewCode()
        {
            string code;
            do code = RoomCode.Generate(rng);
            while (rooms.ContainsKey(code));
            return code;
        }

        // ------------------------------------------------------------------- rooms

        public RoomInfo CreateRoom(string playerName, int maxPlayers, int skin, int clothes)
        {
            Leave();
            var room = new RoomInfo { code = NewCode(), maxPlayers = Math.Max(1, Math.Min(RoomInfo.MaxPlayersLimit, maxPlayers)) };
            room.members.Add(new RoomMember { id = "local", name = PlayerProfile.Sanitize(playerName, true), isHost = true, isLocal = true, skin = skin, clothes = clothes });
            rooms[room.code] = room;
            current = room;
            botTimer = BotDelay;
            Changed();
            return room;
        }

        public JoinResult JoinRoom(string code, string playerName, int skin, int clothes)
        {
            string normalized = RoomCode.Normalize(code);
            if (normalized == null) return JoinResult.BadCode;
            if (!rooms.TryGetValue(normalized, out var room)) return JoinResult.NotFound;
            if (room.state != RoomState.Waiting) return JoinResult.AlreadyStarted;
            if (room.IsFull) return JoinResult.Full;

            Leave();
            room.members.Add(new RoomMember { id = "local", name = PlayerProfile.Sanitize(playerName, true), isLocal = true, skin = skin, clothes = clothes });
            current = room;
            botTimer = BotDelay;
            Changed();
            return JoinResult.Ok;
        }

        public void Leave()
        {
            if (current == null) return;
            var room = current;
            current = null;
            startTimer = -1f;

            var me = room.Local;
            if (me != null && me.isHost)
            {
                room.state = RoomState.Closed;
                rooms.Remove(room.code);
                Closed?.Invoke();
            }
            else if (me != null)
            {
                room.members.Remove(me);
                if (room.state == RoomState.InGame) room.state = RoomState.Waiting;
            }
            RoomChanged?.Invoke();
        }

        public void SetReady(bool ready)
        {
            var me = current?.Local;
            if (me == null || me.isHost) return;
            me.ready = ready;
            Changed();
        }

        public void SetMode(string modeId)
        {
            var me = current?.Local;
            if (me == null || !me.isHost || GameModes.Find(modeId) == null) return;
            current.modeId = modeId;
            Changed();
        }

        public bool CanStart => current != null && current.Local != null && current.Local.isHost && current.state == RoomState.Waiting && current.EveryoneReady;

        public bool StartGame()
        {
            if (!CanStart) return false;
            Begin();
            return true;
        }

        void Begin()
        {
            current.state = RoomState.InGame;
            RoomSession.Begin(current);
            Changed();
            Started?.Invoke();
        }

        void Changed() => RoomChanged?.Invoke();

        // -------------------------------------------------------------- stand-ins

        /// <summary>Advances the stand-ins: they join one by one, then get ready; a stand-in host starts the game when all are ready.</summary>
        public void Tick(float deltaTime)
        {
            if (current == null || current.state != RoomState.Waiting) return;
            var me = current.Local;
            if (me == null) return;

            if (me.isHost)
            {
                if (BotsToAdd > 0 && !current.IsFull && CountBots() < BotsToAdd)
                {
                    botTimer -= deltaTime;
                    if (botTimer <= 0f)
                    {
                        botTimer = BotDelay;
                        int n = CountBots();
                        current.members.Add(new RoomMember { id = "bot-" + n, name = BotNames[(n + 1) % BotNames.Length], isBot = true,
                            skin = (n * 2 + 1) % 5, clothes = (n * 3 + 4) % 10 });
                        Changed();
                    }
                }
                else
                {
                    ReadyUpBots(deltaTime);
                }
            }
            else
            {
                // A stand-in host: once the local player is ready, the host starts after a moment.
                if (me.ready)
                {
                    if (startTimer < 0f) startTimer = BotDelay * 1.5f;
                    startTimer -= deltaTime;
                    if (startTimer <= 0f && current.EveryoneReady) Begin();
                }
                else
                {
                    startTimer = -1f;
                }
            }
        }

        int CountBots()
        {
            int n = 0;
            foreach (var m in current.members)
                if (m.isBot) n++;
            return n;
        }

        void ReadyUpBots(float dt)
        {
            botTimer -= dt;
            if (botTimer > 0f) return;
            botTimer = BotDelay;
            foreach (var m in current.members)
            {
                if (!m.isBot || m.ready) continue;
                m.ready = true;
                Changed();
                return;
            }
        }
    }
}
