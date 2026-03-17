using UnityEngine;

namespace BOTF3D.Core
{
    public class KeyboardInputManagerGalactica : InputManagerGalactica
    {
        // EVENTS
        public static event MoveInputHandler OnMoveInput;
        public static event RotateInputHandler OnRotateInput;
        public static event ZoomInputHandler OnZoomInput;

        void Update()
        {
            // Move
            if (Input.GetKey(KeyCode.W))
            {
                OnMoveInput?.Invoke(Vector3.up); // is up on galactic map CameraFcousGalactica
            }
            if (Input.GetKey(KeyCode.S))
            {
                OnMoveInput?.Invoke(-Vector3.up); // is down
            }
            if (Input.GetKey(KeyCode.A)) // mainly x left right
            {
                OnMoveInput?.Invoke(-Vector3.right);
            }
            if (Input.GetKey(KeyCode.D))
            {
                OnMoveInput?.Invoke(Vector3.right);
            }
            // Rotate
            if (Input.GetKey(KeyCode.Q))
            {
                OnRotateInput?.Invoke(+1f);
            }
            if (Input.GetKey(KeyCode.E))
            {
                OnRotateInput?.Invoke(-1f);
            }
            // up down
            if (Input.GetKey(KeyCode.DownArrow)) // mainly y up down
            {
                OnMoveInput?.Invoke(-Vector3.forward);
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                OnMoveInput?.Invoke(Vector3.forward);
            }
            // Zoom
            if (Input.GetKey(KeyCode.Z))
            {
                OnZoomInput?.Invoke(-1f);
            }
            if (Input.GetKey(KeyCode.X))
            {
                OnZoomInput?.Invoke(1f);
            }
        }
    }
}
