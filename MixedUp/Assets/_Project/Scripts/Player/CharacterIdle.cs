using UnityEngine;

namespace MixedUp
{
    /// <summary>A relaxed idle for a character shown in a menu: a little bounce and floating hands. Uses unscaled time.</summary>
    public class CharacterIdle : MonoBehaviour
    {
        public Transform body, handLeft, handRight;
        public float bounce = 0.025f;
        public float handFloat = 0.035f;
        public float phaseOffset;

        Vector3 leftRest, rightRest;
        bool captured;

        void Capture()
        {
            if (captured) return;
            captured = true;
            if (handLeft != null) leftRest = handLeft.localPosition;
            if (handRight != null) rightRest = handRight.localPosition;
        }

        void Update()
        {
            Capture();
            float t = Time.unscaledTime + phaseOffset;

            if (body != null) body.localPosition = new Vector3(0f, (Mathf.Sin(t * 3.1f) * 0.5f + 0.5f) * bounce, 0f);
            if (handLeft != null) handLeft.localPosition = leftRest + new Vector3(0f, Mathf.Sin(t * 2.3f) * handFloat, Mathf.Sin(t * 1.7f) * 0.03f);
            if (handRight != null) handRight.localPosition = rightRest + new Vector3(0f, Mathf.Sin(t * 2.3f + 1.6f) * handFloat, Mathf.Sin(t * 1.7f + 1.2f) * 0.03f);
        }
    }
}
