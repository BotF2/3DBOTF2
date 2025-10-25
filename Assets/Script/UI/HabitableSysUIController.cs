using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Core;
using TMPro;
using UnityEngine.UI;

public class HabitableSysUIController: MonoBehaviour
{
    public static HabitableSysUIController Instance;
    private Camera galaxyEventCamera;
    private StarSysController starSysController;
    [SerializeField]
    private Canvas parentCanvas;
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
        galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;
        parentCanvas.worldCamera = galaxyEventCamera;
    }
    public void LoadHabitableSysUI(StarSysController starSysController, CivController discoveringFleetCivController)
    {
        int firstUninhabited = (int)CivEnum.ZZUNINHABITED1;
        this.starSysController = starSysController;
        if ((int)this.starSysController.StarSysData.CurrentOwnerCivEnum >= firstUninhabited) // not already been clamed
        {
            TimeManager.Instance.PauseTime(); // ToDo: put a pause indicator on screen
                                              //ToDo: manage open UIs so we keep a UI with interaction pending when a fleet reaches a new target and player need more than one UI still open
            GameObject aNull = new GameObject();
            GalaxyMenuUIController.Instance.OpenMenu(Menu.HabitableSysMenu, aNull, null);
            //YourStarSysUIManager.Instance.CloseUnLoadStarSysUI();
            //FleetUIController.Instance.CloseDestinationSelectionCursor();
            ////FleetSelectionUI.current.UnLoadShipManagerUI();
            //FirstContactUIController.Instance.CloseUnLoadDiplomacyUI();
            //HabitableSysUIToggle.SetActive(true);
           // visitingFleetCivEnum = discoveringFleetCivController.CivData.CivEnum;
            Destroy(aNull);
            ClamSystem(discoveringFleetCivController, starSysController);
        }
    }
    public void CloseUnLoadHabitableSysUI()
    {
        //SwitchToTab(0);
        HabitableSysUIToggle.SetActive(false);
        //TimeManager.Instance.ResumeTime();
    }
    private void ClamSystem(CivController civCon, StarSysController sysCon)
    {
        sysCon.StarSysData.CurrentOwnerCivEnum = civCon.CivData.CivEnum;
  
        civCon.CivData.StarSysOwned.Add(starSysController);
        sysCurrentOwnerNameTMP.text = civCon.CivData.CivShortName;
        starSysController.StarSysData.CurrentCivController = civCon;
    }
}
