using BOTF3D.Core;
using BOTF3D.GamePlay;
using TMPro;
using UnityEngine;

namespace BOTF3D.UI
{
    public class HabitableSysUIController : MonoBehaviour
    {
        public static HabitableSysUIController Instance;
        private Camera galaxyEventCamera;
        private StarSysController starSysController;
        //[SerializeField]
        //private Canvas parentCanvas;
        public GameObject HabitableSysUIToggle;
        [SerializeField]
        private TMP_Text sysCurrentOwnerNameTMP;

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
            // ToDo: 
            // add a Colonize Button in habitable system ui calls HabitableSysUIController.OnColonizeButtonClicked()
            // add a Cancel Button → calls HabitableSysUIController.OnCancelButtonClicked()
            // see methods below for reference on what they should do
            //if ((int)this.starSysController.StarSysData.CurrentOwnerCivEnum >= firstUninhabited)
            //{
            //    TimeManager.Instance.PauseTime();
            //    GameObject aNull = new GameObject();
            //    GalaxyMenuUIController.Instance.OpenMenu(Menu.HabitableSysMenu, aNull);
            //    Destroy(aNull);

            //    // ✅ DON'T claim immediately - wait for player to click "Colonize" button
            //    // ClamSystem(discoveringFleetCivController, starSysController); // ❌ Remove thisYou'll need buttons in your HabitableSysUI prefab:

            //    // ✅ Instead, show UI with colonization option
            //    ShowColonizationOptions(discoveringFleetCivController, starSysController);
            //}
        }

        private void ShowColonizationOptions(CivController civCon, StarSysController sysCon)
        {
            // Display system info
            sysCurrentOwnerNameTMP.text = $"Uninhabited System - {sysCon.StarSysData.SysName}";

            // Store references for later when player clicks "Colonize" button
            // You'll need to add a button to the UI that calls ClaimSystem()
        }

        // ✅ Make public so it can be called from a UI button
        public void ClaimSystem(CivController civCon, StarSysController sysCon)
        {
            // ✅ Add validation: Only claim if still uninhabited
            int firstUninhabited = (int)CivEnum.ZZUNINHABITED1;
            if ((int)sysCon.StarSysData.CurrentOwnerCivEnum < firstUninhabited)
            {
                Debug.LogWarning($"⚠️ System '{sysCon.name}' is already owned by {sysCon.StarSysData.CurrentOwnerCivEnum}!");
                return;
            }

            Debug.Log($"🏴 Claiming system '{sysCon.StarSysData.SysName}' for {civCon.CivData.CivShortName}");

            // Change ownership
            sysCon.StarSysData.CurrentOwnerCivEnum = civCon.CivData.CivEnum;
            sysCon.StarSysData.CurrentCivController = civCon;

            // Add to civilization's owned systems
            if (!civCon.CivData.StarSysWeOwn.Contains(sysCon))
            {
                civCon.CivData.StarSysWeOwn.Add(sysCon);
            }

            // Update UI
            sysCurrentOwnerNameTMP.text = civCon.CivData.CivShortName;

            // ✅ Create UI for the newly owned system (local player only)
            if (GameController.Instance.AreWeLocalPlayer(civCon.CivData.CivEnum))
            {
                StarSysManager.Instance?.InstantiateStarSysUI(sysCon);
                Debug.Log($"✅ Created UI for newly colonized system '{sysCon.name}'");
            }

            // Close habitable system UI
            CloseUnLoadHabitableSysUI();
            TimeManager.Instance.ResumeTime();
        }

        public void CloseUnLoadHabitableSysUI()
        {
            //SwitchToTab(0);
            HabitableSysUIToggle.SetActive(false);
            //TimeManager.Instance.ResumeTime();
        }
    }
}
