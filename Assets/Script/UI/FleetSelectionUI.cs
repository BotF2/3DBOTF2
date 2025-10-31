using Assets.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
/// <summary>
/// This is inteneded as a UI to manage all your fleets in one location.
/// ToDo: make it work
/// </summary>
public class FleetSelectionUI : MonoBehaviour
{

    public static FleetSelectionUI Instance;
    public FleetController clickedFleetController;
    public Canvas parentCanvas;
    [SerializeField]
    private GameObject FleetsManagerUIToggle;
    public List<ShipController> shipControllerList;

    public GameObject fleetGroupDropdownGO;
    [SerializeField]
    private TMP_Dropdown fleetGroupDropdown;
    [SerializeField]
    private TMP_Text CivName;
    private Camera galaxyEventCamera;

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
        FleetsManagerUIToggle.SetActive(false);
        galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;
        parentCanvas.worldCamera = galaxyEventCamera;
    }


    public void LoadShipUIManager(FleetController fleetController)
    {
        GameObject aNull = new GameObject();
        GalaxyMenuUIController.Instance.OpenMenu(Menu.FirstContactMenu, aNull);
        //YourStarSysUIManager.Instance.CloseUnLoadStarSysUI();
        //FleetUIController.Instance.CloseDestinationSelectionCursor();
        FleetsManagerUIToggle.SetActive(true);
        clickedFleetController = fleetController;
        CivName.text = clickedFleetController.FleetData.CivLongName;
        Destroy(aNull);
    }

    public void UnLoadShipManagerUI()
    {
        FleetsManagerUIToggle.SetActive(false);
    }


}
