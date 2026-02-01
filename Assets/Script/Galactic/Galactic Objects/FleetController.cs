using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Core
{
    [RequireComponent(typeof(Rigidbody))]
    /// <summary>
    /// Controlling fleet movement and interactions while the matching FeetData class
    /// holds key info on status and for save game
    /// </summary>
    public class FleetController : MonoBehaviour
    {
        //Fields
        private FleetData fleetData;
        public FleetData FleetData { get { return fleetData; } set { fleetData = value; } }
        public GameObject FleetUIGameObject; //The instantiated fleet UI for this fleet. a prefab clone, not a class but a game object
        public GameObject GalaxyCanvasGo;
        public string Name;
        public int intName = 1;
        private readonly float warpFudgeFactor = 10f;
        private Rigidbody rb;
        private float updateInterval = 0.1f; // ~10 updates/sec (adjust for smoothness vs performance)
        private float lastUpdateTime;
        public MapLineMovable DropLine;
        public MapLineMovable DestinationLine;
        public GameObject BackgroundGalaxyImage;
        private float galaxyWidth = 1f;
        private float galaxyHeight = 1f;
        private float minimapWidth = 200f;
        private float minimapHeight = 400f;
        private bool gotMapSizeFromGameManager = false;
        [SerializeField] private GameObject backgroundGalaxyImage;
        private Camera galaxyEventCamera;
        private readonly GameObject aNull = null; // used to pass a null object to the UI when needed in Diplomacy
        public Canvas FleetUICanvas { get; private set; }
        //public Canvas CanvasToolTip; // not used for now, see start method and in instantiation of fleetController in FleetManager.cs
        public PlayerDefinedTargetController TargetController;
        private Vector3 vectorOffset;
        private readonly float ourZCoordinate;
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
        private readonly TMP_Dropdown shipDropdown;

        public GameObject ShipDropDownGO;
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
        private GalaxyMenuUIController galaxyUI;
        private GalaxyMenuUIController GalaxyUI
        {
            get
            {
                if (galaxyUI == null)
                    galaxyUI = GalaxyMenuUIController.Instance;
                return galaxyUI;
            }
        }
        private FleetMenuUIController fleetUI;
        private FleetMenuUIController FleetUI
        {
            get
            {
                if (fleetUI == null)
                    fleetUI = FleetMenuUIController.Instance;
                return fleetUI;
            }
        }
        private GameController gameController;
        private float distanceToDestination;

        private void Awake()
        {
            gameController = GameController.Instance;
        }

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            if (GalaxyCanvasGo != null)
                FleetUICanvas = GalaxyCanvasGo.GetComponent<Canvas>();
            if (FleetUICanvas != null)
                FleetUICanvas.worldCamera = galaxyEventCamera;
            DestinationLine = this.GetComponentInChildren<MapLineMovable>();
            DestinationLine.GetLineRenderer();
            DestinationLine.transform.SetParent(transform, false);
            if (FleetData != null && FleetData.Destination != null)
            {
                FleetData.Destination = FleetManager.Instance.GalaxyCenter;
            }
            galaxyWidth = GameManager.Instance.GalaxyWidth;
            galaxyHeight = GameManager.Instance.GalaxyHeight;

        }
        private void FixedUpdate()
        {
            // Destroying Fleets with no ships is problematic
            // The FleetController FeetData is still running in script
            // and if the player clicks on or OnTrigerEntere.... it causes errors
            if (FleetData != null && FleetData.Destination != null)
            {
                if (FleetData.Destination != FleetManager.Instance.GalaxyCenter && FleetData.CurrentWarpFactor > 0f)
                {
                    // Always move the fleet (physics)
                    MoveToDesitinationGO(GetDirection(), distanceToDestination);
                    if (!gotMapSizeFromGameManager)
                        GetMapSise();
                    // Throttle visual updates (line rendering, UI)
                    if (Time.time - lastUpdateTime >= updateInterval)
                    {
                        DrawDestinationLine(FleetData.Destination.transform.position);
                        UpdateMinimapPosition(); // Add this
                        lastUpdateTime = Time.time;
                    }
                }
            }
        }

        private void GetMapSise()
        {
            var fleetUIFields = FleetUIGameObject.GetComponent<FleetUI_Fields>();
            if (fleetUIFields == null || fleetUIFields.MinimapRedDot == null) return;
            RectTransform minimapRect = fleetUIFields.MinimapRedDot.parent.GetComponent<RectTransform>();
            float minimapWidth = minimapRect.rect.width;
            float minimapHeight = minimapRect.rect.height;
            gotMapSizeFromGameManager = true;
        }

        private void UpdateMinimapPosition()
        {
            if (FleetUIGameObject == null) return;

            var fleetUIFields = FleetUIGameObject.GetComponent<FleetUI_Fields>();
            if (fleetUIFields == null || fleetUIFields.MinimapRedDot == null) return;

            // Convert world position to minimap coordinates
            Vector2 minimapPos = WorldToMinimapPosition(transform.position);
            fleetUIFields.MinimapRedDot.anchoredPosition = minimapPos;
        }
        public GameObject ShipListUIParent
        {
            get => FleetData?.ShipListUIParent;
            set
            {
                if (FleetData != null)
                    FleetData.ShipListUIParent = value;
                // Process any pending ship UI items now that the parent is available.
                if (ShipManager.Instance != null)
                    ShipManager.Instance.ProcessPendingShipUIs();
            }
        }
        private Vector2 WorldToMinimapPosition(Vector3 worldPos)
        {
            // Assuming the minimap represents a specific area of the galaxy
            //float galaxyWidth = 2f; // GameManager.Instance.GalaxyWidth;
            //float galaxyHeight = 4f; // GameManager.Instance.GalaxyHeight;
            // Get minimap RectTransform
            var fleetUIFields = FleetUIGameObject.GetComponent<FleetUI_Fields>();
            if (fleetUIFields == null || fleetUIFields.MinimapRedDot == null) return Vector2.zero;
            RectTransform minimapRect = fleetUIFields.MinimapRedDot.parent.GetComponent<RectTransform>();
            float minimapWidth = minimapRect.rect.width;
            float minimapHeight = minimapRect.rect.height;
            // Convert world position to minimap coordinates
            float x = (worldPos.x / galaxyWidth) * minimapWidth;
            float y = (worldPos.z / galaxyHeight) * minimapHeight; // Assuming z is forward in world space
            return new Vector2(x, y);
        }
        public Rigidbody GetRigidBody() { return rb; }

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
                bool weAreLocalPlayer = gameController.AreWeLocalPlayer(this.FleetData.CivEnum);

                bool isOurDestination = false;
                if (this.FleetData.Destination == collider.gameObject) // it is our destination
                {
                    isOurDestination = true;
                    if (weAreLocalPlayer)
                    {
                        CloseUnLoadFleetUI(this); // we are there and have other things to do
                    }
                }

                if (collider.gameObject.TryGetComponent(out FleetController hitFleetCon))
                {
                    if (hitFleetCon == this && hitFleetCon == null) return; // ignore self
                    {
                        if (isOurDestination)
                        {
                            ClickCancelDestinationButton();// we stop, cancel destination

                            if (FleetData.CivEnum != hitFleetCon.FleetData.CivEnum)//if not one of ours
                            {
                                OnADestinationThatIsOtherCivFleet(hitFleetCon);
                                FleetUI.MoveBackAnyaFleetUIGO(); // close our fleet UI
                                DiplomacyManager.Instance.FleetControllerVsOtherCivFleet(this, hitFleetCon);
                                //ToDo: resolve an encounter with galaxy object that does not have a civ, black hole, wormhole, trans-warp hub, etc
                                EncounterUnknownFleetGetNameAndSprite(collider.gameObject); // set active sprite and name

                                if (hitFleetCon.FleetData.Destination == this.gameObject) // they are coming for us
                                {
                                    ClickCancelDestinationButton(); // they stop

                                    CloseUnLoadFleetUI(this); // need more code to handle this encounter 
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
                            //OnEnterForeignStarSystem(); // ToDo
                            FleetUI.MoveBackAnyaFleetUIGO(); // close our fleet UI
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
                else if (collider.gameObject.TryGetComponent(component: out PlayerDefinedTargetController _))
                {
                    if (isOurDestination)
                    {
                        ClickCancelDestinationButton(); // we stop, cancel destination
                        Destroy(collider.gameObject); // remove the player defined target
                    }
                }
            }

        }
        private void OnMouseDown()
        {
            var clickedFleetCon = GetComponentInParent<FleetController>();

            if (clickedFleetCon == null) return;

            switch (GalaxyUI.CurrentClickMode)
            {
                case GalaxyClickMode.Normal:
                    GalaxyMenuUIController.Instance.CloseButtonPressed();
                    HandleNormalClick(clickedFleetCon);
                    break;
                case GalaxyClickMode.SetDestination:
                    HandleDestinationClick(clickedFleetCon);
                    break;
                // no case GalaxyClickMode.SelectForNewFleet. that is a new fleet button click not a fleet click
                case GalaxyClickMode.SelectForShipDeploy:
                    if (gameController.AreWeLocalPlayer(clickedFleetCon.FleetData.CivEnum))
                        HandleShipDeploySelection(clickedFleetCon);
                    break;
                case GalaxyClickMode.SelectForShipMerge:
                    if (gameController.AreWeLocalPlayer(clickedFleetCon.FleetData.CivEnum))
                        HandleShipMegerSelection(clickedFleetCon);
                    break;
            }
        }

        private void HandleNormalClick(FleetController clickedFleetCon)
        {
            if (gameController.AreWeLocalPlayer(clickedFleetCon.FleetData.CivEnum))
            {
                GalaxyUI.OpenMenu(Menu.AFleetMenu, this.gameObject);
            }
            else if (DiplomacyManager.Instance.FoundADiplomacyController(CivManager.Instance.LocalPlayerCivContoller, clickedFleetCon.FleetData.CivController))
            { // this is a system local player does not own but we know them
                DiplomacyManager.Instance.ResolveDiplomacyForClickFleetWeKnow(CivManager.Instance.LocalPlayerCivContoller, clickedFleetCon);
            }
        }
        private void HandleDestinationClick(FleetController clickedFleetCon)
        {
            FleetController theFleetConLookingForDestination = galaxyUI.FleetLookingForDestination;
            if (theFleetConLookingForDestination == null) return;
            theFleetConLookingForDestination.fleetData.Destination = this.gameObject; // set the destination of the clicker fleet as this fleet clicked on
            theFleetConLookingForDestination.SetAsDestinationInUI(clickedFleetCon.gameObject);
        }

        private void HandleShipDeploySelection(FleetController clickedFleetCon)
        {
            if (clickedFleetCon != this) { return; }
            MousePointerChanger.Instance.ResetCursor();
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.WhatFleetIsSelectedForShipDiploy(clickedFleetCon);
            var fleetLooking = galaxyUI.FleetLookingForShipDeploy;
            var starSysLooking = galaxyUI.StarSystLookingForShipDeploy;
            if (fleetLooking == null)
            {
                var aSysView = StarSysMenuUIController.Instance.ASystemMenuView.gameObject;
                aSysView.SetActive(true);
                clickedFleetCon.FleetUIGameObject.transform.SetParent(aSysView.transform, false);
                FleetUIGameObject.transform.SetAsLastSibling();
                ShipDeployMenuUIController.Instance.SetUpTopShipLists(starSysLooking.StarSysData.ShipsList);
            }
            else if (starSysLooking == null)
            {
                var aFleetView = FleetUI.AFleetMenuView.gameObject;
                aFleetView.gameObject.SetActive(true);
                clickedFleetCon.FleetUIGameObject.transform.SetParent(aFleetView.transform, false);
                FleetUIGameObject.transform.SetAsLastSibling();
                ShipDeployMenuUIController.Instance.SetUpTopShipLists(fleetLooking.FleetData.ShipsList);
            }
            ShipDeployMenuUIController.Instance.SetUpBottomShipLists(clickedFleetCon, true);
            ShipDeployMenuUIController.Instance.ShowShipDeployMenuView();
        }
        private void HandleShipMegerSelection(FleetController clickedFleetCon)
        {
            if (clickedFleetCon != this) { return; }
            MousePointerChanger.Instance.ResetCursor();
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.WhatFleetIsSelectedForShipMerge(clickedFleetCon);
            var fleetLooking = GalaxyUI.FleetLookingForShipMerge;
            var starSysLooking = GalaxyUI.StarSystLookingForShipMerge;
            if (fleetLooking == null)
            {
                var aStarSysView = StarSysMenuUIController.Instance.ASystemMenuView;
                aStarSysView.gameObject.SetActive(true);
                clickedFleetCon.FleetUIGameObject.transform.SetParent(aStarSysView.transform, false);
                FleetUIGameObject.transform.SetAsLastSibling();
                ShipDeployMenuUIController.Instance.SetUpTopShipLists(fleetLooking.FleetData.ShipsList);
            }
            else if (starSysLooking == null)
            {
                var aFleetView = FleetMenuUIController.Instance.AFleetMenuView.gameObject;
                aFleetView.gameObject.SetActive(true);
                clickedFleetCon.FleetUIGameObject.transform.SetParent(aFleetView.transform, false);
                FleetUIGameObject.transform.SetAsLastSibling();
                ShipDeployMenuUIController.Instance.SetUpTopShipLists(starSysLooking.StarSysData.ShipsList);
            }

            ShipDeployMenuUIController.Instance.SetUpBottomShipLists(clickedFleetCon, false);
            ShipDeployMenuUIController.Instance.ShowShipDeployMenuView();
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

            if (destinationInt < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(destinationInt), destinationInt, $"'{nameof(destinationInt)}' cannot be negative");
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

        private Vector3 GetDirection()
        {
            return (this.FleetData.Destination.transform.position - transform.position).normalized;
        }

        void MoveToDesitinationGO(Vector3 direction, float distance)
        {
            float distanceToDestination = Vector3.Distance(transform.position, this.FleetData.Destination.transform.position);
            float howFast = this.FleetData.CurrentWarpFactor;
            if (howFast > this.FleetData.MaxWarpFactor)
            {
                this.FleetData.CurrentWarpFactor = this.FleetData.MaxWarpFactor;
            }
            Vector3 nextPosition = Vector3.MoveTowards(rb.position, FleetData.Destination.transform.position,
            howFast * warpFudgeFactor * Time.fixedDeltaTime);
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
            //GalaxyUI.ClickCancelDestinationButton(); 
        }
        void OnADestinationThatIsOurOtherFleet(FleetController ourOtherFleet)
        {
            // Logic to handle what happens when the fleet arrives at our other fleet as destination
            // how do we manage both fleets trying to do something with the other fleet?
        }
        void OnADestinationThatIsPlayerTarget()
        {
            // Logic to handle what happens when the fleet arrives at the system destination
            //GalaxyUI.ClickCancelDestinationButton(); 
        }
        //void OnEnterForeignStarSystem()
        //{ 
        //    // do something
        //}
        public void AddToShipList(ShipController shipController)
        {
            if (shipController == null) return;

            // Reparent gameplay ship under this fleet so scene hierarchy and transform stay correct.
            // Keep world position so the ship doesn't jump unexpectedly.
            shipController.transform.SetParent(transform, worldPositionStays: true);

            // Add to FleetData (model). FleetData.AddToShipList should guard duplicates but check anyway.
            if (!FleetData.ShipsList.Contains(shipController))
                FleetData.AddToShipList(shipController);

            // Move the UI representation under the fleet's UI parent if available.
            if (shipController.ShipListUIGameObject != null && FleetData.ShipListUIParent != null)
            {
                shipController.ShipListUIGameObject.transform.SetParent(FleetData.ShipListUIParent.transform, false);
            }

            // Update controller state (max warp etc.)
            UpdateMaxWarp();
        }

        public void RemoveFromShipList(ShipController shipController)
        {
            if (shipController == null) return;

            // Remove from model list
            if (FleetData.ShipsList.Contains(shipController))
                FleetData.RemoveFromShipList(shipController);// ship controllers go are children of a fleet controller go, not to confuse with the ship UI go we see on drag drop

            // Update controller state
            UpdateMaxWarp();

            // If the ship controller was parented to this fleet controller go under GalaxyCenter in the scene hierarchy, unparent it to scene root.
            if (shipController.transform.IsChildOf(transform))
                shipController.transform.SetParent(null, worldPositionStays: true);

            // Optionally move ship UI GO item to a neutral parent if the fleet UI parent still exists.

            //if (shipController.ShipListUIGameObject != null && FleetData.ShipListUIParent != null)
            //{
            //    shipController.ShipListUIGameObject.transform.SetParent(FleetData.ShipListUIParent.transform, false);
            //}
        }

        public void UpdateMaxWarp()
        {
            float maxWarp = 10f;
            for (int i = 0; i < fleetData.ShipsList.Count; i++)
            { // find the slowest ship
                if (fleetData.ShipsList[i] != null && fleetData.ShipsList[i].ShipData.maxWarpFactor < maxWarp)
                {
                    maxWarp = fleetData.ShipsList[i].ShipData.maxWarpFactor;
                }
            }
            fleetData.MaxWarpFactor = maxWarp;
            if (GalaxyUI != null)
                FleetUI.UpdateFleetMaxWarpUI(this, maxWarp);
        }
        public void DestroyFleet(FleetData fleetData, GameObject fleetGO)
        {
            FleetManager.Instance.RemoveFleetNumInUse(fleetData.CivEnum, fleetData.FleetInt);
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
            fleetUI.UpdateFleetWarpUI(this, newWarpValue);
        }

        //internal void SelectedUsForShips(FleetController fleetCon)
        //{
        //    galaxyUI.WhoIsSelectedForShipDiploy(this);
        //}
        public void CloseUnLoadFleetUI(FleetController theFleetCon)
        {
            GalaxyUI.ResetClickMode();
            MousePointerChanger.Instance.ResetCursor();
            FleetUI.UpdateFleetWarpUI(theFleetCon, 0);
            GalaxyUI.CloseMenu(Menu.AFleetMenu); // The single fleet UI
            GalaxyUI.CloseMenu(Menu.FleetMenu);
        }

        internal void RemoveShipFromFleet(ShipController shipController)
        {
            this.FleetData.ShipsList.Remove(shipController);
            if (this.FleetData.ShipsList.Count == 0)
            {
                // no ships left, remove fleet
                FleetManager.Instance.RemoveFleetNumInUse(this.FleetData.CivEnum, this.FleetData.FleetInt);
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
            if (this.FleetData != null)
            {
                FleetManager.Instance.RemoveFleetNumInUse(this.FleetData.CivEnum, this.FleetData.FleetInt);
                if (FleetManager.Instance.FleetControllerList.Contains(this))
                {
                    FleetManager.Instance.FleetControllerList.Remove(this);
                    Destroy(this.gameObject);
                    //TimeManager.Instance.ResumeTime();
                }
            }
        }

        public void CleanupFleetUIs()
        {
            foreach (var diplomacyCon in FleetManager.Instance.FleetControllerList)
            {
                if (diplomacyCon.FleetUIGameObject == null)
                    continue;

                if (!diplomacyCon.FleetUIGameObject.activeInHierarchy)
                {
                    diplomacyCon.FleetUIGameObject = null;
                }
            }
        }
        public void ClickCancelDestinationButton()
        {
            DestinationLine.gameObject.SetActive(false);
            FleetData.LastDestination = FleetData.Destination;
            FleetData.Destination = FleetManager.Instance.GalaxyCenter;
            FleetData.CurrentWarpFactor = 0f; // stop the fleet
            GalaxyUI.CompleteSetDestination();
            FleetUI.ClickCancelDestinationButton(this);
            GalaxyUI.SetClickMode(GalaxyClickMode.Normal);
            MousePointerChanger.Instance.ResetCursor();
        }

        public void SetAsDestinationInUI(GameObject hitObject)
        {

            fleetData.Destination = hitObject;
            GalaxyObjectType destinationType = GalaxyObjectType.None;// start with a blank
            // galaxy object type Enum SystemType if =>1, None =0
            string destinationNameText = "";

            string coordiantesText = "X " + (hitObject.transform.position.x).ToString()
                + " / Y " + (hitObject.transform.position.y).ToString()
                + " / Z " + (hitObject.transform.position.z).ToString();
            if (hitObject.GetComponent<StarSysController>() != null)
            {
                StarSysController starSysController = hitObject.GetComponent<StarSysController>();
                if (DiplomacyManager.Instance.FoundADiplomacyController(CivManager.Instance.LocalPlayerCivContoller, starSysController.StarSysData.CurrentCivController))
                { // if it is our star system we do have a diplomacy controller
                    destinationType = 0;
                    destinationNameText += starSysController.StarSysData.SysName;
                }
                else // unknown system
                {
                    destinationType = starSysController.StarSysData.SystemType;
                }
            }
            else if (hitObject.GetComponent<FleetController>() != null)
            {
                FleetController fleetCon = hitObject.GetComponent<FleetController>();

                if (DiplomacyManager.Instance.FoundADiplomacyController(CivManager.Instance.LocalPlayerCivContoller, fleetCon.FleetData.CivController))
                {
                    destinationType = GalaxyObjectType.Fleet;
                    destinationNameText = fleetCon.FleetData.Name;
                }
                else // unknown fleet
                {
                    destinationType = GalaxyObjectType.UnknownFleet;
                }
            }
            switch (destinationType)
            {
                case GalaxyObjectType.None:
                    break;
                case GalaxyObjectType.BlueStar:
                    destinationNameText = "Blue Star at";
                    break;
                case GalaxyObjectType.WhiteStar:
                    destinationNameText = "White Star at";
                    break;
                case GalaxyObjectType.YellowStar:
                    destinationNameText = "Yellow Star at";
                    break;
                case GalaxyObjectType.OrangeStar:
                    destinationNameText = "Orange Star at";
                    break;
                case GalaxyObjectType.RedStar:
                    destinationNameText = "Red Star at";
                    break;
                case GalaxyObjectType.Nebula:
                case GalaxyObjectType.OmarianNebula:
                case GalaxyObjectType.OrionNebula:
                    destinationNameText = "Nebula at";
                    break;
                case GalaxyObjectType.Station:
                    destinationNameText = "Station at";
                    break;
                case GalaxyObjectType.BlackHole:
                    destinationNameText = "Black Hole at";
                    break;
                case GalaxyObjectType.WormHole:
                    destinationNameText = "WormHole at";
                    break;
                case GalaxyObjectType.TargetDestination:
                    destinationNameText = "Target at";
                    break;
                case GalaxyObjectType.UnknownFleet:
                    destinationNameText = "Fleet at";
                    break;
                case GalaxyObjectType.Fleet:
                    destinationNameText = "Fleet at";
                    break;
                default:
                    destinationNameText = "";
                    break;

            }
            FleetUI.SetAsDestination(destinationNameText, coordiantesText);
        }

        public void GetPlayerDefinedTargetDestination(FleetController fleetCon)
        {
            if (this == fleetCon)
            {
                FleetUI.SetAsDestination("Drag target to", "your destination");
                PlayerDefinedTargetManager.Instance.PlayerTargetFromData(gameObject);
                FleetUI.GetPlayerDefinedTargetDestination(this);
            }
        }

        internal void saveCloseShipDelplyButton(FleetController fleetCon)
        {
            // ToDo should this be in FleetMenuUIController?
        }
    }
}

