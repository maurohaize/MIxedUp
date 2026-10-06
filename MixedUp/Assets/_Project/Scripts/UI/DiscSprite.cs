using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>Gives an Image the procedural round sprite (used for coins and outcome markers).</summary>
    [RequireComponent(typeof(Image))]
    public class DiscSprite : MonoBehaviour
    {
        void Awake() => GetComponent<Image>().sprite = ProceduralSprites.Disc;
    }
}
