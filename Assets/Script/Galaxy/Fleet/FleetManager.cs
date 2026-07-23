// Ignore Spelling: Nums Revealer
using BOTF3D.Combat;

using BOTF3D.UI;
using FischlWorks_FogWar;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Civilization;
using BOTF3D.Audio;



namespace BOTF3D.Galaxy
{
    /// <summary>
    /// Instantiates the fleets (a FleetController and a FleetData) using FleetSO
    /// </summary>
    public class FleetManager : MonoBehaviour, IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        public static FleetManager Instance;
        public GameObject scoutBluePrintPrefab;
        public GameObject destroyerBluePrintPrefab;
        public GameObject cruiserBluePrintPrefab;
        public GameObject ltCruiserBluePrintPrefab;
        public GameObject heavyCruiserBluePrintPrefab;
        public GameObject transportBluePrintPrefab;

        [Header("UI Container References")]
        [SerializeField] private GameObject fleetListContainer;
        [SerializeField] private GameObject galaxyCanvasGO;
        [SerializeField] private GameObject galaxyImage;

        [SerializeField]
        private Canvas parentCanavas;
        [SerializeField]
        private csFogWar fogWar;
        [SerializeField]
        private List<FleetSO> fleetSOList;
        [SerializeField]
        private FleetController fleetPrefab;
        [SerializeField]
        private GameObject fleetUIPrefab;
        [SerializeField]
        private GameObject shipManagerMenuPrefab;
        [SerializeField]
        private Material fogPlaneMaterial;

        // Don't serialize - find at runtime since it's in GalaxyScene
        public GameObject GalaxyCenter { get; private set; }

        public List<FleetController> FleetControllerList { get; private set; } = new List<FleetController>();
        [SerializeField]
        private GameObject canvasShipManager;
        [SerializeField]
        private List<int> destinationIntsInUse = new List<int>() { 0 };
        private readonly Dictionary<CivEnum, List<int>> fleetNumsInUse = new Dictionary<CivEnum, List<int>>();
        private static readonly List<FleetController> FleetControllers = new List<FleetController>();
        public List<FleetController> FleetControllersInGame = FleetControllers;
        [SerializeField]
        private GameObject fleetUIGOContentParent;
        [SerializeField]
        private GameObject fleetShipsContentFolderParent;
        private readonly List<CivEnum> localPlayerCanSeeMyInsigniaList = new List<CivEnum>();
        internal GameObject fleetShipUIGOContentParent;
        public csFogWar.FogRevealer TempFogRevealerFleet;
        private float newFleetSpacer = 0f;

        // ✅ NEW: Persistent container for ALL fleet UIs when not displayed
        [SerializeField] public GameObject FleetUI_ListContainer;

        private void Awake()
        {
            ServiceLocator.Register<FleetManager>(this);
            if (Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                Debug.Log("FleetManager: Instance created");
                FindGalaxyReferences();
            }

            // Fleets are now Mirror network objects (NetworkIdentity + NetworkTransform on
            // FleetPrefab) - clients need this prefab registered so they can spawn the
            // GameObject Mirror tells them about when the server calls NetworkServer.Spawn.
            if (fleetPrefab != null)
                NetworkClient.RegisterPrefab(fleetPrefab.gameObject);
        }

        public void Start()
        {
            if (CivManager.Instance != null && CivManager.Instance.CivSOListAllPossible != null)
            {
                for (int i = 0; i < CivManager.Instance.CivSOListAllPossible.Count; i++)
                {
                    fleetNumsInUse.Add(CivManager.Instance.CivSOListAllPossible[i].CivEnum, new List<int>());
                }
            }
            else
            {
                Debug.LogError("FleetManager.Start: CivManager.Instance or CivSOListAllPossible is null — fleet number tracking will be empty.");
            }

            // Find GalaxyCenter at start
            FindGalaxyReferences();
        }
        /// <summary>
        /// Sets galaxy scene references needed by FleetManager.
        /// Called by GalaxySceneInitializer when the galaxy scene loads.
        /// </summary>
        public void SetGalaxyReferences(GameObject galaxyCenter, GameObject galaxyImage, Canvas canvasGalaxy, GameObject fleetListContainer)
        {
            this.GalaxyCenter = galaxyCenter;
            this.galaxyImage = galaxyImage;
            this.parentCanavas = canvasGalaxy;
            this.galaxyCanvasGO = canvasGalaxy.gameObject;
            this.fleetListContainer = fleetListContainer;

            Debug.Log("FleetManager: Galaxy references set successfully.");
        }
        // NEW: Find galaxy references when needed
        public void FindGalaxyReferences()
        {
            // ✅ CRITICAL: Find GalaxyCenter (it's in the scene, not Inspector)
            if (GalaxyCenter == null)
            {
                GalaxyCenter = GameObject.Find("GalaxyCenter");
                Debug.Log($"FleetManager: Found GalaxyCenter: {GalaxyCenter != null}");
            }

            // Only find if null (fallback safety)
            if (galaxyCanvasGO == null)
            {
                Debug.LogWarning("FleetManager: galaxyCanvasGO not assigned in Inspector! Using Find() as fallback.");
                galaxyCanvasGO = GameObject.Find("CanvasGalaxy");
            }

            if (fleetListContainer == null && galaxyCanvasGO != null)
            {
                Debug.LogWarning("FleetManager: fleetListContainer not assigned! Using FindInHierarchy() as fallback.");
                fleetListContainer = FindInHierarchy(galaxyCanvasGO.transform, "FleetListContainer");
            }

            fleetUIGOContentParent = fleetListContainer;

            if (galaxyImage == null)
            {
                Debug.LogWarning("FleetManager: galaxyImage not assigned in Inspector! Using Find() as fallback.");
                // Try finding as root object first
                galaxyImage = GameObject.Find("GalaxyImage");

                // Last resort: search all loaded scenes for inactive objects
                if (galaxyImage == null)
                {
                    for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                    {
                        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                        if (scene.isLoaded)
                        {
                            foreach (GameObject rootObj in scene.GetRootGameObjects())
                            {
                                galaxyImage = FindInHierarchy(rootObj.transform, "GalaxyImage");
                                if (galaxyImage != null)
                                {
                                    Debug.Log($"FleetManager: Found GalaxyImage in scene '{scene.name}' under '{rootObj.name}'");
                                    break;
                                }
                            }
                            if (galaxyImage != null) break;
                        }
                    }
                }

                Debug.Log($"FleetManager: GalaxyImage final result: {galaxyImage != null}");
            }

            if (fogWar == null)
            {
                fogWar = csFogWar.Instance;
                Debug.Log($"FleetManager: Found FogWar: {fogWar != null}");
            }

            //// CRITICAL: Find fleetUIGOContentParent in CanvasGalaxy
            //if (fleetUIGOContentParent == null)
            //{
            //    var canvasGalaxy = GameObject.Find("CanvasGalaxy");
            //    if (canvasGalaxy != null)
            //    {
            //        fleetUIGOContentParent = FindInHierarchy(canvasGalaxy.transform, "FleetListContainer");

            //        //if (fleetUIGOContentParent == null)
            //        //{
            //        //    fleetUIGOContentParent = FindInHierarchy(canvasGalaxy.transform, "Content");
            //        //}

            //        Debug.Log($"FleetManager: Found fleetUIGOContentParent: {fleetUIGOContentParent != null}");
            //    }
            //}

            if (galaxyCanvasGO == null)
            {
                galaxyCanvasGO = GameObject.Find("CanvasGalaxy");
                Debug.Log($"FleetManager: Found galaxyCanvasGO: {galaxyCanvasGO != null}");
            }

            // ✅ NEW: Find FleetUI_ListContainer
            if (FleetUI_ListContainer == null)
            {
                var canvasGalaxy = GameObject.Find("CanvasGalaxy");
                if (canvasGalaxy != null)
                {
                    FleetUI_ListContainer = FindInHierarchy(canvasGalaxy.transform, "FleetUI_ListContainer");

                    if (FleetUI_ListContainer != null)
                    {
                        Debug.Log($"FleetManager: ✅ Found FleetUI_ListContainer");
                    }
                    else
                    {
                        Debug.LogWarning("FleetManager: ⚠️ FleetUI_ListContainer not found - create it in CanvasGalaxy!");
                    }
                }
            }
        }

        // Helper method for recursive search
        private GameObject FindInHierarchy(Transform parent, string name)
        {
            if (parent.name == name)
                return parent.gameObject;

            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject found = FindInHierarchy(parent.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        public void CleanUpDictionaryForFleetNums()
        {
            var civs = CivManager.Instance.CivSOListAllPossible;

            var civsEnumsInGame = CivManager.Instance.CivEnumsInGame;
            for (int i = 0; i < civs.Count; i++)
            {
                if (!civsEnumsInGame.Contains(civs[i].CivEnum))
                {
                    fleetNumsInUse.Remove(civs[i].CivEnum);
                }
            }
        }

        public void BuildFirstFleetsNearSyst(StarSysController systCon)
        {
            // Fleet creation is now server-authoritative (see InstantiateFleet). Bail out before
            // creating the placeholder empty FleetController below, otherwise on a non-host client
            // InstantiateFleet would return early and leave that placeholder GameObject orphaned
            // in the scene (it's normally destroyed inside InstantiateFleet).
            if (!NetworkServer.active) return;

            // first path here is sent on loading the game for civs with warp, first fleets from Systems/Civs with warp
            FleetSO fleetSO = GetFleetSO_byInt((int)systCon.StarSysData.CurrentOwnerCivEnum);
            var position = systCon.StarSysData.GetPosition();

            CivData thisCivData = CivManager.Instance.GetCivDataByCivEnum(fleetSO.CivOwnerEnum);
            FleetData fleetData = new FleetData(fleetSO);
            fleetData.CurrentWarpFactor = 3f;
            fleetData.CivLongName = thisCivData.CivLongName;
            fleetData.CivShortName = thisCivData.CivShortName;
            var emptyFleet = InsatiateEmptyFleetController();
            FleetController aFleet = InstantiateFleet(emptyFleet, systCon, fleetData, position, false);
        }

        public FleetController InsatiateEmptyFleetController()
        {
            FleetController fleetController = Instantiate(fleetPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            return fleetController;
        }

        /// <summary>
        /// Headless equivalent of StarSysMenuUIController.ClickNewFleetButton + the manual
        /// ship-drag-drop flow: forms a brand-new fleet at the system's position and transfers
        /// the given ships straight into it, with no UI/drag components involved. Built for
        /// AI-owned systems (e.g. StarSysAIManager War mode) that need to dispatch built ships
        /// without a human dragging them into a fleet panel.
        /// </summary>
        public FleetController FormFleetFromSystemShips(StarSysController sysCon, List<ShipController> shipsToMove)
        {
            if (sysCon?.StarSysData == null || shipsToMove == null || shipsToMove.Count == 0) return null;

            FleetSO fleetSO = GetFleetSO_byInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
            var position = sysCon.StarSysData.GetPosition();
            CivData thisCivData = CivManager.Instance.GetCivDataByCivEnum(fleetSO.CivOwnerEnum);

            FleetData fleetData = new FleetData(fleetSO);
            fleetData.CurrentWarpFactor = 0f;
            fleetData.CivLongName = thisCivData.CivLongName;
            fleetData.CivShortName = thisCivData.CivShortName;
            fleetData.CivEnum = thisCivData.CivEnum;
            fleetData.PlayerId = thisCivData.PlayerId;
            // Insignia already set from fleetSO.Insignia by the FleetData(fleetSO) constructor above —
            // don't overwrite it with the civ's own insignia here.
            fleetData.ShipsList = new List<ShipController>();

            FleetController newFleet = InstantiateFleet(null, sysCon, fleetData, position, true);
            if (newFleet == null) return null;

            foreach (var ship in shipsToMove)
            {
                if (ship == null) continue;

                sysCon.StarSysData.ShipsList.Remove(ship);
                ship.ShipData.CurrentStarSysController = null;

                if (!newFleet.FleetData.ShipsList.Contains(ship))
                    newFleet.FleetData.ShipsList.Add(ship);
                ship.ShipData.CurrentFleetController = newFleet;
                ship.transform.SetParent(newFleet.transform, false);
            }

            newFleet.UpdateMaxWarp();
            return newFleet;
        }

        public FleetController InstantiateFleet(FleetController existingFleetCon, StarSysController sysController,
            FleetData fleetData, Vector3 position, bool isNewFleet)
        {
            // Server-authoritative: fleets are now Mirror network objects (NetworkServer.Spawn
            // below), so only the server may build one - remote clients receive it automatically
            // via Mirror's spawn message instead of running this method themselves. In single
            // player / hosting, NetworkServer.active is true locally, so this is unaffected.
            // NOTE: the 3 UI-driven call sites (FleetMenuUIController, ShipDeployMenuUIController,
            // StarSysMenuUIController) currently call this directly and will no-op on non-host
            // clients until those are converted to [Command]s (deferred follow-up work).
            if (!NetworkServer.active)
            {
                Debug.LogWarning("FleetManager.InstantiateFleet: called on a non-server client; ignoring. Fleet creation must be server-authoritative.");
                return null;
            }

            // CRITICAL: Ensure GalaxyCenter exists before proceeding
            if (GalaxyCenter == null)
            {
                FindGalaxyReferences();

                if (GalaxyCenter == null)
                {
                    Debug.LogError("FleetManager.InstantiateFleet: GalaxyCenter is NULL! Cannot create fleet.");
                    return null;
                }
            }

            fleetData.ShipsList.RemoveAll(item => item == null);
            Transform newTrans = null;

            FleetController newFleet = Instantiate(fleetPrefab, new Vector3(0, 0, 0), Quaternion.identity);

            if (isNewFleet)
            {
                GalaxyMenuUIController.Instance.FleetSelectedForShipDeploy = newFleet;
            }

            FleetControllerList.Add(newFleet);
            newFleet.gameObject.layer = 6; // galaxy layer

            // CRITICAL: Set layer for ALL children (recursively)
            SetLayerRecursively(newFleet.gameObject, 6);

            newFleet.BackgroundGalaxyImage = galaxyImage;
            newFleet.FleetData = fleetData;
            newFleet.GalaxyCanvasGo = galaxyCanvasGO;

            // Replicated to every client so FleetController.OnCivEnumChanged can run the same
            // per-client visual/registration setup below (see RegisterFleetControllerAndSetupVisuals)
            // on remote clients, who never run InstantiateFleet themselves - fleet creation is
            // server-authoritative, but visibility/insignia depend on each client's own civ perspective.
            newFleet.SyncedIsNewFleet = isNewFleet;
            newFleet.SyncedCivEnum = fleetData.CivEnum;

            var transGalaxyCenter = GalaxyCenter.transform; // Safe now - we checked above

            if (sysController != null && sysController.StarSysData != null && !isNewFleet)
            {
                newTrans = sysController.transform;
                Destroy(existingFleetCon.gameObject); // destroy the empty original fleet controller
            }
            else if (existingFleetCon != null && isNewFleet)
            {
                newTrans = existingFleetCon.transform;
            }
            else if (sysController != null && sysController.StarSysData != null && isNewFleet)
            {
                newTrans = sysController.transform;
            }

            newFleet.transform.SetParent(transGalaxyCenter, true);

            if (newTrans != null)
            {
                if (!isNewFleet)
                {
                    newFleet.transform.Translate(new Vector3(newTrans.position.x, newTrans.position.y + 20f, newTrans.position.z + 10F));
                }
                else
                {
                    if (newFleetSpacer > 10f)
                        newFleetSpacer = 0;
                    newFleet.transform.Translate(new Vector3(newTrans.position.x - 15f - newFleetSpacer, newTrans.position.y + 15f - newFleetSpacer, newTrans.position.z));
                    newFleetSpacer = newFleetSpacer + 5f;
                }
            }

            fleetData.Position = newFleet.transform.position;

            // Fleet name/number must be assigned BEFORE the fleet UI is instantiated below,
            // since InstantiateFleetUIGameObject -> SetupFleetUIElements reads FleetData.FleetName
            // to populate the "Fleet Name (TMP)" text on the FleetUI prefab. Assigning it after
            // UI creation left that text blank on first instantiation.
            newFleet.transform.localScale = new Vector3(0.4f, 0.4f, 1);
            int fleetInt = GetNewFleetInt(fleetData.CivEnum);
            newFleet.gameObject.name = fleetData.CivShortName.ToString() + " Fleet " + fleetInt.ToString();
            fleetData.FleetName = "Fleet " + fleetInt.ToString();
            newFleet.FleetData.FleetInt = fleetInt;
            newFleet.FleetData.CurrentWarpFactor = 0f;
            // Replicated so remote clients can rebuild this same name themselves - see
            // FleetController.OnCivEnumChanged, which never runs InstantiateFleet and would
            // otherwise have no FleetName at all.
            newFleet.SyncedFleetInt = fleetInt;

            TextMeshProUGUI TheText = newFleet.gameObject.GetComponentInChildren<TextMeshProUGUI>();
            if (TheText != null)
            {
                TheText.text = newFleet.FleetData.FleetName;
                fleetData.FleetName = TheText.text;
            }

            InstantiateFleetUIGameObject(newFleet, isNewFleet);
            // ✅ Now ships can find the ShipListUIParent when their UIs are created
            if (!isNewFleet)
                ShipManager.Instance.BuildShipsOfFirstFleet(newFleet);

            RegisterFleetControllerAndSetupVisuals(newFleet, fleetData); // also sets up the DropLine

            // SAFETY: Check GalaxyCenter still exists
            if (GalaxyCenter != null)
            {
                newFleet.FleetData.Destination = GalaxyCenter;
            }

            foreach (var civCon in CivManager.Instance.CivControllersInGame)
            {
                if (civCon.CivData.CivEnum == fleetData.CivEnum)
                {
                    fleetData.CivController = civCon;
                    break;
                }
            }

            newFleet.gameObject.SetActive(true);
            newFleet.UpdateMaxWarp();
            InstantiateFleetUIGameObject(newFleet, isNewFleet);

            // Replicate this fleet's NetworkIdentity + NetworkTransform to every connected client
            // now that it's fully built - clients get the GameObject via Mirror's spawn message
            // and start receiving position updates immediately.
            Debug.LogWarning($"🛰️[MAXWARP] InstantiateFleet: spawning fleet '{newFleet.gameObject.name}' (civ={newFleet.FleetData.CivEnum}) - SyncedCivEnum={newFleet.SyncedCivEnum}, SyncedFleetInt={newFleet.SyncedFleetInt}, SyncedMaxWarpFactor={newFleet.SyncedMaxWarpFactor}, ShipsList.Count={newFleet.FleetData.ShipsList.Count}.");
            NetworkServer.Spawn(newFleet.gameObject);

            // ✅ NEW: Immediately set up ShipListUIParent for new fleet
            if (isNewFleet && newFleet.FleetUIGameObject != null)
            {
                var uiFields = newFleet.FleetUIGameObject.GetComponent<FleetUI_Fields>();
                if (uiFields != null && uiFields.FleetShipContentGO != null)
                {
                    newFleet.FleetData.ShipListUIParent = uiFields.FleetShipContentGO;
                    Debug.Log($"✅ Set ShipListUIParent for new fleet '{newFleet.name}'");
                }
                else
                {
                    Debug.LogError($"❌ Cannot set ShipListUIParent for new fleet '{newFleet.name}' - FleetUI_Fields or FleetShipContentGO missing!");
                }
            }

            return newFleet;
        }

        /// <summary>
        /// Creates/positions the red line from a fleet's floating sprite down to its position on
        /// the galaxy map plane. Originally ran only inline inside InstantiateFleet (server-only) -
        /// factored out so it also runs from RegisterFleetControllerAndSetupVisuals on remote clients,
        /// who never run InstantiateFleet themselves (see that method's comments) and were previously
        /// left with no DropLine at all.
        /// </summary>
        private void SetUpDropLine(FleetController newFleet)
        {
            FleetChildFields fleetChildFields = newFleet.GetComponent<FleetChildFields>();

            // The line from Fleet to underlying galaxy image
            MapLineMovable[] ourLineToGalaxyImageScript = newFleet.gameObject.GetComponentsInChildren<MapLineMovable>();

            // VALIDATION: Check if MapLineMovable components exist
            if (ourLineToGalaxyImageScript == null || ourLineToGalaxyImageScript.Length == 0)
            {
                Debug.LogError($"FleetManager.SetUpDropLine: No MapLineMovable found in FleetController prefab '{fleetPrefab.name}'! " +
                               $"The DropLine cannot be created for fleet '{newFleet.name}'. " +
                               $"Add a MapLineMovable component to the FleetController prefab in the Inspector.");
                return;
            }

            // CRITICAL: Ensure galaxyImage exists before setting up DropLine
            if (galaxyImage == null)
            {
                Debug.LogWarning($"FleetManager.SetUpDropLine: galaxyImage is null, attempting to find it again...");
                FindGalaxyReferences();

                if (galaxyImage == null)
                {
                    Debug.LogError($"FleetManager.SetUpDropLine: galaxyImage is STILL null! Cannot set up DropLine for fleet '{newFleet.name}'");
                    return;
                }
            }

            bool foundDropLine = false;

            for (int i = 0; i < ourLineToGalaxyImageScript.Length; i++)
            {
                // SAFETY: Check fleetChildFields and DropLine exist
                if (fleetChildFields == null || fleetChildFields.DropLine == null)
                {
                    Debug.LogWarning($"FleetManager.SetUpDropLine: fleetChildFields.DropLine is null for fleet '{newFleet.name}'");
                    continue;
                }

                if (ourLineToGalaxyImageScript[i].gameObject == fleetChildFields.DropLine)
                {
                    ourLineToGalaxyImageScript[i].GetLineRenderer();
                    ourLineToGalaxyImageScript[i].lineRenderer.startColor = Color.red;
                    ourLineToGalaxyImageScript[i].lineRenderer.endColor = Color.red;
                    ourLineToGalaxyImageScript[i].transform.SetParent(newFleet.transform, false);

                    Vector3 galaxyPlanePoint = new Vector3(newFleet.transform.position.x,
                        galaxyImage.transform.position.y, newFleet.transform.position.z);
                    Vector3[] points = { newFleet.transform.position, galaxyPlanePoint };
                    ourLineToGalaxyImageScript[i].SetUpLine(points);
                    newFleet.DropLine = ourLineToGalaxyImageScript[i];
                    foundDropLine = true;
                    Debug.Log($"✅ DropLine set up for fleet '{newFleet.name}'");
                    break; // Found it, no need to continue
                }
            }

            if (!foundDropLine)
            {
                Debug.LogWarning($"FleetManager.SetUpDropLine: Found {ourLineToGalaxyImageScript.Length} MapLineMovable(s) but none matched fleetChildFields.DropLine for fleet '{newFleet.name}'");
            }
        }

        /// <summary>
        /// Per-client-perspective visual setup (fog-of-war revealer vs. visibility agent, insignia
        /// sprite enable/disable, FleetNameGO visibility) plus registration into FleetControllerList/
        /// FleetControllersInGame. Originally ran only once, inline, inside InstantiateFleet (which is
        /// now server-only) - factored out so FleetController.OnCivEnumChanged can also call it on
        /// every remote client when a fleet's SyncedCivEnum arrives, since those clients never run
        /// InstantiateFleet themselves.
        /// </summary>
        public void RegisterFleetControllerAndSetupVisuals(FleetController newFleet, FleetData fleetData)
        {
            if (!FleetControllerList.Contains(newFleet))
                FleetControllerList.Add(newFleet);
            if (!FleetControllersInGame.Contains(newFleet))
                FleetControllersInGame.Add(newFleet);

            FleetChildFields fleetChildFields = newFleet.GetComponent<FleetChildFields>();
            SpriteRenderer srInsignia = fleetChildFields.InsigniaGO.GetComponent<SpriteRenderer>();
            srInsignia.sprite = fleetData.Insignia;
            SpriteRenderer srInsigniaUnknown = fleetChildFields.InsigniaUnknownGO.GetComponent<SpriteRenderer>();

            if (GameController.Instance.AreWeLocalPlayer(fleetData.CivEnum))
            {
                // Explicit SetActive (not just SpriteRenderer.enabled) so this branch is correct even
                // if it ever runs for a freshly-spawned remote-client fleet object whose insignia
                // GameObjects are still at their prefab-default (inactive) state - the non-local
                // branch below always sets these explicitly for the same reason.
                fleetChildFields.InsigniaGO.SetActive(true);
                fleetChildFields.InsigniaUnknownGO.SetActive(false);
                srInsigniaUnknown.enabled = false;
                srInsignia.enabled = true;

                // SAFETY: Only add fog revealer if fogWar exists
                if (fogWar != null)
                {
                    // CRITICAL: updateOnlyOnMove = FALSE so fog updates continuously as fleet moves
                    var ourFogRevealerFleet = new csFogWar.FogRevealer(newFleet.transform, 200, false); // FALSE = always update
                    fogWar.AddFogRevealer(ourFogRevealerFleet);
                    TempFogRevealerFleet = ourFogRevealerFleet;

                    Debug.Log($"Added fog revealer to LOCAL fleet '{newFleet.name}' with continuous updates");
                }
            }
            else
            {
                // If we already had contact with this civ, show the insignia and name
                bool hasContact = localPlayerCanSeeMyInsigniaList.Contains(fleetData.CivEnum);
                if (hasContact)
                {
                    fleetChildFields.FleetNameGO.SetActive(true);
                    srInsignia.enabled = true;
                    fleetChildFields.InsigniaUnknownGO.SetActive(false);
                    srInsigniaUnknown.enabled = false;
                }
                else
                {
                    fleetChildFields.FleetNameGO.SetActive(false);
                    srInsignia.enabled = false;

                    // CRITICAL: Ensure InsigniaUnknownGO is ACTIVE (not just enabled)
                    fleetChildFields.InsigniaUnknownGO.SetActive(true);
                    srInsigniaUnknown.enabled = true;
                }

                // CRITICAL FIX: Add visibility agent AFTER all children exist
                if (fogWar != null)
                {
                    var ourFogVisibilityAgent = newFleet.gameObject.AddComponent<csFogVisibilityAgent>();
                    ourFogVisibilityAgent.FogWar = fogWar;

                    // IMPORTANT: Manually collect renderers (Start() hasn't run yet)
                    var allRenderers = newFleet.GetComponentsInChildren<SpriteRenderer>(true).ToList();

                    // Filter out DropLine renderer if you don't want fog to control it
                    if (fleetChildFields.DropLine != null)
                    {
                        var dropLineRenderer = fleetChildFields.DropLine.GetComponent<SpriteRenderer>();
                        if (dropLineRenderer != null)
                        {
                            allRenderers.Remove(dropLineRenderer);
                        }
                    }

                    // csFogVisibilityAgent.Update() force-sets `enabled` on every renderer in this
                    // list to match raw fog-of-war visibility every frame, with no concept of
                    // "insignia vs unknown". Whichever sprite lost the contact check above (srInsignia
                    // when uncontacted, srInsigniaUnknown when contacted) must be excluded here or the
                    // agent flips it back on the instant the tile is fog-visible - showing the real
                    // insignia for a civ we've never met.
                    allRenderers.Remove(hasContact ? srInsigniaUnknown : srInsignia);

                    ourFogVisibilityAgent.spriteRenderers = allRenderers;

                    // SAFETY: Only check visibility if fog grid is initialized
                    bool initialVisibility = false;

                    // Check if fog is ready by testing if position is in valid grid range
                    if (fogWar.CheckWorldGridRange(newFleet.transform.position))
                    {
                        initialVisibility = fogWar.CheckVisibility(newFleet.transform.position, 0);
                        Debug.Log($"FleetManager: Fog grid ready - initial visibility: {initialVisibility}");
                    }
                    else
                    {
                        // Fog grid not ready yet - default to hidden, agent will update in its Update() loop
                        initialVisibility = false;
                        Debug.Log($"FleetManager: Fog grid NOT ready yet - defaulting visibility to false for '{newFleet.name}'");
                    }

                    foreach (var sr in ourFogVisibilityAgent.spriteRenderers)
                    {
                        sr.enabled = initialVisibility;
                    }

                    Debug.Log($"FleetManager: Added FogVisibilityAgent to '{newFleet.name}' " +
                              $"with {ourFogVisibilityAgent.spriteRenderers.Count} renderers. " +
                              $"Initial visibility: {initialVisibility}");
                }
                else
                {
                    Debug.LogWarning($"FleetManager: fogWar is NULL! Fleet '{newFleet.name}' won't have fog visibility!");

                    // Fallback: Keep renderers enabled if no fog system
                    srInsigniaUnknown.enabled = true;
                }
            }

            // Self-guards on AreWeLocalPlayer, so safe to call unconditionally here - this is what
            // actually builds the FleetUI GameObject the top-ribbon Fleet panel reads from
            // (FleetMenuUIController.FleetControllerList). Without this, a remote client whose local
            // player owns this fleet would have it registered in the list but with no UI to show.
            InstantiateFleetUIGameObject(newFleet, false);

            // Server already sets this up inline inside InstantiateFleet before calling this method
            // (SetUpDropLine is idempotent), but remote clients only ever reach fleet setup through
            // here (see FleetController.OnCivEnumChanged) and previously never got a DropLine at all.
            SetUpDropLine(newFleet);
        }

        private void InstantiateFleetUIGameObject(FleetController fleetCon, bool newFleet)
        {
            if (!GameController.Instance.AreWeLocalPlayer(fleetCon.FleetData.CivEnum))
                return;

            // CRITICAL: Ensure fleetUIGOContentParent exists before creating UI
            if (fleetUIGOContentParent == null)
            {
                FindGalaxyReferences();

                if (fleetUIGOContentParent == null)
                {
                    Debug.LogError($"FleetManager.InstantiateFleetUIGameObject: fleetUIGOContentParent is NULL! Cannot create fleet UI for {fleetCon.name}");
                    return;
                }
            }

            var shipManager = ShipManager.Instance;

            if (fleetCon.FleetUIGameObject == null)
            {
                GameObject thisFleetUIGameObject = (GameObject)Instantiate(fleetUIPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                thisFleetUIGameObject.SetActive(false); // hidden until ShowFleetMenuView activates it
                thisFleetUIGameObject.layer = 5;
                fleetCon.FleetUIGameObject = thisFleetUIGameObject;
                thisFleetUIGameObject.transform.SetParent(fleetUIGOContentParent.transform, false);

                FleetUI_Fields fleetUI_Fields = thisFleetUIGameObject.GetComponent<FleetUI_Fields>();
                if (fleetUI_Fields != null && fleetUI_Fields.FleetShipContentGO != null)
                {
                    fleetCon.FleetData.ShipListUIParent = fleetUI_Fields.FleetShipContentGO;
                    FleetMenuUIController.Instance?.SetupFleetUIElements(fleetCon, thisFleetUIGameObject);
                }
                else
                {
                    Debug.LogWarning($"InstantiateFleetUIGameObject: ShipContent not found in UI prefab for fleet {fleetCon.name}");
                }
            }

            if (shipManager != null)
            {
                shipManager.ProcessPendingShipUIs();
                EnsureFleetShipUIs(fleetCon);
            }
        }

        // Ensures that every ShipController in fleetCon.FleetData.ShipsList has a ShipListUIGameObject
        // and that its UI object is parented to the fleet's ShipListUIParent (if available).
        private void EnsureFleetShipUIs(FleetController fleetCon)
        {
            if (fleetCon == null || fleetCon.FleetData == null) return;

            var shipManager = ShipManager.Instance;
            if (shipManager == null) return;

            GameObject shipListParent = fleetCon.FleetData.ShipListUIParent;
            // If no parent yet, attempt to process pending UIs and return; ProcessPendingShipUIs will reparent queued items.
            if (shipListParent == null)
            {
                shipManager.ProcessPendingShipUIs();
                return;
            }

            var ships = fleetCon.FleetData.ShipsList;
            if (ships == null || ships.Count == 0) return;

            for (int i = 0; i < ships.Count; i++)
            {
                var shipCon = ships[i];
                if (shipCon == null) continue;

                // Create UI item if missing
                if (shipCon.ShipListUIGameObject == null)
                {
                    shipManager.InstantiateShipListUIGameObject(shipCon, fleetCon.gameObject);
                }

                // Ensure proper parent
                if (shipCon.ShipListUIGameObject != null)
                {
                    var currentParent = shipCon.ShipListUIGameObject.transform.parent;
                    if (currentParent == null || currentParent.gameObject != shipListParent)
                    {
                        shipCon.ShipListUIGameObject.transform.SetParent(shipListParent.transform, false);
                    }
                }
            }

            // Final pass to process any items queued by InstantiateShipListUIGameObject
            shipManager.ProcessPendingShipUIs();
        }

        void RemoveFleetConrollerFromAllControllers(FleetController fleetController)
        {
            FleetControllerList.Remove(fleetController);
        }
        void AddFleetConrollerFromAllControllers(FleetController fleetController)
        {
            FleetControllerList.Add(fleetController);
        }
        public void FleetToFleetManagement(FleetController fleetConA, FleetController fleetConB)
        {
            List<ShipController> shipListA = fleetConA.FleetData.GetShipList();
            List<ShipController> shipListB = fleetConB.FleetData.GetShipList();
            // we already know the civ of fleetConA == the civ of fleetConB
            if (GameController.Instance.AreWeLocalPlayer(fleetConA.FleetData.CivEnum))
            {
                //call up UI for civ
            }
            else
            {
                //call up AI for civ fleet management
            }

        }

        public FleetSO GetFleetSO_byInt(int fleetInt)
        {
            FleetSO result = null;
            for (int i = 0; i < fleetSOList.Count; i++)
            //foreach (var fleetSO in fleetSOList)
            {

                if (fleetSOList[i].CivIndex == fleetInt)
                {
                    result = fleetSOList[i];
                    break;
                }
            }
            return result;

        }

        private int GetUniqueIntAsDestination(int destinationInt)
        {
            if (destinationIntsInUse.Contains(destinationInt))
            {
                destinationInt++;
                if (!destinationIntsInUse.Contains(destinationInt))
                    return destinationInt;
            }
            else
            {
                destinationIntsInUse.Add(destinationInt);

            }
            return destinationInt;

        }
        public void RemoveFleet(GameObject go, int asDestinationInt)
        {
            destinationIntsInUse.Remove(asDestinationInt);
            FleetControllerList.Remove(go.GetComponent<FleetController>());
            Destroy(go);
        }
        public int GetNewFleetInt(CivEnum civEnum)
        {
            List<int> ourFleetNumsInUse = fleetNumsInUse[civEnum];
            int numToReturn = 1;
            if (ourFleetNumsInUse.Count == 0)
            {
                ourFleetNumsInUse.Add(numToReturn);
                return numToReturn;
            }
            else
            {
                for (int i = 1; i < ourFleetNumsInUse.Count + 1; i++)
                {
                    if (!ourFleetNumsInUse.Contains(numToReturn))
                    {
                        numToReturn = i;
                    }
                    else
                    {
                        numToReturn = i + 1;
                    }
                }
                ourFleetNumsInUse.Add(numToReturn);
                ourFleetNumsInUse.Sort();
            }
            return numToReturn;
        }
        public void RemoveFleetNumInUse(CivEnum civEnum, int fleetInt)
        {
            fleetNumsInUse[civEnum].Remove(fleetInt);
        }
        public void ExposeAllFleetInsigniaSprites(CivEnum civEnum)
        {
            if (!localPlayerCanSeeMyInsigniaList.Contains(civEnum))
            {
                localPlayerCanSeeMyInsigniaList.Add(civEnum);
            }

            foreach (var fleetController in FleetControllerList)
            {
                if (fleetController.FleetData.CivEnum == civEnum)
                {
                    FleetChildFields fleetChildFields = fleetController.GetComponent<FleetChildFields>();
                    if (fleetChildFields != null)
                    {
                        // Reveal actual insignia
                        if (fleetChildFields.InsigniaGO != null)
                        {
                            fleetChildFields.InsigniaGO.SetActive(true);
                            var sr = fleetChildFields.InsigniaGO.GetComponent<SpriteRenderer>();
                            if (sr != null) sr.enabled = true;
                        }

                        // Hide unknown insignia
                        if (fleetChildFields.InsigniaUnknownGO != null)
                        {
                            fleetChildFields.InsigniaUnknownGO.SetActive(false);
                        }

                        // Reveal fleet name
                        if (fleetChildFields.FleetNameGO != null)
                        {
                            fleetChildFields.FleetNameGO.SetActive(true);
                        }
                    }
                }
            }
        }

        internal void RemoveFleetConIfShipListIsEmpty(ShipController shipController)
        {
            int foundOne = -1;
            for (int i = 0; i < FleetControllersInGame.Count; i++)
            {
                if (shipController.ShipData.CurrentFleetController == FleetControllerList[i])
                {
                    if (FleetControllerList[i].FleetData.ShipsList.Count == 0)
                    {
                        foundOne = i;
                    }
                }
            }
            if (foundOne > -1)
            {
                // Commented out - original code preserved
            }
        }

        internal void DestroyFleetController(FleetController fleetController)
        {
            if (fleetController == null) return;
            RemoveFleetNumInUse(fleetController.FleetData.CivEnum, fleetController.FleetData.FleetInt);
            if (FleetControllersInGame.Contains(fleetController))
                FleetControllersInGame.Remove(fleetController);
            if (FleetControllerList.Contains(fleetController))
                FleetControllerList.Remove(fleetController);
            if (fleetController.FleetUIGameObject != null)
                Destroy(fleetController.FleetUIGameObject);
            if (fleetController.DropLine != null)
                Destroy(fleetController.DropLine.gameObject);

            // Spawned FleetControllers are Mirror NetworkIdentities (see InstantiateFleet's
            // NetworkServer.Spawn) - a plain Destroy() on the server leaves a stale entry in
            // NetworkServer.spawned and never tells clients to remove their own copy.
            // NetworkServer.Destroy sends the proper destroy message first. On a genuine remote
            // client (convoy/ship-transfer cleanup, still unnetworked - see SyncedIsNewFleet's
            // comment) there's no server to route through, so fall back to the local-only Destroy
            // exactly as before.
            if (NetworkServer.active)
                NetworkServer.Destroy(fleetController.gameObject);
            else
                Destroy(fleetController.gameObject);
        }

        // Distance (in galaxy-map units) beyond which a fleet-merge/deploy is routed through a
        // temporary convoy fleet that travels at warp, instead of an instant transfer.
        public const float ConvoyDistanceThreshold = 30f;

        /// <summary>
        /// Creates an empty temporary "convoy" fleet used to carry redeployed ships across a distance
        /// too far for an instant merge. Ships are NOT added here — the caller uses the same
        /// FleetController/StarSysController.AddToShipList used by every other ship transfer, then
        /// configures IsConvoy/ConvoyMergeTarget/ConvoyMergeSystem and sets the convoy's destination
        /// (SetInterceptTarget for a moving fleet target, or Destination for a system target).
        /// Exactly one of sourceFleet/sourceSystem should be non-null; it supplies the spawn position.
        /// </summary>
        public FleetController CreateConvoyFleet(FleetController sourceFleet, StarSysController sourceSystem, CivEnum civEnum)
        {
            FleetSO fleetSO = GetFleetSO_byInt((int)civEnum);
            if (fleetSO == null) return null;

            CivData thisCivData = CivManager.Instance.GetCivDataByCivEnum(fleetSO.CivOwnerEnum);
            Vector3 position = sourceFleet != null ? sourceFleet.transform.position :
                sourceSystem != null ? sourceSystem.StarSysData.GetPosition() : Vector3.zero;

            FleetData fleetData = new FleetData(fleetSO);
            fleetData.CurrentWarpFactor = 0f;
            fleetData.CivLongName = thisCivData.CivLongName;
            fleetData.CivShortName = thisCivData.CivShortName;
            fleetData.CivEnum = thisCivData.CivEnum;
            fleetData.PlayerId = thisCivData.PlayerId;
            // Insignia already set from fleetSO.Insignia by the FleetData(fleetSO) constructor above —
            // don't overwrite it with the civ's own insignia here.
            fleetData.ShipsList = new List<ShipController>();
            fleetData.IsConvoy = true;

            return InstantiateFleet(sourceFleet, sourceSystem, fleetData, position, true);
        }

        // Server-side counterpart of FleetMenuUIController.ClickNewFleetButton(FleetController) - splits
        // an empty new fleet off of sourceFleet at its current position, for the player to drag ships
        // into via the ship-deploy UI. Called via LocalHumanPlayerController.CmdCreateSplitFleet so this
        // also works when a non-host client clicks the button (InstantiateFleet is server-only).
        [Server]
        public FleetController ServerCreateSplitFleet(FleetController sourceFleet)
        {
            if (sourceFleet == null || sourceFleet.FleetData == null || sourceFleet.FleetData.ShipsList.Count < 2)
                return null;

            FleetSO fleetSO = GetFleetSO_byInt((int)sourceFleet.FleetData.CivEnum);
            var position = sourceFleet.FleetData.GetPosition();
            CivData thisCivData = CivManager.Instance.GetCivDataByCivEnum(fleetSO.CivOwnerEnum);

            FleetData fleetData = new FleetData(fleetSO);
            fleetData.CurrentWarpFactor = 0f;
            fleetData.CivLongName = thisCivData.CivLongName;
            fleetData.CivShortName = thisCivData.CivShortName;
            fleetData.CivEnum = thisCivData.CivEnum;
            fleetData.PlayerId = thisCivData.PlayerId;
            fleetData.ShipsList = new List<ShipController>();

            var emptyStarSysCon = StarSysManager.Instance.InstantiateEmptyStarSysController();
            FleetController newFleet = InstantiateFleet(sourceFleet, emptyStarSysCon, fleetData, position, true);
            Destroy(emptyStarSysCon.gameObject);
            return newFleet;
        }

        // Server-side counterpart of ShipListUI_Item's drag-and-drop ship reassignment. That UI item
        // mutates its own client's local FleetData.ShipsList directly for instant visual feedback, but
        // (like InstantiateFleet's callers above) never reaches the server on its own - on the host that
        // local copy IS the server's authoritative copy, so it "just works" there, but a non-host
        // client's edits silently never reach the server's real fleet data at all. Called via
        // LocalHumanPlayerController.CmdTransferShip so the server's own ShipsList lists (which combat,
        // fog of war, and every other system actually reads) stay correct regardless of which client
        // performed the drag.
        [Server]
        public void ServerTransferShip(FleetController fromFleet, FleetController toFleet, int shipID)
        {
            if (fromFleet == null || toFleet == null || fromFleet.FleetData == null || toFleet.FleetData == null || shipID == 0)
                return;
            if (fromFleet == toFleet)
                return;

            ShipController ship = fromFleet.FleetData.ShipsList.Find(s => s != null && s.ShipData != null && s.ShipData.ShipID == shipID);
            if (ship == null)
            {
                Debug.LogWarning($"ServerTransferShip: ship id {shipID} not found in fleet '{fromFleet.name}' - already moved, or the requesting client's local copy has diverged from the server.");
                return;
            }

            fromFleet.FleetData.ShipsList.Remove(ship);
            if (!toFleet.FleetData.ShipsList.Contains(ship))
                toFleet.FleetData.ShipsList.Add(ship);

            ship.ShipData.CurrentFleetController = toFleet;
            ship.ShipData.CurrentStarSysController = null;
            if (ship.transform != null && toFleet.gameObject != null)
                ship.transform.SetParent(toFleet.transform, false);

            fromFleet.UpdateMaxWarp();
            toFleet.UpdateMaxWarp();

            Debug.Log($"ServerTransferShip: moved ship id {shipID} ('{ship.ShipData.ShipName}') from fleet '{fromFleet.name}' to '{toFleet.name}'. fromFleet now has {fromFleet.FleetData.ShipsList.Count}, toFleet now has {toFleet.FleetData.ShipsList.Count}.");
        }

        // Server-side counterpart of StarSysMenuUIController.ClickNewFleetButton(StarSysController) -
        // forms an empty new fleet at sysCon's position, for the player to drag docked ships into via
        // the ship-deploy UI. Called via LocalHumanPlayerController.CmdCreateFleetFromSystem so this also
        // works when a non-host client clicks the button (InstantiateFleet is server-only).
        [Server]
        public FleetController ServerCreateFleetFromSystem(StarSysController sysCon)
        {
            if (sysCon == null || sysCon.StarSysData.ShipsList.Count == 0) return null;

            FleetSO fleetSO = GetFleetSO_byInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
            var position = sysCon.StarSysData.GetPosition();
            CivData thisCivData = CivManager.Instance.GetCivDataByCivEnum(fleetSO.CivOwnerEnum);

            FleetData fleetData = new FleetData(fleetSO);
            fleetData.CurrentWarpFactor = 0f;
            fleetData.CivLongName = thisCivData.CivLongName;
            fleetData.CivShortName = thisCivData.CivShortName;
            fleetData.CivEnum = thisCivData.CivEnum;
            fleetData.PlayerId = thisCivData.PlayerId;
            fleetData.ShipsList = new List<ShipController>();

            return InstantiateFleet(null, sysCon, fleetData, position, true);
        }

        internal void RemoveFogWarRevealer(csFogWar.FogRevealer tempFogRevealerFleet)
        {
            fogWar.RemoveFogRevealer(tempFogRevealerFleet);
        }

        /// <summary>
        /// Called AFTER fog grid is initialized - sets initial visibility for all non-local fleets
        /// </summary>
        public void InitializeFleetFogAgents()
        {
            if (fogWar == null)
            {
                Debug.LogWarning("InitializeFleetFogAgents: fogWar is null!");
                return;
            }

            Debug.Log("=== InitializeFleetFogAgents: Fog grid ready, updating fleet visibility ===");

            foreach (var fleet in FleetControllerList)
            {
                if (fleet == null) continue;

                // Skip local player fleets - they have FogRevealers, not agents
                if (GameController.Instance.AreWeLocalPlayer(fleet.FleetData.CivEnum))
                {
                    Debug.Log($"  Skipping local fleet: {fleet.name}");
                    continue;
                }

                var fogAgent = fleet.GetComponent<csFogVisibilityAgent>();
                if (fogAgent != null && fogAgent.spriteRenderers != null && fogAgent.spriteRenderers.Count > 0)
                {
                    // NOW fog grid is ready - check visibility
                    bool isVisible = fogWar.CheckVisibility(fleet.transform.position, 0);

                    foreach (var sr in fogAgent.spriteRenderers)
                    {
                        sr.enabled = isVisible;
                    }

                    Debug.Log($"  Fleet '{fleet.name}' at {fleet.transform.position} visibility: {isVisible}");
                }
                else
                {
                    Debug.LogWarning($"  Fleet '{fleet.name}' missing fog agent or sprite renderers!");
                }
            }

            Debug.Log("=== InitializeFleetFogAgents: Complete ===");
        }

        [Header("Debug Options")]
        [SerializeField] private bool showFleetVisibilityDebug = false; // Toggle in Inspector

        // Add this method for debugging
        private void OnGUI()
        {
            if (!showFleetVisibilityDebug || FleetControllerList == null) return;

            GUILayout.BeginArea(new Rect(10, 300, 400, 400));
            GUILayout.Label("=== Fleet Visibility Debug ===");

            foreach (var fleet in FleetControllerList)
            {
                if (fleet == null) continue;

                bool isLocal = GameController.Instance.AreWeLocalPlayer(fleet.FleetData.CivEnum);
                var fleetChildFields = fleet.GetComponent<FleetChildFields>();

                string visibility = "UNKNOWN";
                if (fleetChildFields != null)
                {
                    bool insigniaActive = fleetChildFields.InsigniaGO.activeSelf;
                    bool unknownActive = fleetChildFields.InsigniaUnknownGO.activeSelf;
                    visibility = $"Insignia:{insigniaActive}, Unknown:{unknownActive}";
                }

                var fogAgent = fleet.GetComponent<csFogVisibilityAgent>();
                bool hasFog = fogAgent != null;

                GUILayout.Label($"{fleet.name} | Local:{isLocal} | {visibility} | HasFogAgent:{hasFog}");
            }

            GUILayout.EndArea();
        }

        // Helper method to set layer recursively
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;

            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    

        private void OnDestroy()
        {
            ServiceLocator.Unregister<FleetManager>(); }
    }
}