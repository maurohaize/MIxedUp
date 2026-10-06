using TMPro;
using UnityEngine;

namespace MixedUp
{
    /// <summary>"E - PASS BOX" style hint for whatever the local player can currently interact with.</summary>
    public class PromptHud : MonoBehaviour
    {
        public TMP_Text label;
        [Tooltip("Object toggled with the prompt; falls back to the label itself.")]
        public GameObject container;
        public Color enabledColor = new Color(0.18f, 0.13f, 0.1f);
        public Color blockedColor = new Color(0.55f, 0.45f, 0.4f);

        PlayerInteractor interactor;
        string lastKey;
        string lastArg;
        bool lastEnabled;
        Language lastLanguage;
        string lastBinding;
        bool lastCanTake;

        public void Bind(PlayerInteractor playerInteractor) => interactor = playerInteractor;

        void Update()
        {
            var target = container != null ? container : label.gameObject;

            if (interactor == null || (interactor.Current == null && interactor.TakeTarget == null))
            {
                if (target.activeSelf) target.SetActive(false);
                lastKey = null;
                return;
            }

            var prompt = interactor.Current != null ? interactor.CurrentPrompt : default;
            bool canTake = interactor.TakeTarget != null;
            string arg = prompt.Args != null && prompt.Args.Length > 0 ? prompt.Args[0] as string : null;
            string binding = GameInput.Label(GameInput.Interact);

            bool unchanged = prompt.Key == lastKey && arg == lastArg && prompt.Enabled == lastEnabled
                             && Localization.Current == lastLanguage && binding == lastBinding && canTake == lastCanTake;
            if (!unchanged)
            {
                lastKey = prompt.Key;
                lastArg = arg;
                lastEnabled = prompt.Enabled;
                lastLanguage = Localization.Current;
                lastBinding = binding;
                lastCanTake = canTake;

                string text = prompt.Key != null ? Localization.Get(prompt.Key, prompt.Args) : string.Empty;
                text = prompt.Enabled ? binding + " - " + text : text;
                if (canTake)
                {
                    string take = GameInput.Label(GameInput.Take) + " - " + Localization.Get("prompt.take_all");
                    text = text.Length > 0 ? text + "     " + take : take;
                }
                label.text = text;
                label.color = prompt.Enabled || prompt.Key == null ? enabledColor : blockedColor;
            }

            if (!target.activeSelf) target.SetActive(true);
        }
    }
}
