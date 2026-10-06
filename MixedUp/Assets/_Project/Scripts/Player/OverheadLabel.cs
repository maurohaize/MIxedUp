using System.Text;
using TMPro;
using UnityEngine;

namespace MixedUp
{
    /// <summary>World-space name, health and carried boxes above a player, billboarded to the camera.</summary>
    public class OverheadLabel : MonoBehaviour
    {
        public TMP_Text text;
        public PlayerStatus status;
        public PlayerController controller;
        public string displayNameKey = "ui.teammate";
        [Tooltip("A name to show instead of the localized one (the name of a player in a room).")]
        public string literalName;

        readonly StringBuilder builder = new StringBuilder();
        float refreshTimer;
        Camera cam;

        void LateUpdate()
        {
            if (text == null || status == null) return;

            bool isMine = controller != null && controller.isLocal;
            text.gameObject.SetActive(!isMine);
            if (isMine) return;

            if (cam == null) cam = Camera.main;
            if (cam != null) text.transform.rotation = cam.transform.rotation;

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.15f;

            builder.Clear();
            builder.Append(!string.IsNullOrEmpty(literalName) ? literalName : Localization.Get(displayNameKey)).Append('\n');
            if (status.IsDead)
            {
                builder.Append("KO");
            }
            else
            {
                builder.Append(Mathf.CeilToInt(status.Health)).Append(" HP");
                var slots = status.Inventory.Slots;
                for (int i = 0; i < slots.Count; i++)
                    if (!slots[i].IsEmpty) builder.Append('\n').Append(slots[i].box.DisplayName);
            }

            string result = builder.ToString();
            if (text.text != result) text.text = result;
        }
    }
}
