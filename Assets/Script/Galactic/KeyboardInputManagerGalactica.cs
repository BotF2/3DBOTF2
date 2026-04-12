using UnityEngine;
using UnityEngine.InputSystem;

namespace BOTF3D.Core
{
    /// <summary>
    /// Galaxy keyboard input manager using new Input System
    /// Listens to Input Actions and fires events for camera movement
    /// </summary>
    public class KeyboardInputManagerGalactica : InputManagerGalactica
    {
        // ✅ Events (same as before)
        public static event MoveInputHandler OnMoveInput;
        public static event RotateInputHandler OnRotateInput;
        public static event ZoomInputHandler OnZoomInput;

        // ✅ Input Actions reference
        private GalaxyControls galaxyControls;
        private GalaxyControls.GalaxyActions galaxyActions;

        private void Awake()
        {
            // Create the input actions wrapper
            galaxyControls = new GalaxyControls();
            galaxyActions = galaxyControls.Galaxy;

            // Subscribe to input events
            galaxyActions.Move.performed += OnMovePerformed;
            galaxyActions.Move.canceled += OnMoveCanceled;

            galaxyActions.UpDown.performed += OnUpDownPerformed;
            galaxyActions.UpDown.canceled += OnUpDownCanceled;

            galaxyActions.Rotate.performed += OnRotatePerformed;
            galaxyActions.Rotate.canceled += OnRotateCanceled;

            galaxyActions.Zoom.performed += OnZoomPerformed;
            galaxyActions.Zoom.canceled += OnZoomCanceled;

            Debug.Log("✅ KeyboardInputManagerGalactica: Input Actions initialized");
        }

        private void OnEnable()
        {
            // Enable the input actions when this component is enabled
            galaxyActions.Enable();
            Debug.Log("✅ Galaxy controls enabled");
        }

        private void OnDisable()
        {
            // Disable the input actions when this component is disabled
            galaxyActions.Disable();
            Debug.Log("⏸️ Galaxy controls disabled");
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (galaxyControls != null)
            {
                galaxyActions.Move.performed -= OnMovePerformed;
                galaxyActions.Move.canceled -= OnMoveCanceled;

                galaxyActions.UpDown.performed -= OnUpDownPerformed;
                galaxyActions.UpDown.canceled -= OnUpDownCanceled;

                galaxyActions.Rotate.performed -= OnRotatePerformed;
                galaxyActions.Rotate.canceled -= OnRotateCanceled;

                galaxyActions.Zoom.performed -= OnZoomPerformed;
                galaxyActions.Zoom.canceled -= OnZoomCanceled;
            }

            // Dispose of the input actions
            galaxyControls?.Dispose();
            Debug.Log("🧹 Galaxy controls disposed");
        }

        // ✅ Move (WASD) callback
        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();

            // Convert Vector2 to 3D movement
            // X → Right/Left (horizontal map movement)
            // Y → Up/Down (vertical map movement)
            Vector3 moveDirection = new Vector3(moveInput.x, moveInput.y, 0f);

            OnMoveInput?.Invoke(moveDirection);
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            // Stop movement when keys released (optional, depends on camera controller)
        }

        // ✅ UpDown (Arrow Keys) callback
        private void OnUpDownPerformed(InputAction.CallbackContext context)
        {
            Vector2 upDownInput = context.ReadValue<Vector2>();

            // Y axis from arrows controls forward/back (Z in 3D space)
            Vector3 moveDirection = new Vector3(0f, 0f, upDownInput.y);

            OnMoveInput?.Invoke(moveDirection);
        }

        private void OnUpDownCanceled(InputAction.CallbackContext context)
        {
            // Stop movement when arrow keys released
        }

        // ✅ Rotate (Q/E) callback
        private void OnRotatePerformed(InputAction.CallbackContext context)
        {
            float rotateValue = context.ReadValue<float>();
            OnRotateInput?.Invoke(rotateValue);
        }

        private void OnRotateCanceled(InputAction.CallbackContext context)
        {
            OnRotateInput?.Invoke(0f); // Stop rotation
        }

        // ✅ Zoom (Z/X) callback
        private void OnZoomPerformed(InputAction.CallbackContext context)
        {
            float zoomValue = context.ReadValue<float>();
            OnZoomInput?.Invoke(zoomValue);
        }

        private void OnZoomCanceled(InputAction.CallbackContext context)
        {
            OnZoomInput?.Invoke(0f); // Stop zoom
        }
    }
}
