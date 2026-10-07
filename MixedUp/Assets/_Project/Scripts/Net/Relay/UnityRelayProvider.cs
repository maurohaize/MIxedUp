using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Online rooms with a short code, through Unity Relay. This assembly only compiles when the Relay package is installed
    /// (see the version define in the .asmdef); the game registers it at start-up and the lobby then offers the code option.
    /// Needs the project linked to a Unity project (Project Settings > Services) with Relay enabled.
    /// </summary>
    public sealed class UnityRelayProvider : IRelayProvider
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => RelayProviders.Current = new UnityRelayProvider();

        static async Task SignIn()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        public async Task<string> CreateAsync(UnityTransport transport, int maxPlayers)
        {
            await SignIn();
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            transport.SetRelayServerData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);
            return joinCode;
        }

        public async Task JoinAsync(UnityTransport transport, string joinCode)
        {
            await SignIn();
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            transport.SetRelayServerData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.HostConnectionData);
        }
    }
}
