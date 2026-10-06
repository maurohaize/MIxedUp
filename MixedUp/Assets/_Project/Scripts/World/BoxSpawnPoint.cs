using UnityEngine;

namespace MixedUp
{
    /// <summary>A place where a box can be put when the level starts. The LevelDirector decides which ones are used.</summary>
    public class BoxSpawnPoint : MonoBehaviour
    {
        [Tooltip("In the classic puzzle this spot always holds this kind of box (empty = any).")]
        public string classicBoxId;
        [Tooltip("A hard-to-reach spot (tower, tunnel, ledge...). Used by the challenge mode and never by the easy modes.")]
        public bool hard;
        [Tooltip("Spots of the original map: always allowed in the random modes too.")]
        public bool original;

        public Vector3 Position => transform.position;
        /// <summary>True when the current game mode put a box here.</summary>
        public bool InUse { get; set; }

        void OnDrawGizmos()
        {
            Gizmos.color = hard ? new Color(0.9f, 0.3f, 0.2f) : new Color(0.3f, 0.8f, 0.4f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.55f, Vector3.one * 1.1f);
        }
    }
}
