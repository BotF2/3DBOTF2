using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.UIElements;

namespace Assets.Core
{
    [RequireComponent(typeof(Toggle))]
    public class CombatUIController : MonoBehaviour
    {
        public static CombatUIController Instance;
        public CombatController CombatController; // this is the combat controller that will handle the combat UI and orders
        private Camera galaxyEventCamera; //will we need this?
        [SerializeField]
        private Canvas parentCanvas;
        [SerializeField]
        private GameObject combatOrdersUI;
        [SerializeField]
        private GameObject combatUI;
        [SerializeField]
        private GameObject combatUI_Prefab;// GameObject controlles this active UI on/off
        [SerializeField]
        private List<ShipController> friendShipControllers;
        [SerializeField]
        private List<ShipController> enemyShipControllers;
        [SerializeField]
        private List<GameObject> listOfShipsUiGos;
        public static Toggle Engage, Rush, Retreat, Formation, ProtectTransports, TargetTransports;
        public List<Toggle> toggleOrderList = new List<Toggle>() { Engage, Rush, Retreat, Formation, ProtectTransports, TargetTransports };
        private Toggle activeLocalPlayerToggle;
        private Toggle previousToggle;
        public static Orders order;


        private void Start()
        {
            previousToggle = toggleOrderList[0];
           // CombatManager.Instance.SetCombatOrder(Orders.Engage);
            //galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;
            //parentCanvas.worldCamera = galaxyEventCamera;
            //combatOrdersUI.SetActive(false);
            //combatUI.SetActive(false);
        }
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
        public void SetupShipUIData()
        { // populate the fleet UIs with the data from the fleetControllers...

            for (int j = 0; j < ShipManager.Instance.ShipControllerGameList.Count; j++)
            {
                var shipCon = ShipManager.Instance.ShipControllerGameList[j];
                //if (GameController.Instance.AreWeLocalPlayer(shipCon.ShipData.CivEnum))
                //{
                //    for (int i = 0; i < shipCon.FleetData.ShipsList.Count; i++)
                //    {
                //        if (shipCon.FleetData.ShipsList[i].ShipListUIGameObject != null)
                //        {
                //            var transforms = shipCon.FleetUIGameObject.transform.GetComponentsInChildren<Transform>();
                //            for (int k = 0; k < transforms.Length; k++)
                //            {
                //                if (transforms[k].gameObject.name == "ShipContent")
                //                {
                //                    shipContainer = transforms[k].gameObject;
                //                    break;
                //                }
                //            }
                //            shipCon.FleetData.ShipsList[i].ShipListUIGameObject.transform.SetParent(shipContainer.transform, false);
                //        }
                //    }
                //}
                //if (shipCon.FleetUIGameObject != null)
                //{
                //    shipCon.FleetUIGameObject.SetActive(true);
                //    shipCon.FleetUIGameObject.transform.SetParent(fleetListContainer.transform, false);
                //}
            }
        }
        public void ActivePlayerToggle(Toggle activeToggleOrder)
        {

            switch (activeToggleOrder.name.ToUpper())
            {
                //case "TOGGLE_ENGAGE":
                //    CombatManager.Instance.SetCombatOrder(Orders.Engage);
                //    order = Orders.Engage;
                //    Debug.Log("Active Engage.");
                //    break;
                //case "TOGGLE_RUSH":
                //    Debug.Log("Active Rush.");
                //    CombatManager.Instance.SetCombatOrder(Orders.Rush);
                //    order = Orders.Rush;
                //    break;
                //case "TOGGLE_RETREAT":
                //    Debug.Log("Active Retreat.");
                //    CombatManager.Instance.SetCombatOrder(Orders.Retreat);
                //    order = Orders.Retreat;
                //    break;
                //case "TOGGLE_FORMATION":
                //    Debug.Log("Active Formation.");
                //    CombatManager.Instance.SetCombatOrder(Orders.Formation);
                //    order = Orders.Formation;
                //    break;
                //case "TOGGLE_PROTECT_TRANSPORTS":
                //    Debug.Log("Active Protect Transports.");
                //    CombatManager.Instance.SetCombatOrder(Orders.ProtectTransports);
                //    order = Orders.ProtectTransports;
                //    break;
                //case "TOGGLE_TARGET_TRANSPORTS":
                //    Debug.Log("Active Target Transports.");
                //    CombatManager.Instance.SetCombatOrder(Orders.TargetTransports);
                //    order = Orders.TargetTransports;
                //    break;
                default:
                    break;
            }
        }

        private void OnToggleChanged(bool isOn)  
        {  
            if (isOn)  
            {  
                ActivePlayerToggle(activeLocalPlayerToggle);  
            }  
        }  

        public void OpenCombatUI(GameObject thisCombatUIGameObject)
        {
            RectTransform[] rectTransforms = thisCombatUIGameObject.GetComponentsInChildren<RectTransform>();
            for (int i = 0; i < rectTransforms.Length; i++)
            {
                switch (rectTransforms[i].name)
                {
                    case "Toggle_ENGAGE":
                        rectTransforms[i].gameObject.SetActive(true);
                        break;
                    case "Toggle_RUSH":
                        rectTransforms[i].gameObject.SetActive(true);
                        break;
                    case "Toggle_RETREAT":
                        rectTransforms[i].gameObject.SetActive(true);
                        break;
                    case "Toggle_FORMATION":
                        rectTransforms[i].gameObject.SetActive(true);
                        break;
                    case "Toggle_TARGET_TRANSPORTS":
                        rectTransforms[i].gameObject.SetActive(true);
                        break;
                    case "Tobble_PROTECT_TRANSPORTS":
                        rectTransforms[i].gameObject.SetActive(true);
                        break;
                    case "Load MainMenu":
                        rectTransforms[i].gameObject.SetActive(true);
                        break;
                    case "ButtonEnterCombat":
                        rectTransforms[i].gameObject.SetActive(true);
                        break;
                    default:
                        break;
                }
            }
            TextMeshProUGUI[] ourTMPs = thisCombatUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0; i < ourTMPs.Length; i++)
            {
                int techLevelInt = (int)CivManager.Instance.LocalPlayerCivContoller.CivData.TechLevel / 100; // Early Tech level = 100, Supreme = 900;
                ourTMPs[i].enabled = true;
                var name = ourTMPs[i].name;

                //switch (name)
                //{
                //    case "Text FleetName (TMP)":
                //        ourTMPs[i].text = fleetCon.FleetData.Name;
                //        break;
                //    case "Destination Name Text":
                //        ourTMPs[i].text = "No Destination";
                //        break;
                //    case "FleetMaxWarpFactor":
                //        ourTMPs[i].text = fleetCon.FleetData.MaxWarpFactor.ToString("0.0");
                //        break;
                //}
            }
            Toggle[] lisToggles = thisCombatUIGameObject.GetComponentsInChildren<Toggle>();
            foreach (var aToggle in lisToggles)
            {
                var combatCon = thisCombatUIGameObject.GetComponent<CombatController>();
                switch (aToggle.name)
                {
                    case "TOGGLE_ENGAGE":
                        aToggle.onValueChanged.RemoveAllListeners();
                        aToggle.onValueChanged.AddListener(OnToggleChanged);
                        //aToggle.onClick.AddListener(() => combatCon.SelectedDestinationCursor(fleetCon));
                        break;
                    //case "Cancel Destination Button":
                    //    aToggle.onClick.RemoveAllListeners();
                    //    aToggle.onClick.AddListener(() => fleetCon.ClickCancelDestinationButton(fleetCon));
                    //    break;
                    //case "DestinationDragTarget Button":
                    //    aToggle.onClick.RemoveAllListeners();
                    //    aToggle.onClick.AddListener(() => fleetCon.GetPlayerDefinedTargetDestination(fleetCon));
                    //    break;
                    //case "ButtonCloseFleetUI":
                    //    fleetCon.FleetData.FleetButtonUIClose = aToggle;
                    //    aToggle.onClick.RemoveAllListeners();
                    //    aToggle.onClick.AddListener(() => fleetCon.CloseUnLoadFleetUI());  //fleetCon));
                    //    break;
                    //case "ButtonShipManager":
                    //    // ToDo: open ship manager UI, instantiate prefab  similar to systems build menu with drag and drop
                    //    aToggle.onClick.RemoveAllListeners();
                    //    aToggle.onClick.AddListener(() => fleetCon.OnClickShipManager(fleetCon));  //fleetCon));
                    //    break;
                    default:
                        break;
                }
            }
            //for (int i = 0; i < fleetCon.FleetData.ShipsList.Count; i++)
            //{
            //    if (fleetCon.FleetData.ShipsList[i].ShipListUIGameObject != null)
            //    {
            //        var transforms = fleetCon.FleetUIGameObject.transform.GetComponentsInChildren<Transform>();
            //        for (int k = 0; k < transforms.Length; k++)
            //        {
            //            if (transforms[k].gameObject.name == "ShipContent")
            //            {
            //                shipContainer = transforms[k].gameObject;
            //                break;
            //            }
            //        }
            //        fleetCon.FleetData.ShipsList[i].ShipListUIGameObject.transform.SetParent(shipContainer.transform, false);
            //    }
            //}

            //if (fleetCon.FleetUIGameObject != null)
            //{
            //    fleetCon.FleetUIGameObject.SetActive(true);
            //    fleetCon.FleetUIGameObject.transform.SetParent(fleetListContainer.transform, false);

            //}
        }
    }
}

