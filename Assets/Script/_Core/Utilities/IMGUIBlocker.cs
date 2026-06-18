using UnityEngine;

namespace BOTF3D.Core
{
    /// <summary>
    /// Lets IMGUI windows block world-space camera input.
    /// Call Register() in OnGUI after GUI.Window(); call IsMouseOver() in Update() before
    /// processing mouse input. OnGUI runs after Update, so registration is always one frame
    /// behind — one frame of lag is imperceptible and correct for blocking purposes.
    /// </summary>
    public static class IMGUIBlocker
    {
        private static readonly Rect[] _rects = new Rect[8];
        private static int _count;
        private static int _lastFrame = -2;

        /// <summary>Register a visible IMGUI window rect. Call inside OnGUI after GUI.Window().</summary>
        public static void Register(Rect windowRect)
        {
            int f = Time.frameCount;
            if (f != _lastFrame) { _count = 0; _lastFrame = f; }
            if (_count < _rects.Length) _rects[_count++] = windowRect;
        }

        /// <summary>
        /// Returns true if the mouse cursor is over any registered IMGUI window.
        /// Safe to call from Update(). Treats data older than one frame as stale.
        /// </summary>
        public static bool IsMouseOver()
        {
            if (_count == 0 || Time.frameCount > _lastFrame + 1) return false;
            Vector2 p = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            for (int i = 0; i < _count; i++)
                if (_rects[i].Contains(p)) return true;
            return false;
        }
    }
}
