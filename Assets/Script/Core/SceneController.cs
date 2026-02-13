using Assets.Core;
using Assets.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

namespace Assets.GamePlay
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

        public void LoadCombatScene(DiplomacyController diplomacyController)
        {
            Debug.Log("=== LoadCombatScene: Starting ===");

            // ✅ 1. PAUSE TIME (stop galaxy simulation)
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.PauseTime();
                Debug.Log("  ✅ Time paused");
            }

            // ✅ 2. DISABLE GALAXY INPUT
            var keyboardInput = FindFirstObjectByType<KeyboardInputManagerGalactica>();
            if (keyboardInput != null)
            {
                keyboardInput.enabled = false;
                Debug.Log("  ✅ Galaxy input disabled");
            }

            // ✅ 3. HIDE GALAXY CAMERA
            var galaxyCamera = GetGalaxyCameraDragNDrop();
            if (galaxyCamera != null)
            {
                galaxyCamera.SetActive(false);
                Debug.Log("  ✅ Galaxy camera hidden");
            }

            // ✅ 4. HIDE FOG OF WAR
            for (int i = 0; i < persistentObjects.Count; i++)
            {
                if (persistentObjects[i] != null && persistentObjects[i].name == "FogPlaneParent")
                {
                    persistentObjects[i].SetActive(false);
                    Debug.Log("  ✅ FogPlaneParent hidden");
                }
            }

            // ✅ 5. GET ACTUAL GALAXY SCENE NAME (don't hardcode!)
            previousSceneName = null;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name.Contains("Galaxy"))
                {
                    previousSceneName = scene.name;
                    Debug.Log($"  ✅ Found Galaxy scene: {previousSceneName}");
                    break;
                }
            }

            if (string.IsNullOrEmpty(previousSceneName))
            {
                Debug.LogWarning("  ⚠️ Could not find Galaxy scene - using fallback name");
                previousSceneName = "GalaxyScene"; // Fallback
            }

            if (diplomacyController.DiplomacyData.CivEnumSideOne <= CivEnum.TERRAN ||
                diplomacyController.DiplomacyData.CivEnumSideTwo <= CivEnum.TERRAN)
            {
                // ✅ 6. HIDE GALAXY SCENE (all root objects)
                HideScene(previousSceneName);
                Debug.Log($"  ✅ Hidden scene: {previousSceneName}");

                // ✅ 7. CLOSE ALL GALAXY MENUS
                if (GalaxyMenuUIController.Instance != null)
                {
                    GalaxyMenuUIController.Instance.CloseAllMenus();
                    Debug.Log("  ✅ Closed all galaxy menus");
                }

                // ✅ 8. LOAD COMBAT SCENE
                SceneManager.LoadSceneAsync("CombatScene", LoadSceneMode.Additive);
                Debug.Log("  ✅ Loading CombatScene...");

                // ✅ 9. SET COMBAT DIPLOMACY CONTEXT
                CombatManager.Instance.SetDiplomacyController(diplomacyController);

                Debug.Log("=== LoadCombatScene: Complete ===");
            }
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
}
