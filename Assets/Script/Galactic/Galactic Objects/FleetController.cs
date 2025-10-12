using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Assets.Core
{
    /// <summary>
    /// Controlling fleet movement and interactions while the matching FeetData class
    /// holds key info on status and for save game
    /// </summary>
    public class FleetController : MonoBehaviour
    {
        //Fields
        private FleetManager _manager;
        private FleetData fleetData;
        public FleetData FleetData { get { return fleetData; } set { fleetData = value; } }
        public GameObject FleetUIGameObject; //The instantiated fleet UI for this fleet. a prefab clone, not a class but a game object
        public GameObject RightSideShipManagementFleetUIGO; 
        public string Name;
        public int intName = 1;
        private float warpFudgeFactor = 10f;
        private Rigidbody rb;
        public MapLineMovable DropLine;
        public MapLineMovable DestinationLine;
        public GameObject BackgroundGalaxyImage;
        [SerializeField]
        private List<string> shipDropdownOptions;
        private Camera galaxyEventCamera;
        private GameObject aNull = null; // used to pass a null object to the UI when needed in Diplomacy
        public Canvas FleetUICanvas { get; private set; }
        //public Canvas CanvasToolTip; // not used for now, see start method and in instantiation of fleetController in FleetManager.cs
        public PlayerDefinedTargetController TargetController;
        private Vector3 vectorOffset;
        private float ourZCoordinate;
        [SerializeField]
        private GameObject warpUpButtonGO;
        [SerializeField]
        private GameObject warpDownButtonGO;
        [SerializeField]
        private float warpChange = 0.1f;
        [SerializeField]
        private Slider warpSlider;
        [SerializeField]
        private TextMeshProUGUI warpSliderText;
        [SerializeField]
        private float maxSliderValue = 10f;
        [SerializeField]
        private List<ShipData> shipList;
        private TMP_Dropdown shipDropdown;
        [SerializeField]
        public GameObject ShipDropdownGO;
        [SerializeField]
        private TMP_Text dropdownShipText;
        [SerializeField]
        private TMP_Text FleetName;
        [SerializeField]
        private TextMeshProUGUI destinationName; 
        [SerializeField]
        private TextMeshProUGUI destinationCoordinates; 
        [SerializeField]
        private TMP_Text selectDestinationBttonText;
        internal int ownerId;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            var CanvasGO = GameObject.Find("CanvasGalaxyMenuRibbon");
            FleetUICanvas = CanvasGO.GetComponent<Canvas>();
            FleetUICanvas.worldCamera = galaxyEventCamera;
            if (FleetData != null && FleetData.ShipsList != null)
            { 
                for (int i = 0; i < FleetData.ShipsList.Count; i++)
                {
                    if (FleetData.ShipsList[i].ShipData.maxWarpFactor < this.FleetData.MaxWarpFactor)
                    { this.FleetData.MaxWarpFactor = FleetData.ShipsList[i].ShipData.maxWarpFactor; }
                }
            }
            DestinationLine = this.GetComponentInChildren<MapLineMovable>();
            DestinationLine.GetLineRenderer();
            DestinationLine.transform.SetParent(transform, false);
            if (FleetData != null && FleetData.Destination != null)
            {
                FleetData.Destination = FleetManager.Instance.GalaxyCenter;
            }

        }
        private void FixedUpdate()
        {
            // Destroying Fleets with no ships is problematic
            // The FleetController FeetData is still running in script
            // and if the player clicks on or OnTrigerEntere.... it causes errors
            //if (FleetData != null && FleetData.ShipsList.Count == 0)
            //{
            //    OnDestroy();
            //}   
            if (FleetData != null && FleetData.Destination != null)
            {
                if (FleetData.Destination != FleetManager.Instance.GalaxyCenter && this.FleetData.CurrentWarpFactor > 0f)
                {
                    MoveToDesitinationGO();
                    DrawDestinationLine(FleetData.Destination.transform.position);
                }
            }
        }
        public Rigidbody GetRigidbody() { return rb; }

        public void Init(FleetManager fleetManager)
        {
            _manager = fleetManager;
        }
        private void OnMouseDown()
        {
            Ray ray = galaxyEventCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            GameObject fleetGo = hit.collider.gameObject;
            if (fleetGo.CompareTag("GalaxyImage")) return;

            FleetController clickedFleetCon = fleetGo.GetComponentInChildren<FleetController>();
            var galaxyUI = GalaxyMenuUIController.Instance;

            switch (galaxyUI.CurrentClickMode)
            {
                case GalaxyClickMode.Normal:
                    HandleNormalClick(clickedFleetCon);
                    break;

                case GalaxyClickMode.SetDestination:
                    HandleDestinationClick(clickedFleetCon);
                    break;

                case GalaxyClickMode.SelectForShipExchange:
                    HandleShipExchangeClick(clickedFleetCon);
                    break;
            }
            //Ray ray = galaxyEventCamera.ScreenPointToRay(Input.mousePosition);
            //RaycastHit hit;

            //if (Physics.Raycast(ray, out hit))
            //{
            //    GameObject fleetGo = hit.collider.gameObject;
            //    if (fleetGo.tag != "GalaxyImage")
            //    {
            //        // What a fleet FleetController does with a click
            //        FleetController clickedFleetCon = fleetGo.GetComponentInChildren<FleetController>();
            //        if (GalaxyMenuUIController.Instance.MouseClickSetsDestination == false && GalaxyMenuUIController.Instance.MouseClickSellectsSysOrFleetForShipExchange == false) // the destination mouse pointer is off so open FleetUI for this FleetController
            //        {
            //            if (GameController.Instance.AreWeLocalPlayer(clickedFleetCon.FleetData.CivEnum))
            //            {
            //                GalaxyMenuUIController.Instance.OpenMenu(Menu.AFleetMenu, fleetGo);
            //            }
            //        }
            //        else if (GalaxyMenuUIController.Instance.MouseClickSetsDestination == true && clickedFleetCon == this)
            //        {
            //            FleetController theFleetConLookingForDestination = MousePointerChanger.Instance.fleetConBehindGalaxyMapDestinationCursor;
            //            theFleetConLookingForDestination.fleetData.Destination = this.gameObject; // set the destination of the clicker fleet as this fleet clicked on
            //            theFleetConLookingForDestination.SetAsDestinationInUI(fleetGo);
            //        }
            //        else if (GalaxyMenuUIController.Instance.MouseClickSellectsSysOrFleetForShipExchange == true && clickedFleetCon == this)
            //        {
            //            // load the right side fleet UI prefab into the AFleetMenuView
            //            GalaxyMenuUIController.Instance.LoadRightSideShipManagerFleetUIPrefab(fleetGo);
            //            MousePointerChanger.Instance.ResetCursor();
            //            GalaxyMenuUIController.Instance.activeFleetOrSystemControllerForShipExchange = this.RightSideShipManagementFleetUIGO;
            //        }
            //    }
            //}

        }
        private void HandleNormalClick(FleetController clickedFleetCon)
        {
            if (GameController.Instance.AreWeLocalPlayer(clickedFleetCon.FleetData.CivEnum))
            {
                GalaxyMenuUIController.Instance.OpenMenu(Menu.AFleetMenu, this.gameObject);
            }
        }

        private void HandleDestinationClick(FleetController clickedFleetCon)
        {
            if (clickedFleetCon != this) return;

            this.fleetData.Destination = gameObject;
            this.SetAsDestinationInUI(clickedFleetCon.gameObject);
        }

        private void HandleShipExchangeClick(FleetController clickedFleetCon)
        {
            if (clickedFleetCon != this) return;

            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.LoadRightSideShipManagerFleetUIPrefab(clickedFleetCon.gameObject);
            MousePointerChanger.Instance.ResetCursor();
            //galaxyUI.activeFleetOrSystemControllerForShipExchange = RightSideShipManagementFleetUIGO;
        }


        private void OnMouseDrag()
        {
            if (this.TargetController != null)
            {
                this.TargetController.gameObject.transform.position = GetMouseWorldPosition() + vectorOffset;
            }
        }
        void OnTriggerEnter(Collider collider) // Not using OnCollisionEnter....
        {
            if (FleetData != null)
            {
                bool weAreLocalPlayer = GameController.Instance.AreWeLocalPlayer(this.FleetData.CivEnum);

                bool isOurDestination = false;
                if (this.FleetData.Destination == collider.gameObject) // it is our destination
                {
                    isOurDestination = true;
                    if (weAreLocalPlayer)
                    {
                        CloseUnLoadFleetUI(); // we are there and have other things to do
                    }
                }

                if (collider.gameObject.TryGetComponent(out FleetController hitFleetCon))
                {
                    if (isOurDestination)
                    {
                        ClickCancelDestinationButton();// we stop, cancel destination

                        if (FleetData.CivEnum != hitFleetCon.FleetData.CivEnum)//if not one of ours
                        {
                            OnADestinationThatIsOtherCivFleet(hitFleetCon);

                            DiplomacyManager.Instance.FleetControllerVsOtherCivFleet(this, hitFleetCon);
                            //ToDo: resolve an encounter with galaxy object that does not have a civ, black hole, wormhole, trans-warp hub, etc
                            EncounterUnknownFleetGetNameAndSprite(collider.gameObject); // set active sprite and name

                            if (hitFleetCon.FleetData.Destination == this.gameObject) // they are coming for us
                            {
                                ClickCancelDestinationButton(); // they stop

                                CloseUnLoadFleetUI(); // need more code to handle this encounter 
                            }

                        }
                        else //our fleet
                        {
                            // do ships management?
                            OnADestinationThatIsOurOtherFleet(hitFleetCon); // we are the same civ fleets, do ships?
                        }
                    }
                    else
                    {
                        // not our destination ignore for now
                    }
                }
                else if (collider.gameObject.TryGetComponent(out StarSysController sysCon)) // only the fleetController reports a collision for now, not the system
                {
                    if (isOurDestination)
                    {
                        ClickCancelDestinationButton(); // we stop, cancel destination

                        if (this.FleetData.CivEnum != sysCon.StarSysData.CurrentOwnerCivEnum) // not our system
                        {
                            if (weAreLocalPlayer)
                            {
                                EncounterUnknownSystemShowName(collider.gameObject); // update Galaxy view to expose insignia/name
                            }
                            OnEnterForeignStarSystem(); // ToDo
                            DiplomacyManager.Instance.ResolveEncounterOtherCivSystem(this, sysCon);

                        }
                        else // ToDo: enter our system
                        {

                        }
                    }
                    else
                    {
                        // not our destination ignore for now
                    }
                }
                else if (collider.gameObject.TryGetComponent(out PlayerDefinedTargetController freddy))
                {
                    if (isOurDestination)
                    {
                        ClickCancelDestinationButton(); // we stop, cancel destination
                        Destroy(collider.gameObject); // remove the player defined target
                    }
                }
            }

        }
        private Vector3 GetMouseWorldPosition()
        {
            // pixel coordinates (x,y)
            Vector3 mousePoint = Input.mousePosition;

            //z coordinate of game object on screen
            mousePoint.z = ourZCoordinate;

            return galaxyEventCamera.ScreenToWorldPoint(mousePoint);
        }
        private void OnRemoveDestination(GameObject destination, int destinationInt) // for the C# event system
        {
            if (destination == this.FleetData.Destination)
            {
                // not implemented, looking for a good use case
            }
        }
        private void NewDestination(GameObject hitObject) // here is a destination
        {

            DestinationLine.gameObject.SetActive(true);
            DestinationLine.lineRenderer.gameObject.SetActive(true);
            DestinationLine.lineRenderer.enabled = true;
            DestinationLine.lineRenderer.startColor = Color.blue;
            DestinationLine.lineRenderer.endColor = Color.red;
            // turn off cursor of destination

            MousePointerChanger.Instance.ResetCursor(); // reset to default cursor because we just got to destination
            //MousePointerChanger.Instance.HaveGalaxyMapCursor = false;
            SetAsDestinationInUI(hitObject);

        }
        public void PlayerTargetAsNewDestination(GameObject destinationGo)
        {
            this.FleetData.Destination = destinationGo;
        }


        private void EncounterUnknownSystemShowName(GameObject hitGO)
        {
            var sysData = hitGO.GetComponent<StarSysController>().StarSysData;

            StarSysManager.Instance.ExposeAllSystemName(sysData.CurrentOwnerCivEnum);
            FleetManager.Instance.ExposeAllFleetInsigniaSprites(sysData.CurrentOwnerCivEnum);

        }
        private void EncounterUnknownFleetGetNameAndSprite(GameObject hitGO)
        {
            var fleetData = hitGO.GetComponent<FleetController>().FleetData;
            StarSysManager.Instance.ExposeAllSystemName(fleetData.CivEnum);
            FleetManager.Instance.ExposeAllFleetInsigniaSprites(fleetData.CivEnum);
        }

        void MoveToDesitinationGO()
        {
            Vector3 direction = (this.FleetData.Destination.transform.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, this.FleetData.Destination.transform.position);
            if (this.FleetData.CurrentWarpFactor > this.FleetData.MaxWarpFactor)
            {
                this.FleetData.CurrentWarpFactor = this.FleetData.MaxWarpFactor;
            }
            Vector3 nextPosition = Vector3.MoveTowards(rb.position, FleetData.Destination.transform.position,
            FleetData.CurrentWarpFactor * warpFudgeFactor * Time.fixedDeltaTime);
            rb.MovePosition(nextPosition); // kinematic with physics movement
            this.FleetData.Position = nextPosition;
            Vector3 galaxyPlanePoint = new Vector3(rb.position.x, -60f, rb.position.z);
            Vector3[] points = { rb.position, galaxyPlanePoint };
            DropLine.SetUpLine(points);
        }
        void DrawDestinationLine(Vector3 destinationPoint)
        {
            if (DestinationLine != null) { }
            else
            {
                DestinationLine = this.GetComponentInChildren<MapLineMovable>();
                DestinationLine.GetLineRenderer();
                DestinationLine.transform.SetParent(transform, false);
                DestinationLine.enabled = true;
            }

            Vector3[] points = { transform.position, destinationPoint };
            DestinationLine.gameObject.SetActive(true);
            DestinationLine.lineRenderer.startColor = Color.blue;
            DestinationLine.lineRenderer.endColor = Color.red;
            DestinationLine.SetUpLine(points);
        }
        void OnADestinationThatIsOtherCivFleet(FleetController theirFleetCon)
        {
            // Fleet only Logic to handle what happens when our fleet arrives at their fleet destination
            //GalaxyMenuUIController.Instance.ClickCancelDestinationButton(); 
        }
        void OnADestinationThatIsOurOtherFleet(FleetController ourOtherFleet)
        {
            // Logic to handle what happens when the fleet arrives at our other fleet as destination
            // how do we manage both fleets trying to do something with the other fleet?
        }
        void OnADestinationThatIsPlayerTarget()
        {
            // Logic to handle what happens when the fleet arrives at the system destination
            //GalaxyMenuUIController.Instance.ClickCancelDestinationButton(); 
        }
        void OnEnterForeignStarSystem()
        {
            // do something
        }
        public void AddToShipList(ShipController shipController)
        {
            foreach (var ShipData in this.FleetData.GetShipList())
                FleetData.AddToShipList(shipController);
            UpdateMaxWarp();
        }
        public void RemoveFromShipList(ShipController shipController)
        {
            this.FleetData.RemoveFromShipList(shipController);
            UpdateMaxWarp();
        }
        public void UpdateMaxWarp()
        {
            float maxWarp = 10f;
            for (int i = 0; i < fleetData.ShipsList.Count; i++)
            { // find the slowest ship
                if (fleetData.ShipsList[i].ShipData.maxWarpFactor < maxWarp)
                {
                    maxWarp = fleetData.ShipsList[i].ShipData.maxWarpFactor;
                }
            }
            fleetData.MaxWarpFactor = maxWarp;
            if (GalaxyMenuUIController.Instance != null)
                GalaxyMenuUIController.Instance.UpdateFleetMaxWarpUI(this, maxWarp);
        }
        public void DestroyFleet(FleetData fleetData, GameObject fleetGO)
        {
            FleetManager.Instance.RemoveFleetInt(fleetData.CivEnum, fleetData.FleetInt);
            if (FleetManager.Instance.FleetControllerList.Contains(this))
            {
                FleetManager.Instance.FleetControllerList.Remove(this);
                Destroy(fleetGO.gameObject);
               // TimeManager.Instance.ResumeTime();
            }
        }

        public void FleetOnWarpUpClick(FleetController fleetCon)
        {
            if (this == fleetCon)
            {
                warpChange = 0.1f;
            }
            if (fleetCon.FleetData.CurrentWarpFactor + warpChange > fleetCon.FleetData.MaxWarpFactor)
            {
                warpChange = 0f;
                return;
            }
            SliderOnValueChange(fleetCon.FleetData.CurrentWarpFactor + warpChange); // this called method updates the UI too
        }
        public void FleetOnWarpDownClick(FleetController fleetCon)
        {
            if (this == fleetCon)
            {
                warpChange = -0.1f;
            }
            if (fleetCon.FleetData.CurrentWarpFactor - warpChange < 0f)
            {
                warpChange = 0f;
                return;
            }
            SliderOnValueChange(fleetCon.FleetData.CurrentWarpFactor + warpChange);
        }

        public void SliderOnValueChange(float newWarpValue)
        {
            float maxSliderValue = this.FleetData.MaxWarpFactor;

            if (newWarpValue < 0f)
            {
                newWarpValue = 0f;
            }
            if (newWarpValue > maxSliderValue)
            {
                newWarpValue = maxSliderValue;
            }

            FleetData.CurrentWarpFactor = newWarpValue;
            GalaxyMenuUIController.Instance.UpdateFleetWarpUI(this, newWarpValue);
        }
        public void SelectedShipManageCursor()
        {
            //if (GalaxyMenuUIController.Instance.CurrentClickMode != GalaxyClickMode.SelectForShipExchange)
            //{
                GalaxyMenuUIController.Instance.BeginShipExchange();
                GalaxyMenuUIController.Instance.SetClickMode(GalaxyClickMode.SelectForShipExchange);
                MousePointerChanger.Instance.SetShipExchangeCursor();
            //}
        }
        public void ClickCancelShipManageButton()
        {
            GalaxyMenuUIController.Instance.ClickCancelShipManageButton();
            GalaxyMenuUIController.Instance.ResetClickMode();
            //GalaxyMenuUIController.Instance.activeFleetOrSystemControllerForShipExchange = null;
            MousePointerChanger.Instance.ResetCursor();
        }
        public void SelectedDestinationCursor(FleetController fleetCon)
        {
            //if (GalaxyMenuUIController.Instance.CurrentClickMode != GalaxyClickMode.SetDestination)
            //{
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.BeginSetDestination(fleetCon);
            galaxyUI.SetClickMode(GalaxyClickMode.SetDestination);
            galaxyUI.SelectedDestinationCursor(this);
            MousePointerChanger.Instance.SetDestinationCursor();//ChangeToGalaxyMapCursorForLocalPlayer(fleetCon);
               // MousePointerChanger.Instance.HaveGalaxyMapCursor = true;
            //}
        }
        public void ClickCancelDestinationButton()
        {
            DestinationLine.gameObject.SetActive(false);
            FleetData.LastDestination = FleetData.Destination; // save previous destination if we want to continue later 
            FleetData.Destination = FleetManager.Instance.GalaxyCenter;
            FleetData.CurrentWarpFactor = 0f; // stop the fleet
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.CompleteSetDestination();
            galaxyUI.ClickCancelDestinationButton(this);
            galaxyUI.SetClickMode(GalaxyClickMode.Normal);
        }

        public void SetAsDestinationInUI(GameObject hitObject)
        {

            //FleetData.Destination = hitObject;           
            int typeOfDestination = -1;// galaxy object type Enum SystemType if =>1
            string destinationNameText = "";
            //destinationCoordinates.text
            string coordiantesText = "X " + (hitObject.transform.position.x).ToString()
                + " / Y " + (hitObject.transform.position.y).ToString()
                + " / Z " + (hitObject.transform.position.z).ToString();
            if (hitObject.GetComponent<StarSysController>() != null)
            {
                StarSysController starSysController = hitObject.GetComponent<StarSysController>();

                if (DiplomacyManager.Instance.FoundADiplomacyController(CivManager.Instance.LocalPlayerCivContoller, starSysController.StarSysData.CurrentCivController))
                {
                    typeOfDestination = -1;
                    destinationNameText += starSysController.StarSysData.SysName;
                }
                else // unknown system
                {
                    typeOfDestination = (int)starSysController.StarSysData.SystemType;
                }
            }
            else if (hitObject.GetComponent<FleetController>() != null)
            {
                FleetController fleetCon = hitObject.GetComponent<FleetController>();

                if (DiplomacyManager.Instance.FoundADiplomacyController(CivManager.Instance.LocalPlayerCivContoller, fleetCon.FleetData.CivController))
                {
                    typeOfDestination = -1;
                    //destinationName.text
                    destinationNameText = fleetCon.FleetData.Name;
                }
                else // unknown fleet
                {
                    typeOfDestination = (int)GalaxyObjectType.UnknownFleet;
                }
            }
            switch (typeOfDestination)
            {
                case -1:
                    break;
                case (int)GalaxyObjectType.BlueStar:
                    destinationNameText = "Blue Star at";
                    break;
                case (int)GalaxyObjectType.WhiteStar:
                    destinationNameText = "White Star at";
                    break;
                case (int)GalaxyObjectType.YellowStar:
                    destinationNameText = "Yellow Star at";
                    break;
                case (int)GalaxyObjectType.OrangeStar:
                    destinationNameText = "Orange Star at";
                    break;
                case (int)GalaxyObjectType.RedStar:
                    destinationNameText = "Red Star at";
                    break;
                case (int)GalaxyObjectType.Nebula:
                case (int)GalaxyObjectType.OmarianNebula:
                case (int)GalaxyObjectType.OrionNebula:
                    destinationNameText = "Nebula at";
                    break;
                case (int)GalaxyObjectType.Station:
                    destinationNameText = "Station at";
                    break;
                case (int)GalaxyObjectType.BlackHole:
                    destinationNameText = "Black Hole at";
                    break;
                case (int)GalaxyObjectType.WormHole:
                    destinationNameText = "WormHole at";
                    break;
                case (int)GalaxyObjectType.TargetDestination:
                    destinationNameText = "Target at";
                    break;
                case (int)GalaxyObjectType.UnknownFleet:
                    destinationNameText = "Fleet at";
                    break;
                default:
                    destinationName.text = "";
                    break;

            }
            GalaxyMenuUIController.Instance.SetAsDestination(destinationNameText, coordiantesText);
        }

        public void GetPlayerDefinedTargetDestination(FleetController fleetCon)
        {
            if (this == fleetCon)
            {
                GalaxyMenuUIController.Instance.SetAsDestination("Drag target to", "your destination");
                PlayerDefinedTargetManager.Instance.PlayerTargetFromData(gameObject);
                GalaxyMenuUIController.Instance.GetPlayerDefinedTargetDestination(this);    
            }
        }
        public void OnClickShipManager(FleetController fleetCon)
        {
            if (fleetCon == this && fleetCon == GameController.Instance.AreWeLocalPlayer(FleetData.CivEnum))
            {
                GameObject notAMenu = new GameObject();
                GalaxyMenuUIController.Instance.OpenMenu(Menu.ShipsMenu, notAMenu);
                Destroy(notAMenu);
            }
        }

        private void ReorderDropdownOptions(TMP_Dropdown dropdown)
        {
            List<TMP_Dropdown.OptionData> options = dropdown.options;
            options.Reverse();
            // Update the UI
            dropdown.RefreshShownValue();
        }
        public void CloseUnLoadFleetUI()
        {
            GalaxyMenuUIController.Instance.ResetClickMode();
            MousePointerChanger.Instance.ResetCursor();
            GalaxyMenuUIController.Instance.CloseMenu(Menu.AFleetMenu); // The single fleet UI
            GalaxyMenuUIController.Instance.CloseMenu(Menu.ShipsMenu);
        }
        private string GetDebuggerDisplay()
        {
            return ToString();
        }

        internal void RemoveShipFromFleet(ShipController shipController)
        {
            this.FleetData.ShipsList.Remove(shipController);
            if (this.FleetData.ShipsList.Count == 0)
            {
                // no ships left, remove fleet
                FleetManager.Instance.RemoveFleetInt(this.FleetData.CivEnum, this.FleetData.FleetInt);
                FleetData.ShipsList.Remove(shipController);
                //Destroy(this.gameObject);
            }
            else
            {
                UpdateMaxWarp();
            }
        }
        public void IsTheFleetDestroyed()
        {
            if (this.FleetData.ShipsList.Count == 0)
            {
                OnDestroy();
            }
        }
        private void OnDestroy()
        {
            //StopAllCoroutines();
            if (this.FleetData != null && FleetData.FleetInt != null)
            { 
                FleetManager.Instance.RemoveFleetInt(this.FleetData.CivEnum, this.FleetData.FleetInt);
                if (FleetManager.Instance.FleetControllerList.Contains(this))
                {
                    FleetManager.Instance.FleetControllerList.Remove(this);
                    Destroy(this.gameObject);
                    //TimeManager.Instance.ResumeTime();
                }
            }
        }
    }
}

