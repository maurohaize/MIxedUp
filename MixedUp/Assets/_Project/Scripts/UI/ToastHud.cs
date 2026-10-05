using TMPro;
using UnityEngine;

namespace MixedUp
{
    /// <summary>Short fading messages such as "BOX DELIVERED".</summary>
    public class ToastHud : MonoBehaviour
    {
        public TMP_Text label;
        public CanvasGroup group;
        public float duration = 2.2f;
        public float fadeTime = 0.5f;

        float timer;

        void OnEnable()
        {
            GameEvents.Toast += Show;
            group.alpha = 0f;
        }

        void OnDisable() => GameEvents.Toast -= Show;

        void Show(string key, object[] args)
        {
            label.text = Localization.Get(key, args);
            timer = duration;
        }

        void Update()
        {
            if (timer <= 0f) return;
            timer -= Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(timer / fadeTime);
        }
    }
}
