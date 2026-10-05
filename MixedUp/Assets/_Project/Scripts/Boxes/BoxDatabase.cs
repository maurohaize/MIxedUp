using UnityEngine;

namespace MixedUp
{
    /// <summary>Registry of every box type. The index is stable and cheap to send over the network.</summary>
    [CreateAssetMenu(fileName = "BoxDatabase", menuName = "MixedUp/Box Database")]
    public class BoxDatabase : ScriptableObject
    {
        public BoxData[] boxes = System.Array.Empty<BoxData>();

        public BoxData GetById(string id)
        {
            foreach (var box in boxes)
                if (box != null && box.id == id) return box;
            return null;
        }

        public BoxData GetByIndex(int index) =>
            index >= 0 && index < boxes.Length ? boxes[index] : null;

        public int IndexOf(BoxData data) => System.Array.IndexOf(boxes, data);
    }
}
