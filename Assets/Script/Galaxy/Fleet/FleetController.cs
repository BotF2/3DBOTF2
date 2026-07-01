using BOTF3D.Combat;
using BOTF3D.Core;
using BOTF3D.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Civilization;
using BOTF3D.Audio;



namespace BOTF3D.Galaxy
{
    [RequireComponent(typeof(Rigidbody))]
    /// <summary>
    /// Controlling fleet movement and interactions while the matching FeetData class
    /// holds key info on status and for save game
    /// </summary>
    public class FleetController : MonoBehaviour
    {
        public void Initialize() { }
        public void UpdateState() { }
        //Fields
        private FleetData fleetData;
        public FleetData FleetData { get { return fleetData; } set { fleetData = value; } }
        [SerializeField]
        private GameObject _fleetUIGameObject;

        public GameObject FleetUIGameObject
        {
            get => _fleetUIGameObject;
            set
            {
                if (value != null && (value == this.gameObject || value.transform.IsChildOf(this.transform)))
                {
                    Debug.LogError($"❌ FleetUIGameObject on '{name}' cannot be set to its own 3D GameObject or a child of it! This would deactivate the fleet from the galaxy map. Assignment blocked.");
                    return;
                }
                _fleetUIGameObject = value;
            }
        }
        public GameObject GalaxyCanvasGo;
        public string Name;
        public int intName = 1;
        // Units-per-second scaler. Tune this in the Inspector on the fleet prefab.
        // 4f ≈ 60% slower than the original 10f baseline.
        [SerializeField] private float warpFudgeFactor = 4f;
        private Rigidbody rb;
        private float updateInterval = 0.1f; // ~10 updates/sec (adjust for smoothness vs performance)
        private float lastUpdateTime;
        public MapLineMovable DropLine;
        public MapLineMovable DestinationLine;
        public GameObject BackgroundGalaxyImage;

        // Which fleet we're currently pursuing (set via the Intercept button)
        public static FleetController PendingInterceptFleet; // fleet waiting for player to pick a target
        private Vector3 interceptPoint;
        private float interceptUpdateTimer;
        private const float INTERCEPT_UPDATE_INTERVAL = 0.5f;
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
            galaxyWidth = GalaxyView.Instance.GalaxyWidth;
            galaxyHeight = GalaxyView.Instance.GalaxyHeight;

        }
        private void FixedUpdate()
        {
            if (FleetData == null) return;
            if (TimeManager.Instance == null || TimeManager.Instance.TurnPhase != TurnPhase.TurnProgression) return;

            // ── Intercept mode ────────────────────────────────────────────────
            if (FleetData.InterceptTarget != null)
            {
                if (FleetData.InterceptTarget == null || FleetData.InterceptTarget.gameObject == null)
                {
                    CancelIntercept(); // target was destroyed
                }
                else if (FleetData.CurrentWarpFactor > 0f)
                {
                    interceptUpdateTimer -= Time.fixedDeltaTime;
                    if (interceptUpdateTimer <= 0f)
                    {
                        interceptPoint = ComputeInterceptPoint();
                        interceptUpdateTimer = INTERCEPT_UPDATE_INTERVAL;
                    }

                    MoveToInterceptPoint();

                    if (!gotMapSizeFromGameManager) GetMapSise();
                    if (Time.time - lastUpdateTime >= updateInterval)
                    {
                        DrawDestinationLine(interceptPoint);
                        UpdateMinimapPosition();
                        lastUpdateTime = Time.time;
                    }
                }
                return;
            }

            // ── Normal destination mode ───────────────────────────────────────
            if (FleetData.Destination != null && FleetData.CurrentWarpFactor > 0f)
            {
                if (FleetData.Destination != FleetManager.Instance?.GalaxyCenter)
                {
                    distanceToDestination = Vector3.Distance(transform.position, FleetData.Destination.transform.position);
                    MoveToDesitinationGO(GetDirection());
                    if (!gotMapSizeFromGameManager) GetMapSise();
                    if (Time.time - lastUpdateTime >= updateInterval)
                    {
                        DrawDestinationLine(FleetData.Destination.transform.position);
                        UpdateMinimapPosition();
                        lastUpdateTime = Time.time;
                    }
                }
            }
        }

        // ── Intercept helpers ─────────────────────────────────────────────────

        public void SetInterceptTarget(FleetController target)
        {
            FleetData.InterceptTarget = target;
            interceptPoint = target.transform.position;
            interceptUpdateTimer = 0f; // force immediate recompute
            FleetData.CurrentWarpFactor = FleetData.MaxWarpFactor; // full speed ahead
            Debug.Log($"{name}: intercept target set to '{target.name}'");
        }

        public void CancelIntercept()
        {
            FleetData.InterceptTarget = null;
            interceptPoint = Vector3.zero;
        }

        private Vector3 ComputeInterceptPoint()
        {
            var target = FleetData.InterceptTarget;
            if (target == null) return transform.position;

            Vector3 targetPos = target.transform.position;

            // If target is stopped, just go straight to it
            if (target.FleetData.CurrentWarpFactor <= 0f ||
                target.FleetData.Destination == null ||
                target.FleetData.Destination == FleetManager.Instance.GalaxyCenter)
                return targetPos;

            Vector3 targetDest = target.FleetData.Destination.transform.position;
            Vector3 targetDir  = (targetDest - targetPos).normalized;
            float   targetSpeed = target.FleetData.CurrentWarpFactor * warpFudgeFactor;
            float   ourSpeed    = FleetData.CurrentWarpFactor * warpFudgeFactor;
            if (ourSpeed <= 0f) return targetPos;

            // Estimate time to close the gap and predict where target will be
            float dist         = Vector3.Distance(transform.position, targetPos);
            float timeEstimate = dist / ourSpeed;
            Vector3 predicted  = targetPos + targetDir * targetSpeed * timeEstimate;

            // Don't predict past the target's own destination
            float distToDest = Vector3.Distance(targetPos, targetDest);
            if (Vector3.Distance(targetPos, predicted) > distToDest)
                predicted = targetDest;

            return predicted;
        }

        private void MoveToInterceptPoint()
        {
            float howFast   = Mathf.Min(FleetData.CurrentWarpFactor, FleetData.MaxWarpFactor);
            Vector3 nextPos = Vector3.MoveTowards(rb.position, interceptPoint,
                howFast * warpFudgeFactor * Time.fixedDeltaTime);
            rb.MovePosition(nextPos);
            FleetData.Position = nextPos;

            Vector3 galaxyPlanePoint = new Vector3(rb.position.x, -60f, rb.position.z);
            DropLine.SetUpLine(new Vector3[] { rb.position, galaxyPlanePoint });
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

            // Convert world position to mini-map coordinates
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
            // Assuming the mini-map represents a specific area of the galaxy
            //float galaxyWidth = 2f; // GameManager.Instance.GalaxyWidth;
            //float galaxyHeight = 4f; // GameManager.Instance.GalaxyHeight;
            // Get mini-map RectTransform
            var fleetUIFields = FleetUIGameObject.GetComponent<FleetUI_Fields>();
            if (fleetUIFields == null || fleetUIFields.MinimapRedDot == null) return Vector2.zero;
            RectTransform minimapRect = fleetUIFields.MinimapRedDot.parent.GetComponent<RectTransform>();
            float minimapWidth = minimapRect.rect.width;
            float minimapHeight = minimapRect.rect.height;
            // Convert world position to mini-map coordinates
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
                float initialWarpFactor = this.FleetData.CurrentWarpFactor;

                bool isOurDestination = false;
                if (this.FleetData.Destination == collider.gameObject) // it is our destination
                {
                    isOurDestination = true;
                    if (weAreLocalPlayer)
                    {
                        SliderOnValueChange(0f); // stop the fleet on arrival
                        FleetUI.UpdateFleetWarpUI(this, 0f);
                        CloseUnLoadFleetUI(this); // we are there and have other things to do
                    }
                }

                if (collider.gameObject.TryGetComponent(out FleetController hitFleetCon))
                {
                    if (hitFleetCon == this || hitFleetCon == null) return; // ignore self

                    // Stop both fleets on contact (whether queued or immediate)
                    bool contactIsIntercept = (FleetData.InterceptTarget == hitFleetCon);
                    if (contactIsIntercept) CancelIntercept();

                    if (isOurDestination || contactIsIntercept)
                    {
                        ClickCancelDestinationButton(); // we stop

                        if (FleetData.CivEnum != hitFleetCon.FleetData.CivEnum) // enemy fleet
                        {
                            hitFleetCon.FleetData.CurrentWarpFactor = 0f; // stop them too
                            EncounterUnknownFleetGetNameAndSprite(collider.gameObject);

                            bool duringProgression = BOTF3D.Core.TimeManager.Instance != null &&
                                BOTF3D.Core.TimeManager.Instance.TurnPhase == BOTF3D.Core.TurnPhase.TurnProgression;

                            if (duringProgression)
                            {
                                // Defer: queue and let ProcessTurnEvents handle it
                                GalaxyEncounterQueue.Instance?.EnqueueFleetVsFleet(this, hitFleetCon);
                                Debug.Log($"OnTriggerEnter: FleetVsFleet queued (TurnProgression) — {name} vs {hitFleetCon.name}");
                            }
                            else
                            {
                                OnADestinationThatIsOtherCivFleet(hitFleetCon);
                                FleetUI.MoveBackAnyaFleetUIGO();
                                DiplomacyManager.Instance.FleetControllerVsOtherCivFleet(this, hitFleetCon);
                            }

                            if (hitFleetCon.FleetData.Destination == this.gameObject)
                                CloseUnLoadFleetUI(this);
                        }
                        else // friendly fleet
                        {
                            OnADestinationThatIsOurOtherFleet(hitFleetCon);
                        }
                    }
                }
                else if (collider.gameObject.TryGetComponent(out StarSysController sysCon))
                {
                    if (isOurDestination)
                    {
                        ClickCancelDestinationButton();

                        int firstUninhabited = (int)CivEnum.ZZUNINHABITED1;

                        if ((int)sysCon.StarSysData.CurrentOwnerCivEnum >= firstUninhabited)
                        {
                            if (sysCon.StarSysData.IsHabitable)
                            {
                                Debug.Log($"Fleet arrived at uninhabited habitable system '{sysCon.StarSysData.SysName}'");
                                if (weAreLocalPlayer)
                                {
                                    FleetUI.MoveBackAnyaFleetUIGO();
                                    HabitableSysUIController.Instance?.LoadHabitableSysUI(sysCon, this.FleetData.CivController);
                                }
                            }
                            else
                            {
                                Debug.Log($"Fleet arrived at uninhabited non-habitable system '{sysCon.StarSysData.SysName}'");
                            }
                        }
                        else if (this.FleetData.CivEnum != sysCon.StarSysData.CurrentOwnerCivEnum)
                        {
                            if (weAreLocalPlayer)
                                EncounterUnknownSystemShowName(collider.gameObject);

                            bool duringProgression = BOTF3D.Core.TimeManager.Instance != null &&
                                BOTF3D.Core.TimeManager.Instance.TurnPhase == BOTF3D.Core.TurnPhase.TurnProgression;

                            if (duringProgression)
                            {
                                GalaxyEncounterQueue.Instance?.EnqueueFleetVsSystem(this, sysCon);
                                Debug.Log($"OnTriggerEnter: FleetVsSystem queued (TurnProgression) — {name} at {sysCon.name}");
                            }
                            else
                            {
                                FleetUI.MoveBackAnyaFleetUIGO();
                                DiplomacyManager.Instance.ResolveEncounterOtherCivSystem(this, sysCon);
                            }
                        }
                        else
                        {
                            Debug.Log($"Fleet arrived at our own system '{sysCon.StarSysData.SysName}'");
                        }
                    }
                }
                else if (collider.gameObject.TryGetComponent(out PlayerDefinedTargetController targetCon))
                {
                    // Only destroy the target if it is our destination, the fleet was moving, AND it's not currently being dragged
                    if (isOurDestination && initialWarpFactor > 0f && !targetCon.IsDragging)
                    {
                        ClickCancelDestinationButton(); // we stop, cancel destination & remove the player defined target
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
                    // ✅ Only close ship deploy if it's actually open!
                    if (ShipDeployMenuUIController.Instance != null &&
                        ShipDeployMenuUIController.Instance.ShipDeployPanel != null &&
                        ShipDeployMenuUIController.Instance.ShipDeployPanel.activeSelf)
                    {
                        GalaxyMenuUIController.Instance.CloseShipDeployMenu();
                        if (StarSysMenuUIController.Instance != null)
                        {
                            StarSysMenuUIController.Instance.MoveBackAnyStarSysUIGO();
                            Debug.Log("HandleNormalClick: Cleaned up star system UIs before opening new UI");
                        }
                        // ✅ NEW: Ensure all fleet UIs are moved back to storage after closing deploy menu
                        if (FleetMenuUIController.Instance != null)
                        {
                            FleetMenuUIController.Instance.MoveBackAnyaFleetUIGO();
                            Debug.Log("OnMouseDown: Cleaned up fleet UIs after closing ship deploy menu");
                        }
                    }
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
                        HandleShipMergeSelection(clickedFleetCon);
                    break;
                case GalaxyClickMode.SelectForIntercept:
                    HandleInterceptSelection(clickedFleetCon);
                    break;
            }
        }

        private void HandleNormalClick(FleetController clickedFleetCon)
        {
            // Menu system will handle cleanup when transitioning between menus
            if (gameController.AreWeLocalPlayer(clickedFleetCon.FleetData.CivEnum))
            {
                GalaxyUI.OpenMenu(Menu.AFleetMenu, this.gameObject);
            }
            else if (DiplomacyManager.Instance.FoundADiplomacyController(CivManager.Instance.LocalPlayerCivController, clickedFleetCon.FleetData.CivController))
            { // this is a fleet local player does not own but we know them
                DiplomacyManager.Instance.ResolveDiplomacyForClickFleetWeKnow(CivManager.Instance.LocalPlayerCivController, clickedFleetCon);
            }
        }
        private void HandleDestinationClick(FleetController clickedFleetCon)
        {
            var pursuing = GalaxyUI.FleetLookingForDestination;
            if (pursuing == null || clickedFleetCon == pursuing) return;

            if (pursuing.TargetController != null)
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(pursuing);

            // Clicked a moving fleet → use intercept logic
            pursuing.SetInterceptTarget(clickedFleetCon);

            var fields = pursuing.FleetUIGameObject?.GetComponent<FleetUI_Fields>();
            if (fields != null)
            {
                fields.InterceptTargetButton?.gameObject.SetActive(false);
                fields.CancelInterceptButton?.gameObject.SetActive(true);
                if (fields.DestinationName != null)
                    fields.DestinationName.text = clickedFleetCon.FleetData.FleetName;
                if (fields.DestinationCoordinates != null)
                    fields.DestinationCoordinates.text = "";
            }

            GalaxyUI.CompleteSetDestination();
            MousePointerChanger.Instance?.ResetCursor();
        }

        private void HandleInterceptSelection(FleetController clickedFleetCon)
        {
            if (PendingInterceptFleet == null) return;
            if (clickedFleetCon == PendingInterceptFleet) return; // can't intercept self
            if (clickedFleetCon.FleetData.CivEnum == PendingInterceptFleet.FleetData.CivEnum) return; // same civ

            PendingInterceptFleet.SetInterceptTarget(clickedFleetCon);
            PendingInterceptFleet = null;
            GalaxyUI.ResetClickMode();
            MousePointerChanger.Instance?.ResetCursor();
        }

        private void HandleShipDeploySelection(FleetController clickedFleetCon)
        {
            if (clickedFleetCon != this) { return; }
            MousePointerChanger.Instance.ResetCursor();
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.WhatFleetIsSelectedForShipDiploy(clickedFleetCon);
            var fleetLooking = galaxyUI.FleetLookingForShipDeploy;
            var starSysLooking = galaxyUI.StarSystLookingForShipDeploy;

            if (fleetLooking != null && fleetLooking != this) // We have a fleet looking for ship deploy
            {
                var aFleetView = FleetUI.AFleetMenuView.gameObject;
                aFleetView.gameObject.SetActive(true);

                // Parent both fleet UIs
                if (fleetLooking.FleetUIGameObject != null)
                {
                    fleetLooking.FleetUIGameObject.transform.SetParent(aFleetView.transform, false);
                    fleetLooking.FleetUIGameObject.SetActive(true);
                }

                clickedFleetCon.FleetUIGameObject.transform.SetParent(aFleetView.transform, false);
                clickedFleetCon.FleetUIGameObject.SetActive(true);
                FleetUIGameObject.transform.SetAsLastSibling();

                ShipDeployMenuUIController.Instance.SetUpTopShipLists(fleetLooking.FleetData.ShipsList);
                ShipDeployMenuUIController.Instance.SetUpBottomShipLists(clickedFleetCon, true);
            }
            else if (starSysLooking != null) // We have a star system looking for ship deploy
            {
                var aSysView = StarSysMenuUIController.Instance.ASystemMenuView.gameObject;
                aSysView.SetActive(true);

                // ✅ Update star system UI with current values (minimap, facilities, etc.)
                if (starSysLooking.StarSysUIGameObject != null)
                {
                    starSysLooking.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                    starSysLooking.StarSysUIGameObject.SetActive(true);

                    // Update facility UI to show current load values
                    var starSysUI = StarSysMenuUIController.Instance;
                    if (starSysUI != null)
                    {
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.Factory);
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.Shipyard);
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.ShieldGenerator);
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.OrbitalBattery);
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.ResearchCenter);

                        // Update minimap position
                        var sysUIFields = starSysLooking.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                        if (sysUIFields != null && sysUIFields.redDot != null)
                        {
                            Vector3 sysPos = starSysLooking.transform.position;
                            sysUIFields.redDot.anchoredPosition = new Vector2(sysPos.x * 0.12f, sysPos.z * 0.12f);
                        }
                    }
                }

                clickedFleetCon.FleetUIGameObject.transform.SetParent(aSysView.transform, false);
                clickedFleetCon.FleetUIGameObject.SetActive(true);
                FleetUIGameObject.transform.SetAsLastSibling();

                ShipDeployMenuUIController.Instance.SetUpTopShipLists(starSysLooking.StarSysData.ShipsList);
                ShipDeployMenuUIController.Instance.SetUpBottomShipLists(clickedFleetCon, true);
            }

            ShipDeployMenuUIController.Instance.ShowShipDeployMenuView();
        }
        private void HandleShipMergeSelection(FleetController clickedFleetCon)
        {
            if (clickedFleetCon != this) { return; }
            MousePointerChanger.Instance.ResetCursor();
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.WhatFleetIsSelectedForShipMerge(clickedFleetCon);
            var fleetLooking = galaxyUI.FleetLookingForShipMerge;
            var starSysLooking = galaxyUI.StarSystLookingForShipMerge;

            var shipDeployUI = ShipDeployMenuUIController.Instance;

            if (fleetLooking != null && fleetLooking != this) // Fleet-to-Fleet merge
            {
                var aFleetView = FleetMenuUIController.Instance.AFleetMenuView.gameObject;
                aFleetView.gameObject.SetActive(true);

                // ✅ Add VerticalLayoutGroup if not present
                var layoutGroup = aFleetView.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = aFleetView.AddComponent<VerticalLayoutGroup>();
                    layoutGroup.childAlignment = TextAnchor.UpperLeft;
                    layoutGroup.spacing = 20f; // Space between fleet UIs
                    layoutGroup.childForceExpandHeight = false;
                    layoutGroup.childForceExpandWidth = false;
                    layoutGroup.childControlHeight = false;
                    layoutGroup.childControlWidth = false;
                }

                // Parent source fleet UI to container (TOP position)
                if (fleetLooking.FleetUIGameObject != null)
                {
                    fleetLooking.FleetUIGameObject.transform.SetParent(aFleetView.transform, false);
                    fleetLooking.FleetUIGameObject.transform.SetAsFirstSibling();
                    fleetLooking.FleetUIGameObject.SetActive(true);
                    Debug.Log($"✅ Source fleet UI parented to AFleetMenuView (top)");
                }

                // Parent target fleet UI to container (BOTTOM position)
                clickedFleetCon.FleetUIGameObject.transform.SetParent(aFleetView.transform, false);
                clickedFleetCon.FleetUIGameObject.transform.SetAsLastSibling();
                clickedFleetCon.FleetUIGameObject.SetActive(true);
                Debug.Log($"✅ Target fleet UI parented to AFleetMenuView (bottom)");
                List<BOTF3D.Combat.ShipController> combinedShipsList = new List<BOTF3D.Combat.ShipController>();

                combinedShipsList.AddRange(fleetLooking.FleetData.ShipsList);
                combinedShipsList.AddRange(clickedFleetCon.FleetData.ShipsList);

                Debug.Log($"Merge Fleet-to-Fleet: {fleetLooking.FleetData.ShipsList.Count} + {clickedFleetCon.FleetData.ShipsList.Count} = {combinedShipsList.Count} ships");

                shipDeployUI.SetUpTopShipLists(new System.Collections.Generic.List<ShipController>());
                shipDeployUI.SetUpBottomShipListsForMerge(combinedShipsList, clickedFleetCon, fleetLooking, null, null);
            }
            else if (starSysLooking != null) // System-to-Fleet merge
            {
                var aFleetView = FleetMenuUIController.Instance.AFleetMenuView.gameObject;
                aFleetView.gameObject.SetActive(true);

                // ✅ Add VerticalLayoutGroup if not present
                var layoutGroup = aFleetView.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = aFleetView.AddComponent<VerticalLayoutGroup>();
                    layoutGroup.childAlignment = TextAnchor.UpperLeft;
                    layoutGroup.spacing = 20f;
                    layoutGroup.childForceExpandHeight = false;
                    layoutGroup.childForceExpandWidth = false;
                    layoutGroup.childControlHeight = false;
                    layoutGroup.childControlWidth = false;
                }

                // Parent system UI to container (TOP position)
                if (starSysLooking.StarSysUIGameObject != null)
                {
                    starSysLooking.StarSysUIGameObject.transform.SetParent(aFleetView.transform, false);
                    starSysLooking.StarSysUIGameObject.transform.SetAsLastSibling();
                    starSysLooking.StarSysUIGameObject.SetActive(true);
                    Debug.Log($"✅ System UI parented to AFleetMenuView (top)");

                    // Update system facility UI
                    var starSysUI = StarSysMenuUIController.Instance;
                    if (starSysUI != null)
                    {
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.Factory);
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.Shipyard);
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.ShieldGenerator);
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.OrbitalBattery);
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.ResearchCenter);
                    }
                }

                // Parent fleet UI to container (BOTTOM position)
                clickedFleetCon.FleetUIGameObject.transform.SetParent(aFleetView.transform, false);
                clickedFleetCon.FleetUIGameObject.transform.SetAsLastSibling();
                clickedFleetCon.FleetUIGameObject.SetActive(true);
                Debug.Log($"✅ Fleet UI parented to AFleetMenuView (bottom)");

                var combinedShipsList = new System.Collections.Generic.List<ShipController>();
                combinedShipsList.AddRange(starSysLooking.StarSysData.ShipsList);
                combinedShipsList.AddRange(clickedFleetCon.FleetData.ShipsList);

                Debug.Log($"Merge System-to-Fleet: {starSysLooking.StarSysData.ShipsList.Count} + {clickedFleetCon.FleetData.ShipsList.Count} = {combinedShipsList.Count} ships");

                shipDeployUI.SetUpTopShipLists(new System.Collections.Generic.List<ShipController>());
                shipDeployUI.SetUpBottomShipListsForMerge(combinedShipsList, clickedFleetCon, null, starSysLooking, null);
            }

            shipDeployUI.ShowShipDeployMenuView();
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
            SetAsDestinationInUI(destinationGo);
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

        void MoveToDesitinationGO(Vector3 direction)
        {
            float howFast = this.FleetData.CurrentWarpFactor;
            if (howFast > this.FleetData.MaxWarpFactor)
                this.FleetData.CurrentWarpFactor = this.FleetData.MaxWarpFactor;

            Vector3 nextPosition = Vector3.MoveTowards(rb.position, FleetData.Destination.transform.position,
                howFast * warpFudgeFactor * Time.fixedDeltaTime);
            rb.MovePosition(nextPosition);
            this.FleetData.Position = nextPosition;
            Vector3 galaxyPlanePoint = new Vector3(rb.position.x, -60f, rb.position.z);
            DropLine.SetUpLine(new Vector3[] { rb.position, galaxyPlanePoint });
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
                // Fleet is empty. EndCombat() calls DestroyFleetController() which also cleans up FleetUIGameObject and DropLine.
                FleetManager.Instance.RemoveFleetNumInUse(this.FleetData.CivEnum, this.FleetData.FleetInt);
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
            // Remove fog revealer when fleet is destroyed
            if (FischlWorks_FogWar.csFogWar.Instance != null && transform != null)
            {
                FischlWorks_FogWar.csFogWar.Instance.RemoveRevealer(transform);
            }

            // Existing cleanup code...
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
        public void CloseShipDeploy(FleetController fleetCon)
        {
            if (fleetCon == this)
            {
                if (fleetCon.TargetController != null)
                {
                    PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(fleetCon);
                }
                if (ShipDeployMenuUIController.Instance != null)
                {
                    ShipDeployMenuUIController.Instance.OnSaveCloseButtonClicked();
                }
                fleetCon.FleetUIGameObject.SetActive(false);

                //ShipDeployMenuUIController.Instance.CloseShipDeployMenuView();
            }
        }
        public void ClickCancelDestinationButton()
        {
            // Zero warp FIRST — unconditional stop before any failable lookups
            FleetData.LastDestination = FleetData.Destination;
            FleetData.CurrentWarpFactor = 0f;
            FleetData.Destination = FleetManager.Instance != null ? FleetManager.Instance.GalaxyCenter : null;

            // Clear any active intercept (fleet-chasing) state
            if (FleetData.InterceptTarget != null)
                CancelIntercept();
            PendingInterceptFleet = null;

            // Sync warp slider to 0 so scroll/drag can't silently restore a non-zero value
            FleetUI?.UpdateFleetWarpUI(this, 0f);

            if (TargetController != null)
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(this);

            if (DestinationLine != null)
                DestinationLine.gameObject.SetActive(false);

            GalaxyUI?.CompleteSetDestination();
            FleetUI?.ClickCancelDestinationButton(this);
            GalaxyUI?.SetClickMode(GalaxyClickMode.Normal);
            MousePointerChanger.Instance?.ResetCursor();
        }

        public void SetAsDestinationInUI(GameObject hitObject)
        {
            fleetData.Destination = hitObject;
            GalaxyObjectType destinationType = GalaxyObjectType.None;
            string destinationNameText = "";

            string coordiatesText = "X " + (hitObject.transform.position.x).ToString()
                + " / Y " + (hitObject.transform.position.y).ToString()
                + " / Z " + (hitObject.transform.position.z).ToString();

            if (hitObject.GetComponent<StarSysController>() != null)
            {
                StarSysController starSysController = hitObject.GetComponent<StarSysController>();
                if (DiplomacyManager.Instance.FoundADiplomacyController(CivManager.Instance.LocalPlayerCivController, starSysController.StarSysData.CurrentCivController))
                {
                    destinationType = 0;
                    destinationNameText += starSysController.StarSysData.SysName;
                }
                else
                {
                    destinationType = starSysController.StarSysData.SystemType;
                }
            }
            else if (hitObject.GetComponent<FleetController>() != null)
            {
                FleetController fleetCon = hitObject.GetComponent<FleetController>();
                if (DiplomacyManager.Instance.FoundADiplomacyController(CivManager.Instance.LocalPlayerCivController, fleetCon.FleetData.CivController))
                {
                    destinationType = GalaxyObjectType.Fleet;
                    destinationNameText = fleetCon.FleetData.FleetName;
                }
                else
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
                case GalaxyObjectType.ORIONNEBULA:
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
                case GalaxyObjectType.Fleet:
                    destinationNameText = "Fleet at";
                    break;
                default:
                    destinationNameText = "";
                    break;
            }

            FleetMenuUIController.Instance?.SetAsDestination(destinationNameText, coordiatesText);
        }

        public void GetPlayerDefinedTargetDestination(FleetController fleetCon)
        {
            if (fleetCon == null || fleetCon.FleetUIGameObject == null) return;

            // ✅ CRITICAL: Tell GalaxyMenuUIController which fleet is looking for destination
            var galaxyUI = GalaxyMenuUIController.Instance;
            if (galaxyUI != null)
            {
                galaxyUI.BeginSetDestination(fleetCon);
            }

            // Get buttons from the specific fleet's UI
            var fields = fleetCon.FleetUIGameObject.GetComponent<FleetUI_Fields>();
            if (fields != null)
            {
                if (fields.DestinationDragTarget != null)
                    fields.DestinationDragTarget.gameObject.SetActive(false);
                if (fields.CancelDestination != null)
                    fields.CancelDestination.gameObject.SetActive(true);
                if (fields.SelectDestination != null)
                    fields.SelectDestination.gameObject.SetActive(true);
            }

            MousePointerChanger.Instance.SetDestinationCursor();

            // ✅ Create the PlayerDefinedTarget
            PlayerDefinedTargetManager.Instance.PlayerTargetFromData(fleetCon.gameObject);
        }
    }
}

