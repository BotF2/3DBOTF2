using BOTF3D.GamePlay;
using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Core
{
    public class PlayerDefinedTargetManager : MonoBehaviour
    {
        public static PlayerDefinedTargetManager Instance;

        [Header("Prefabs (assign in Inspector)")]
        [SerializeField] private PlayerDefinedTargetController playerTargetPrefab;

        [Header("Scene References (assign in Inspector or found at runtime)")]
        [SerializeField] private GameObject galaxyImageGO;
        [SerializeField] private Camera galaxyEventCamera;

        public GameObject GalaxyCenter { get; private set; }
        public string nameDestination;

        [SerializeField] private PlayerDefinedTargetSO playerDefinedTargetSO;
        public List<PlayerDefinedTargetController> PlayerTargetConList { get; private set; } = new List<PlayerDefinedTargetController>();

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

        void Start()
        {
            // ✅ Find galaxy references at start
            FindGalaxyReferences();
        }

        /// <summary>
        /// Finds galaxy scene references if not assigned in Inspector.
        /// </summary>
        private void FindGalaxyReferences()
        {
            // ✅ Find GalaxyCenter
            if (GalaxyCenter == null)
            {
                GalaxyCenter = GameObject.Find("GalaxyCenter");
                Debug.Log($"PlayerDefinedTargetManager: Found GalaxyCenter: {GalaxyCenter != null}");
            }

            // ✅ Find GalaxyImage
            if (galaxyImageGO == null)
            {
                galaxyImageGO = GameObject.Find("GalaxyImage");

                if (galaxyImageGO == null)
                {
                    Debug.LogWarning("PlayerDefinedTargetManager: GalaxyImage not assigned and not found - searching in hierarchy...");

                    for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                    {
                        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                        if (scene.isLoaded)
                        {
                            foreach (GameObject rootObj in scene.GetRootGameObjects())
                            {
                                galaxyImageGO = FindInHierarchy(rootObj.transform, "GalaxyImage");
                                if (galaxyImageGO != null)
                                {
                                    Debug.Log($"PlayerDefinedTargetManager: Found GalaxyImage in scene '{scene.name}'");
                                    break;
                                }
                            }
                            if (galaxyImageGO != null) break;
                        }
                    }
                }

                Debug.Log($"PlayerDefinedTargetManager: GalaxyImage: {galaxyImageGO != null}");
            }

            // ✅ Find MainCamera
            if (galaxyEventCamera == null)
            {
                var mainCameraGO = GameObject.FindGameObjectWithTag("MainCamera");
                if (mainCameraGO != null)
                {
                    galaxyEventCamera = mainCameraGO.GetComponent<Camera>();
                    Debug.Log($"PlayerDefinedTargetManager: Found MainCamera: {galaxyEventCamera != null}");
                }
            }
        }

        /// <summary>
        /// Called by GalaxySceneInitializer when GalaxyScene loads.
        /// </summary>
        public void SetGalaxyReferences(GameObject center, GameObject image, Camera camera)
        {
            GalaxyCenter = center;
            galaxyImageGO = image;
            galaxyEventCamera = camera;
            Debug.Log("PlayerDefinedTargetManager: Galaxy references set by initializer");
        }

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

        public void PlayerTargetFromData(GameObject fleetGO)
        {
            // ✅ CRITICAL: Ensure references exist before instantiating
            if (GalaxyCenter == null || galaxyImageGO == null || galaxyEventCamera == null)
            {
                Debug.LogWarning("PlayerDefinedTargetManager.PlayerTargetFromData: Missing references, attempting to find them...");
                FindGalaxyReferences();
            }

            // ✅ Validate all critical references
            if (playerTargetPrefab == null)
            {
                Debug.LogError("PlayerDefinedTargetManager.PlayerTargetFromData: playerTargetPrefab is NULL! Assign it in Inspector.");
                return;
            }

            if (GalaxyCenter == null)
            {
                Debug.LogError("PlayerDefinedTargetManager.PlayerTargetFromData: GalaxyCenter is NULL! Cannot create target.");
                return;
            }

            if (galaxyImageGO == null)
            {
                Debug.LogError("PlayerDefinedTargetManager.PlayerTargetFromData: galaxyImageGO is NULL! Cannot create drop line.");
                return;
            }

            if (galaxyEventCamera == null)
            {
                Debug.LogError("PlayerDefinedTargetManager.PlayerTargetFromData: galaxyEventCamera is NULL! Cannot set camera.");
                return;
            }

            if (fleetGO.GetComponent<FleetController>().FleetData.CivEnum == GameController.Instance.GameData.LocalPlayerCivEnum)
            {
                PlayerDefinedTargetData playerTargetData = new PlayerDefinedTargetData();
                playerTargetData.Insignia = playerDefinedTargetSO.Insignia;
                playerTargetData.Description = playerDefinedTargetSO.Description;
                playerTargetData.CivOwnerEnum = GameController.Instance.GameData.LocalPlayerCivEnum;

                Debug.Log($"✅ Creating PlayerDefinedTarget for fleet '{fleetGO.name}'");
                this.InstantiatePlayerTarget(playerTargetData, fleetGO);
            }
        }

        public void InstantiatePlayerTarget(PlayerDefinedTargetData playerTargetData, GameObject fleetGO)
        {
            Vector3 position = fleetGO.transform.position;
            PlayerDefinedTargetController playerDefinedTargetCon = Instantiate(playerTargetPrefab, new Vector3(0, 0, 0),
                    Quaternion.identity);

            PlayerTargetConList.Add(playerDefinedTargetCon);
            playerDefinedTargetCon.gameObject.layer = 6;

            var playerController = playerDefinedTargetCon.GetComponentInChildren<PlayerDefinedTargetController>();
            playerController.galaxyEventCamera = galaxyEventCamera;
            playerController.galaxyBackgroundImage = galaxyImageGO;
            playerController.PlayerTargetData = playerTargetData;

            playerController.PlayerTargetData.FleetController = fleetGO.GetComponentInChildren<FleetController>();
            playerController.PlayerTargetData.CivOwnerEnum = playerController.PlayerTargetData.FleetController.FleetData.CivEnum;

            playerDefinedTargetCon.transform.SetParent(GalaxyCenter.transform, true);
            playerDefinedTargetCon.transform.Translate(new Vector3(position.x + 20f, position.y, position.z));

            playerDefinedTargetCon.transform.localScale = new Vector3(1f, 1f, 1f);

            playerDefinedTargetCon.gameObject.SetActive(true);

            // ✅ NOTE: Already added on line 60, removing duplicate
            // PlayerTargetConList.Add(playerDefinedTargetCon);

            Canvas[] canvasArray = playerDefinedTargetCon.GetComponentsInChildren<Canvas>();
            for (int j = 0; j < canvasArray.Length; j++)
            {
                if (canvasArray[j].name == "CanvasToolTip")
                {
                    playerController.CanvasToolTip = canvasArray[j];
                }
            }

            var transGalaxyCenter = GalaxyCenter.gameObject.transform;
            var trans = fleetGO.gameObject.transform;
            MapLineMovable itemMapLineScript = playerDefinedTargetCon.GetComponentInChildren<MapLineMovable>();

            itemMapLineScript.GetLineRenderer();
            itemMapLineScript.lineRenderer.startColor = Color.yellow;
            itemMapLineScript.lineRenderer.endColor = Color.yellow;
            itemMapLineScript.transform.SetParent(playerDefinedTargetCon.transform, false);

            Vector3 galaxyPlanePoint = new Vector3(playerDefinedTargetCon.transform.position.x,
                galaxyImageGO.transform.position.y, playerDefinedTargetCon.transform.position.z);
            Vector3[] points = { playerDefinedTargetCon.transform.position, galaxyPlanePoint };
            itemMapLineScript.SetUpLine(points);
            playerController.DropLine = itemMapLineScript;

            fleetGO.GetComponent<FleetController>().TargetController = playerController;

            Debug.Log($"✅ PlayerDefinedTarget instantiated at position {playerDefinedTargetCon.transform.position}");
        }

        void AddPlayerControllerToAllControllers(PlayerDefinedTargetController playerTargetController)
        {
            // ManagersPlayerTargetControllerList.Add(playerTargetController);
        }

        void RemovePlayerControllerToAllControllers(PlayerDefinedTargetController playerTargetController)
        {
            // ManagersPlayerTargetControllerList.Remove(playerTargetController);
        }

        /// <summary>
        /// Destroys the PlayerDefinedTarget associated with the given fleet.
        /// </summary>
        public void DestroyPlayerTarget(FleetController fleetCon)
        {
            if (fleetCon == null || fleetCon.TargetController == null) return;

            var targetController = fleetCon.TargetController;

            // Remove from list
            if (PlayerTargetConList.Contains(targetController))
            {
                PlayerTargetConList.Remove(targetController);
            }

            // Clear fleet reference
            fleetCon.TargetController = null;

            // Destroy the GameObject
            if (targetController.gameObject != null)
            {
                Destroy(targetController.gameObject);
                Debug.Log($"PlayerDefinedTargetManager: Destroyed player target for fleet '{fleetCon.name}'");
            }
        }

        /// <summary>
        /// Destroys all player targets for a given civilization.
        /// </summary>
        public void DestroyAllPlayerTargets(CivEnum civEnum)
        {
            for (int i = PlayerTargetConList.Count - 1; i >= 0; i--)
            {
                var targetController = PlayerTargetConList[i];
                if (targetController != null && targetController.PlayerTargetData?.CivOwnerEnum == civEnum)
                {
                    if (targetController.PlayerTargetData?.FleetController != null)
                    {
                        targetController.PlayerTargetData.FleetController.TargetController = null;
                    }
                    Destroy(targetController.gameObject);
                    PlayerTargetConList.RemoveAt(i);
                }
            }
            Debug.Log($"PlayerDefinedTargetManager: Destroyed all targets for civ '{civEnum}'");
        }
    }
}
