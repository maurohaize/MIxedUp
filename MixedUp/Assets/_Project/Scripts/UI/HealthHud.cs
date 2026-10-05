using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    public class HealthHud : MonoBehaviour
    {
        public RectTransform fill;
        public TMP_Text valueLabel;
        public Image damageFlash;

        PlayerStatus status;
        float flash;

        public void Bind(PlayerStatus playerStatus)
        {
            if (status != null) status.Damaged -= OnDamaged;
            status = playerStatus;
            if (status != null) status.Damaged += OnDamaged;
        }

        void OnDestroy()
        {
            if (status != null) status.Damaged -= OnDamaged;
        }

        void OnDamaged(float amount, DeathCause cause) => flash = Mathf.Min(0.4f, flash + amount * 0.02f);

        void Update()
        {
            if (status == null) return;

            fill.anchorMax = new Vector2(Mathf.Clamp01(status.Health01), 1f);
            UiUtil.SetText(valueLabel, Mathf.CeilToInt(status.Health).ToString());

            flash = Mathf.MoveTowards(flash, 0f, Time.unscaledDeltaTime * 1.5f);
            if (damageFlash != null)
            {
                var c = damageFlash.color;
                c.a = flash;
                damageFlash.color = c;
            }
        }
    }
}
