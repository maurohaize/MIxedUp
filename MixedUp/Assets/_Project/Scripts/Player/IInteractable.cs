using UnityEngine;

namespace MixedUp
{
    public struct InteractionPrompt
    {
        public string Key;
        public object[] Args;
        public bool Enabled;

        public static InteractionPrompt Allowed(string key, params object[] args) =>
            new InteractionPrompt { Key = key, Args = args, Enabled = true };

        public static InteractionPrompt Blocked(string key, params object[] args) =>
            new InteractionPrompt { Key = key, Args = args, Enabled = false };
    }

    /// <summary>Anything a player can press the interact key on.</summary>
    public interface IInteractable
    {
        Transform InteractionTransform { get; }

        /// <summary>Returns true when a prompt should be shown; Enabled says whether E will do something.</summary>
        bool TryGetPrompt(PlayerInteractor who, out InteractionPrompt prompt);

        void Interact(PlayerInteractor who);
    }
}
