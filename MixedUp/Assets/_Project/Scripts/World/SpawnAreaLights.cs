using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// The lights and flags of an out-of-the-way spot (tunnel, tower...). They are only there when the game mode actually puts a
    /// box in that spot: a tunnel nobody has to visit stays dark and unmarked.
    /// </summary>
    public class SpawnAreaLights : MonoBehaviour
    {
        public BoxSpawnPoint[] points = System.Array.Empty<BoxSpawnPoint>();
        public GameObject dressing;

        public bool Wanted
        {
            get
            {
                foreach (var point in points)
                    if (point != null && point.InUse) return true;
                return false;
            }
        }

        public void Refresh()
        {
            if (dressing != null) dressing.SetActive(Wanted);
        }
    }
}
