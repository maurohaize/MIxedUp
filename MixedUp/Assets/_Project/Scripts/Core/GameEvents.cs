using System;

namespace MixedUp
{
    /// <summary>Tiny global bus for fire-and-forget UI notifications.</summary>
    public static class GameEvents
    {
        public static event Action<string, object[]> Toast;

        public static void RaiseToast(string key, params object[] args) => Toast?.Invoke(key, args);

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Toast = null;
    }
}
