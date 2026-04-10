// Ignore Spelling: BOTF

using BOTF3D.Core;
using BOTF3D.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

namespace BOTF3D.GamePlay
{
    public class SceneController : MonoBehaviour
    {
        public static SceneController Instance { get; private set; }

        private static string previousSceneName;
        public List<GameObject> persistentObjects;

        [Header("Scene References (assign in Inspector or found at runtime)")]
        [SerializeField] private GameObject galaxyCameraDragNDrop;
        public GameObject ShipCombatCameraGO;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                MarkPeristentObject();
            }
            else
            {
                CleanUPAndDestroy();
                return;
            }
        }
        private void Start()
        {
            // Subscribe to scene loaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"GameManager: Scene loaded: {scene.name}");

            // When MainMenu scene loads, find the controller
            if (scene.name.Contains("MainMenu") && GameManager.Instance.mainMenuUIController == null)
            {
                GameManager.Instance.InitializeGameManagerWithMainMenuUIController();
            }

            // When GalaxyScene loads, find the galaxy image
            if (scene.name == "GalaxyScene")
            {
                //RegisterGalaxy();
                GalaxyView.Instance.CalculateGalaxyBounds();
            }
        }
        /// <summary>
        /// Lazy initialization - finds camera component when needed.
        /// </summary>
        private GameObject GetGalaxyCameraDragNDrop()
        {
            if (galaxyCameraDragNDrop == null)
            {
                // ✅ FIXED: Search by component instead of GameObject name
                var cameraComponent = FindFirstObjectByType<GalaxyCameraDragMoveZoom>();

                if (cameraComponent != null)
                {
                    galaxyCameraDragNDrop = cameraComponent.gameObject;
                    Debug.Log($"SceneController: Found GalaxyCameraDragMoveZoom on GameObject '{galaxyCameraDragNDrop.name}'");
                }
                else
                {
                    Debug.Log("SceneController: GalaxyCameraDragMoveZoom not found yet (scene may be loading or inactive)");
                }
            }

            return galaxyCameraDragNDrop;
        }
        /// <summary>
        /// Called by GalaxySceneInitializer when GalaxyScene loads.
        /// </summary>
        public void SetGalaxyReferences(GameObject galaxyCamera)
        {
            galaxyCameraDragNDrop = galaxyCamera;
            Debug.Log("SceneController: GalaxyCameraDragMoveZoom set by initializer");
        }

        private void CleanUPAndDestroy()
        {
            Destroy(gameObject);
        }

        private void MarkPeristentObject()
        {
            for (int i = 0; i < persistentObjects.Count; i++)
            {
                if (persistentObjects[i] != null)
                {
                    DontDestroyOnLoad(persistentObjects[i]);
                }
            }
        }

        /// <summary>
        /// Load combat scene additively, keeping galaxy scene loaded
        /// </summary>
        public void LoadCombatScene(FleetController playerFleet, FleetController enemyFleet, StarSysController starSysCon)
        {
            Debug.Log("=== SceneController: LoadCombatScene ===");

            // ✅ Hide diplomacy UI before combat (use EXISTING method)
            if (DiplomacyMenuUIController.Instance != null)
            {
                DiplomacyMenuUIController.Instance.HideA_DiplomacyMenuView(); // ✅ This exists!
                DiplomacyMenuUIController.Instance.HideDiplomacyMenuView(); // ✅ And this!
                Debug.Log("  Closed diplomacy UI");
            }
            List<ShipController> shipControllers1 = new List<ShipController>();
            List<ShipController> shipControllers2 = new List<ShipController>();
            CombatType combatType = CombatType.None;
            if (playerFleet == null)
            {
                shipControllers1 = starSysCon.StarSysData.ShipsList;
                shipControllers2 = enemyFleet.FleetData.ShipsList;
                combatType = CombatType.StarSystemCombat;
                Debug.Log($"  Player fleet null and '{starSysCon.name}' in combat with {enemyFleet.name}");
            }
            else if (starSysCon == null)
            {
                shipControllers1 = playerFleet.FleetData.ShipsList;
                shipControllers2 = enemyFleet.FleetData.ShipsList;
                combatType = CombatType.DeepSpaceCombat;
                Debug.Log($"  Star system null and '{playerFleet.name}' in combat with '{playerFleet.name}' and enemy fleet '{enemyFleet.name}' ShipControllers");
            }
            else if (enemyFleet == null)
            {
                shipControllers1 = playerFleet.FleetData.ShipsList;
                shipControllers2 = starSysCon.StarSysData.ShipsList;
                combatType = CombatType.StarSystemCombat;
                Debug.Log($"  Enemy fleet null and '{playerFleet.name}' in combat with '{starSysCon.name}' ShipControllers");
            }
            // Start combat scene load coroutine
            StartCoroutine(LoadCombatSceneAdditive(shipControllers1, shipControllers2, combatType));
        }

        private IEnumerator LoadCombatSceneAdditive(List<ShipController> shipControllers1, List<ShipController> shipControllers2, CombatType combatType)
        {
            Debug.Log("=== LoadCombatSceneAdditive: Starting async load ===");

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("CombatScene", LoadSceneMode.Additive);

            while (!asyncLoad.isDone)
            {
                Debug.Log($"  Loading combat scene: {asyncLoad.progress * 100:F1}%");
                yield return null;
            }

            Debug.Log("✅ Combat scene loaded");

            // ✅ Get scene references
            Scene galaxyScene = SceneManager.GetSceneByName("GalaxyScene");
            Scene combatScene = SceneManager.GetSceneByName("CombatScene");

            // ✅ STEP 1: FIRST disable ALL EventSystems in BOTH scenes (while objects are still inactive)
            Debug.Log("Step 1: Disabling all EventSystems...");

            var allEventSystems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
            //👉 In this scenario: we don’t care about order, just need to find them all So Mode None is exactly what you want
            foreach (var es in allEventSystems)
            {
                es.enabled = false;
                Debug.Log($"  ✅ Disabled EventSystem on: '{es.gameObject.name}' in scene '{es.gameObject.scene.name}'");
            }

            // ✅ STEP 2: Deactivate all galaxy scene objects
            if (galaxyScene.isLoaded)
            {
                foreach (GameObject go in galaxyScene.GetRootGameObjects())
                {
                    go.SetActive(false);
                }
                Debug.Log("  ✅ Galaxy scene deactivated");
            }

            // ✅ STEP 3: Set combat scene as active
            if (combatScene.isLoaded)
            {
                SceneManager.SetActiveScene(combatScene);
                Debug.Log($"  ✅ Combat scene set as active scene");
            }
            else
            {
                Debug.LogError("  ❌ Combat scene failed to load!");
                yield break;
            }

            // ✅ STEP 4: Activate combat scene objects
            var combatRootObjects = combatScene.GetRootGameObjects();
            int activatedCount = 0;

            foreach (var obj in combatRootObjects)
            {
                if (!obj.activeSelf)
                {
                    obj.SetActive(true);
                    activatedCount++;
                    Debug.Log($"    ✅ ACTIVATED: '{obj.name}'");
                }
            }

            Debug.Log($"  ✅ Activated {activatedCount} combat objects");

            // ✅ STEP 5: Enable ONLY Combat EventSystem
            UnityEngine.EventSystems.EventSystem combatEventSystem = null;

            foreach (var obj in combatRootObjects)
            {
                var es = obj.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true);
                if (es != null)
                {
                    combatEventSystem = es;
                    es.enabled = true;
                    Debug.Log($"  ✅ Enabled EventSystem in Combat scene: '{obj.name}'");
                    break; // Only enable the FIRST one found
                }
            }

            // ✅ Create EventSystem if missing
            if (combatEventSystem == null)
            {
                Debug.LogWarning("  ⚠️ No EventSystem found in Combat scene - creating one...");
                var eventSystemGO = new GameObject("EventSystem (Combat)");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                SceneManager.MoveGameObjectToScene(eventSystemGO, combatScene);
                Debug.Log("  ✅ Created EventSystem for Combat scene");
            }

            // ✅ STEP 6: Double-check - disable any OTHER EventSystems that might have re-enabled
            var allEventSystemsAfter = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>(true);
            int disabledCount = 0;

            foreach (var es in allEventSystemsAfter)
            {
                if (es != combatEventSystem && es.enabled)
                {
                    es.enabled = false;
                    disabledCount++;
                    Debug.LogWarning($"  ⚠️ Disabled duplicate EventSystem on: '{es.gameObject.name}'");
                }
            }

            if (disabledCount > 0)
            {
                Debug.Log($"  ✅ Disabled {disabledCount} duplicate EventSystems");
            }

            // ✅ Wait TWO frames for Awake() and Start() to run
            yield return null;
            yield return null;

            // ✅ Enable combat camera
            if (ShipCombatCameraController.Instance != null)
            {
                var camera = ShipCombatCameraController.Instance.GetComponentInChildren<Camera>();
                if (camera != null)
                {
                    camera.enabled = true;
                    Debug.Log($"  ✅ Combat camera enabled");
                }
            }
            CombatManager.Instance.RequestCombat(shipControllers1, shipControllers2, combatType);
            Debug.Log("=== LoadCombatSceneAdditive: Complete ===");
        }

        /// <summary>
        /// Unload combat scene and return to galaxy
        /// </summary>
        public void ReturnToGalaxyFromCombat()
        {
            Debug.Log("ReturnToGalaxyFromCombat: Starting");

            // ✅ 1. Unload combat scene
            StartCoroutine(UnloadCombatSceneAndResumeGalaxy());
        }

        private IEnumerator UnloadCombatSceneAndResumeGalaxy()
        {
            Debug.Log("=== UnloadCombatSceneAndResumeGalaxy: Starting ===");

            Scene combatScene = SceneManager.GetSceneByName("CombatScene");
            if (combatScene.isLoaded)
            {
                AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(combatScene);

                while (!asyncUnload.isDone)
                {
                    yield return null;
                }

                Debug.Log("  ✅ Combat scene unloaded");
            }

            // ✅ Set galaxy scene as active
            Scene galaxyScene = SceneManager.GetSceneByName("GalaxyScene");
            if (galaxyScene.isLoaded)
            {
                SceneManager.SetActiveScene(galaxyScene);
                Debug.Log("  ✅ Galaxy scene set as active");
            }

            // ✅ CRITICAL: Reactivate galaxy scene objects
            foreach (GameObject go in galaxyScene.GetRootGameObjects())
            {
                go.SetActive(true);
            }
            Debug.Log("  ✅ Galaxy scene objects reactivated");

            // ✅ CRITICAL: Wait TWO frames for Awake() and Start() to complete
            yield return null;
            yield return null;

            // ✅ NEW: Force activate main galaxy menu ribbon AFTER UI controllers have initialized
            if (GalaxyMenuUIController.Instance != null)
            {
                Debug.Log("  ✅ Forcing activation of main galaxy menu ribbon...");

                // Find CanvasGalaxy
                var canvasGalaxy = GameObject.Find("CanvasGalaxy");
                if (canvasGalaxy != null)
                {
                    // Search for MainGalaxyMenuRibbon
                    Transform menuRibbon = canvasGalaxy.transform.Find("MainGalaxyMenuRibbon");
                    if (menuRibbon != null)
                    {
                        menuRibbon.gameObject.SetActive(true);
                        Debug.Log($"    ✅ Activated MainGalaxyMenuRibbon");
                    }
                    else
                    {
                        Debug.LogWarning("    ⚠️ MainGalaxyMenuRibbon not found! Searching recursively...");
                        menuRibbon = FindInHierarchy(canvasGalaxy.transform, "MainGalaxyMenuRibbon");
                        if (menuRibbon != null)
                        {
                            menuRibbon.gameObject.SetActive(true);
                            Debug.Log($"    ✅ Activated MainGalaxyMenuRibbon (recursive search)");
                        }
                        else
                        {
                            Debug.LogError("    ❌ Could not find MainGalaxyMenuRibbon!");
                        }
                    }
                }
                else
                {
                    Debug.LogError("  ❌ CanvasGalaxy not found!");
                }
            }

            // ✅ NEW: Re-enable galaxy EventSystem
            var galaxyEventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (galaxyEventSystem != null)
            {
                if (!galaxyEventSystem.enabled)
                {
                    galaxyEventSystem.enabled = true;
                    Debug.Log($"  ✅ Re-enabled galaxy EventSystem");
                }
            }
            else
            {
                Debug.Log("  ⚠️ No EventSystem found! Creating one...");
                CreateGalaxyEventSystem();
            }

            // ✅ Re-enable galaxy camera
            if (GalaxyCameraDragMoveZoom.Instance != null)
            {
                GalaxyCameraDragMoveZoom.Instance.enabled = true;
                Debug.Log("  ✅ Re-enabled galaxy camera");
            }

            // ✅ Resume time
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.ResumeTime();
                Debug.Log("  ✅ Resumed galaxy time");
            }

            // ✅ Wait one more frame before refreshing UI
            yield return null;

            // ✅ Refresh UI data
            if (FleetMenuUIController.Instance != null)
            {
                FleetMenuUIController.Instance.SetupFleetUIData();
                Debug.Log("  ✅ Refreshed fleet UI data");
            }

            if (StarSysMenuUIController.Instance != null)
            {
                StarSysMenuUIController.Instance.SetupSystemUIData();
                Debug.Log("  ✅ Refreshed system UI data");

                // ✅ CRITICAL: Move system UIs back to home storage and deactivate them
                StarSysMenuUIController.Instance.MoveBackAnyStarSysUIGO();
                Debug.Log("  ✅ Moved system UIs to home storage (deactivated)");
            }

            // ✅ NEW: Resume build coroutines for all systems
            if (StarSysManager.Instance != null)
            {
                Debug.Log("  ✅ Resuming build coroutines for all systems...");

                foreach (var sysCon in StarSysManager.Instance.StarSysControllerList)
                {
                    if (sysCon != null && sysCon.StarSysBuildManager != null)
                    {
                        // Resume facility builds
                        if (!sysCon.StarSysBuildManager.IsBuildingFacility && sysCon.sysBuildQueueList.Count > 0)
                        {
                            Debug.Log($"    Resuming facility build for system '{sysCon.name}'");
                            sysCon.StarSysBuildManager.StartNextFacilityBuildIfAny();
                        }

                        // Resume ship builds
                        if (!sysCon.StarSysBuildManager.IsBuildingShip && sysCon.sysShipBuildQueueList.Count > 0)
                        {
                            Debug.Log($"    Resuming ship build for system '{sysCon.name}'");
                            sysCon.StarSysBuildManager.StartNextShipBuildIfAny();
                        }
                    }
                }

                Debug.Log("  ✅ Build coroutines resumed");
            }

            Debug.Log("=== UnloadCombatSceneAndResumeGalaxy: Complete ===");
        }

        /// <summary>
        /// Helper method to find objects recursively in hierarchy
        /// </summary>
        private Transform FindInHierarchy(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindInHierarchy(parent.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        /// Creates a galaxy EventSystem if missing
        /// </summary>
        private void CreateGalaxyEventSystem()
        {
            var eventSystemGO = new GameObject("EventSystem (Galaxy)");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Scene galaxyScene = SceneManager.GetSceneByName("GalaxyScene");
            if (galaxyScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(eventSystemGO, galaxyScene);
            }

            Debug.Log("  ✅ Created EventSystem for Galaxy scene");
        }

        private void HideScene(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid())
            {
                Debug.Log($"HideScene: Hiding {scene.rootCount} root objects in '{sceneName}'");

                foreach (GameObject obj in scene.GetRootGameObjects())
                {
                    obj.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning($"HideScene: Scene '{sceneName}' is not valid/loaded");
            }
        }

        private void ExposeScene(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid())
            {
                Debug.Log($"ExposeScene: Showing {scene.rootCount} root objects in '{sceneName}'");

                foreach (GameObject obj in scene.GetRootGameObjects())
                {
                    obj.SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning($"ExposeScene: Scene '{sceneName}' is not valid/loaded");
            }
        }

        public void UnloadCombatScene()
        {
            Debug.Log("=== UnloadCombatScene: Starting ===");

            // ✅ STEP 1: Clear ShipCombatCameraController instance reference
            if (ShipCombatCameraController.Instance != null)
            {
                ShipCombatCameraController.Instance = null;
                Debug.Log("  ✅ Cleared ShipCombatCameraController.Instance");
            }

            // ✅ STEP 2: Get scene references
            Scene combatScene = SceneManager.GetSceneByName("CombatScene");
            Scene galaxyScene = SceneManager.GetSceneByName("GalaxyScene"); // ✅ ADD THIS LINE!

            // Validate galaxy scene exists
            if (!galaxyScene.isLoaded)
            {
                Debug.LogError("UnloadCombatScene: GalaxyScene is not loaded!");
                return;
            }

            // ✅ STEP 3: Disable Combat EventSystem BEFORE unloading
            if (combatScene.isLoaded)
            {
                foreach (GameObject go in combatScene.GetRootGameObjects())
                {
                    var eventSystem = go.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true);
                    if (eventSystem != null)
                    {
                        eventSystem.enabled = false;
                        Debug.Log($"  ✅ Disabled EventSystem in Combat scene on: '{go.name}'");
                    }
                }

                SceneManager.UnloadSceneAsync(combatScene);
                Debug.Log("  ✅ Combat scene unloading...");
            }

            // ✅ STEP 4: Activate galaxy scene objects FIRST (so we can find references)
            Debug.Log("  Activating Galaxy scene objects...");
            foreach (GameObject go in galaxyScene.GetRootGameObjects())
            {
                go.SetActive(true);
            }

            // ✅ STEP 5: Re-enable Galaxy EventSystem
            foreach (GameObject go in galaxyScene.GetRootGameObjects())
            {
                var eventSystem = go.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true);
                if (eventSystem != null)
                {
                    eventSystem.enabled = true;
                    Debug.Log($"  ✅ Re-enabled EventSystem in Galaxy scene on: '{go.name}'");
                }
            }

            // ✅ STEP 6: Set Galaxy as active scene
            SceneManager.SetActiveScene(galaxyScene);
            Debug.Log("  ✅ Galaxy scene set as active");

            // ✅ STEP 7: NOW find and show galaxy camera (objects are active now)
            var galaxyCameraDandD = GetGalaxyCameraDragNDrop();
            if (galaxyCameraDandD != null)
            {
                galaxyCameraDandD.SetActive(true);

                if (GalaxyCameraDragMoveZoom.Instance != null)
                {
                    var camera = GalaxyCameraDragMoveZoom.Instance.GetComponentInChildren<Camera>();
                    if (camera != null)
                    {
                        camera.enabled = true;
                        Debug.Log("  ✅ Galaxy camera enabled");
                    }
                }
            }
            else
            {
                Debug.LogWarning("  ⚠️ Could not find GalaxyCameraDragMoveZoom - may need manual camera setup");
            }

            // ✅ STEP 8: Show fog of war
            for (int i = 0; i < persistentObjects.Count; i++)
            {
                if (persistentObjects[i] != null && persistentObjects[i].name == "FogPlaneParent")
                {
                    persistentObjects[i].SetActive(true);
                    Debug.Log("  ✅ FogPlaneParent shown");
                }
            }

            // ✅ STEP 9: Re-enable galaxy input
            var keyboardInput = FindFirstObjectByType<KeyboardInputManagerGalactica>();
            if (keyboardInput != null)
            {
                keyboardInput.enabled = true;
                Debug.Log("  ✅ Galaxy input re-enabled");
            }

            // ✅ STEP 10: Resume time
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.ResumeTime();
                Debug.Log("  ✅ Time resumed");
            }

            Debug.Log("=== UnloadCombatScene: Complete ===");
        }
    }

    /// <summary>
    /// Stores combat context so we can return to galaxy with correct state
    /// </summary>
    public static class CombatContext
    {
        public static FleetController PlayerFleet { get; set; }
        public static FleetController EnemyFleet { get; set; }
        public static StarSysController StarSystem { get; set; }

        public static void Clear()
        {
            PlayerFleet = null;
            EnemyFleet = null;
            StarSystem = null;
        }
    }
}
