using BOTF3D.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;



enum TurnDirection
{
    up,
    right,
    down,
    left
}
namespace BOTF3D.GamePlay
{
    public class ShipCombatCameraController : MonoBehaviour
    {
        /// <summary>
        /// multi-target camera controller for combat scene. 
        /// https://lopespm.com/libraries/games/2018/12/27/camera-multi-target.html
        /// </summary>

        public static ShipCombatCameraController Instance { get; set; }
        public float Pitch;
        public float Yaw;
        public float Roll;
        public float PaddingLeft = 100f;
        public float PaddingRight = 100f;
        public float PaddingUp = 100f;
        public float PaddingDown = 100f;
        public float MoveSmoothTime = 0.19f;
        [SerializeField]
        private Camera _shipCamera;
        private GameObject[] _targets = new GameObject[0];
        private DebugProjection _debugProjection;
        private Vector3 _velocity = Vector3.zero;
        #region added to cameraMuliTararget
        private Vector3 _cameraOffSet;
        private bool _warpingIn = false;
        public bool WarpingInOver = false;
        private float _autoRotationTimer = 5f;
        private float _rotationDirectionTimer = 4f;
        public Vector3 _cameraTarget;
        public float _mouseRotationSpeed = 5.0f;
        private TurnDirection _turnDirection { get; set; } = TurnDirection.left;
        private Vector3 _axisOfRotation;

        // ✅ Public accessors for CombatUIManager
        public Vector3 CameraOffSet
        {
            get { return _cameraOffSet; }
            set { _cameraOffSet = value; }
        }

        public float MouseRotationSpeed
        {
            get { return _mouseRotationSpeed; }
        }

        public void SetAutoRotationTimer(float time)
        {
            _autoRotationTimer = time;
        }
        #endregion
        public struct PositionAndRotation
        {
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public PositionAndRotation(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }
        }
        enum DebugProjection { DISABLE, IDENTITY, ROTATED }
        enum ProjectionEdgeHits { TOP_BOTTOM, LEFT_RIGHT }

        public void SetTargets(GameObject[] targets)
        {
            _targets = targets;
        }

        private void Awake()
        {
            _debugProjection = DebugProjection.ROTATED;
            _autoRotationTimer = 5f;

            if (Instance != null && Instance != this)
            {
                Debug.Log("Duplicate ShipCombatCameraController found! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // ❌ REMOVE: DontDestroyOnLoad(gameObject);
            // ✅ Camera should live in CombatScene only!
            Debug.Log("✅ ShipCombatCameraController: Instance assigned (scene-based)");
        }
        private void Start()
        {
            _warpingIn = false;
            WarpingInOver = false;
            if (_shipCamera == null)
            {
                _shipCamera = GetComponent<Camera>();
            }
            gameObject.transform.position = new Vector3(0, 500, -800);
            _cameraOffSet = gameObject.transform.position - _cameraTarget;
        }

        private void LateUpdate()
        {
            Scene scene = SceneManager.GetSceneByName("CombatScene");
            if (!scene.isLoaded || _targets.Length == 0)
            {
                return;
            }
            if (_warpingIn)
            {
                _shipCamera.fieldOfView = 95f;
            }
            else if (WarpingInOver)
            {

                if (_targets.Length == 0)
                    return;
                else
                {
                    List<GameObject> theTargetList = _targets.ToList();
                    for (int i = 0; i < theTargetList.Count; i++)
                    {
                        if (_targets[i] == null)
                        {
                            theTargetList.Remove(_targets[i]);
                        }
                    }
                    _targets = theTargetList.ToArray();
                }
                if (_targets.Length == 0)
                    return;
                else
                {
                    var targetPositionAndRotation = TargetPositionAndRotation(_targets);

                    _cameraOffSet = gameObject.transform.position - _cameraTarget;

                    if (Input.GetKey("space"))
                    {
                        //manually rotate the camera with space bar + mouse
                        _autoRotationTimer = 5.0f;
                        Quaternion cameraTurnAngleX = Quaternion.AngleAxis(Input.GetAxis("Mouse X") * _mouseRotationSpeed, Vector3.up);
                        _cameraOffSet = cameraTurnAngleX * _cameraOffSet;
                        Vector3 newPositionX = _cameraTarget + _cameraOffSet;
                        gameObject.transform.position = Vector3.Slerp(gameObject.transform.position, newPositionX, MoveSmoothTime);
                        Quaternion cameraTurnAngleY = Quaternion.AngleAxis(Input.GetAxis("Mouse Y") * 1.8f * _mouseRotationSpeed, Vector3.right);
                        _cameraOffSet = cameraTurnAngleY * _cameraOffSet;
                        Vector3 newPositionY = _cameraTarget + _cameraOffSet;
                        gameObject.transform.position = Vector3.Slerp(gameObject.transform.position, newPositionY, MoveSmoothTime);
                        gameObject.transform.LookAt(_cameraTarget);
                    }
                    else if (_autoRotationTimer > 0)
                    {
                        // ✅ Use manual lerp with unscaledDeltaTime instead of SmoothDamp
                        float smoothFactor = Time.unscaledDeltaTime / MoveSmoothTime;
                        transform.position = Vector3.Lerp(transform.position, targetPositionAndRotation.Position, smoothFactor);
                        transform.rotation = Quaternion.Lerp(transform.rotation, targetPositionAndRotation.Rotation, smoothFactor);

                        _autoRotationTimer -= Time.unscaledDeltaTime;  // ✅ Changed from Time.deltaTime
                    }
                    else
                    {
                        // autorotation code
                        float Rotation = 0.2f;
                        if (_rotationDirectionTimer <= 0)
                        {
                            _rotationDirectionTimer = 4f;
                            _autoRotationTimer = 5f;

                            if (_turnDirection != TurnDirection.left)
                                _turnDirection++;
                            else _turnDirection = TurnDirection.up;
                        }
                        switch (_turnDirection)
                        {
                            case TurnDirection.up:
                                _axisOfRotation = Vector3.right;
                                break;
                            case TurnDirection.right:
                                _axisOfRotation = Vector3.down;
                                break;
                            case TurnDirection.down:
                                _axisOfRotation = Vector3.left;
                                break;
                            case TurnDirection.left:
                                _axisOfRotation = Vector3.up;
                                break;
                            default:
                                break;
                        }
                        if (_rotationDirectionTimer > 2f)
                            transform.RotateAround(_cameraTarget, _axisOfRotation, Rotation);
                        else
                            transform.RotateAround(_cameraTarget, _axisOfRotation, -Rotation);

                        _rotationDirectionTimer -= Time.unscaledDeltaTime;  // ✅ Changed from Time.deltaTime
                    }
                }
            }
        }
        public void OnShipDestroyed(ShipController shipController)
        {
            List<GameObject> theTargetList = _targets.ToList();
            theTargetList.Remove(shipController.gameObject);
            _targets = theTargetList.ToArray();
        }
        PositionAndRotation TargetPositionAndRotation(GameObject[] targets)
        {
            if (targets.Length == 0)
                return new PositionAndRotation(transform.position, transform.rotation);
            else
            {
                _cameraTarget = calculateCentroid(targets);
                float halfVerticalFovRad = (_shipCamera.fieldOfView * Mathf.Deg2Rad) / 2f;
                float halfHorizontalFovRad = Mathf.Atan(Mathf.Tan(halfVerticalFovRad) * _shipCamera.aspect);

                var rotation = Quaternion.Euler(Pitch, Yaw, Roll);
                var inverseRotation = Quaternion.Inverse(rotation);
                var targetsList = targets.ToList();
                targetsList.RemoveAll(t => t == null);
                targets = targetsList.ToArray();
                var targetsRotatedToCameraIdentity = targets.Select(target => inverseRotation * target.transform.position).ToArray();
                if (targets.Length == 0)
                    return new PositionAndRotation(transform.position, transform.rotation);
                else
                {
                    float furthestPointDistanceFromCamera = targetsRotatedToCameraIdentity.Max(target => target.z);
                    float projectionPlaneZ = furthestPointDistanceFromCamera + 3f;

                    ProjectionHits viewProjectionLeftAndRightEdgeHits =
                        ViewProjectionEdgeHits(targetsRotatedToCameraIdentity, ProjectionEdgeHits.LEFT_RIGHT, projectionPlaneZ, halfHorizontalFovRad).AddPadding(PaddingRight, PaddingLeft);
                    ProjectionHits viewProjectionTopAndBottomEdgeHits =
                        ViewProjectionEdgeHits(targetsRotatedToCameraIdentity, ProjectionEdgeHits.TOP_BOTTOM, projectionPlaneZ, halfVerticalFovRad).AddPadding(PaddingUp, PaddingDown);

                    var requiredCameraPerpedicularDistanceFromProjectionPlane =
                        Mathf.Max(
                            RequiredCameraPerpedicularDistanceFromProjectionPlane(viewProjectionTopAndBottomEdgeHits, halfVerticalFovRad),
                            RequiredCameraPerpedicularDistanceFromProjectionPlane(viewProjectionLeftAndRightEdgeHits, halfHorizontalFovRad)
                    );

                    Vector3 cameraPositionIdentity = new Vector3(
                        (viewProjectionLeftAndRightEdgeHits.Max + viewProjectionLeftAndRightEdgeHits.Min) / 2f,
                        (viewProjectionTopAndBottomEdgeHits.Max + viewProjectionTopAndBottomEdgeHits.Min) / 2f,
                        projectionPlaneZ - requiredCameraPerpedicularDistanceFromProjectionPlane);

                    DebugDrawProjectionRays(cameraPositionIdentity,
                        viewProjectionLeftAndRightEdgeHits,
                        viewProjectionTopAndBottomEdgeHits,
                        requiredCameraPerpedicularDistanceFromProjectionPlane,
                        targetsRotatedToCameraIdentity,
                        projectionPlaneZ,
                        halfHorizontalFovRad,
                        halfVerticalFovRad);

                    return new PositionAndRotation(rotation * cameraPositionIdentity, rotation);
                }
            }
        }

        private static float RequiredCameraPerpedicularDistanceFromProjectionPlane(ProjectionHits viewProjectionEdgeHits, float halfFovRad)
        {
            float distanceBetweenEdgeProjectionHits = viewProjectionEdgeHits.Max - viewProjectionEdgeHits.Min;
            return (distanceBetweenEdgeProjectionHits / 2f) / Mathf.Tan(halfFovRad);
        }

        private ProjectionHits ViewProjectionEdgeHits(IEnumerable<Vector3> targetsRotatedToCameraIdentity, ProjectionEdgeHits alongAxis, float projectionPlaneZ, float halfFovRad)
        {
            float[] projectionHits = targetsRotatedToCameraIdentity
                .SelectMany(target => TargetProjectionHits(target, alongAxis, projectionPlaneZ, halfFovRad))
                .ToArray();
            return new ProjectionHits(projectionHits.Max(), projectionHits.Min());
        }

        private float[] TargetProjectionHits(Vector3 target, ProjectionEdgeHits alongAxis, float projectionPlaneDistance, float halfFovRad)
        {
            float distanceFromProjectionPlane = projectionPlaneDistance - target.z;
            float projectionHalfSpan = Mathf.Tan(halfFovRad) * distanceFromProjectionPlane;

            if (alongAxis == ProjectionEdgeHits.LEFT_RIGHT)
            {
                return new[] { target.x + projectionHalfSpan, target.x - projectionHalfSpan };
            }
            else
            {
                return new[] { target.y + projectionHalfSpan, target.y - projectionHalfSpan };
            }
        }
        private void DebugDrawProjectionRays(Vector3 cameraPositionIdentity, ProjectionHits viewProjectionLeftAndRightEdgeHits,
            ProjectionHits viewProjectionTopAndBottomEdgeHits, float requiredCameraPerpedicularDistanceFromProjectionPlane,
            IEnumerable<Vector3> targetsRotatedToCameraIdentity, float projectionPlaneZ, float halfHorizontalFovRad,
            float halfVerticalFovRad)
        {

            if (_debugProjection == DebugProjection.DISABLE)
                return;

            DebugDrawProjectionRay(
                cameraPositionIdentity,
                new Vector3((viewProjectionLeftAndRightEdgeHits.Max - viewProjectionLeftAndRightEdgeHits.Min) / 2f,
                    (viewProjectionTopAndBottomEdgeHits.Max - viewProjectionTopAndBottomEdgeHits.Min) / 2f,
                    requiredCameraPerpedicularDistanceFromProjectionPlane), new Color32(31, 119, 180, 255));
            DebugDrawProjectionRay(
                cameraPositionIdentity,
                new Vector3((viewProjectionLeftAndRightEdgeHits.Max - viewProjectionLeftAndRightEdgeHits.Min) / 2f,
                    -(viewProjectionTopAndBottomEdgeHits.Max - viewProjectionTopAndBottomEdgeHits.Min) / 2f,
                    requiredCameraPerpedicularDistanceFromProjectionPlane), new Color32(31, 119, 180, 255));
            DebugDrawProjectionRay(
                cameraPositionIdentity,
                new Vector3(-(viewProjectionLeftAndRightEdgeHits.Max - viewProjectionLeftAndRightEdgeHits.Min) / 2f,
                    (viewProjectionTopAndBottomEdgeHits.Max - viewProjectionTopAndBottomEdgeHits.Min) / 2f,
                    requiredCameraPerpedicularDistanceFromProjectionPlane), new Color32(31, 119, 180, 255));
            DebugDrawProjectionRay(
                cameraPositionIdentity,
                new Vector3(-(viewProjectionLeftAndRightEdgeHits.Max - viewProjectionLeftAndRightEdgeHits.Min) / 2f,
                    -(viewProjectionTopAndBottomEdgeHits.Max - viewProjectionTopAndBottomEdgeHits.Min) / 2f,
                    requiredCameraPerpedicularDistanceFromProjectionPlane), new Color32(31, 119, 180, 255));

            foreach (var target in targetsRotatedToCameraIdentity)
            {
                float distanceFromProjectionPlane = projectionPlaneZ - target.z;
                float halfHorizontalProjectionVolumeCircumcircleDiameter = Mathf.Sin(Mathf.PI - ((Mathf.PI / 2f) + halfHorizontalFovRad)) / (distanceFromProjectionPlane);
                float projectionHalfHorizontalSpan = Mathf.Sin(halfHorizontalFovRad) / halfHorizontalProjectionVolumeCircumcircleDiameter;
                float halfVerticalProjectionVolumeCircumcircleDiameter = Mathf.Sin(Mathf.PI - ((Mathf.PI / 2f) + halfVerticalFovRad)) / (distanceFromProjectionPlane);
                float projectionHalfVerticalSpan = Mathf.Sin(halfVerticalFovRad) / halfVerticalProjectionVolumeCircumcircleDiameter;

                DebugDrawProjectionRay(target,
                    new Vector3(projectionHalfHorizontalSpan, 0f, distanceFromProjectionPlane),
                    new Color32(214, 39, 40, 255));
                DebugDrawProjectionRay(target,
                    new Vector3(-projectionHalfHorizontalSpan, 0f, distanceFromProjectionPlane),
                    new Color32(214, 39, 40, 255));
                DebugDrawProjectionRay(target,
                    new Vector3(0f, projectionHalfVerticalSpan, distanceFromProjectionPlane),
                    new Color32(214, 39, 40, 255));
                DebugDrawProjectionRay(target,
                    new Vector3(0f, -projectionHalfVerticalSpan, distanceFromProjectionPlane),
                    new Color32(214, 39, 40, 255));
            }
        }
        private void DebugDrawProjectionRay(Vector3 start, Vector3 direction, Color color)
        {
            Quaternion rotation = _debugProjection == DebugProjection.IDENTITY ? Quaternion.identity : transform.rotation;
            Debug.DrawRay(rotation * start, rotation * direction, color);
        }

        public Vector3 calculateCentroid(GameObject[] centerPoints)
        {
            if (centerPoints.Length == 0)
                return transform.position;
            else
            {
                var centroid = new Vector3(0, 0, 0);
                var numPoints = centerPoints.Count();
                if (centroid == null || numPoints == 0)
                    return transform.position;
                else
                {
                    foreach (var point in centerPoints)
                    {
                        if (point != null && point.transform != null)
                            centroid += point.transform.position;
                    }

                    centroid /= numPoints;

                    return centroid;
                }
            }
        }
        internal void SetWarpingIn(bool isWarping)
        {
            _warpingIn = isWarping;
        }
        internal void SetWarpingInOver(bool isWarpingDone)
        {
            WarpingInOver = isWarpingDone;
        }
    }
}













