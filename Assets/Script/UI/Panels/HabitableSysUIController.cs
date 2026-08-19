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

        private void Start()
        {
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
                TimeManager.Instance.PauseTime();
                GameObject aNull = new GameObject();
                GalaxyMenuUIController.Instance.OpenMenu(Menu.HabitableSysMenu, aNull);
                Destroy(aNull);

                // Just an announcement - Colonize/Claim System live in the Fleet menu, which
                // opens alongside this popup (see FleetController's uninhabited-arrival branch).
                ShowSystemAnnouncement(starSysController);
            }
        }

        private void ShowSystemAnnouncement(StarSysController sysCon)
        {
            HabitableSysUIToggle.SetActive(true);

            sysCurrentOwnerNameTMP.text = "Uninhabited";
            if (starSysNameTMP != null)
                starSysNameTMP.text = sysCon.StarSysData.SysName;
        }

        public void CloseUnLoadHabitableSysUI()
        {
            HabitableSysUIToggle.SetActive(false);
            TimeManager.Instance.ResumeTime();
        }
    }
}
