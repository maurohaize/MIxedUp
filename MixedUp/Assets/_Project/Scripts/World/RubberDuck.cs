using UnityEngine;

namespace MixedUp
{
    /// <summary>A rubber duck bobbing in the river. Squeeze it (E) and it quacks, hops and floats away a little.</summary>
    public class RubberDuck : MonoBehaviour, IInteractable
    {
        public Transform visual;
        public float bobHeight = 0.04f;

        float hop;
        float phase;
        Vector3 basePosition;

        public int Squeezes { get; private set; }
        public static event System.Action<RubberDuck> Squeaked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Squeaked = null;

        public Transform InteractionTransform => transform;

        void Awake()
        {
            if (visual == null) visual = transform;
            basePosition = visual.localPosition;
            phase = Random.value * 6.28f;
        }

        void Update()
        {
            hop = Mathf.MoveTowards(hop, 0f, Time.deltaTime * 3f);
            float bob = Mathf.Sin(Time.time * 1.7f + phase) * bobHeight;
            visual.localPosition = basePosition + Vector3.up * (bob + Mathf.Sin(hop * Mathf.PI) * 0.45f);
            visual.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.3f + phase) * 5f, visual.localEulerAngles.y + hop * 540f * Time.deltaTime, 0f);
        }

        public bool TryGetPrompt(PlayerInteractor who, out InteractionPrompt prompt)
        {
            prompt = InteractionPrompt.Allowed("prompt.squeeze");
            return true;
        }

        public void Interact(PlayerInteractor who)
        {
            Squeezes++;
            hop = 1f;
            GameEvents.RaiseToast("duck.squeak");
            Squeaked?.Invoke(this);
        }
    }
}
