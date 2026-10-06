using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>The people playing together in the level that is being loaded or played. Empty in a solo game.</summary>
    public static class RoomSession
    {
        static RoomInfo active;

        public static RoomInfo Active => active;
        public static bool IsMultiplayer => active != null && active.members.Count > 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active = null;

        public static void Begin(RoomInfo room) => active = room;
        public static void End() => active = null;

        /// <summary>The other members of the room (everyone but the local player).</summary>
        public static List<RoomMember> Others()
        {
            var others = new List<RoomMember>();
            if (active == null) return others;
            foreach (var m in active.members)
                if (!m.isLocal) others.Add(m);
            return others;
        }
    }

    /// <summary>The room service the menus use. Offline for now; a networked service replaces it in phase 3.</summary>
    public static class RoomServices
    {
        static IRoomService service;

        public static IRoomService Current => service ?? (service = new LocalRoomService());

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => service = null;

        /// <summary>For tests: use another implementation.</summary>
        public static void Use(IRoomService other) => service = other;
    }
}
