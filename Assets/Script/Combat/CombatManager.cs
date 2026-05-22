using BOTF3D.Audio;
using BOTF3D.Combat;
using BOTF3D.GamePlay;
using BOTF3D.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BOTF3D.Core
{
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        [Header("Combat Prefabs & Assets")]
        public GameObject CombatUICanvas;
        public GameObject Combat3DCanvas;
        public GameObject GameOverCanvas { get; private set; }
        public GameObject HealthbarPrefab;
        [SerializeField] private CombatController combatConPrefab;
        [SerializeField] private SoundData dropOutOfWarpSoundData;

        [Header("Weapon Prefabs")]
        public List<GameObject> TorpedoPrefabs;
        public List<GameObject> BeamPrefabs;

        [Header("Weapon Audio Clips")]
        public AudioClip[] BeamFireClips;
        public AudioClip[] TorpedoFireClips;

        // Weapon prefab properties (assigned from lists based on civ)
        public GameObject SideOneTorpedoPrefab { get; private set; }
        public GameObject SideTwoTorpedoPrefab { get; private set; }
        public GameObject SideOneBeamPrefab { get; private set; }
        public GameObject SideTwoBeamPrefab { get; private set; }
        public AudioClip SideOneBeamFireClip { get; private set; }
        public AudioClip SideTwoBeamFireClip { get; private set; }
        public AudioClip SideOneTorpedoFireClip { get; private set; }
        public AudioClip SideTwoTorpedoFireClip { get; private set; }

        // Combat queue system
        private Queue<PendingCombat> combatQueue = new Queue<PendingCombat>();
        public CombatController ActiveCombatController { get; private set; }
        private List<CombatController> allCombatControllers = new List<CombatController>();
        private bool isProcessingCombat = false;
        private CombatController _cachedCombatConPrefab; // Cache the prefab reference

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate CombatManager found! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Cache the prefab BEFORE DontDestroyOnLoad
            _cachedCombatConPrefab = combatConPrefab;

            // Verify it's actually assigned
            if (_cachedCombatConPrefab == null)
            {
                Debug.LogError("❌ combatConPrefab is NULL in Awake! Check Inspector assignment in the scene.");
                Debug.LogError($"   CombatManager is on GameObject: {gameObject.name}");
                Debug.LogError($"   In scene: {gameObject.scene.name}");
            }
            else
            {
                Debug.Log($"✅ combatConPrefab cached successfully: {_cachedCombatConPrefab.name}");
            }

            DontDestroyOnLoad(gameObject);

            // Verify it survived the move
            if (combatConPrefab == null && _cachedCombatConPrefab != null)
            {
                Debug.LogWarning("⚠️ combatConPrefab was cleared by DontDestroyOnLoad - restoring from cache");
                combatConPrefab = _cachedCombatConPrefab;
            }

            Debug.Log("✅ CombatManager initialized.");
        }

        /// <summary>
        /// Request a combat - will be queued if another is active
        /// </summary>
        public void RequestCombat(List<ShipController> sideOneShips, List<ShipController> sideTwoShips, CombatType combatType)
        {
            var pendingCombat = new PendingCombat
            {
                SideOneShips = sideOneShips,
                SideTwoShips = sideTwoShips,
                CombatType = combatType
            };

            combatQueue.Enqueue(pendingCombat);
            Debug.Log($"⏸️ Combat queued. Total in queue: {combatQueue.Count}");

            // Start processing if not already doing so
            if (!isProcessingCombat)
            {
                StartCoroutine(ProcessCombatQueue());
            }
        }

        /// <summary>
        /// Process one combat at a time from the queue
        /// </summary>
        private IEnumerator ProcessCombatQueue()
        {
            isProcessingCombat = true;

            while (combatQueue.Count > 0)
            {
                var pendingCombat = combatQueue.Dequeue();
                Debug.Log($"🎮 Starting combat. Remaining in queue: {combatQueue.Count}");

                yield return null;

                CombatController combatController = null;
                yield return StartCoroutine(InstantiateCombatControllerCoroutine(
                    pendingCombat.SideOneShips,
                    pendingCombat.SideTwoShips,
                    (controller) => combatController = controller
                ));

                if (combatController != null)
                {
                    combatController.CombatData.CombatType = pendingCombat.CombatType;

                    // Set ActiveCombatController FIRST
                    ActiveCombatController = combatController;

                    // Setup local player UI
                    SetUpLocalPlayer();

                    // Wait for combat to finish
                    while (ActiveCombatController != null && !ActiveCombatController.isClosing)
                    {
                        yield return null;
                    }

                    Debug.Log("✅ Combat finished. Processing next in queue...");
                    yield return new WaitForSeconds(0.5f);
                }
            }

            isProcessingCombat = false;
            Debug.Log("✅ Combat queue empty.");
        }

        /// <summary>
        /// Coroutine version of InstantiateCombatController
        /// </summary>
        private IEnumerator InstantiateCombatControllerCoroutine(
            List<ShipController> sideOneShipCons,
            List<ShipController> sideTwoShipCons,
            System.Action<CombatController> callback)
        {
            // Wait one more frame to ensure scene objects are fully initialized
            yield return null;

            var controller = InstantiateCombatController(sideOneShipCons, sideTwoShipCons);
            callback?.Invoke(controller);
        }

        /// <summary>
        /// Instantiate a new CombatController with simplified setup (no animators)
        /// </summary>
        public CombatController InstantiateCombatController(List<ShipController> sideOneShipCons, List<ShipController> sideTwoShipCons)
        {
            if (combatConPrefab == null)
            {
                Debug.LogError("InstantiateCombatController: combatConPrefab is null! Assign it in Inspector.");
                return null;
            }

            FindCombatSceneReferences();

            if (CombatUICanvas == null)
            {
                Debug.LogError("❌ Cannot instantiate combat - CombatUICanvas not found!");
                return null;
            }

            Debug.Log("📦 Instantiating NEW CombatController...");

            // Create combat data
            CombatData combatData = new CombatData
            {
                SideOneShipCons = sideOneShipCons,
                SideTwoShipCons = sideTwoShipCons,
                CivEnumSideOne = sideOneShipCons[0].ShipData.CivEnum,
                CivEnumSideTwo = sideTwoShipCons[0].ShipData.CivEnum,
                OrderSideOne = CombatOrders.Engage,
                OrderSideTwo = CombatOrders.Engage
            };

            // Instantiate new controller
            CombatController aCombatController = Instantiate(combatConPrefab, Vector3.zero, Quaternion.identity);
            aCombatController.transform.SetParent(transform, false);
            aCombatController.name = $"CombatController_{aCombatController.CombatID}";

            // Set combat data
            combatData.CombatID = aCombatController.CombatID;
            aCombatController.CombatData = combatData;
            aCombatController.isMoving = false;
            aCombatController.isClosing = false;
            aCombatController.WarpingIn = true;
            aCombatController.WarpingAnimationOver = false;
            aCombatController.ShipCombatCanvas = Combat3DCanvas.GetComponent<Canvas>();
            aCombatController.warpInSound = dropOutOfWarpSoundData;

            // Assign weapon prefabs based on civs
            CivEnum sideOneCiv = sideOneShipCons[0].ShipData.CivEnum;
            CivEnum sideTwoCiv = sideTwoShipCons[0].ShipData.CivEnum;

            SideOneTorpedoPrefab = GetTorpedoPrefabs(aCombatController, sideOneCiv);
            SideTwoTorpedoPrefab = GetTorpedoPrefabs(aCombatController, sideTwoCiv);
            SideOneBeamPrefab = GetBeamPrefabs(aCombatController, sideOneCiv);
            SideTwoBeamPrefab = GetBeamPrefabs(aCombatController, sideTwoCiv);

            aCombatController.SideOneTorpedoPrefab = SideOneTorpedoPrefab;
            aCombatController.SideTwoTorpedoPrefab = SideTwoTorpedoPrefab;
            aCombatController.SideOneBeamPrefab = SideOneBeamPrefab;
            aCombatController.SideTwoBeamPrefab = SideTwoBeamPrefab;

            // Assign audio clips based on civs
            SideOneBeamFireClip = GetBeamFireClip(sideOneCiv);
            SideTwoBeamFireClip = GetBeamFireClip(sideTwoCiv);
            SideOneTorpedoFireClip = GetTorpedoFireClip(sideOneCiv);
            SideTwoTorpedoFireClip = GetTorpedoFireClip(sideTwoCiv);

            aCombatController.SideOneBeamFireClip = SideOneBeamFireClip;
            aCombatController.SideTwoBeamFireClip = SideTwoBeamFireClip;
            aCombatController.SideOneTorpedoFireClip = SideOneTorpedoFireClip;
            aCombatController.SideTwoTorpedoFireClip = SideTwoTorpedoFireClip;

            Debug.Log($"✅ CombatController instantiated: {aCombatController.name}");

            // Populate ship data and setup positions
            aCombatController.PopulateShipData(aCombatController);

            // Add to tracking list
            allCombatControllers.Add(aCombatController);

            return aCombatController;
        }

        /// <summary>
        /// Find combat scene references (canvases only - no animators)
        /// </summary>
        private void FindCombatSceneReferences()
        {
            Debug.Log("=== Finding CombatScene References ===");

            Scene combatScene = SceneManager.GetSceneByName("CombatScene");
            if (!combatScene.isLoaded)
            {
                Debug.LogError("❌ CombatScene is not loaded!");
                return;
            }

            GameObject[] rootObjects = combatScene.GetRootGameObjects();
            Debug.Log($"🔍 Found {rootObjects.Length} root objects in CombatScene");

            // Only search for canvases now (no more animators)
            foreach (GameObject root in rootObjects)
            {
                CheckAndAssignCanvases(root);
                SearchChildrenForCanvases(root.transform);
            }

            // Validate
            if (CombatUICanvas == null)
            {
                Debug.LogError("❌ CombatUICanvas not found!");
            }
            else
            {
                Debug.Log($"✅ CombatUICanvas found: {CombatUICanvas.name}");
            }

            if (Combat3DCanvas == null)
            {
                Debug.LogError("❌ Combat3DCanvas not found!");
            }
            else
            {
                Debug.Log($"✅ Combat3DCanvas found: {Combat3DCanvas.name}");
            }

            if (GameOverCanvas == null)
            {
                Debug.LogError("❌ GameOverCanvas not found!");
            }
            else
            {
                Debug.Log($"✅ GameOverCanvas found: {GameOverCanvas.name}");
            }
        }

        private void CheckAndAssignCanvases(GameObject obj)
        {
            if (obj.name == "CombatUICanvas" && CombatUICanvas == null)
            {
                CombatUICanvas = obj;
            }
            else if (obj.name == "Combat3DCanvas" && Combat3DCanvas == null)
            {
                Combat3DCanvas = obj;
            }
            else if (obj.name == "GameOverCanvas" && GameOverCanvas == null)
            {
                GameOverCanvas = obj;
            }
        }

        private void SearchChildrenForCanvases(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject child = parent.GetChild(i).gameObject;
                CheckAndAssignCanvases(child);
                SearchChildrenForCanvases(child.transform);
            }
        }

        /// <summary>
        /// Get full hierarchy path of a GameObject for debugging
        /// </summary>
        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private GameObject GetTorpedoPrefabs(CombatController aCombatController, CivEnum civEnum)
        {
            GameObject torpedoPrefab = TorpedoPrefabs[TorpedoPrefabs.Count - 1]; // default to minor civ prefab

            for (int i = 0; i < TorpedoPrefabs.Count; i++)
            {
                if (i == (int)civEnum)
                {
                    torpedoPrefab = TorpedoPrefabs[i];
                    return torpedoPrefab; // Return the prefab for the specific civ
                }
            }
            return torpedoPrefab; // Return the default prefab if no match found
        }

        private GameObject GetBeamPrefabs(CombatController aCombatController, CivEnum civEnum)
        {
            GameObject beamPrefab = BeamPrefabs[BeamPrefabs.Count - 1];
            for (int i = 0; i < BeamPrefabs.Count; i++)
            {
                if (i == (int)civEnum)
                {
                    beamPrefab = BeamPrefabs[i];
                    return beamPrefab;
                }
            }
            return beamPrefab;
        }

        /// <summary>
        /// Get civilization-specific beam fire audio clip
        /// For 7 playable civs (FED=0 through TERRAN=6), use their index
        /// For all other civs (index > 7), use the last clip (minor civ fallback)
        /// </summary>
        private AudioClip GetBeamFireClip(CivEnum civEnum)
        {
            if (BeamFireClips == null || BeamFireClips.Length == 0)
            {
                Debug.LogWarning($"⚠️ BeamFireClips array is null or empty!");
                return null;
            }

            int civIndex = (int)civEnum;

            // For playable civs (0-6), use their specific clip
            if (civIndex >= 0 && civIndex < BeamFireClips.Length - 1)
            {
                AudioClip clip = BeamFireClips[civIndex];
                Debug.Log($"✅ Assigned BeamFireClip for {civEnum} (index {civIndex}): {clip?.name ?? "NULL"}");
                return clip;
            }

            // For minor civs (index > 7), use the last clip as fallback
            AudioClip fallbackClip = BeamFireClips[BeamFireClips.Length - 1];
            Debug.Log($"✅ Assigned fallback BeamFireClip for {civEnum} (index {civIndex}): {fallbackClip?.name ?? "NULL"}");
            return fallbackClip;
        }

        /// <summary>
        /// Get civilization-specific torpedo fire audio clip
        /// For 7 playable civs (FED=0 through TERRAN=6), use their index
        /// For all other civs (index > 7), use the last clip (minor civ fallback)
        /// </summary>
        private AudioClip GetTorpedoFireClip(CivEnum civEnum)
        {
            if (TorpedoFireClips == null || TorpedoFireClips.Length == 0)
            {
                Debug.LogWarning($"⚠️ TorpedoFireClips array is null or empty!");
                return null;
            }

            int civIndex = (int)civEnum;

            // For playable civs (0-6), use their specific clip
            if (civIndex >= 0 && civIndex < TorpedoFireClips.Length - 1)
            {
                AudioClip clip = TorpedoFireClips[civIndex];
                Debug.Log($"✅ Assigned TorpedoFireClip for {civEnum} (index {civIndex}): {clip?.name ?? "NULL"}");
                return clip;
            }

            // For minor civs (index > 7), use the last clip as fallback
            AudioClip fallbackClip = TorpedoFireClips[TorpedoFireClips.Length - 1];
            Debug.Log($"✅ Assigned fallback TorpedoFireClip for {civEnum} (index {civIndex}): {fallbackClip?.name ?? "NULL"}");
            return fallbackClip;
        }

        /// <summary>
        /// Called when a combat ends
        /// </summary>
        public void OnCombatEnded(CombatController controller)
        {
            Debug.Log($"🏁 Combat {controller.CombatID} ended");

            allCombatControllers.Remove(controller);

            if (ActiveCombatController == controller)
            {
                ActiveCombatController = null;
            }

            Destroy(controller.gameObject);
        }

        public void EndCombatTimePause()
        {
            TimeManager.Instance.ResumeTime(); // Resume the game when combat UI is closed
        }

        public void SetUpLocalPlayer()
        {
            StartCoroutine(SetUpLocalPlayerAfterSceneLoad());
        }

        /// <summary>
        /// Setup local player UI after CombatScene fully loads
        /// </summary>
        private IEnumerator SetUpLocalPlayerAfterSceneLoad()
        {
            // Wait two frames per copilot-instructions.md
            yield return null;
            yield return null;

            GameObject thisCombatUIGameObject = CombatUICanvas;

            if (thisCombatUIGameObject == null)
            {
                Debug.LogError("❌ CombatUICanvas is null - cannot setup UI!");
                yield break;
            }

            // Use persistent CombatUIManager
            if (CombatUIManager.Instance != null && ActiveCombatController != null)
            {
                CombatUIManager.Instance.SetupForCombat(ActiveCombatController, thisCombatUIGameObject, Combat3DCanvas, GameOverCanvas);
                Debug.Log("✅ CombatUIManager configured for local player");
            }
            else
            {
                Debug.LogError("❌ CombatUIManager.Instance or ActiveCombatController is null!");
            }
        }

        internal void SetDiplomacyController(DiplomacyController diplomacyController)
        {
            // inquiry the CivRelationsManager's Dictionary for current fleet/system ships data
            var sideOneShips = new List<ShipController>();
            var sideTwoShips = new List<ShipController>();
            var intelCon = IntelligenceManager.Instance.ReturnAnIntelligenceController(diplomacyController.DiplomacyData.CivEnumSideOne, diplomacyController.DiplomacyData.CivEnumSideTwo);

            if (intelCon != null)
            {
                if (intelCon.IntelligenceData.LastSeenFleetOfSideOne != null)
                {
                    sideOneShips = intelCon.IntelligenceData.LastSeenFleetOfSideOne.FleetData.ShipsList;
                    if (intelCon.IntelligenceData.LastSeenFleetOfSideTwo != null)
                    {
                        sideTwoShips = intelCon.IntelligenceData.LastSeenFleetOfSideTwo.FleetData.ShipsList;
                        if (sideOneShips.Count > 0 && sideTwoShips.Count > 0)
                            InstantiateCombatController(sideOneShips, sideTwoShips);
                    }
                    else if (intelCon.IntelligenceData.LastSeenStarSysController != null)
                    {
                        sideTwoShips = intelCon.IntelligenceData.LastSeenStarSysController.StarSysData.ShipsList;
                        if (sideOneShips.Count > 0 && sideTwoShips.Count > 0)
                            InstantiateCombatController(sideOneShips, sideTwoShips);
                    }
                }
                else if (intelCon.IntelligenceData.LastSeenFleetOfSideTwo.FleetData != null)
                {
                    sideTwoShips = intelCon.IntelligenceData.LastSeenFleetOfSideTwo.FleetData.ShipsList;
                    sideOneShips = intelCon.IntelligenceData.LastSeenStarSysController.StarSysData.ShipsList;
                }
            }
        }

        internal void RemoveThisShipController(ShipController shipController)
        {
            for (int i = 0; i < allCombatControllers.Count; i++)
            {
                for (int j = 0; j < allCombatControllers[i].CombatData.SideOneShipCons.Count; j++)
                {
                    if (allCombatControllers[i].CombatData.SideOneShipCons[j] == shipController)
                    {
                        bool v = allCombatControllers[i].CombatData.SideOneShipCons.Remove(shipController);
                        Scene combatScene = SceneManager.GetSceneByName("CombatScene");
                        combatScene.GetRootGameObjects().ToList().ForEach(go => Destroy(go));
                        ShipCombatCameraController.Instance.WarpingInOver = false;
                        break;
                    }
                }
                for (int j = 0; j < allCombatControllers[i].CombatData.SideTwoShipCons.Count; j++)
                {
                    if (allCombatControllers[i].CombatData.SideTwoShipCons[j] == shipController)
                    {
                        bool v = allCombatControllers[i].CombatData.SideTwoShipCons.Remove(shipController);
                        Scene combatScene = SceneManager.GetSceneByName("CombatScene");
                        combatScene.GetRootGameObjects().ToList().ForEach(go => Destroy(go));
                        ShipCombatCameraController.Instance.WarpingInOver = false;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Clean up after combat ends
        /// </summary>
        private void CleanupCombat()
        {
            Debug.Log("🧹 CombatManager: Cleaning up combat...");

            // Clear Unity Editor selection to prevent MissingReferenceException
#if UNITY_EDITOR
            UnityEditor.Selection.activeObject = null;
#endif

            // Unload scene
            if (SceneManager.GetSceneByName("CombatScene").isLoaded)
            {
                SceneManager.UnloadSceneAsync("CombatScene");
            }

            // Clear references
            CombatUICanvas = null;
            Combat3DCanvas = null;
            GameOverCanvas = null;

            Debug.Log("✅ Combat cleanup complete");
        }

        /// <summary>
        /// Data structure for queued combats
        /// </summary>
        public class PendingCombat
        {
            public List<ShipController> SideOneShips;
            public List<ShipController> SideTwoShips;
            public CombatType CombatType;
        }
    }
}
