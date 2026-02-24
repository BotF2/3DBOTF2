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

        /// <summary>
        /// Lazy initialization - finds camera only when needed and if not already assigned.
        /// </summary>
        private GameObject GetGalaxyCameraDragNDrop()
        {
            if (galaxyCameraDragNDrop == null)
            {
                galaxyCameraDragNDrop = GameObject.Find("GalaxyCameraDragMoveZoom");

                if (galaxyCameraDragNDrop != null)
                {
                    Debug.Log("SceneController: Found GalaxyCameraDragMoveZoom");
                }
                else
                {
                    Debug.LogWarning("SceneController: GalaxyCameraDragMoveZoom not found - GalaxyScene may not be loaded yet");
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
        public void LoadCombatScene(FleetController playerFleet, FleetController enemyFleet, StarSysController combatLocation)
        {
            Debug.Log($"LoadCombatScene: Starting combat at {combatLocation?.name}");

            // ✅ 1. Pause galaxy time
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.PauseTime();
                Debug.Log("  Paused galaxy time");
            }

            // ✅ 2. Hide all galaxy UIs
            if (GalaxyMenuUIController.Instance != null)
            {
                GalaxyMenuUIController.Instance.CloseAllMenus();
                Debug.Log("  Closed all galaxy menus");
            }

            // ✅ 3. Disable galaxy camera
            if (GalaxyCameraDragMoveZoom.Instance != null)
            {
                GalaxyCameraDragMoveZoom.Instance.enabled = false;
                Debug.Log("  Disabled galaxy camera");
            }

            // ✅ NEW: Disable galaxy EventSystem to prevent conflicts
            var galaxyEventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (galaxyEventSystem != null)
            {
                galaxyEventSystem.enabled = false;
                Debug.Log($"  ✅ Disabled galaxy EventSystem: {galaxyEventSystem.gameObject.name}");
            }

            // ✅ 4. Store combat context
            CombatContext.PlayerFleet = playerFleet;
            CombatContext.EnemyFleet = enemyFleet;
            CombatContext.CombatLocation = combatLocation;

            // ✅ 5. Load combat scene
            StartCoroutine(LoadCombatSceneAdditive());
        }

        private IEnumerator LoadCombatSceneAdditive()
        {
            Debug.Log("=== LoadCombatSceneAdditive: Starting async load ===");

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("CombatScene", LoadSceneMode.Additive);

            while (!asyncLoad.isDone)
            {
                Debug.Log($"  Loading combat scene: {asyncLoad.progress * 100:F1}%");
                yield return null;
            }

            Debug.Log("✅ Combat scene loaded additively");

            Scene combatScene = SceneManager.GetSceneByName("CombatScene");
            if (combatScene.isLoaded)
            {
                SceneManager.SetActiveScene(combatScene);
                Debug.Log($"  ✅ Combat scene set as active scene");
                Debug.Log($"  Combat scene root objects: {combatScene.rootCount}");

                // ✅ Activate ALL inactive root objects
                var rootObjects = combatScene.GetRootGameObjects();
                int activatedCount = 0;

                foreach (var obj in rootObjects)
                {
                    bool wasActive = obj.activeSelf;

                    if (!wasActive)
                    {
                        obj.SetActive(true);
                        activatedCount++;
                        Debug.Log($"    ✅ ACTIVATED: '{obj.name}' (was inactive)");
                    }
                    else
                    {
                        Debug.Log($"    - '{obj.name}' (already active)");
                    }
                }

                Debug.Log($"  ✅ Activated {activatedCount} inactive root objects");
            }
            else
            {
                Debug.LogError("  ❌ Combat scene failed to load!");
                yield break;
            }

            // ✅ Wait TWO frames for Awake() and Start() to run
            yield return null;
            yield return null;

            // ✅ CRITICAL DEBUG: Search for CombatController in scene
            Debug.Log("=== Searching for CombatController ===");

            var allCombatControllers = FindObjectsByType<CombatController>(FindObjectsSortMode.None);
            Debug.Log($"  Found {allCombatControllers.Length} CombatController(s) in all scenes");

            foreach (var cc in allCombatControllers)
            {
                Debug.Log($"    - CombatController on '{cc.gameObject.name}' in scene '{cc.gameObject.scene.name}' (active: {cc.gameObject.activeSelf}, enabled: {cc.enabled})");
            }

            // ✅ Check if Instance is set
            if (CombatController.Instance != null)
            {
                Debug.Log($"  ✅ CombatController.Instance found: {CombatController.Instance.gameObject.name}");

                CombatController.Instance.InitializeCombat(
                    CombatContext.PlayerFleet,
                    CombatContext.EnemyFleet,
                    CombatContext.CombatLocation);
            }
            else
            {
                Debug.LogError("  ❌ CombatController.Instance is STILL NULL!");
                Debug.LogError("  DIAGNOSIS:");
                Debug.LogError("    1. Check if 'CombatController' GameObject exists in CombatScene");
                Debug.LogError("    2. Check if CombatController script is attached to it");
                Debug.LogError("    3. Check if Awake() has errors preventing Instance assignment");
                Debug.LogError($"    4. Found {allCombatControllers.Length} CombatController components - check logs above");

                // ✅ FALLBACK: Try to use any CombatController found
                if (allCombatControllers.Length > 0)
                {
                    Debug.LogWarning($"  ⚠️ Using fallback CombatController: {allCombatControllers[0].gameObject.name}");

                    allCombatControllers[0].InitializeCombat(
                        CombatContext.PlayerFleet,
                        CombatContext.EnemyFleet,
                        CombatContext.CombatLocation);
                }
            }

            // ✅ Enable combat camera
            if (ShipCombatCameraController.Instance != null)
            {
                var camera = ShipCombatCameraController.Instance.GetComponent<Camera>();
                if (camera != null)
                {
                    camera.enabled = true;
                    Debug.Log($"  ✅ Combat camera enabled");
                }
            }

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

            // ✅ NEW: Re-enable galaxy EventSystem
            var galaxyEventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (galaxyEventSystem != null && !galaxyEventSystem.enabled)
            {
                galaxyEventSystem.enabled = true;
                Debug.Log($"  ✅ Re-enabled galaxy EventSystem");
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

            // ✅ Refresh UI
            if (FleetMenuUIController.Instance != null)
            {
                FleetMenuUIController.Instance.SetupFleetUIData();
            }

            if (StarSysMenuUIController.Instance != null)
            {
                StarSysMenuUIController.Instance.SetupSystemUIData();
            }

            // ✅ Clean up combat context
            CombatContext.Clear();

            Debug.Log("=== UnloadCombatSceneAndResumeGalaxy: Complete ===");
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

            // ✅ 1. SHOW GALAXY SCENE
            if (!string.IsNullOrEmpty(previousSceneName))
            {
                ExposeScene(previousSceneName);
                Debug.Log($"  ✅ Exposed scene: {previousSceneName}");
            }

            // ✅ 2. SHOW GALAXY CAMERA
            var galaxyCamera = GetGalaxyCameraDragNDrop();
            if (galaxyCamera != null)
            {
                galaxyCamera.SetActive(true);
                Debug.Log("  ✅ Galaxy camera shown");
            }

            // ✅ 3. SHOW FOG OF WAR
            for (int i = 0; i < persistentObjects.Count; i++)
            {
                if (persistentObjects[i] != null && persistentObjects[i].name == "FogPlaneParent")
                {
                    persistentObjects[i].SetActive(true);
                    Debug.Log("  ✅ FogPlaneParent shown");
                }
            }

            // ✅ 4. RE-ENABLE GALAXY INPUT
            var keyboardInput = FindFirstObjectByType<KeyboardInputManagerGalactica>();
            if (keyboardInput != null)
            {
                keyboardInput.enabled = true;
                Debug.Log("  ✅ Galaxy input re-enabled");
            }

            // ✅ 5. RESUME TIME
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.ResumeTime();
                Debug.Log("  ✅ Time resumed");
            }

            // ✅ 6. HIDE COMBAT SCENE
            HideScene("CombatScene");
            Debug.Log("  ✅ Combat scene hidden");

            Debug.Log("=== UnloadCombatScene: Complete ===");
        }

        public void LoadNextScene(string sceneName)
        {
            SceneManager.LoadSceneAsync(sceneName);
        }
    }

    /// <summary>
    /// Stores combat context so we can return to galaxy with correct state
    /// </summary>
    public static class CombatContext
    {
        public static FleetController PlayerFleet { get; set; }
        public static FleetController EnemyFleet { get; set; }
        public static StarSysController CombatLocation { get; set; }

        public static void Clear()
        {
            PlayerFleet = null;
            EnemyFleet = null;
            CombatLocation = null;
        }
    }
}
