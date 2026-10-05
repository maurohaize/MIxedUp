using UnityEngine;

namespace MixedUp
{
    /// <summary>Keeps a flat object (sprite icon) facing the main camera.</summary>
    public class Billboard : MonoBehaviour
    {
        Camera cam;

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
