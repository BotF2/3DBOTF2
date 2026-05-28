using BOTF3D.Combat;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using BOTF3D.Core;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



enum TurnDirection { up, right, down, left }

namespace BOTF3D.Combat
{
    public class ShipCombatCameraController : MonoBehaviour, IController
    {
        public void Initialize() { }
        public void UpdateState() { }
        /// <summary>
        /// Multi-target camera controller for combat scene.
        /// Guarantees all ships remain in view at all times after warp-in.
        /// </summary>

        public static ShipCombatCameraController Instance { get; set; }

        [Header("Camera Angle")]
        [Tooltip("Tilt down from horizontal. 0 = pure side view. Keep low (0-10) to avoid ships drifting to top of screen.")]
        [Range(0f, 60f)]
        public float Pitch = 10f;//At Pitch = 10f the camera will be 10° above the battle plane
        [Tooltip("Yaw rotation of the camera view plane. 0 = looking along -Z.")]
        public float Yaw = 0f;
        public float Roll = 0f;

        [Header("Framing")]
        [Tooltip("Extra world-unit margin added around all ships so they are not right at the edge.")]
        public float FramingMargin = 20f;

        [Tooltip("Vertical offset applied to camera target point. Positive = ships appear lower in frame, Negative = ships appear higher.")]
        [Range(-100f, 100f)]
        public float VerticalFramingOffset = 90f; // Are the ships to high or low? higher value moves ships down.

        [Tooltip("Field of view after warp-in. Higher value = wider = more scene visible.")]
        [Range(40f, 110f)]
        public float CombatFieldOfView = 60f;

        [Tooltip("Field of view during warp-in animation.")]
        [Range(60f, 120f)]
        public float WarpFieldOfView = 95f;

        [Header("Movement")]
        [Tooltip("How quickly camera smooths to the calculated position. Lower = snappier.")]
        [Range(0.05f, 1f)]
        public float MoveSmoothTime = 0.15f;

        [Tooltip("Mouse orbit speed when holding Space.")]
        public float MouseRotationSpeed = 5f;

        [Header("Auto-Rotation")]
        [Tooltip("Seconds of idle before cinematic auto-rotation begins.")]
        public float AutoRotationDelay = 8f;
        [Tooltip("Degrees per frame for cinematic auto-orbit.")]
        public float AutoRotationSpeed = 0.15f;

        // ── Internals ──────────────────────────────────────────────────────────
        [SerializeField] private Camera _shipCamera;
        private GameObject[] _targets = new GameObject[0];
        private bool _warpingIn = false;
        public bool WarpingInOver = false;
        private float _autoRotationTimer;
        private float _rotationDirectionTimer = 4f;
        private TurnDirection _turnDirection = TurnDirection.left;
        private Vector3 _axisOfRotation;
        private Vector3 _cameraTarget;
        private Vector3 _cameraOffset;

        // ── Public API ─────────────────────────────────────────────────────────
        public Vector3 CameraOffSet { get => _cameraOffset; set => _cameraOffset = value; }

        public void SetTargets(GameObject[] targets) => _targets = targets;
        public void SetWarpingIn(bool isWarping) => _warpingIn = isWarping;
        public void SetWarpingInOver(bool isDone) => WarpingInOver = isDone;
        public void SetAutoRotationTimer(float time) => _autoRotationTimer = time;

        // ── Structs / Enums ────────────────────────────────────────────────────
        public struct PositionAndRotation
        {
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public PositionAndRotation(Vector3 position, Quaternion rotation)
            { Position = position; Rotation = rotation; }
        }
        enum ProjectionEdgeHits { TOP_BOTTOM, LEFT_RIGHT }

        // ── Unity Lifecycle ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _autoRotationTimer = AutoRotationDelay;

            // Ensure only one AudioListener
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            for (int i = 1; i < listeners.Length; i++)
                Destroy(listeners[i]);
            if (listeners.Length == 0)
                gameObject.AddComponent<AudioListener>();
        }

        private void Start()
        {
            _warpingIn = false;
            WarpingInOver = false;
            if (_shipCamera == null)
                _shipCamera = GetComponent<Camera>();

            // Park camera directly behind center along -Z axis (no vertical tilt)
            transform.position = new Vector3(0f, 800f, -800f);
            _cameraOffset = transform.position - _cameraTarget;
        }

        private void LateUpdate()
        {
            Scene scene = SceneManager.GetSceneByName("CombatScene");
            if (!scene.isLoaded) return;

            // ── Warp-in phase: wide FOV, no movement ──────────────────────────
            if (_warpingIn)
            {
                _shipCamera.fieldOfView = WarpFieldOfView;
                return;
            }

            // ── Post warp-in: keep all ships in frame ─────────────────────────
            if (!WarpingInOver || _targets.Length == 0) return;

            // Prune destroyed/null targets
            _targets = _targets.Where(t => t != null).ToArray();
            if (_targets.Length == 0) return;

            // Always calculate the ideal position that fits all ships
            PositionAndRotation ideal = CalculateIdealPosition(_targets);

            // ── Manual orbit: Space + Mouse ───────────────────────────────────
            if (Input.GetKey("space"))
            {
                _autoRotationTimer = AutoRotationDelay;

                Quaternion yawDelta = Quaternion.AngleAxis(Input.GetAxis("Mouse X") * MouseRotationSpeed, Vector3.up);
                Quaternion pitchDelta = Quaternion.AngleAxis(-Input.GetAxis("Mouse Y") * MouseRotationSpeed, transform.right);

                _cameraOffset = yawDelta * pitchDelta * _cameraOffset;
                transform.position = Vector3.Lerp(transform.position, _cameraTarget + _cameraOffset, MoveSmoothTime);
                transform.LookAt(_cameraTarget);
            }
            // ── Settle into ideal framing position ────────────────────────────
            else if (_autoRotationTimer > 0f)
            {
                _autoRotationTimer -= Time.unscaledDeltaTime;

                float smoothFactor = Mathf.Clamp01(Time.unscaledDeltaTime / MoveSmoothTime);
                transform.position = Vector3.Lerp(transform.position, ideal.Position, smoothFactor);

                // ✅ Always LookAt centroid - ships are always centered regardless of Pitch
                transform.LookAt(_cameraTarget, Vector3.up);

                _cameraOffset = transform.position - _cameraTarget;
            }
            // ── Cinematic auto-orbit (always re-validates framing) ────────────
            else
            {
                if (_rotationDirectionTimer <= 0f)
                {
                    _rotationDirectionTimer = 4f;
                    _autoRotationTimer = AutoRotationDelay;
                    _turnDirection = _turnDirection == TurnDirection.left ? TurnDirection.up : _turnDirection + 1;
                }

                _axisOfRotation = _turnDirection switch
                {
                    TurnDirection.up => Vector3.right,
                    TurnDirection.right => Vector3.down,
                    TurnDirection.down => Vector3.left,
                    _ => Vector3.up,
                };

                float dir = _rotationDirectionTimer > 2f ? 1f : -1f;
                transform.RotateAround(_cameraTarget, _axisOfRotation, AutoRotationSpeed * dir);
                transform.LookAt(_cameraTarget);

                // ✅ After rotating, pull back to ideal distance so ships stay in frame
                float idealDist = Vector3.Distance(_cameraTarget, ideal.Position);
                float currentDist = Vector3.Distance(_cameraTarget, transform.position);
                if (currentDist < idealDist)
                {
                    Vector3 dir3 = (transform.position - _cameraTarget).normalized;
                    transform.position = _cameraTarget + dir3 * idealDist;
                }

                _rotationDirectionTimer -= Time.unscaledDeltaTime;
            }
        }

        // ── Ship destroyed ─────────────────────────────────────────────────────
        public void OnShipDestroyed(ShipController shipController)
        {
            _targets = _targets.Where(t => t != null && t != shipController.gameObject).ToArray();
        }

        // ── Core framing algorithm ─────────────────────────────────────────────
        /// <summary>
        /// Projects each ship into camera local space and solves for the exact
        /// distance that keeps every ship within the FOV. Ships are always centered.
        /// Works correctly at any Pitch angle.
        /// </summary>
        private PositionAndRotation CalculateIdealPosition(GameObject[] targets)
        {
            _shipCamera.fieldOfView = CombatFieldOfView;
            Vector3 shipCentroid = CalculateCentroid(targets);

            // Apply vertical offset to camera target to center ships in frame
            _cameraTarget = shipCentroid + Vector3.up * VerticalFramingOffset;

            // ── Step 1: Build camera direction axes from Pitch / Yaw ────────────
            float pitchRad = Pitch * Mathf.Deg2Rad;
            float yawRad = Yaw * Mathf.Deg2Rad;

            // Unit vector pointing FROM centroid TOWARD camera (opposite of forward)
            Vector3 camDir = new Vector3(
                 Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                 Mathf.Sin(pitchRad),
                -Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
            ).normalized;

            // Camera forward = toward centroid
            Vector3 forward = -camDir;
            Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
            // Recalculate up so axes are orthonormal (handles non-zero pitch)
            Vector3 up = Vector3.Cross(right, forward).normalized;

            // ── Step 2: Compute FOV half-angles ─────────────────────────────────
            float halfVertFovRad = _shipCamera.fieldOfView * Mathf.Deg2Rad * 0.5f;
            float halfHorizFovRad = Mathf.Atan(Mathf.Tan(halfVertFovRad) * _shipCamera.aspect);

            // ── Step 3: For each ship, find required camera distance D ──────────
            // Key insight: when camera is at C + camDir * D:
            //   depth  = D - dot(S - centroid, camDir)          [varies with D]
            //   horiz  = dot(S - centroid, right)               [CONSTANT vs D]
            //   vert   = dot(S - centroid, up)                  [CONSTANT vs D]
            //
            // For ship to be within FOV: |horiz| / depth <= tan(halfHorizFov)
            // Solving for D: D >= |horiz| / tan(halfHorizFov) + dot(S-C, camDir)
            // Same for vertical axis.
            // So take the max required D across all ships.

            float requiredD = 0f;

            foreach (var t in targets)
            {
                if (t == null) continue;

                // Calculate offset from ship centroid (not adjusted target)
                Vector3 offset = t.transform.position - shipCentroid;

                float depth_offset = Vector3.Dot(offset, camDir); // how far ship is along camDir
                float horiz = Vector3.Dot(offset, right);
                float vert = Vector3.Dot(offset, up);

                // Required D to keep this ship horizontally in frame (with margin)
                float dForHoriz = (Mathf.Abs(horiz) + FramingMargin) / Mathf.Tan(halfHorizFovRad)
                                  + depth_offset;

                // Required D to keep this ship vertically in frame (with margin)
                float dForVert = (Mathf.Abs(vert) + FramingMargin) / Mathf.Tan(halfVertFovRad)
                                  + depth_offset;

                requiredD = Mathf.Max(requiredD, dForHoriz, dForVert);
            }

            // Enforce a sensible minimum so camera doesn't clip into ships
            requiredD = Mathf.Max(requiredD, 200f);

            // ── Step 4: Build final position and always LookAt centroid ──────────
            Vector3 cameraPos = _cameraTarget + camDir * requiredD;
            Quaternion lookAt = Quaternion.LookRotation(forward, up);

            return new PositionAndRotation(cameraPos, lookAt);
        }

        private static float RequiredDistance(ProjectionHits hits, float halfFovRad)
        {
            return (hits.Max - hits.Min) * 0.5f / Mathf.Tan(halfFovRad);
        }

        private ProjectionHits ProjectionEdgeHitsAlongAxis(
            IEnumerable<Vector3> positions,
            ProjectionEdgeHits axis,
            float projPlaneZ,
            float halfFovRad,
            float margin)
        {
            float[] hits = positions
                .SelectMany(p => SingleTargetHits(p, axis, projPlaneZ, halfFovRad))
                .ToArray();

            return new ProjectionHits(hits.Max() + margin, hits.Min() - margin);
        }

        private float[] SingleTargetHits(Vector3 target, ProjectionEdgeHits axis, float projPlaneDist, float halfFovRad)
        {
            float d = projPlaneDist - target.z;
            float span = Mathf.Tan(halfFovRad) * d;

            return axis == ProjectionEdgeHits.LEFT_RIGHT
                ? new[] { target.x + span, target.x - span }
                : new[] { target.y + span, target.y - span };
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        public Vector3 CalculateCentroid(GameObject[] objects)
        {
            if (objects == null || objects.Length == 0) return transform.position;
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var obj in objects)
            {
                if (obj != null) { sum += obj.transform.position; count++; }
            }
            return count > 0 ? sum / count : transform.position;
        }

        // Keep old name for any callers
        public Vector3 calculateCentroid(GameObject[] objects) => CalculateCentroid(objects);

        private struct ProjectionHits
        {
            public float Max, Min;
            public ProjectionHits(float max, float min) { Max = max; Min = min; }
        }
    }
}
