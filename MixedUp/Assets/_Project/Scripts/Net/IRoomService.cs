using System;

namespace MixedUp
{
    /// <summary>
    /// Everything a lobby needs from a multiplayer back end: make a room, join one with its code, ready up, pick the mode and
    /// start. The offline LocalRoomService implements it with stand-in players; a Relay/Lobby implementation (phase 3) will
    /// plug in behind the same interface, and the menus will not change.
    /// </summary>
    public interface IRoomService
    {
        RoomInfo Current { get; }

        /// <summary>Raised whenever the room changes (someone joined or left, ready flags, mode, state).</summary>
        event Action RoomChanged;
        /// <summary>The host started the game: load the level with the people in the room.</summary>
        event Action Started;
        /// <summary>The room was closed (the host left).</summary>
        event Action Closed;

        RoomInfo CreateRoom(string playerName, int maxPlayers, int skin, int clothes);
        JoinResult JoinRoom(string code, string playerName, int skin, int clothes);
        void Leave();
        void SetReady(bool ready);
        void SetMode(string modeId);
        bool CanStart { get; }
        bool StartGame();
    }
}
