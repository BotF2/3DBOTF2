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
        [SerializeField]
        private GameObject fleetUIGameObject;
        public GameObject FleetUIGameObject; //The instantiated fleet UI for this fleet. a prefab clone, not a class but a game object
        // instantiated by FleetManager from a prefab and added to FleetController
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
        public Canvas CanvasToolTip;
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
        private TextMeshProUGUI destinationName; // = new TextMeshProUGUI();
        [SerializeField]
        private TextMeshProUGUI destinationCoordinates; // = new TextMeshProUGUI();
        [SerializeField]
        private TMP_Text selectDestinationBttonText;
        

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            var CanvasGO = GameObject.Find("CanvasGalaxyMenuRibbon");
            FleetUICanvas = CanvasGO.GetComponent<Canvas>();
            FleetUICanvas.worldCamera = galaxyEventCamera;
            CanvasToolTip.worldCamera = galaxyEventCamera;
            FleetData.CurrentWarpFactor = 0f;
            for (int i = 0; i < FleetData.ShipsList.Count; i++)
            {
                if (FleetData.ShipsList[i].ShipData.maxWarpFactor < this.FleetData.MaxWarpFactor)
                { this.FleetData.MaxWarpFactor = FleetData.ShipsList[i].ShipData.maxWarpFactor; }
            }
            DestinationLine = this.GetComponentInChildren<MapLineMovable>();
            DestinationLine.GetLineRenderer();
            DestinationLine.transform.SetParent(transform, false);
            FleetData.Destination = FleetManager.Instance.GalaxyCenter;
            destinationCoordinates = new TextMeshProUGUI();
            destinationName = new TextMeshProUGUI();
        }
        void Update()
        {

        }

        private void FixedUpdate()
        {
            if (FleetData.Destination != FleetManager.Instance.GalaxyCenter && this.FleetData.CurrentWarpFactor > 0f)
            {
                MoveToDesitinationGO();
                DrawDestinationLine(FleetData.Destination.transform.position);
            }
        }
        public Rigidbody GetRigidbody() { return rb; }

        public void Init(FleetManager fleetManager)
        {
            _manager = fleetManager;
            //rb = GetComponent<Rigidbody>();
            //rb.isKinematic = true;
            //galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            //var CanvasGO = GameObject.Find("CanvasGalaxyMenuRibbon");
            //FleetUICanvas = CanvasGO.GetComponent<Canvas>();
            //FleetUICanvas.worldCamera = galaxyEventCamera;
            //CanvasToolTip.worldCamera = galaxyEventCamera;
        }
        private void OnMouseDown()
        {
            Ray ray = galaxyEventCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject fleetGo = hit.collider.gameObject;
                if (fleetGo.tag != "GalaxyImage")
                {
                    // What a fleet FleetController does with a hit
                    FleetController clickedFleetCon = fleetGo.GetComponentInChildren<FleetController>();
                    if (GalaxyMenuUIController.Instance.MouseClickSetsDestination == false) // the destination mouse pointer is off so open FleetUI for this FleetController
                    {
                        if (GameController.Instance.AreWeLocalPlayer(clickedFleetCon.FleetData.CivEnum))
                        {
                            GalaxyMenuUIController.Instance.OpenMenu(Menu.AFleetMenu, fleetGo);
                        }
                    }
                    else if (GalaxyMenuUIController.Instance.MouseClickSetsDestination == true && clickedFleetCon == this)
                    {
                        FleetController theFleetConLookingForDestination = MousePointerChanger.Instance.fleetConBehindGalaxyMapDestinationCursor;
                        theFleetConLookingForDestination.FleetData.Destination = fleetGo; // set the destination for the fleet looking for a destination
                        theFleetConLookingForDestination.SetAsDestinationInUI(fleetGo);
                    }
                }
            }

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

            if (collider.gameObject.GetComponent<FleetController>() != null)
            {
                FleetController hitFleetCon = collider.gameObject.GetComponent<FleetController>();

                if (isOurDestination)
                {
                    ClickCancelDestinationButton(this);// we stop, cancel destination

                    if (FleetData.CivEnum != hitFleetCon.FleetData.CivEnum)//if not one of ours
                    {
                        OnADestinationThatIsOtherCivFleet(hitFleetCon); //ToDo for fleets
                                                                        // encounter leads to diplomacy and then change in menu
                                                                        //Not sure whey we do not need this: if (gameObject.GetInstanceID() < collider.gameObject.GetInstanceID()) // only one side reports the collision

                        EncounterManager.Instance.ResolveEncounter(this, hitFleetCon);
                        EncounterUnknownFleetGetNameAndSprite(collider.gameObject); // setactive sprite and name

                        if (hitFleetCon.FleetData.Destination == this.gameObject) // they are coming for us
                        {
                            ClickCancelDestinationButton(hitFleetCon); // they stop

                            CloseUnLoadFleetUI(); // need more code to handle this encounter 
                        }

                    }
                    else //our fleet
                    {
                        // do ships?
                        OnADestinationThatIsOurOtherFleet(hitFleetCon); // we are the same civ fleets, do ships?
                    }
                }
            }
            else if (collider.gameObject.GetComponent<StarSysController>() != null) // only the fleetController reporst a collition for now, not the sys
            {
                var sysCon = collider.gameObject.GetComponent<StarSysController>();
                if (isOurDestination)
                {
                    ClickCancelDestinationButton(this); // we stop, cancel destination

                    if (this.FleetData.CivEnum != sysCon.StarSysData.CurrentOwnerCivEnum)
                    {
                        OnEnterForeignStarSystem(); // ToDo

                        EncounterManager.Instance.ResolveEncounter(this, sysCon);
                        if (weAreLocalPlayer)
                        {
                            EncounterUnknownSystemShowName(collider.gameObject); // update Galaxy view to expose insignia/name
                        }
                    }
                    else // ToDo: enter our system
                    {

                    }
                }
            }
            else if (collider.gameObject.GetComponent<PlayerDefinedTargetController>() != null)
            {
                if (isOurDestination)
                {
                    ClickCancelDestinationButton(this); // we stop, cancel destination
                    Destroy(collider.gameObject); // remove the player defined target
                }
            }

        }
        private Vector3 GetMouseWorldPosition()
        {
            // pixel coordinates (x,y)
            Vector3 mousePoint = Input.mousePosition;

            //z coordiante of game object on screen
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
            MousePointerChanger.Instance.HaveGalaxyMapCursor = false;
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

        public void OnFleetEncounteredPlayerDefinedTarget(PlayerDefinedTargetController playerTargetController)
        {
            if (this.FleetData.Destination == playerTargetController.gameObject)
            {
                //ClickCancelDestinationButton(this);
                Destroy(playerTargetController.gameObject);
                DestinationLine.lineRenderer.positionCount = 0;
                CloseUnLoadFleetUI();
            }
            //????PlayerDefinedTargetManager.current.
            //FleetManager.current.
            //1) player get the FleetController of the new fleet GO
            //2) ?build a deep space starbase vs a partol point for travel
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
            // Fleet only Logic to handle what happens when our fleet arrives at thier fleet destination
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
            }
        }
        public void ShipManageClick(FleetController fleetCon) // open ship manager UI
        {
            FleetManager.Instance.InstantiateFleetsShipManagerUI(this);
            GalaxyMenuUIController.Instance.OpenMenu(Menu.ManageShipsMenu, null);
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
        public void SelectedDestinationCursor(FleetController fleetCon)
        {
            if (MousePointerChanger.Instance.HaveGalaxyMapCursor == false)
            {
                GalaxyMenuUIController.Instance.MouseClickSetsDestination = true;
                GalaxyMenuUIController.Instance.SelectedDestinationCursor(this);
                MousePointerChanger.Instance.ChangeToGalaxyMapCursor(fleetCon);
                MousePointerChanger.Instance.HaveGalaxyMapCursor = true;
            }
        }


        public void ClickCancelDestinationButton(FleetController fleetCon)
        {
            DestinationLine.gameObject.SetActive(false);
            FleetData.LastDestination = FleetData.Destination; // save previous destination if we want to continue later 
            FleetData.Destination = FleetManager.Instance.GalaxyCenter;
            FleetData.CurrentWarpFactor = 0f; // stop the fleet
            //MousePointerChanger.Instance.ResetCursor();
            //MousePointerChanger.Instance.HaveGalaxyMapCursor = false;
            GalaxyMenuUIController.Instance.ClickCancelDestinationButton(this);
            GalaxyMenuUIController.Instance.MouseClickSetsDestination = false;
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
                destinationName.text = "Target at";
                PlayerDefinedTargetManager.instance.PlayerTargetFromData(gameObject);
                GalaxyMenuUIController.Instance.GetPlayerDefinedTargetDestination(this);
            }
        }
        public void OnClickShipManager()
        {
            GameObject notAMenu = new GameObject();
            GalaxyMenuUIController.Instance.OpenMenu(Menu.AFleetMenu, notAMenu);
            //SubMenuManager.Instance.OpenMenu(Menu.FleetsMenu, notAMenu);
            Destroy(notAMenu);
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
            GalaxyMenuUIController.Instance.MouseClickSetsDestination = false;
            MousePointerChanger.Instance.ResetCursor();
            GalaxyMenuUIController.Instance.CloseMenu(Menu.AFleetMenu); // The single fleet UI
            GalaxyMenuUIController.Instance.CloseMenu(Menu.FleetsMenu);
        }
        private string GetDebuggerDisplay()
        {
            return ToString();
        }
    }
}

