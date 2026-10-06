using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>
    /// The transition between the 3D world and the 2D truck: a sheet of paper sweeps across the screen,
    /// the scene is swapped behind it, and the sheet peels away. Runs on unscaled time.
    /// </summary>
    public class PaperWipe : MonoBehaviour
    {
        public RectTransform sheet;
        public Image sheetImage;
        public float coverTime = 0.4f;
        public float holdTime = 0.15f;
        public float revealTime = 0.35f;

        public bool IsPlaying { get; private set; }

        /// <summary>Covers the screen, calls onCovered, then reveals what is underneath.</summary>
        public IEnumerator Play(Action onCovered)
        {
            IsPlaying = true;
            sheet.gameObject.SetActive(true);
            SetAlpha(1f);

            for (float t = 0f; t < coverTime; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / coverTime);
                sheet.localScale = new Vector3(k, 1f, 1f);
                yield return null;
            }
            sheet.localScale = Vector3.one;

            onCovered?.Invoke();
            yield return new WaitForSecondsRealtime(holdTime);

            for (float t = 0f; t < revealTime; t += Time.unscaledDeltaTime)
            {
                SetAlpha(1f - Mathf.SmoothStep(0f, 1f, t / revealTime));
                yield return null;
            }

            sheet.gameObject.SetActive(false);
            IsPlaying = false;
        }

        void SetAlpha(float alpha)
        {
            var c = sheetImage.color;
            c.a = alpha;
            sheetImage.color = c;
        }
    }
}
