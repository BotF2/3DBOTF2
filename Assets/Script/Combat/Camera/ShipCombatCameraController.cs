using BOTF3D.Core;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Multi-target combat camera.
    ///
    /// The Camera component lives on a CHILD object under CombatCameraParent.
    /// We clear the child's local offsets in Start() and drive the camera's world
    /// transform directly every frame. Moving/rotating the parent instead caused
    /// the camera to orbit 230 units around the parent pivot.
    ///
    /// Pitch and Yaw are read EVERY frame so Inspector changes take effect
    /// immediately. _orbitRotation accumulates mouse/cinematic orbit on top of
    /// the base direction; resetting it to identity resets to the Inspector values.
    /// </summary>
    public class ShipCombatCameraController : MonoBehaviour, IController
    {
        public void Initialize() { }
        public void UpdateState() { }

        public static ShipCombatCameraController Instance { get; set; }

        [Header("Camera Angle")]
        [Tooltip("Elevation above the battle plane (degrees). 0 = pure side view, 90 = top-down.")]
        [Range(0f, 80f)]
        public float Pitch = 35f;

        [Tooltip("Horizontal rotation. 0 = looking along -Z.")]
        public float Yaw = 0f;

        [Tooltip("World-unit margin added around each ship when computing required distance.")]
        public float FramingMargin = 5f;

        [Tooltip("Pull-back when no transports are present.")]
        [Range(1f, 3f)]
        public float ZoomPullbackCombatOnly = 1.3f;

        [Tooltip("Pull-back when transports are present (they sit further out, so less pull-back needed).")]
        [Range(1f, 3f)]
        public float ZoomPullbackWithTransports = 1.0f;

        [Tooltip("Closest the camera will ever get, regardless of ship count or proximity. " +
                 "Calibrated so two ships 100 units apart still look comfortable. Increase to zoom out more.")]
        [Range(50f, 2000f)]
        public float MinimumCameraDistance = 150f;

        [Header("FOV")]
        [Range(20f, 110f)]
        public float CombatFieldOfView = 60f;
        [Range(60f, 120f)]
        public float WarpFieldOfView = 95f;

        [Header("Movement")]
        [Tooltip("Smooth zoom-in time (seconds). Zoom-out is always instant to keep ships in frame.")]
        [Range(0.05f, 2f)]
        public float MoveSmoothTime = 0.3f;

        [Tooltip("How quickly the camera re-centres after a ship is destroyed (seconds).")]
        [Range(0.05f, 2f)]
        public float CentroidSmoothTime = 0.4f;

        [Header("Orbit")]
        public float MouseRotationSpeed = 5f;
        public float AutoRotationDelay = 8f;
        public float AutoRotationSpeed = 0.15f;

        [SerializeField] private Camera _shipCamera;

        // ── State ───────────────────────────────────────────────────────────────
        private GameObject[] _targets = new GameObject[0];
        private bool _warpingIn;
        public bool WarpingInOver;

        // Accumulated orbit rotation applied on top of the base Pitch/Yaw direction.
        // Reset to identity when warping in; Inspector Pitch/Yaw is re-read every frame.
        private Quaternion _orbitRotation = Quaternion.identity;
        private bool _snapNextFrame = true;

        // _cameraDir is recomputed every LateUpdate — never cached across frames.
        private Vector3 _cameraDir;

        // Smoothed centroid — lerps toward the raw centroid each frame so that
        // ship destruction causes a pan rather than an instant camera jump.
        private Vector3 _smoothedCentroid;

        private float _autoRotationTimer;
        private float _rotationDirectionTimer = 4f;
        private Vector3 _cameraOffset;

        // ── Public API ──────────────────────────────────────────────────────────
        public Vector3 CameraOffSet { get => _cameraOffset; set => _cameraOffset = value; }
        public void SetTargets(GameObject[] targets) => _targets = targets;

        public void SetWarpingIn(bool isWarping)
        {
            _warpingIn = isWarping;
            if (isWarping)
            {
                _orbitRotation = Quaternion.identity;
                _snapNextFrame = true;
            }
        }

        public void SetWarpingInOver(bool isDone) => WarpingInOver = isDone;
        public void SetAutoRotationTimer(float t) => _autoRotationTimer = t;

        // Legacy struct kept for any callers that reference it by type
        public struct PositionAndRotation
        {
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public PositionAndRotation(Vector3 p, Quaternion r) { Position = p; Rotation = r; }
        }

        // ── Unity Lifecycle ─────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _autoRotationTimer = AutoRotationDelay;

            var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            for (int i = 1; i < listeners.Length; i++) Destroy(listeners[i]);
            if (listeners.Length == 0) gameObject.AddComponent<AudioListener>();
        }

        private void Start()
        {
            _warpingIn = false;
            WarpingInOver = false;
            _orbitRotation = Quaternion.identity;
            _snapNextFrame = true;

            if (_shipCamera == null)
                _shipCamera = GetComponentInChildren<Camera>();

            // Clear any child offset so we can drive the camera's world transform directly.
            if (_shipCamera != null && _shipCamera.transform != transform)
            {
                _shipCamera.transform.localPosition = Vector3.zero;
                _shipCamera.transform.localRotation = Quaternion.identity;
            }

            // Pre-position camera centred on the combat area so ships are framed during warp-in.
            // Ships stop at ±200 (combat) / ±400 (transports) on the X-axis; centroid is the origin.
            if (_shipCamera != null)
                _shipCamera.fieldOfView = WarpFieldOfView;

            Vector3 combatCentre = Vector3.zero;
            Vector3 startDir = PitchYawToDir(Pitch, Yaw);
            float halfFovRad = WarpFieldOfView * 0.5f * Mathf.Deg2Rad;
            // Fit ships out to ±400 (transport positions) with a 10% margin
            float startDist = Mathf.Max(MinimumCameraDistance, 400f / Mathf.Tan(halfFovRad) * 1.1f);
            Vector3 startPos = combatCentre + startDir * startDist;

            transform.position = startPos;
            if (_shipCamera != null)
            {
                _shipCamera.transform.position = startPos;
                Vector3 lookDir = (combatCentre - startPos).normalized;
                if (lookDir != Vector3.zero)
                    _shipCamera.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
            }
        }

        private void LateUpdate()
        {
            if (!SceneManager.GetSceneByName("CombatScene").isLoaded) return;

            if (_warpingIn)
            {
                _shipCamera.fieldOfView = WarpFieldOfView;
                return;
            }

            if (!WarpingInOver || _targets.Length == 0) return;

            _targets = _targets.Where(t => t != null).ToArray();
            if (_targets.Length == 0) return;

            _shipCamera.fieldOfView = CombatFieldOfView;

            // Recompute base direction from Inspector values every frame so Pitch/Yaw
            // changes take effect immediately without restarting Play Mode.
            _cameraDir = (_orbitRotation * PitchYawToDir(Pitch, Yaw)).normalized;

            Vector3 centroid = CalculateCentroid(_targets);

            // ── Orbit input ────────────────────────────────────────────────────
            bool snap = _snapNextFrame;
            _snapNextFrame = false;

            // Smooth the centroid so ship destruction causes a pan, not a jump.
            if (snap)
                _smoothedCentroid = centroid;
            else
            {
                float ca = 1f - Mathf.Exp(-Time.unscaledDeltaTime / CentroidSmoothTime);
                _smoothedCentroid = Vector3.Lerp(_smoothedCentroid, centroid, ca);
            }

            if (Input.GetKey("space"))
            {
                _autoRotationTimer = AutoRotationDelay;

                float yawDelta = Input.GetAxis("Mouse X") * MouseRotationSpeed;
                float pitchDelta = -Input.GetAxis("Mouse Y") * MouseRotationSpeed;
                Vector3 right = Vector3.Cross(_cameraDir, Vector3.up).normalized;

                _orbitRotation = Quaternion.AngleAxis(yawDelta, Vector3.up)
                               * Quaternion.AngleAxis(pitchDelta, right)
                               * _orbitRotation;

                // Recompute direction after orbit change
                _cameraDir = (_orbitRotation * PitchYawToDir(Pitch, Yaw)).normalized;
                snap = true;
            }
            else if (_autoRotationTimer > 0f)
            {
                _autoRotationTimer -= Time.unscaledDeltaTime;
                // Settle: direction frozen, only distance updates.
            }
            else
            {
                // Cinematic auto-orbit: yaw only so ships stay level.
                if (_rotationDirectionTimer <= 0f)
                {
                    _rotationDirectionTimer = 4f;
                    _autoRotationTimer = AutoRotationDelay;
                }
                else
                {
                    float dir = _rotationDirectionTimer > 2f ? 1f : -1f;
                    _orbitRotation = Quaternion.AngleAxis(AutoRotationSpeed * dir, Vector3.up)
                                   * _orbitRotation;
                    _cameraDir = (_orbitRotation * PitchYawToDir(Pitch, Yaw)).normalized;
                    _rotationDirectionTimer -= Time.unscaledDeltaTime;
                }
            }

            // ── Framing ────────────────────────────────────────────────────────
            bool hasTransports = System.Array.Exists(_targets,
                t => t != null && t.TryGetComponent<ShipController>(out var sc)
                               && sc.ShipData?.ShipType == ShipType.Transport);
            float pullback = hasTransports ? ZoomPullbackWithTransports : ZoomPullbackCombatOnly;
            float requiredDist = ComputeRequiredDistance(_smoothedCentroid) * Mathf.Max(pullback, 1f);
            // Never let the camera get closer than MinimumCameraDistance regardless of how
            // few ships remain or how close together they are.
            requiredDist = Mathf.Max(requiredDist, MinimumCameraDistance);

            Vector3 desiredPos = _smoothedCentroid + _cameraDir * requiredDist;
            float currentDist = (_shipCamera.transform.position - _smoothedCentroid).magnitude;

            // Snap outward immediately if ships need more room (never let them exit the frame).
            // Only smooth inward (zoom-in) to keep close-approach visually gradual.
            if (snap || currentDist < requiredDist - 0.5f)
            {
                _shipCamera.transform.position = desiredPos;
            }
            else
            {
                float alpha = 1f - Mathf.Exp(-Time.unscaledDeltaTime / MoveSmoothTime);
                _shipCamera.transform.position = Vector3.Lerp(
                    _shipCamera.transform.position, desiredPos, alpha);
            }

            // Rotate the camera to look at centroid.
            Vector3 lookDir = (_smoothedCentroid - _shipCamera.transform.position).normalized;
            if (lookDir != Vector3.zero)
            {
                Vector3 upVec = Mathf.Abs(Vector3.Dot(_cameraDir, Vector3.up)) > 0.98f
                    ? Vector3.forward : Vector3.up;
                _shipCamera.transform.rotation = Quaternion.LookRotation(lookDir, upVec);
            }

            // Keep the parent (AudioListener) co-located with the camera.
            transform.position = _shipCamera.transform.position;

            _cameraOffset = _shipCamera.transform.position - _smoothedCentroid;
        }

        // ── Framing math ───────────────────────────────────────────────────────
        private float ComputeRequiredDistance(Vector3 centroid)
        {
            Vector3 forward = -_cameraDir;
            Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
            Vector3 up = Vector3.Cross(right, forward).normalized;

            float halfVertFov = _shipCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float halfHorizFov = Mathf.Atan(Mathf.Tan(halfVertFov) * _shipCamera.aspect);

            float maxD = 0f;
            foreach (var t in _targets)
            {
                if (t == null) continue;

                Vector3 offset = t.transform.position - centroid;
                float camDot = Vector3.Dot(offset, _cameraDir);
                float horizEx = Mathf.Abs(Vector3.Dot(offset, right)) + FramingMargin;
                float vertEx = Mathf.Abs(Vector3.Dot(offset, up)) + FramingMargin;

                float dH = horizEx / Mathf.Tan(halfHorizFov) + camDot;
                float dV = vertEx / Mathf.Tan(halfVertFov) + camDot;
                maxD = Mathf.Max(maxD, dH, dV);
            }
            return Mathf.Max(maxD, 50f);
        }

        // ── Ship destroyed ─────────────────────────────────────────────────────
        public void OnShipDestroyed(ShipController shipController)
        {
            _targets = _targets.Where(t => t != null && t != shipController.gameObject).ToArray();
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        public Vector3 CalculateCentroid(GameObject[] objects)
        {
            if (objects == null || objects.Length == 0) return transform.position;
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var obj in objects)
                if (obj != null) { sum += obj.transform.position; count++; }
            return count > 0 ? sum / count : transform.position;
        }

        public Vector3 calculateCentroid(GameObject[] objects) => CalculateCentroid(objects);

        private static Vector3 PitchYawToDir(float pitchDeg, float yawDeg)
        {
            float p = pitchDeg * Mathf.Deg2Rad;
            float y = yawDeg * Mathf.Deg2Rad;
            return new Vector3(
                Mathf.Sin(y) * Mathf.Cos(p),
                Mathf.Sin(p),
               -Mathf.Cos(y) * Mathf.Cos(p)
            ).normalized;
        }
    }
}
