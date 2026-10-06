using UnityEngine;

namespace MixedUp
{
    /// <summary>The marker between two neighbouring slots, plus the burning fuse under it.</summary>
    public class PuzzleJunctionView : MonoBehaviour
    {
        public OutcomeMarker marker;
        public GameObject fuseRoot;
        public RectTransform fuseFill;

        public void Show(CombinationOutcome outcome, bool known, bool fuseLit, float fuseRemaining01)
        {
            marker.Show(outcome, known);
            if (fuseRoot.activeSelf != fuseLit) fuseRoot.SetActive(fuseLit);
            if (fuseLit) fuseFill.anchorMax = new Vector2(fuseRemaining01, 1f);
        }
    }
}
