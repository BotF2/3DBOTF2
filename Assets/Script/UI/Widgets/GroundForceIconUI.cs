using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    /// <summary>
    /// A single ground force entry instantiated into GroundForceGrid (GroundForceElement prefab).
    /// Unlike OrbitalBatteryIconUI, ground forces have no power load and no on/off state — an
    /// icon simply exists (visible) for as long as its underlying facility GameObject is present.
    ///
    /// It does have a training state, though: a unit trained via TroopButtonAdd sits in
    /// StarSysData.TrainingGroundForces until the next Advance Turn, desaturated the same tinting
    /// way OrbitalBatteryIconUI grays out a powered-off battery (civilians are "in basic training"
    /// while gray, not yet counted as real troops - see StarSysManager.TrainGroundForceUnit).
    /// </summary>
    public class GroundForceIconUI : MonoBehaviour
    {
        private static readonly Color TrainedColor = Color.white;
        private static readonly Color TrainingColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        [Tooltip("Icon image tinted based on training state. Defaults to this GameObject's own Image if unassigned.")]
        [SerializeField] private Image iconImage;

        [HideInInspector] public GameObject SourceFacilityGO;

        private void Awake()
        {
            if (iconImage == null)
                iconImage = GetComponent<Image>();
        }

        public void SetTraining(bool isTraining)
        {
            if (iconImage != null)
                iconImage.color = isTraining ? TrainingColor : TrainedColor;
        }
    }
}
