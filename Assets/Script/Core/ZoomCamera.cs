using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Core
{
    public class ZoomCamera : MonoBehaviour

    {
    //    public Camera _shipCamera;
    //    public ShipCombatCameraController cameraMultiTarget;
    //    bool _startZoomerUpdate = false;
    //    bool _doneWithWideAngle = false;
    //    float _startTimer = 0;

    //    void Update()
    //    {
    //        if (_startZoomerUpdate)
    //        {
    //            if (!_doneWithWideAngle && _shipCamera.fieldOfView > 24)
    //                _shipCamera.fieldOfView = _shipCamera.fieldOfView - (Time.time - _startTimer) / 5;
    //            if (!_doneWithWideAngle && _shipCamera.fieldOfView <= 24)
    //                _doneWithWideAngle = true;

    //            if (_doneWithWideAngle) // && cameraMultiTarget._rotateAroundTarget)
    //            {
    //                if (Input.mouseScrollDelta == Vector2.zero)
    //                    return;
    //                else//GameManager.current._statePassedCombatInit)
    //                {
    //                    bool turnOffNormalize = false;
    //                    if (cameraMultiTarget._normalizeFieldOfView)
    //                    {
    //                        cameraMultiTarget._normalizeFieldOfView = false;
    //                        turnOffNormalize = true;
    //                    }
    //                    if (Input.mouseScrollDelta.y > 0 && _shipCamera.fieldOfView > 10) //GetAxis("Mouse ScrollWheel") 
    //                    {
    //                        _shipCamera.fieldOfView--;
    //                    }
    //                    if (Input.mouseScrollDelta.y < 0 && _shipCamera.fieldOfView < 30) //.GetAxis("Mouse ScrollWheel") < 0 
    //                    {
    //                        _shipCamera.fieldOfView++;
    //                    }
    //                    if (turnOffNormalize)
    //                    {
    //                        cameraMultiTarget._normalizeFieldOfView = true;
    //                        turnOffNormalize = false;
    //                    }

    //                }
    //            }
    //        }
    //    }
    //    public void ZoomIn()
    //    {
    //        _doneWithWideAngle = false;
    //        _startZoomerUpdate = true;
    //        _shipCamera.fieldOfView = 60;
    //        _startTimer = Time.time;
    //    }
    //    public void TurnOfZoomerUpdate()
    //    {
    //        _startZoomerUpdate = false;
    //        _doneWithWideAngle = false;
    //    }

    }
}
