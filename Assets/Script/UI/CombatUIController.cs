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
        public Enum sideOneEnum;
        public Enum sideTwoEnum;
        private Camera galaxyEventCamera; //will we need this?
        //public CombatData CombatData;
        [SerializeField]
        private Canvas parentCanvas;
        [SerializeField]
        private GameObject combatOrdersUI;
        [SerializeField]
        private GameObject combatUI;
        //[SerializeField]
        //private GameObject combatUI_Prefab;// GameObject controlles this active UI on/off
        [SerializeField]
        private List<ShipController> friendShipControllers;
        [SerializeField]
        private List<ShipController> enemyShipControllers;
        [SerializeField]
        private List<GameObject> listOfShipsUiGos;
        public static Toggle Engage, Rush, Retreat, Formation, TargetTransports;
        public List<Toggle> toggleOrderList = new List<Toggle>() { Engage, Rush, Retreat, Formation, TargetTransports };
        private Toggle activeLocalPlayerToggle;
        private Toggle previousToggle;
        public static Orders order; // think this should be in the CombatController, but we will see how it goes

        private void Start()
        {
            previousToggle = toggleOrderList[0];
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
        public void ActivePlayerToggle(Toggle activeToggle)
        {
            switch (activeToggle.name.ToUpper())
            {
                case "TOGGLE_ENGAGE":
                    CombatController.SetCombatOrder(Orders.Engage);
                    Debug.Log("Active Engage.");
                    break;
                case "TOGGLE_RUSH":
                    Debug.Log("Active Rush.");
                    CombatController.SetCombatOrder(Orders.Rush);
                    break;
                case "TOGGLE_RETREAT":
                    Debug.Log("Active Retreat.");
                    CombatController.SetCombatOrder(Orders.Retreat);
                    break;
                case "TOGGLE_FORMATION":
                    Debug.Log("Active Formation.");
                    CombatController.SetCombatOrder(Orders.Formation);
                    break;
                case "TOGGLE_TARGET_TRANSPORTS":
                    Debug.Log("Active Target Transports.");
                    CombatController.SetCombatOrder(Orders.TargetTransports);
                    break;
                default:
                    break;
            }
        }
        private void OnToggleENGAGE(bool isOn)
        {  
            if (Engage.isOn)  
            {  
                if (previousToggle != Engage)  
                {  
                    previousToggle.isOn = false;  
                }  
                previousToggle = Engage;  
                ActivePlayerToggle(Engage);  
            }
        }
        private void OnToggleRUSH(bool isOn)
        {
            if (Rush.isOn)
            {
                if (previousToggle != Rush)
                {
                    previousToggle.isOn = false;
                }
                previousToggle = Rush;
                ActivePlayerToggle(Rush);
            }
        }
        private void OnToggleRETREAT(bool isOn)
        {
            if (Retreat.isOn)
            {
                if (previousToggle != Retreat)
                {
                    previousToggle.isOn = false;
                }
                previousToggle = Retreat;
                ActivePlayerToggle(Retreat);
            }
        }
        private void OnToggleFORMATION(bool isOn)
        {
            if (Formation.isOn)
            {
                if (previousToggle != Formation)
                {
                    previousToggle.isOn = false;
                }
                previousToggle = Formation;
                ActivePlayerToggle(Formation);
            }
        }
        private void OnToggleTARGET_TRANSPORTS(bool isOn)
        {
            if (TargetTransports.isOn)
            {
                if (previousToggle != TargetTransports)
                {
                    previousToggle.isOn = false;
                }
                previousToggle = TargetTransports;
                ActivePlayerToggle(TargetTransports  );
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
                        CombatController.IssueCombatOrder(Orders.Engage, true);
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
            Toggle[] ArrayToggles = thisCombatUIGameObject.GetComponentsInChildren<Toggle>();
            foreach (var aToggle in ArrayToggles)
            {
                CombatController = thisCombatUIGameObject.GetComponent<CombatController>();
                switch (aToggle.name)
                {
                    case "Toggle_ENGAGE":
                        Engage = aToggle;
                        Engage.onValueChanged.RemoveAllListeners();
                        Engage.onValueChanged.AddListener(OnToggleENGAGE);
                        break;
                    case "Toggle_RUSH":
                        Rush = aToggle;
                        Rush.onValueChanged.RemoveAllListeners();
                        Rush.onValueChanged.AddListener(OnToggleRUSH);
                        break;
                    case "Toggle_RETREAT":
                        Retreat = aToggle;
                        Retreat.onValueChanged.RemoveAllListeners();
                        Retreat.onValueChanged.AddListener(OnToggleRETREAT);
                        break;
                    case "Toggle_FORMATION":
                        Formation = aToggle;
                        Formation.onValueChanged.RemoveAllListeners();
                        Formation.onValueChanged.AddListener(OnToggleFORMATION);
                        break;
                    case "Toggle_TARGET_TRANSPORTS":
                        TargetTransports = aToggle;
                        TargetTransports.onValueChanged.RemoveAllListeners();
                        TargetTransports.onValueChanged.AddListener(OnToggleTARGET_TRANSPORTS);
                        break;
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

