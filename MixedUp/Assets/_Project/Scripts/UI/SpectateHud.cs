using TMPro;
using UnityEngine;

namespace MixedUp
{
    /// <summary>"Watching Ane - E: next player" shown while the local player is dead and follows a teammate.</summary>
    public class SpectateHud : MonoBehaviour
    {
        public TMP_Text label;
        public GameObject container;

        string lastName;
        Language lastLanguage;

        void Update()
        {
            string name = ThirdPersonCamera.SpectatedName;
            bool show = name != null && GameManager.Instance != null && GameManager.Instance.IsSpectating;
            if (container.activeSelf != show) container.SetActive(show);
            if (!show) { lastName = null; return; }
            if (name == lastName && Localization.Current == lastLanguage) return;
            lastName = name;
            lastLanguage = Localization.Current;
            label.text = Localization.Get("spectate.watching", name, GameInput.Label(GameInput.Interact));
        }
    }
}
