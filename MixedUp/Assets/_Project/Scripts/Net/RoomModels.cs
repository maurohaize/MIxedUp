using System;
using System.Collections.Generic;
using System.Text;

namespace MixedUp
{
    /// <summary>One person in a room: the colours of their character and whether they are ready to start.</summary>
    public sealed class RoomMember
    {
        public string id;
        public string name;
        public int skin = CharacterCustomization.DefaultSkin;
        public int clothes = CharacterCustomization.DefaultClothes;
        public bool ready;
        public bool isHost;
        public bool isLocal;
        /// <summary>A stand-in controlled by the computer (the offline room service uses them).</summary>
        public bool isBot;
    }

    public enum RoomState { Waiting, InGame, Closed }

    /// <summary>A room where up to four players gather before a game. The host chooses the game mode.</summary>
    public sealed class RoomInfo
    {
        public const int MaxPlayersLimit = 4;

        public string code;
        public int maxPlayers = MaxPlayersLimit;
        public string modeId = "classic";
        public RoomState state = RoomState.Waiting;
        public readonly List<RoomMember> members = new List<RoomMember>();

        public bool IsFull => members.Count >= maxPlayers;
        public RoomMember Host => members.Find(m => m.isHost);
        public RoomMember Local => members.Find(m => m.isLocal);

        public bool EveryoneReady
        {
            get
            {
                foreach (var m in members)
                    if (!m.isHost && !m.ready) return false;
                return members.Count > 0;
            }
        }
    }

    public enum JoinResult { Ok, NotFound, Full, AlreadyStarted, BadCode }

    /// <summary>Room codes: six characters that are easy to read out loud (no 0/O or 1/I/L).</summary>
    public static class RoomCode
    {
        public const int Length = 6;
        const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        public static string Generate(Random rng)
        {
            var text = new StringBuilder(Length);
            for (int i = 0; i < Length; i++) text.Append(Alphabet[rng.Next(Alphabet.Length)]);
            return text.ToString();
        }

        /// <summary>Upper-cases what the player typed and drops anything that cannot be part of a code. Null if it is not a valid code.</summary>
        public static string Normalize(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            var text = new StringBuilder(Length);
            foreach (char c in input.ToUpperInvariant())
            {
                if (Alphabet.IndexOf(c) >= 0) text.Append(c);
            }
            return text.Length == Length ? text.ToString() : null;
        }
    }
}
