using BOTF3D.Core;

using TMPro;
using UnityEngine;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.UI
{
    public class HabitableSysUIController : MonoBehaviour
    {
        public void Initialize() { }
        public void UpdateState() { }
        public static HabitableSysUIController Instance;
        private Camera galaxyEventCamera;
        private StarSysController starSysController;
        //[SerializeField]
        //private Canvas parentCanvas;
        public GameObject HabitableSysUIToggle;
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

        // HabitableSysUIToggle lives under CanvasHabitableSysUI, which starts disabled in the scene
        // (m_IsActive: 0). Toggling HabitableSysUIToggle alone did nothing visible - a child's own
        // active flag is irrelevant while an ancestor is inactive (GameObject.activeInHierarchy
        // stays false) - so the Canvas itself must be activated too. Cached via
        // GetComponentInParent(true) instead of a new Inspector field, since the Canvas is already
        // reachable from the existing reference and needs no new scene wiring.
        private GameObject parentCanvasGO;

        private void Start()
        {
            parentCanvasGO = HabitableSysUIToggle.GetComponentInParent<Canvas>(true)?.gameObject;
            HabitableSysUIToggle.SetActive(false);
            //if (galaxyEventCamera == null)
            //galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;
            //parentCanvas.worldCamera = galaxyEventCamera;
        }
        public void LoadHabitableSysUI(StarSysController starSysController, CivController discoveringFleetCivController)
        {
            int firstUninhabited = (int)CivEnum.ZZUNINHABITED1;
            this.starSysController = starSysController;
            if ((int)this.starSysController.StarSysData.CurrentOwnerCivEnum >= firstUninhabited)
            {
                OpenPopup(starSysController);

                // Just an announcement - Colonize/Claim System live in the Fleet menu, which
                // opens alongside this popup (see FleetController's uninhabited-arrival branch).
                ShowSystemAnnouncement(starSysController, "Uninhabited");
            }
        }

        private void OpenPopup(StarSysController starSysController)
        {
            GameObject aNull = new GameObject();
            GalaxyMenuUIController.Instance.OpenMenu(Menu.HabitableSysMenu, aNull);
            Destroy(aNull);
        }

        private void ShowSystemAnnouncement(StarSysController sysCon, string ownerStatusText)
        {
            if (parentCanvasGO != null)
                parentCanvasGO.SetActive(true);
            HabitableSysUIToggle.SetActive(true);

            sysCurrentOwnerNameTMP.text = ownerStatusText;
            if (starSysNameTMP != null)
                starSysNameTMP.text = sysCon.StarSysData.SysName;

            FleetMenuUIController.Instance?.SetPopupClearance(HabitableSysUIToggle.GetComponent<RectTransform>());
        }

        public void CloseVisual()
        {
            HabitableSysUIToggle.SetActive(false);
            if (parentCanvasGO != null)
                parentCanvasGO.SetActive(false);
            FleetMenuUIController.Instance?.ResetPopupClearance();
        }

        public void CloseUnLoadHabitableSysUI()
        {
            CloseVisual();
            TurnEventQueue.Instance?.NotifyDismissed();
        }
    }
}
