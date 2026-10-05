using UnityEngine;

namespace MixedUp
{
    /// <summary>Configuration of one kind of box. Add a new box by creating an asset, not code.</summary>
    [CreateAssetMenu(fileName = "Box_", menuName = "MixedUp/Box")]
    public class BoxData : ScriptableObject
    {
        [Tooltip("Stable identifier used for saves, networking and combination rules.")]
        public string id;
        public string nameKey;
        public string descriptionKey;
        public Color color = Color.white;
        public Sprite icon;
        public BoxEffect[] effects = System.Array.Empty<BoxEffect>();

        public string DisplayName => Localization.Get(nameKey);
        public string Description => Localization.Get(descriptionKey);

        public bool HasEffect<T>() where T : BoxEffect
        {
            if (effects == null) return false;
            foreach (var effect in effects)
                if (effect is T) return true;
            return false;
        }

        public float MaxSeverity(PlayerStatus status, float heldTime)
        {
            float max = 0f;
            if (effects == null) return max;
            var ctx = new EffectContext(status, heldTime, 0f, status.Hazards);
            foreach (var effect in effects)
                if (effect != null) max = Mathf.Max(max, effect.Severity01(ctx));
            return max;
        }
    }
}
