// Ignore Spelling: Nums Revealer
using BOTF3D.Combat;

using BOTF3D.UI;
using FischlWorks_FogWar;
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
            newFleet.transform.localScale = new Vector3(0.7f, 0.7f, 1);
            int fleetInt = GetNewFleetInt(fleetData.CivEnum);
            newFleet.gameObject.name = fleetData.CivShortName.ToString() + " Fleet " + fleetInt.ToString();
            fleetData.FleetName = "Fleet " + fleetInt.ToString();
            newFleet.FleetData.FleetInt = fleetInt;
            FleetControllersInGame.Add(newFleet);
            newFleet.FleetData.CurrentWarpFactor = 0f;

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

            FleetChildFields fleetChildFields = newFleet.GetComponent<FleetChildFields>();
            SpriteRenderer srInsignia = fleetChildFields.InsigniaGO.GetComponent<SpriteRenderer>();
            srInsignia.sprite = newFleet.FleetData.Insignia;
            SpriteRenderer srInsigniaUnknown = fleetChildFields.InsigniaUnknownGO.GetComponent<SpriteRenderer>();

            if (GameController.Instance.AreWeLocalPlayer(fleetData.CivEnum))
            {
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
                if (localPlayerCanSeeMyInsigniaList.Contains(fleetData.CivEnum))
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

            // The line from Fleet to underlying galaxy image
            MapLineMovable[] ourLineToGalaxyImageScript = newFleet.gameObject.GetComponentsInChildren<MapLineMovable>();

            // VALIDATION: Check if MapLineMovable components exist
            if (ourLineToGalaxyImageScript == null || ourLineToGalaxyImageScript.Length == 0)
            {
                Debug.LogError($"FleetManager.InstantiateFleet: No MapLineMovable found in FleetController prefab '{fleetPrefab.name}'! " +
                               $"The DropLine cannot be created for fleet '{newFleet.name}'. " +
                               $"Add a MapLineMovable component to the FleetController prefab in the Inspector.");
            }
            else
            {
                // CRITICAL: Ensure galaxyImage exists before setting up DropLine
                if (galaxyImage == null)
                {
                    Debug.LogWarning($"FleetManager.InstantiateFleet: galaxyImage is null, attempting to find it again...");
                    FindGalaxyReferences();

                    if (galaxyImage == null)
                    {
                        Debug.LogError($"FleetManager.InstantiateFleet: galaxyImage is STILL null! Cannot set up DropLine for fleet '{newFleet.name}'");
                    }
                }

                if (galaxyImage != null)
                {
                    bool foundDropLine = false;

                    for (int i = 0; i < ourLineToGalaxyImageScript.Length; i++)
                    {
                        // SAFETY: Check fleetChildFields and DropLine exist
                        if (fleetChildFields == null || fleetChildFields.DropLine == null)
                        {
                            Debug.LogWarning($"FleetManager.InstantiateFleet: fleetChildFields.DropLine is null for fleet '{newFleet.name}'");
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
                        Debug.LogWarning($"FleetManager.InstantiateFleet: Found {ourLineToGalaxyImageScript.Length} MapLineMovable(s) but none matched fleetChildFields.DropLine for fleet '{newFleet.name}'");
                    }
                }
            }

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