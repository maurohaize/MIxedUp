using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;

namespace MixedUp
{
    /// <summary>
    /// What the game needs from an internet relay (Unity Relay): host a game and get a short join code, or join with one.
    /// The implementation lives in its own assembly (Scripts/Net/Relay) that only exists once the Unity Gaming Services
    /// packages are installed, so the rest of the game compiles and plays without them.
    /// </summary>
    public interface IRelayProvider
    {
        /// <summary>Creates a relay allocation for `maxPlayers`, points the transport at it and returns the join code.</summary>
        Task<string> CreateAsync(UnityTransport transport, int maxPlayers);
        /// <summary>Points the transport at the host that published `joinCode`.</summary>
        Task JoinAsync(UnityTransport transport, string joinCode);
    }

    public static class RelayProviders
    {
        public static IRelayProvider Current { get; set; }
        public static bool Available => Current != null;
    }
}
