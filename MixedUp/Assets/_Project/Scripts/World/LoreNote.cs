using UnityEngine;

namespace MixedUp
{
    /// <summary>A note lying in the level. Reading it teaches the player one combination rule.</summary>
    public class LoreNote : MonoBehaviour, IInteractable
    {
        public CombinationRules rules;
        public BoxData a;
        public BoxData b;
        public Transform visual;
        public float bobHeight = 0.08f;

        Vector3 visualBase;
        float phase;

        public Transform InteractionTransform => transform;

        void Awake()
        {
            if (visual != null) visualBase = visual.localPosition;
            phase = Random.value * 6.28f;
        }

        void Update()
        {
            if (visual != null)
                visual.localPosition = visualBase + Vector3.up * (Mathf.Sin(Time.time * 2f + phase) * bobHeight);
        }

        public bool TryGetPrompt(PlayerInteractor who, out InteractionPrompt prompt)
        {
            prompt = InteractionPrompt.Allowed("prompt.read_note");
            return true;
        }

        public void Interact(PlayerInteractor who)
        {
            var rule = rules != null ? rules.Find(a, b) : null;
            if (rule == null) return;

            CombinationManual.Discover(rule);
            GameEvents.RaiseToast(rule.hintKey);
        }
    }
}
