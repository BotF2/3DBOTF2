using UnityEngine;

namespace BOTF3D.UI
{
    /// <summary>
    /// A single ground force entry instantiated into GroundForceGrid (GroundForceElement prefab).
    /// Unlike OrbitalBatteryIconUI, ground forces have no power load and no on/off state — an
    /// icon simply exists (visible) for as long as its underlying facility GameObject is present.
    /// </summary>
    public class GroundForceIconUI : MonoBehaviour
    {
        [HideInInspector] public GameObject SourceFacilityGO;
    }
}
