// Ignore Spelling: Nums Revealer
using Assets.GamePlay;
using Assets.UI;
using FischlWorks_FogWar;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Core
{
    /// <summary>
    /// Instantiates the fleets (a FleetController and a FleetData) using FleetSO
    /// </summary>
    public class FleetManager : MonoBehaviour
    {
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

        public void Start()
        {
            for (int i = 0; i < CivManager.Instance.CivSOListAllPossible.Count; i++)
            {
                fleetNumsInUse.Add(CivManager.Instance.CivSOListAllPossible[i].CivEnum, new List<int>());
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

        public FleetController InstantiateFleet(FleetController originalFleetCon, StarSysController systCon, FleetData newFleetData, Vector3 position, bool newFleet)
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

            newFleetData.ShipsList.RemoveAll(item => item == null);
            Transform newTrans = null;

            FleetController newFleetController = Instantiate(fleetPrefab, new Vector3(0, 0, 0), Quaternion.identity);

            if (newFleet)
            {
                GalaxyMenuUIController.Instance.FleetSelectedForShipDeploy = newFleetController;
            }

            FleetControllerList.Add(newFleetController);
            newFleetController.gameObject.layer = 6; // galaxy layer
            newFleetController.BackgroundGalaxyImage = galaxyImage;
            newFleetController.FleetData = newFleetData;
            newFleetController.GalaxyCanvasGo = galaxyCanvasGO;

            var transGalaxyCenter = GalaxyCenter.transform; // Safe now - we checked above

            if (systCon.StarSysData != null && !newFleet)
            {
                newTrans = systCon.transform;
                Destroy(originalFleetCon.gameObject); // destroy the empty original fleet controller
            }
            else if (originalFleetCon != null && newFleet)
            {
                newTrans = originalFleetCon.transform;
            }
            else if (systCon.StarSysData != null && newFleet)
            {
                newTrans = systCon.transform;
            }

            newFleetController.transform.SetParent(transGalaxyCenter, true);

            if (newTrans != null)
            {
                if (!newFleet)
                {
                    newFleetController.transform.Translate(new Vector3(newTrans.position.x + 15f, newTrans.position.y + 15f, newTrans.position.z));
                }
                else
                {
                    if (newFleetSpacer > 10f)
                        newFleetSpacer = 0;
                    newFleetController.transform.Translate(new Vector3(newTrans.position.x - 15f - newFleetSpacer, newTrans.position.y + 15f - newFleetSpacer, newTrans.position.z));
                    newFleetSpacer = newFleetSpacer + 5f;
                }
            }

            newFleetData.Position = newFleetController.transform.position;

            if (!newFleet)
                ShipManager.Instance.BuildShipsOfFirstFleet(newFleetController);

            newFleetController.transform.localScale = new Vector3(0.7f, 0.7f, 1);
            int fleetInt = GetNewFleetInt(newFleetData.CivEnum);
            newFleetController.gameObject.name = newFleetData.CivShortName.ToString() + " Fleet " + fleetInt.ToString();
            newFleetData.Name = "Fleet " + fleetInt.ToString();
            newFleetController.FleetData.FleetInt = fleetInt;
            FleetControllersInGame.Add(newFleetController);
            newFleetController.FleetData.CurrentWarpFactor = 0f;

            TextMeshProUGUI TheText = newFleetController.gameObject.GetComponentInChildren<TextMeshProUGUI>();
            if (TheText != null)
            {
                TheText.text = newFleetController.FleetData.Name;
                newFleetData.Name = TheText.text;
            }

            FleetChildFields fleetChildFields = newFleetController.GetComponent<FleetChildFields>();
            SpriteRenderer srInsignia = fleetChildFields.InsigniaGO.GetComponent<SpriteRenderer>();
            srInsignia.sprite = newFleetController.FleetData.Insignia;
            SpriteRenderer srInsigniaUnknown = fleetChildFields.InsigniaUnknownGO.GetComponent<SpriteRenderer>();

            if (GameController.Instance.AreWeLocalPlayer(newFleetData.CivEnum))
            {
                srInsigniaUnknown.enabled = false;
                srInsignia.enabled = true;

                if (fogWar != null)
                {
                    // ✅ Increase radius if galaxy is large (adjust based on your galaxy size)
                    float revealRadius = 200f; // Try 400f or 600f if fleets are far apart

                    var ourFogRevealerFleet = new csFogWar.FogRevealer(newFleetController.transform, 600, true);
                    fogWar.AddFogRevealer(ourFogRevealerFleet);
                    TempFogRevealerFleet = ourFogRevealerFleet;

                    Debug.Log($"✅ FogRevealer added to local fleet '{newFleetController.name}' with radius {revealRadius}");
                }
            }
            else // Non-local player fleet
            {
                // ✅ Keep fleet name hidden until discovered
                fleetChildFields.FleetNameGO.SetActive(false);

                // ✅ Show unknown insignia, hide actual insignia
                srInsignia.enabled = false;
                srInsigniaUnknown.enabled = true;

                // ✅ CRITICAL: Ensure the InsigniaUnknownGO itself is ACTIVE (not just SpriteRenderer.enabled)
                if (fleetChildFields.InsigniaUnknownGO != null)
                {
                    fleetChildFields.InsigniaUnknownGO.SetActive(true);

                    // ✅ Ensure it's on the correct layer
                    fleetChildFields.InsigniaUnknownGO.layer = 6; // Galaxy layer

                    Debug.Log($"✅ InsigniaUnknownGO active for non-local fleet '{newFleetController.name}' on layer {fleetChildFields.InsigniaUnknownGO.layer}");
                }

                // ✅ Ensure the entire fleet GameObject is visible
                newFleetController.gameObject.SetActive(true);

                // SAFETY: Only add fog visibility agent if fogWar exists
                if (fogWar != null)
                {
                    var ourFogVisibilityAgent = newFleetController.gameObject.AddComponent<csFogVisibilityAgent>();
                    ourFogVisibilityAgent.FogWar = fogWar;

                    // ✅ CRITICAL: Configure the fog visibility agent
                    //ourFogVisibilityAgent.IsVisible = false; // Start hidden by fog
                    //ourFogVisibilityAgent.SetVisibilityDistance(600f); // Adjust based on your galaxy size

                    Debug.Log($"✅ csFogVisibilityAgent added to non-local fleet '{newFleetController.name}'");
                }
                else
                {
                    Debug.LogWarning($"⚠️ FogWar is NULL! Non-local fleet '{newFleetController.name}' won't have fog visibility");
                }
            }

            // The line from Fleet to underlying galaxy image
            MapLineMovable[] ourLineToGalaxyImageScript = newFleetController.gameObject.GetComponentsInChildren<MapLineMovable>();

            // VALIDATION: Check if MapLineMovable components exist
            if (ourLineToGalaxyImageScript == null || ourLineToGalaxyImageScript.Length == 0)
            {
                Debug.LogError($"FleetManager.InstantiateFleet: No MapLineMovable found in FleetController prefab '{fleetPrefab.name}'! " +
                               $"The DropLine cannot be created for fleet '{newFleetController.name}'. " +
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
                        Debug.LogError($"FleetManager.InstantiateFleet: galaxyImage is STILL null! Cannot set up DropLine for fleet '{newFleetController.name}'");
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
                            Debug.LogWarning($"FleetManager.InstantiateFleet: fleetChildFields.DropLine is null for fleet '{newFleetController.name}'");
                            continue;
                        }

                        if (ourLineToGalaxyImageScript[i].gameObject == fleetChildFields.DropLine)
                        {
                            ourLineToGalaxyImageScript[i].GetLineRenderer();
                            ourLineToGalaxyImageScript[i].lineRenderer.startColor = Color.red;
                            ourLineToGalaxyImageScript[i].lineRenderer.endColor = Color.red;
                            ourLineToGalaxyImageScript[i].transform.SetParent(newFleetController.transform, false);

                            Vector3 galaxyPlanePoint = new Vector3(newFleetController.transform.position.x,
                                galaxyImage.transform.position.y, newFleetController.transform.position.z);
                            Vector3[] points = { newFleetController.transform.position, galaxyPlanePoint };
                            ourLineToGalaxyImageScript[i].SetUpLine(points);
                            newFleetController.DropLine = ourLineToGalaxyImageScript[i];
                            foundDropLine = true;
                            Debug.Log($"✅ DropLine set up for fleet '{newFleetController.name}'");
                            break; // Found it, no need to continue
                        }
                    }

                    if (!foundDropLine)
                    {
                        Debug.LogWarning($"FleetManager.InstantiateFleet: Found {ourLineToGalaxyImageScript.Length} MapLineMovable(s) but none matched fleetChildFields.DropLine for fleet '{newFleetController.name}'");
                    }
                }
            }

            // SAFETY: Check GalaxyCenter still exists
            if (GalaxyCenter != null)
            {
                newFleetController.FleetData.Destination = GalaxyCenter;
            }

            foreach (var civCon in CivManager.Instance.CivControllersInGame)
            {
                if (civCon.CivData.CivEnum == newFleetData.CivEnum)
                {
                    newFleetData.CivController = civCon;
                    break;
                }
            }

            newFleetController.gameObject.SetActive(true);
            newFleetController.UpdateMaxWarp();
            InstantiateFleetUIGameObject(newFleetController, newFleet);

            return newFleetController;
        }
        private void InstantiateFleetUIGameObject(FleetController fleetCon, bool newFleet)
        {
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
            if (fleetCon.FleetData.CivEnum == GameController.Instance.GameData.LocalPlayerCivEnum)
            {
                if (fleetCon.FleetUIGameObject == null)
                {
                    GameObject thisFleetUIGameObject = (GameObject)Instantiate(fleetUIPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                    thisFleetUIGameObject.SetActive(true);
                    thisFleetUIGameObject.layer = 5;
                    fleetCon.FleetUIGameObject = thisFleetUIGameObject;
                    fleetCon.FleetUIGameObject.SetActive(true);
                    thisFleetUIGameObject.transform.SetParent(fleetUIGOContentParent.transform, false);
                    FleetUI_Fields fleetUI_Fields = thisFleetUIGameObject.GetComponent<FleetUI_Fields>();
                    if (fleetUI_Fields != null && fleetUI_Fields.FleetShipContentGO != null)
                    {
                        fleetCon.FleetData.ShipListUIParent = fleetUI_Fields.FleetShipContentGO;
                        FleetMenuUIController.Instance.SetupFleetUIElements(fleetCon, thisFleetUIGameObject);
                    }
                    else
                    {
                        Debug.LogWarning($"InstantiateFleetUIGameObject: ShipContent not found in UI prefab for system {fleetCon.name}");
                    }
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
            go.IsDestroyed();
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
            localPlayerCanSeeMyInsigniaList.Add(civEnum);
            foreach (var fleetController in FleetControllerList)
            {
                if (fleetController.FleetData.CivEnum == civEnum)
                {
                    FleetChildFields fleetChildFields = fleetController.GetComponent<FleetChildFields>();
                    fleetChildFields.InsigniaUnknownGO.SetActive(false);
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
            RemoveFleetNumInUse(fleetController.FleetData.CivEnum, fleetController.FleetData.FleetInt);
            if (FleetControllersInGame.Contains(fleetController))
                FleetControllersInGame.Remove(fleetController);
            RemoveFleetNumInUse(fleetController.FleetData.CivEnum, fleetController.FleetData.FleetInt);
            Destroy(fleetController.FleetUIGameObject);
            Destroy(fleetController.DropLine.gameObject);
            Destroy(fleetController.gameObject);
        }

        internal void RemoveFogWarRevealer(csFogWar.FogRevealer tempFogRevealerFleet)
        {
            fogWar.RemoveFogRevealer(tempFogRevealerFleet);
        }

        // Add this method for debugging
        private void OnGUI()
        {
            if (FleetControllerList == null) return;

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
    }
}