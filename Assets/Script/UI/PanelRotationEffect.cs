using UnityEngine;

namespace BOTF3D.UI
{
    /// <summary>
    /// Smoothly rotates a UI panel back and forth creating a swaying effect.
    /// </summary>
    public class PanelRotationEffect : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y; // Y for perspective swing
        [SerializeField] private float minRotation = -15f;  // Swing left
        [SerializeField] private float maxRotation = 15f;  // Swing right
        [SerializeField] private float rotationSpeed = 0.25f; // Cycles per second

        public enum RotationAxis { X, Y, Z }

        private Vector3 initialRotation;
        private bool rotationInitialized = false;

        private void Start()
        {
            // Store initial rotation
            initialRotation = transform.localEulerAngles;
            rotationInitialized = true;

            Debug.Log($"PanelRotationEffect: Started on '{gameObject.name}' | Axis:{rotationAxis} Min:{minRotation} Max:{maxRotation} Speed:{rotationSpeed}");
            Debug.Log($"  Initial rotation: {initialRotation}");
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy || !rotationInitialized) return;

            // Smooth oscillation using PingPong
            float t = Mathf.PingPong(Time.time * rotationSpeed, 1f);
            float currentRotation = Mathf.Lerp(minRotation, maxRotation, t);

            // Apply rotation based on selected axis, preserving initial values for other axes
            switch (rotationAxis)
            {
                case RotationAxis.X:
                    transform.localEulerAngles = new Vector3(currentRotation, initialRotation.y, initialRotation.z);
                    break;
                case RotationAxis.Y:
                    transform.localEulerAngles = new Vector3(initialRotation.x, currentRotation, initialRotation.z);
                    break;
                case RotationAxis.Z:
                    transform.localEulerAngles = new Vector3(initialRotation.x, initialRotation.y, currentRotation);
                    break;
            }

            // Debug every second (60 frames)
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"PanelRotationEffect: Time={Time.time:F2} | t={t:F2} | {rotationAxis}={currentRotation:F2}° | Speed={rotationSpeed}");
            }
        }

        /// <summary>
        /// Allows runtime adjustment of rotation speed for testing
        /// </summary>
        public void SetRotationSpeed(float speed)
        {
            rotationSpeed = speed;
            Debug.Log($"PanelRotationEffect: Speed changed to {speed}");
        }
    }
}
