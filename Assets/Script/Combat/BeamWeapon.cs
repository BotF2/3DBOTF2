using UnityEngine;
using Assets.Core;
using System.Collections.Generic;

public class BeamWeapon: MonoBehaviour
{
    public LineRenderer LineRenderer;
    public Transform TargetTransform;
    public Transform WeaponTransform;
    [SerializeField]
    private Transform[] _weaponAndTargetTrans = new Transform[2];
    private void Start()
    {
        LineRenderer = GetComponent<LineRenderer>();
        if (LineRenderer == null)
        {
            Debug.LogError("LineRenderer component not found on BeamWeapon GameObject.");
            return;
        }
    }
    public void SetWeaponAndTarget(Transform weapon, Transform target )
    {
        TargetTransform = target;
        WeaponTransform = weapon;
        if (TargetTransform == null)
        {
            Debug.LogWarning("TargetTransform is null. Beam will not be rendered.");
            return;
        }
        _weaponAndTargetTrans[0] = WeaponTransform;
        _weaponAndTargetTrans[1] = TargetTransform;
        //UpdateBeam();
    }
    private void Update()
    {
        if (LineRenderer == null || _weaponAndTargetTrans[0] == null || _weaponAndTargetTrans[1] == null)
        {
            return;
        }
        LineRenderer.positionCount = 2;
        LineRenderer.SetPosition(0, _weaponAndTargetTrans[0].position);
        LineRenderer.SetPosition(1, _weaponAndTargetTrans[1].position);
    }
}
