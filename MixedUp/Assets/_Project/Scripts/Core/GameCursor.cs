using UnityEngine;

namespace MixedUp
{
    /// <summary>Gives the game its own hand-drawn mouse pointer. It is set once at start-up and shows wherever the cursor is visible.</summary>
    public static class GameCursor
    {
        const string ResourcePath = "Cursor/cursor";
        /// <summary>Where the tip of the arrow is inside the 64x64 picture.</summary>
        static readonly Vector2 Hotspot = new Vector2(3f, 3f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            var texture = Resources.Load<Texture2D>(ResourcePath);
            if (texture != null) Cursor.SetCursor(texture, Hotspot, CursorMode.Auto);
        }
    }
}
