using BOTF3D.Core;

using TMPro;
using UnityEngine;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.UI
{
    /// <summary>
    /// Contact popup for uninhabited systems that are IsTerraformable but not yet IsHabitable
    /// (e.g. LEO). Mirrors HabitableSysUIController but targets CanvasTerraforableSysUI instead of
    /// CanvasHabitableSysUI - opens alongside the Fleet menu (see FleetController.OnTriggerEnter's
    /// uninhabited-system branch), which holds the Claim/Terraform order buttons.
    /// </summary>
    public class TerraformableSysUIController : MonoBehaviour
    {
        public void Initialize() { }
        public void UpdateState() { }
        public static TerraformableSysUIController Instance;
        private StarSysController starSysController;
        public GameObject TerraformableSysUIToggle;
        [SerializeField]
        private TMP_Text sysCurrentOwnerNameTMP;
        [SerializeField]
        private TMP_Text starSysNameTMP;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        // TerraformableSysUIToggle lives under CanvasTerraforableSysUI, which starts disabled in the
        // scene (m_IsActive: 0). Toggling TerraformableSysUIToggle alone did nothing visible - a
        // child's own active flag is irrelevant while an ancestor is inactive
        // (GameObject.activeInHierarchy stays false) - so the Canvas itself must be activated too.
        // Cached via GetComponentInParent(true) instead of a new Inspector field, since the Canvas
        // is already reachable from the existing reference and needs no new scene wiring.
        private GameObject parentCanvasGO;

        private void Start()
        {
            parentCanvasGO = TerraformableSysUIToggle.GetComponentInParent<Canvas>(true)?.gameObject;
            TerraformableSysUIToggle.SetActive(false);
        }

        public void LoadTerraformableSysUI(StarSysController starSysController, CivController discoveringFleetCivController)
        {
            int firstUninhabited = (int)CivEnum.ZZUNINHABITED1;
            this.starSysController = starSysController;
            if ((int)this.starSysController.StarSysData.CurrentOwnerCivEnum >= firstUninhabited)
            {
                OpenPopup();

                string statusText = starSysController.StarSysData.IsTerraforming
                    ? $"Terraforming - {TurnsRemaining(starSysController)} turn(s) remaining"
                    : "Uninhabited - Requires Terraforming";
                ShowSystemAnnouncement(starSysController, statusText);
            }
        }

        private int TurnsRemaining(StarSysController sysCon)
        {
            int stardatesLeft = Mathf.Max(0, sysCon.StarSysData.TerraformCompleteStardate - TimeManager.Instance.CurrentStarDate());
            return Mathf.CeilToInt((float)stardatesLeft / TimeManager.Instance.StarDatesPerTurn);
        }

        private void OpenPopup()
        {
            TimeManager.Instance.PauseTime();
            GameObject aNull = new GameObject();
            GalaxyMenuUIController.Instance.OpenMenu(Menu.HabitableSysMenu, aNull);
            Destroy(aNull);
        }

        private void ShowSystemAnnouncement(StarSysController sysCon, string ownerStatusText)
        {
            if (parentCanvasGO != null)
                parentCanvasGO.SetActive(true);
            TerraformableSysUIToggle.SetActive(true);

            sysCurrentOwnerNameTMP.text = ownerStatusText;
            if (starSysNameTMP != null)
                starSysNameTMP.text = sysCon.StarSysData.SysName;

            FleetMenuUIController.Instance?.SetPopupClearance(TerraformableSysUIToggle.GetComponent<RectTransform>());
        }

        public void CloseUnLoadTerraformableSysUI()
        {
            TerraformableSysUIToggle.SetActive(false);
            if (parentCanvasGO != null)
                parentCanvasGO.SetActive(false);
            TimeManager.Instance.ResumeTime();

            FleetMenuUIController.Instance?.ResetPopupClearance();
        }
    }
}
