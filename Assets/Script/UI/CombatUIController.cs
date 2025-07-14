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
        public CivEnum sideOneEnum;
        public CivEnum sideTwoEnum;
        public CivEnum CivEnumLocalPlayer; // the local player enum, used to determine which side the local player is on
        public GameObject CombatOrdersUI;
        public int negIsSideOnePosIsSideTwo = 1; // used to determine which side the local player is on, -1 for side one, 1 for side two
        [SerializeField]
        private List<ShipController> sideOneShipControllers; // mainly local player vs not local player
        [SerializeField]
        private List<ShipController> sideTwoShipControllers;
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
            CivEnumLocalPlayer = GameController.Instance.GameData.LocalPlayerCivEnum; // get the local player civ enum from the game controller
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
        private void SetSideOneOrderOrSideTwo()
        {
            if (sideOneShipControllers.Count > 0)
            {
                if (CivEnumLocalPlayer == sideOneShipControllers[0].ShipData.CivEnum)
                {
                    negIsSideOnePosIsSideTwo = -1; // local player is on side one
                }
            }
        }
        public void ActivePlayerToggle(Toggle activeToggle)
        {
            switch (activeToggle.name.ToUpper())
            {
                case "TOGGLE_ENGAGE":
                    CombatController.SetCombatOrder(Orders.Engage); // wait for enter combat to act on the chosen order
                    //CombatController.ActOnCombatOrders(order);
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

        public void OpenCombatUI(GameObject thisCombatUIGameObject)
        {
            RectTransform[] rectTransforms = thisCombatUIGameObject.GetComponentsInChildren<RectTransform>();
            for (int i = 0; i < rectTransforms.Length; i++)
            {
                switch (rectTransforms[i].name)
                {
                    case "PanelCombat_Menu":
                        CombatOrdersUI = rectTransforms[i].gameObject;
                        CombatOrdersUI.SetActive(true);
                        
                        break;
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
            Button[] ArrayButtons = thisCombatUIGameObject.GetComponentsInChildren<Button>();
            foreach (var button in ArrayButtons)
            {
                switch (button.name)
                {
                    case "ButtonEnterCombat":
                        button.onClick.RemoveAllListeners();
                        button.onClick.AddListener(EnterShipCombatPhase);
                        break;
                    default:
                        break;
                }
            }
        }
        // Add the missing method definition for OpenCombatUI to resolve the CS0103 error.
        private void EnterShipCombatPhase()
        {
            CombatOrdersUI.SetActive(false);
            CombatManager.Instance.CombatShipCanvas.SetActive(true);
            ActOnCombatOrders(order);
            Debug.Log("Combat UI opened.");
        }
        private void ActOnCombatOrders(Orders order)
        {
            SetSideOneOrderOrSideTwo();
            List<ShipController> shipControllers;
            if (negIsSideOnePosIsSideTwo < 0)
            {
                shipControllers = sideOneShipControllers;
            }
            else
            {
                shipControllers = sideTwoShipControllers;
            }
            // This method will handle the combat orders based on the selected order
            switch (order)
            {
                case Orders.Engage:
                    CombatController.SetThisCombatOrder(Orders.Engage, CivEnumLocalPlayer);
                    CombatController.ActOnCombatOrders(shipControllers, negIsSideOnePosIsSideTwo);
                    Debug.Log("Engaging in combat.");
                    break;
                case Orders.Rush:
                    CombatController.SetThisCombatOrder(Orders.Rush, CivEnumLocalPlayer);
                    CombatController.ActOnCombatOrders(shipControllers, negIsSideOnePosIsSideTwo);
                    Debug.Log("Rushing towards the enemy.");
                    break;
                case Orders.Retreat:
                    CombatController.SetThisCombatOrder(Orders.Retreat, CivEnumLocalPlayer);
                    CombatController.ActOnCombatOrders(shipControllers, negIsSideOnePosIsSideTwo);
                    Debug.Log("Retreating from combat.");
                    break;
                case Orders.Formation:
                    CombatController.SetThisCombatOrder(Orders.Formation, CivEnumLocalPlayer);
                    CombatController.ActOnCombatOrders(shipControllers, negIsSideOnePosIsSideTwo);
                    Debug.Log("Forming a combat formation.");
                    break;
                case Orders.TargetTransports:
                    CombatController.SetThisCombatOrder(Orders.TargetTransports, CivEnumLocalPlayer);
                    CombatController.ActOnCombatOrders(shipControllers, negIsSideOnePosIsSideTwo);
                    Debug.Log("Targeting enemy transports.");
                    break;
                default:
                    CombatController.SetThisCombatOrder(Orders.Engage, CivEnumLocalPlayer);
                    CombatController.ActOnCombatOrders(shipControllers, negIsSideOnePosIsSideTwo);
                    Debug.Log("Unknown order.");
                    break;
            }
        }
    }
}

