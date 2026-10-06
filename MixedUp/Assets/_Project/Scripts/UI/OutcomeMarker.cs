using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>A round marker (ink ring, coloured disc, symbol) showing a combination outcome or "?".</summary>
    public class OutcomeMarker : MonoBehaviour
    {
        public Image ring;
        public Image disc;
        public TMP_Text symbol;

        void Awake()
        {
            ring.sprite = ProceduralSprites.Disc;
            disc.sprite = ProceduralSprites.Disc;
        }

        public void Show(CombinationOutcome outcome, bool known)
        {
            disc.color = OutcomeStyle.Color(outcome, known);
            UiUtil.SetText(symbol, OutcomeStyle.Symbol(outcome, known));
        }
    }
}
