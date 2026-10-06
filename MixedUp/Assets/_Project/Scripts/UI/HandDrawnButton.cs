using UnityEngine;
using UnityEngine.EventSystems;

namespace MixedUp
{
    /// <summary>
    /// Gives a button the lively feel of a hand-made sign: a slight resting tilt, and it tips and grows when hovered or
    /// selected with the keyboard / gamepad, and squashes when pressed. Uses unscaled time so it works while paused.
    /// </summary>
    public class HandDrawnButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        public float restTilt;
        public float hoverTilt = -1.6f;
        public float hoverScale = 1.06f;
        public float pressedScale = 0.96f;
        public float speed = 16f;

        bool hovered, selected, pressed;
        float tilt, scale = 1f;

        void OnEnable()
        {
            hovered = selected = pressed = false;
            tilt = restTilt;
            scale = 1f;
            Apply();
        }

        void Update()
        {
            bool active = hovered || selected;
            float targetTilt = active ? hoverTilt : restTilt;
            float targetScale = pressed ? pressedScale : active ? hoverScale : 1f;

            float k = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
            tilt = Mathf.Lerp(tilt, targetTilt, k);
            scale = Mathf.Lerp(scale, targetScale, k);
            Apply();
        }

        void Apply()
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;
        public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }
        public void OnSelect(BaseEventData eventData) => selected = true;
        public void OnDeselect(BaseEventData eventData) => selected = false;
        public void OnPointerDown(PointerEventData eventData) => pressed = true;
        public void OnPointerUp(PointerEventData eventData) => pressed = false;
    }
}
