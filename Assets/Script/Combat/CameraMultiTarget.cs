using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Core
{
    enum TurnDirection
    {
        up,
        right,
        down,
        left
    }

    public class CameraMultiTarget : MonoBehaviour
    {
        public float Pitch;
        public float Yaw;
        public float Roll;
        public float PaddingLeft = 310f;
        public float PaddingRight = 310f;
        public float PaddingUp = 310f;
        public float PaddingDown = 310f;
        public float MoveSmoothTime = 0.19f;
        private Camera _camera;
        public Camera ShipCamera;
        private float _shipCameraFieldOfView;
        private float _shipCameraFOV_counter = 0;
        public GameObject CameraHolder;
        private GameObject[] _targets = new GameObject[0];
        private DebugProjection _debugProjection;

        #region added to cameraMuliTararget
        private Vector3 _cameraOffSet;
        private bool _warpingIn = true;
        private bool _spaceKey = false;
        private bool _firstTimeMouseRotate = true;
        public bool _normalizeFieldOfView = false;
        private float _autoRotationTimer = 5f;
        private float _rotationDirectionTimer = 2f;
        public Vector3 _cameraTarget;
        public float _mouseRotationSpeed = 5.0f;
        private TurnDirection _turnDirection { get; set; } = TurnDirection.left;
        private Vector3 _axisOfRotation;
        public float RotateSmoothTime = 0.001f;
        private float AngularVelocity = 0.0f;
        //int counter = 0;
        #endregion

        enum DebugProjection { DISABLE, IDENTITY, ROTATED }
        enum ProjectionEdgeHits { TOP_BOTTOM, LEFT_RIGHT }

        public void SetTargets(GameObject[] targets)
        {
            _targets = targets;
        }

        private void Awake()
        {
            _camera = Camera.main; // give gameObject(GalaxyEventCamera) and unity Camera.main the same position and rotation at end of LateUpdate		
            _debugProjection = DebugProjection.ROTATED;
            _shipCameraFieldOfView = ShipCamera.fieldOfView;
        }

        private void LateUpdate()
        {

            if (_targets.Length == 0)
            {
                return;
            }
            else
            {
                List<GameObject> survivingTargets = new List<GameObject>();

                for (int i = 0; i < _targets.Count(); i++)
                {
                    if (_targets[i] != null)
                    {
                        survivingTargets.Add(_targets[i]);
                    }
                }

                _targets = survivingTargets.ToArray(); // update _destination to surviving ships

            }

            if (GameManager.Instance._warpingInIsOver)
                _warpingIn = false;

            if (_warpingIn)
            {
                var _delatFOV = _shipCameraFOV_counter + 0.05f;
                _shipCameraFOV_counter = _shipCameraFOV_counter + 0.05f;
                ShipCamera.fieldOfView = 60f - _delatFOV;
            }

            var targetPositionAndRotation = TargetPositionAndRotation(_targets);

            if (Input.GetKey("space") && !_warpingIn)
            {
                _spaceKey = true;
                _autoRotationTimer = 5.0f;
            }
            else if (!_warpingIn)
            {
                _spaceKey = false;
            }
            Vector3 velocity = Vector3.zero;
            gameObject.transform.position = Vector3.SmoothDamp(gameObject.transform.position, targetPositionAndRotation.Position, ref velocity, MoveSmoothTime);
            if (!_spaceKey)
            {
                if (_autoRotationTimer > 0)
                {
                    if (_autoRotationTimer < 4.5f)
                    {
                        NormalizFieldOfView();
                    }

                    _cameraOffSet = gameObject.transform.position - _cameraTarget;
                    //gameObject.transform.position = Vector3.SmoothDamp(gameObject.transform.position, targetPositionAndRotation.position, ref velocity, MoveSmoothTime);

                    var target_rot = Quaternion.LookRotation(_cameraTarget - gameObject.transform.position); //_cameraTarget.transform.position
                    var delta = Quaternion.Angle(gameObject.transform.rotation, target_rot);
                    if (delta > 0.0f)
                    {
                        var t = Mathf.SmoothDampAngle(delta, 0.0f, ref AngularVelocity, RotateSmoothTime);
                        t = 1f - t / delta;
                        gameObject.transform.rotation = Quaternion.Slerp(gameObject.transform.rotation, target_rot, t);
                    }

                    if (!_warpingIn && !_spaceKey)
                    {
                        _autoRotationTimer -= Time.deltaTime;
                        if (_autoRotationTimer <= 0.0f)
                            if (_turnDirection != TurnDirection.left)
                                _turnDirection++;
                            else _turnDirection = TurnDirection.up;
                    }
                }
                else
                {
                    // autoratation code
                    _firstTimeMouseRotate = true;
                    float Rotation = 0.015f;

                    if (_rotationDirectionTimer < 2f)
                    {
                        NormalizFieldOfView();
                    }
                    if (_rotationDirectionTimer <= 0)
                    {
                        _rotationDirectionTimer = 2f;
                        _autoRotationTimer = 5f;
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
                    AutoRotation(Rotation, _axisOfRotation);
                    _rotationDirectionTimer -= Time.deltaTime;
                    gameObject.transform.LookAt(_cameraTarget);
                }
            }
            else
            {
                // this 'else' is spacebar key is down so rotate with mouse
                Quaternion cameraTurnAngleX = Quaternion.AngleAxis(Input.GetAxis("Mouse X") * _mouseRotationSpeed, Vector3.up);
                _cameraOffSet = cameraTurnAngleX * _cameraOffSet;
                Vector3 newPositionX = _cameraTarget + _cameraOffSet;
                gameObject.transform.position = Vector3.Slerp(gameObject.transform.position, newPositionX, MoveSmoothTime);
                Quaternion cameraTurnAngleY = Quaternion.AngleAxis(Input.GetAxis("Mouse Y") * _mouseRotationSpeed, Vector3.right);
                _cameraOffSet = cameraTurnAngleY * _cameraOffSet;
                Vector3 newPositionY = _cameraTarget + _cameraOffSet;
                gameObject.transform.position = Vector3.Slerp(gameObject.transform.position, newPositionY, MoveSmoothTime);
                gameObject.transform.LookAt(_cameraTarget);
                if (_firstTimeMouseRotate)
                {
                    _autoRotationTimer = 5f;
                    _firstTimeMouseRotate = false;
                }
            }

            _camera.transform.position = gameObject.transform.position;
            _camera.transform.rotation = gameObject.transform.rotation;
            //_camera.fieldOfView = _shipCameraFieldOfView;

        }

        #region Auto Rotation around average target
        private void AutoRotation(float Rotating, Vector3 direction)
        {
            Quaternion cameraTurnAngleX = Quaternion.AngleAxis(Rotating * _mouseRotationSpeed, direction);
            _cameraOffSet = cameraTurnAngleX * _cameraOffSet;
            Vector3 newPositionX = _cameraTarget + _cameraOffSet;
            gameObject.transform.position = Vector3.Slerp(gameObject.transform.position, newPositionX, MoveSmoothTime);
        }
        #endregion
        private void NormalizFieldOfView()
        {
            // ..normalize shipcamera field of view
            if (ShipCamera.fieldOfView >= _shipCameraFieldOfView + 0.5f || ShipCamera.fieldOfView <= _shipCameraFieldOfView - 0.5f)   //_autoRotationTimer < 1.5f)
            {
                if (ShipCamera.fieldOfView <= _shipCameraFieldOfView)
                {
                    ShipCamera.fieldOfView += 0.005f;
                }
                else if (ShipCamera.fieldOfView >= _shipCameraFieldOfView)
                {
                    ShipCamera.fieldOfView -= 0.005f;
                }
            }
            _shipCameraFieldOfView = ShipCamera.fieldOfView;

        }

        PositionAndRotation TargetPositionAndRotation(GameObject[] targets)
        {
            _cameraTarget = calculateCentroid(targets);
            float halfVerticalFovRad = (_camera.fieldOfView * Mathf.Deg2Rad) / 2f;
            float halfHorizontalFovRad = Mathf.Atan(Mathf.Tan(halfVerticalFovRad) * _camera.aspect);

            var rotation = Quaternion.Euler(Pitch, Yaw, Roll);
            var inverseRotation = Quaternion.Inverse(rotation);

            var targetsRotatedToCameraIdentity = targets.Select(target => inverseRotation * target.transform.position).ToArray();

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
            Quaternion rotation = _debugProjection == DebugProjection.IDENTITY ? Quaternion.identity : gameObject.transform.rotation;
            Debug.DrawRay(rotation * start, rotation * direction, color);
        }

        public Vector3 calculateCentroid(GameObject[] centerPoints)
        {
            var centroid = new Vector3(0, 0, 0);
            var numPoints = centerPoints.Count();
            foreach (var point in centerPoints)
            {
                centroid += point.transform.position;
            }

            centroid /= numPoints;

            return centroid;
        }
        //public void WarpIsOver()
        //      {
        //	_warpingIn = false;
        //      }
    }
}


