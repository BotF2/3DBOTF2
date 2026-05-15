using BOTF3D.Audio;
using BOTF3D.Combat;
using BOTF3D.GamePlay;
using BOTF3D.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        [Header("Animator References (Assigned in Inspector)")]
        private GameObject _sideOneA1Parent;
        private GameObject _sideOneA2Parent;
        private GameObject _sideOneA3Parent;
        private GameObject _sideTwoA1Parent;
        private GameObject _sideTwoA2Parent;
        private GameObject _sideTwoA3Parent;

        [Header("Weapon Prefabs")]
        public List<GameObject> TorpedoPrefabs;
        public List<GameObject> BeamPrefabs;
        public AudioClip[] BeamFireClips;
        public AudioClip[] TorpedoFireClips;

        // ✅ Combat queue system
        private Queue<PendingCombat> combatQueue = new Queue<PendingCombat>();
        public CombatController ActiveCombatController { get; private set; }


        private List<CombatController> allCombatControllers = new List<CombatController>();
        // ✅ NEW: Track combat state
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

            // ✅ Cache the prefab BEFORE DontDestroyOnLoad
            _cachedCombatConPrefab = combatConPrefab;

            // ✅ Verify it's actually assigned
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

            // ✅ Verify it survived the move
            if (combatConPrefab == null && _cachedCombatConPrefab != null)
            {
                Debug.LogWarning("⚠️ combatConPrefab was cleared by DontDestroyOnLoad - restoring from cache");
                combatConPrefab = _cachedCombatConPrefab;
            }

            Debug.Log("✅ CombatManager initialized.");
        }
        /// <summary>
        /// ✅ NEW: Request a combat - will be queued if another is active
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
        /// ✅ NEW: Process one combat at a time from the queue
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

                    // ✅ Set ActiveCombatController FIRST
                    ActiveCombatController = combatController;

                    // ✅ ADD THIS LINE - Now both Instance and ActiveCombatController are non-null
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
        // ✅ Add coroutine version
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
        internal void SetDiplomacyController(DiplomacyController diplomacyController)
        {
            // inquiry the CivRelationsManager's Dictionary for current fleet/system ships data
            // to populate the combat data
            var sideOneShips = new List<ShipController>();
            var sideTwoShips = new List<ShipController>();
            var intelCon = IntelligenceManager.Instance.ReturnAnIntelligenceController(diplomacyController.DiplomacyData.CivEnumSideOne, diplomacyController.DiplomacyData.CivEnumSideTwo); //var intelData = CivRelationsManager.Instance.GetRelationsData(diplomacyController.DiplomacyData.CivSideOne, diplomacyController.DiplomacyData.CivSideTwo);
            if (intelCon != null)
            {
                if (intelCon == null)
                {
                    Debug.LogError("IntelData is null in CivRelationsData for civs: " + diplomacyController.DiplomacyData.CivEnumSideOne + " and " + diplomacyController.DiplomacyData.CivEnumSideTwo);
                    return;
                }
                if (intelCon.IntelligenceData.LastSeenFleetOfSideOne != null)
                {

                    sideOneShips = intelCon.IntelligenceData.LastSeenFleetOfSideOne.FleetData.ShipsList;
                    if (intelCon.IntelligenceData.LastSeenFleetOfSideTwo != null)
                    {
                        sideTwoShips = intelCon.IntelligenceData.LastSeenFleetOfSideTwo.FleetData.ShipsList;
                        //if (sideOneShips.Count > 0 && sideTwoShips.Count > 0)
                        // InstantiateCombatController(sideOneShips, sideTwoShips);
                    }
                    else if (intelCon.IntelligenceData.LastSeenStarSysController != null)
                    {
                        sideTwoShips = intelCon.IntelligenceData.LastSeenStarSysController.StarSysData.ShipsList;
                        //if (sideOneShips.Count > 0 && sideTwoShips.Count > 0)
                        // InstantiateCombatController(sideOneShips, sideTwoShips);
                    }
                }
                else if (intelCon.IntelligenceData.LastSeenFleetOfSideTwo.FleetData != null)
                {
                    sideTwoShips = intelCon.IntelligenceData.LastSeenFleetOfSideTwo.FleetData.ShipsList;
                    sideOneShips = intelCon.IntelligenceData.LastSeenStarSysController.StarSysData.ShipsList;
                }
            }
        }

        public CombatController InstantiateCombatController(List<ShipController> sideOneShipCons, List<ShipController> sideTwoShipCons)
        {
            // ✅ Validate inputs
            if (sideOneShipCons == null || sideOneShipCons.Count == 0)
            {
                Debug.LogError("InstantiateCombatController: sideOneShipCons is null or empty!");
                return null;
            }

            if (sideTwoShipCons == null || sideTwoShipCons.Count == 0)
            {
                Debug.LogError("InstantiateCombatController: sideTwoShipCons is null or empty!");
                return null;
            }

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
            if (combatConPrefab == null)
            {
                // Try to use cached version
                if (_cachedCombatConPrefab != null)
                {
                    Debug.LogWarning("⚠️ Using cached combatConPrefab reference");
                    combatConPrefab = _cachedCombatConPrefab;
                }
                else
                {
                    Debug.LogError("InstantiateCombatController: combatConPrefab is null! Assign it in Inspector.");
                    Debug.LogError($"   Check GameObject '{gameObject.name}' in the starting scene (before DontDestroyOnLoad)");
                    return null;
                }
            }
            // ✅ Instantiate new controller (never reuse)
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

            // Assign animators (found at runtime from scene)
            if (_sideOneA1Parent != null)
            {
                aCombatController.sideOneA1Parent = _sideOneA1Parent;
            }
            else
            {
                Debug.LogError("❌ _sideOneA1Parent is null! FindCombatSceneReferences() may have failed.");
            }

            if (_sideOneA2Parent != null)
            {
                aCombatController.sideOneA2Parent = _sideOneA2Parent;
            }
            else
            {
                Debug.LogError("❌ _sideOneA2Parent is null!");
            }

            if (_sideOneA3Parent != null)
            {
                aCombatController.sideOneA3Parent = _sideOneA3Parent;
            }
            else
            {
                Debug.LogError("❌ _sideOneA3Parent is null!");
            }

            if (_sideTwoA1Parent != null)
            {
                aCombatController.sideTwoA1Parent = _sideTwoA1Parent;
            }
            else
            {
                Debug.LogError("❌ _sideTwoA1Parent is null!");
            }

            if (_sideTwoA2Parent != null)
            {
                aCombatController.sideTwoA2Parent = _sideTwoA2Parent;
            }
            else
            {
                Debug.LogError("❌ _sideTwoA2Parent is null!");
            }

            if (_sideTwoA3Parent != null)
            {
                aCombatController.sideTwoA3Parent = _sideTwoA3Parent;
            }
            else
            {
                Debug.LogError("❌ _sideTwoA3Parent  is null!");
            }
            // Assign animators - only set the individual fields, don't populate the list
            // The list will be populated by CombatController.Start()
            aCombatController.sideOneA1Parent = _sideOneA1Parent;
            aCombatController.sideOneA2Parent = _sideOneA2Parent;
            aCombatController.sideOneA3Parent = _sideOneA3Parent;
            aCombatController.sideTwoA1Parent = _sideTwoA1Parent;
            aCombatController.sideTwoA2Parent = _sideTwoA2Parent;
            aCombatController.sideTwoA3Parent = _sideTwoA3Parent;
            // ✅ REMOVED: Don't add to animators list here - Start() will do it
            // Assign weapon prefabs
            aCombatController.SideOneTorpedoPrefab = GetTorpedoPrefabs(aCombatController, combatData.CivEnumSideOne);
            aCombatController.SideTwoTorpedoPrefab = GetTorpedoPrefabs(aCombatController, combatData.CivEnumSideTwo);
            aCombatController.SideOneBeamPrefab = GetBeamPrefabs(aCombatController, combatData.CivEnumSideOne);
            aCombatController.SideTwoBeamPrefab = GetBeamPrefabs(aCombatController, combatData.CivEnumSideTwo);
            // ✅ ADD AUDIO CLIP ASSIGNMENT
            aCombatController.SideOneBeamFireClip = GetBeamFireClip(combatData.CivEnumSideOne);
            aCombatController.SideTwoBeamFireClip = GetBeamFireClip(combatData.CivEnumSideTwo);
            aCombatController.SideOneTorpedoFireClip = GetTorpedoFireClip(combatData.CivEnumSideOne);
            aCombatController.SideTwoTorpedoFireClip = GetTorpedoFireClip(combatData.CivEnumSideTwo);


            // Add to tracking list
            allCombatControllers.Add(aCombatController);

            // Populate and start
            aCombatController.PopulateShipData(aCombatController);
            aCombatController.TrySetPlayerOrders(combatData);

            // ❌ REMOVE THIS LINE - SetUpLocalPlayer will be called later
            // SetUpLocalPlayer();

            TimeManager.Instance.PauseTime(); // Pause galaxy time during combat

            Debug.Log($"✅ CombatController {aCombatController.CombatID} instantiated successfully");
            return aCombatController;

        }
        /// <summary>
        /// Called after CombatScene loads to find all combat scene references (searches all children)
        /// </summary>
        private void FindCombatSceneReferences()
        {
            Scene combatScene = SceneManager.GetSceneByName("CombatScene");

            Debug.Log($"🔍 Scene search - Scene name: '{combatScene.name}', IsLoaded: {combatScene.isLoaded}, IsValid: {combatScene.IsValid()}");

            if (!combatScene.isLoaded)
            {
                Debug.LogError("❌ CombatScene not loaded - cannot find references!");
                return;
            }

            // Get all root GameObjects in combat scene
            GameObject[] rootObjects = combatScene.GetRootGameObjects();

            Debug.Log($"🔍 Found {rootObjects.Length} root objects in CombatScene:");

            // ✅ List ALL objects with exact name details
            for (int i = 0; i < rootObjects.Length; i++)
            {
                GameObject root = rootObjects[i];
                Debug.Log($"   [{i}] Root: '{root.name}' (Active: {root.activeSelf}, Length: {root.name.Length} chars)");

                // ✅ Show if it matches what we're looking for
                if (root.name == "CombatUICanvas")
                {
                    Debug.Log($"       ⭐ EXACT MATCH for CombatUICanvas!");
                }
                else if (root.name.Contains("CombatUI"))
                {
                    Debug.Log($"       ⚠️ Contains 'CombatUI' but not exact match. Comparing:");
                    Debug.Log($"          Looking for: 'CombatUICanvas' (length: {"CombatUICanvas".Length})");
                    Debug.Log($"          Found:       '{root.name}' (length: {root.name.Length})");

                    // Character-by-character comparison
                    for (int j = 0; j < Mathf.Max(root.name.Length, "CombatUICanvas".Length); j++)
                    {
                        char expected = j < "CombatUICanvas".Length ? "CombatUICanvas"[j] : '?';
                        char actual = j < root.name.Length ? root.name[j] : '?';
                        if (expected != actual)
                        {
                            Debug.Log($"          Char [{j}]: expected '{expected}' (code: {(int)expected}), got '{actual}' (code: {(int)actual})");
                        }
                    }
                }

                // Check root object first
                CheckAndAssignReferences(root);

                // ✅ Then search all children recursively
                SearchChildrenRecursive(root.transform);
            }

            // ✅ Final status report
            Debug.Log($"📊 Search Results:");
            Debug.Log($"   S1A1 Parent: {(_sideOneA1Parent != null ? "✅ FOUND" : "❌ NOT FOUND")}");
            Debug.Log($"   S1A2 Parent: {(_sideOneA2Parent != null ? "✅ FOUND" : "❌ NOT FOUND")}");
            Debug.Log($"   S1A3 Parent: {(_sideOneA3Parent != null ? "✅ FOUND" : "❌ NOT FOUND")}");
            Debug.Log($"   S2A1 Parent: {(_sideTwoA1Parent != null ? "✅ FOUND" : "❌ NOT FOUND")}");
            Debug.Log($"   S2A2 Parent: {(_sideTwoA2Parent != null ? "✅ FOUND" : "❌ NOT FOUND")}");
            Debug.Log($"   S2A3 Parent: {(_sideTwoA3Parent != null ? "✅ FOUND" : "❌ NOT FOUND")}");
            Debug.Log($"   CombatUICanvas: {(CombatUICanvas != null ? $"✅ FOUND at '{GetGameObjectPath(CombatUICanvas)}'" : "❌ NOT FOUND")}");
            Debug.Log($"   Combat3DCanvas: {(Combat3DCanvas != null ? $"✅ FOUND" : "❌ NOT FOUND")}");

            // Validate all required references were found
            bool allParentsFound = _sideOneA1Parent != null && _sideOneA2Parent != null &&
                                   _sideOneA3Parent != null && _sideTwoA1Parent != null &&
                                   _sideTwoA2Parent != null && _sideTwoA3Parent != null;

            if (!allParentsFound)
            {
                Debug.LogError("❌ Not all parent references were found in CombatScene!");
            }

            if (CombatUICanvas == null)
            {
                Debug.LogError("❌ CombatUICanvas not found in CombatScene! Check GameObject name in scene hierarchy.");
                Debug.LogError("   💡 Make sure:");
                Debug.LogError("      1. GameObject is named EXACTLY 'CombatUICanvas' (case-sensitive, no extra spaces)");
                Debug.LogError("      2. GameObject is in CombatScene (not a different scene)");
                Debug.LogError("      3. GameObject is active in hierarchy (not disabled)");
            }

            if (Combat3DCanvas == null)
            {
                Debug.LogWarning("⚠️ Combat3DCanvas not found in CombatScene!");
            }

            if (allParentsFound && CombatUICanvas != null)
            {
                Debug.Log("✅ All combat scene references found successfully");
            }
            if (CombatUICanvas == null)
            {
                Debug.LogWarning("⚠️ Attempting fallback search using GameObject.Find...");

                // Find in all loaded scenes
                CombatUICanvas = GameObject.Find("CombatUICanvas");

                if (CombatUICanvas != null)
                {
                    Debug.Log($"   ✅ Found CombatUICanvas via GameObject.Find at: {GetGameObjectPath(CombatUICanvas)}");
                    Debug.Log($"      Scene: {CombatUICanvas.scene.name}, Active: {CombatUICanvas.activeSelf}");
                }
                else
                {
                    // Try with spaces or common typos
                    string[] possibleNames = { "CombatUI Canvas", "Combat UI Canvas", "CombatUiCanvas", "combatUICanvas" };
                    foreach (string name in possibleNames)
                    {
                        var go = GameObject.Find(name);
                        if (go != null)
                        {
                            Debug.LogWarning($"   ⚠️ Found similar object '{name}' - using as CombatUICanvas");
                            CombatUICanvas = go;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ✅ NEW: Recursively search all children for combat references
        /// </summary>
        private void SearchChildrenRecursive(Transform parent)
        {
            foreach (Transform child in parent)
            {
                CheckAndAssignReferences(child.gameObject);

                // Recurse into children
                if (child.childCount > 0)
                {
                    SearchChildrenRecursive(child);
                }
            }
        }

        /// <summary>
        /// ✅ NEW: Check a single GameObject and assign if it matches
        /// </summary>
        private void CheckAndAssignReferences(GameObject go)
        {
            switch (go.name)
            {
                case "S1A1":
                    if (_sideOneA1Parent == null)
                    {
                        _sideOneA1Parent = go;
                        Debug.Log($"     ✅ Found and assigned S1A1 parent at path: {GetGameObjectPath(go)}");
                    }
                    break;
                case "S1A2":
                    if (_sideOneA2Parent == null)
                    {
                        _sideOneA2Parent = go;
                        Debug.Log($"     ✅ Found and assigned S1A2 parent at path: {GetGameObjectPath(go)}");
                    }
                    break;
                case "S1A3":
                    if (_sideOneA3Parent == null)
                    {
                        _sideOneA3Parent = go;
                        Debug.Log($"     ✅ Found and assigned S1A3 parent at path: {GetGameObjectPath(go)}");
                    }
                    break;
                case "S2A1":
                    if (_sideTwoA1Parent == null)
                    {
                        _sideTwoA1Parent = go;
                        Debug.Log($"     ✅ Found and assigned S2A1 parent at path: {GetGameObjectPath(go)}");
                    }
                    break;
                case "S2A2":
                    if (_sideTwoA2Parent == null)
                    {
                        _sideTwoA2Parent = go;
                        Debug.Log($"     ✅ Found and assigned S2A2 parent at path: {GetGameObjectPath(go)}");
                    }
                    break;
                case "S2A3":
                    if (_sideTwoA3Parent == null)
                    {
                        _sideTwoA3Parent = go;
                        Debug.Log($"     ✅ Found and assigned S2A3 parent at path: {GetGameObjectPath(go)}");
                    }
                    break;
                case "CombatUICanvas":
                    if (CombatUICanvas == null)
                    {
                        CombatUICanvas = go;
                        Debug.Log($"     ✅ Found and assigned CombatUICanvas at path: {GetGameObjectPath(go)}");
                    }
                    break;
                case "Combat3DCanvas":
                    if (Combat3DCanvas == null)
                    {
                        Combat3DCanvas = go;
                        Debug.Log($"     ✅ Found and assigned Combat3DCanvas at path: {GetGameObjectPath(go)}");
                    }
                    break;
                case "GameOverCanvas":
                    if (GameOverCanvas == null)
                    {
                        GameOverCanvas = go;
                        Debug.Log($"     ✅ Found and assigned GameOverCanvas at path: {GetGameObjectPath(go)}");
                    }
                    break;
            }
        }

        /// <summary>
        /// ✅ NEW: Get full hierarchy path of a GameObject for debugging
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
            GameObject torbedoPrefab = TorpedoPrefabs[TorpedoPrefabs.Count - 1]; // default to minor civ prefab

            for (int i = 0; i < TorpedoPrefabs.Count; i++)
            {
                if (i == (int)civEnum)
                {
                    torbedoPrefab = TorpedoPrefabs[i];
                    return torbedoPrefab; // Return the prefab for the specific civ
                }
            }
            return torbedoPrefab; // Return the default prefab if no match found
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

            // ✅ For playable civs (0-6), use their specific clip
            if (civIndex >= 0 && civIndex < BeamFireClips.Length - 1)
            {
                AudioClip clip = BeamFireClips[civIndex];
                Debug.Log($"✅ Assigned BeamFireClip for {civEnum} (index {civIndex}): {clip?.name ?? "NULL"}");
                return clip;
            }

            // ✅ For minor civs (index > 7), use the last clip as fallback
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

            // ✅ For playable civs (0-6), use their specific clip
            if (civIndex >= 0 && civIndex < TorpedoFireClips.Length - 1)
            {
                AudioClip clip = TorpedoFireClips[civIndex];
                Debug.Log($"✅ Assigned TorpedoFireClip for {civEnum} (index {civIndex}): {clip?.name ?? "NULL"}");
                return clip;
            }

            // ✅ For minor civs (index > 7), use the last clip as fallback
            AudioClip fallbackClip = TorpedoFireClips[TorpedoFireClips.Length - 1];
            Debug.Log($"✅ Assigned fallback TorpedoFireClip for {civEnum} (index {civIndex}): {fallbackClip?.name ?? "NULL"}");
            return fallbackClip;
        }

        /// <summary>
        /// ✅ NEW: Called when a combat ends
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
            // ✅ Wait two frames per copilot-instructions.md
            yield return null;
            yield return null;

            GameObject thisCombatUIGameObject = CombatUICanvas;

            if (thisCombatUIGameObject == null)
            {
                Debug.LogError("❌ CombatUICanvas is null - cannot setup UI!");
                yield break;
            }

            // ✅ Use persistent CombatUIManager
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

        /// <summary>
        /// ✅ NEW: Data structure for queued combats
        /// </summary>
        public class PendingCombat
        {
            public List<ShipController> SideOneShips;
            public List<ShipController> SideTwoShips;
            public CombatType CombatType;
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
                        ShipCombatCameraController.Instance.WarpingInOver = false; // also turns off auto-rotation of camera
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

            // ✅ Clear Unity Editor selection to prevent MissingReferenceException
#if UNITY_EDITOR
            UnityEditor.Selection.activeObject = null;
#endif

            // Existing cleanup code...
            if (ActiveCombatController != null)
            {
                // Your existing cleanup
            }

            // Unload scene
            if (SceneManager.GetSceneByName("CombatScene").isLoaded)
            {
                SceneManager.UnloadSceneAsync("CombatScene");
            }

            // Clear references
            CombatUICanvas = null;
            Combat3DCanvas = null;
            GameOverCanvas = null;
            _sideOneA1Parent = null;
            _sideOneA2Parent = null;
            _sideOneA3Parent = null;
            _sideTwoA1Parent = null;
            _sideTwoA2Parent = null;
            _sideTwoA3Parent = null;

            Debug.Log("✅ Combat cleanup complete");
        }
    }
}

