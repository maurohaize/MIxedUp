using System;
using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>Tracks live player controllers and which one belongs to this machine.</summary>
    public static class PlayerRegistry
    {
        static readonly List<PlayerController> players = new List<PlayerController>();

        public static IReadOnlyList<PlayerController> All => players;
        public static PlayerController Local { get; private set; }
        public static event Action<PlayerController> LocalChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            players.Clear();
            Local = null;
            LocalChanged = null;
        }

        public static void Register(PlayerController player)
        {
            if (!players.Contains(player)) players.Add(player);
            if (player.isLocal && Local != player)
            {
                Local = player;
                LocalChanged?.Invoke(player);
            }
        }

        public static void Unregister(PlayerController player)
        {
            players.Remove(player);
            if (Local == player)
            {
                Local = null;
                LocalChanged?.Invoke(null);
            }
        }
    }
}
