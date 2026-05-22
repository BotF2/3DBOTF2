// Ignore Spelling: BOTF Kling Unregister sys

using BOTF3D.Combat;
using BOTF3D.GamePlay;
using BOTF3D.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;


namespace BOTF3D.Core
{
    public class ShipManager : MonoBehaviour
    {
        public static ShipManager Instance;

        [SerializeField]
        private BOTF3D.Combat.ShipController shipConPrefab;
        [Header("Ship UI")]
        [SerializeField]
        private GameObject shipListUIPrefab; // prefab for the ship list UI in the galaxy menu

        public List<ShipController> ShipControllerList = new List<ShipController>();

        //public ShipSORegistry ShipSORegistry;
        [Header("Weapon Prefabs")]
        public GameObject targetGOPrefab;
        public GameObject[] torpedoPrefabs;
        public GameObject[] beamWeaponPrefabs;
        int shipIndex = 0;
        [Header("Weapon Audio Clips")]
        public AudioClip[] beamFireClips;     // Index matches civ: 0=FED, 1=ROM, 2=KLING, 3=CARD, 4=DOM, 5=BORG, 6=TERRAN, 7+=Minor fallback
        public AudioClip[] torpedoFireClips;  // Same indexing as beamFireClips
        private List<(ShipController shipController, GameObject uiParent)> pendingShipUIs = new List<(ShipController, GameObject)>();
        [Header("Ship ScriptableObjects - Ship Templates")]
        [SerializeField] public List<ShipSO> FedShipSOList;
        [SerializeField] public List<ShipSO> RomShipSOList;
        [SerializeField] public List<ShipSO> KlingShipSOList;
        [SerializeField] public List<ShipSO> CardShipSOList;
        [SerializeField] public List<ShipSO> DomShipSOList;
        [SerializeField] public List<ShipSO> BorgShipSOList;
        [SerializeField] public List<ShipSO> TerranShipSOList;
        [SerializeField] public List<ShipSO> MinorShipSOList;
        [SerializeField] public ShipSO Test;

        [Header("Ship Prefabs")]
        [SerializeField] private ShipController galaxyShipPrefab;

        public List<ShipData> AllShipsData = new List<ShipData>();
        public List<ShipController> AllShipControllers = new List<ShipController>();

        // Scene-specific tracking (cleared when scenes unload)
        private Dictionary<int, ShipController> galaxyShipControllers = new Dictionary<int, ShipController>();
        private Dictionary<int, ShipController> combatShipControllers = new Dictionary<int, ShipController>();
        // Pending UI items that couldn't be properly parented when created
        private readonly List<ShipController> shipConPendingShipUI = new List<ShipController>();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
            // ✅ Auto-populate civ lists if empty (Editor only)
            if (FedShipSOList.Count == 0)
            {
                Debug.Log("ShipManager: Auto-populating ship lists from assets...");
                AutoPopulateShipLists();
            }
            else
            {
                Debug.Log($"ShipManager: Ship lists already populated (Fed: {FedShipSOList.Count})");
            }
#else
    // ✅ In builds, verify lists are populated
    int totalShips = FedShipSOList.Count + RomShipSOList.Count + KlingShipSOList.Count + 
                     CardShipSOList.Count + DomShipSOList.Count + BorgShipSOList.Count + TerranShipSOList.Count;
    
    if (totalShips == 0)
    {
        Debug.LogError("❌ ShipManager: All ship lists are empty! Run Auto-Populate in Editor before building!");
    }
    else
    {
        Debug.Log($"✅ ShipManager: {totalShips} ships loaded across all civs");
    }
#endif
        }
        #region Possible AI Ship Manager for Galaxy and Combat Scenes
        //  blue print, In GalaxyScene - create ship from SO
        //    var scoutSO = ShipManager.Instance.GetShipSO(CivEnum.FED, ShipType.Scout);
        //    var ship = ShipManager.Instance.InstantiateGalaxyShip(scoutSO, position, CivEnum.FED);

        //    // Transition to combat
        //    ShipManager.Instance.TransitionShipsToCombat(fleet.FleetData.ShipsList);

        //// In CombatScene - create combat instances
        //foreach (var shipData in fleet.FleetData.ShipsList.Select(s => s.ShipData))
        //{
        //    ShipManager.Instance.InstantiateCombatShip(shipData, combatPosition);
        //}

        //// After combat - sync results
        //ShipManager.Instance.SyncCombatResultsToGalaxy();

        public ShipSO GetShipSO(CivEnum civEnum, ShipType shipType)
        {
            List<ShipSO> shipList = GetShipSOListByCiv(civEnum);
            if (shipList == null) return null;

            return shipList.FirstOrDefault(s => s.ShipType == shipType);
        }
        // Get the appropriate ship SO list for a civilization
        public List<ShipSO> GetShipSOListByCiv(CivEnum civEnum)
        {
            switch (civEnum)
            {
                case CivEnum.FED: return FedShipSOList;
                case CivEnum.ROM: return RomShipSOList;
                case CivEnum.KLING: return KlingShipSOList;
                case CivEnum.CARD: return CardShipSOList;
                case CivEnum.DOM: return DomShipSOList;
                case CivEnum.BORG: return BorgShipSOList;
                case CivEnum.TERRAN: return TerranShipSOList;
                default: // search minor ship list for the given minor civ
                    if (MinorShipSOList == null || MinorShipSOList.Count == 0)
                    {
                        Debug.LogWarning($"GetShipSOListByCiv: MinorShipSOList is empty!");
                        return new List<ShipSO>();
                    }

                    var minorCivShips = MinorShipSOList
                        .Where(s => s != null && s.CivEnum == civEnum)
                        .ToList();

                    if (minorCivShips.Count == 0)
                    {
                        Debug.LogWarning($"GetShipSOListByCiv: No ships found for minor civ {civEnum} in MinorShipSOList");
                    }
                    else
                    {
                        Debug.Log($"GetShipSOListByCiv: Found {minorCivShips.Count} ships for {civEnum}");
                    }

                    return minorCivShips;
            }
        }
        public ShipController InstantiateGalaxyShip(ShipSO shipSO, Vector3 position, CivEnum civEnum)
        {
            if (galaxyShipPrefab == null)
            {
                Debug.LogError("ShipManager: galaxyShipPrefab is null!");
                return null;
            }

            // Create ShipData from SO
            ShipData shipData = new ShipData(shipSO);
            shipData.CivEnum = civEnum;
            shipData.Position = position;

            // Instantiate the ship controller
            ShipController shipController = Instantiate(galaxyShipPrefab, position, Quaternion.identity);
            shipController.ShipData = shipData;
            shipController.name = $"{shipData.ShipName}";

            // Register with manager
            RegisterGalaxyShip(shipController);
            AllShipControllers.Add(shipController);

            Debug.Log($"ShipManager: Instantiated galaxy ship {shipController.name}");

            return shipController;
        }
        // Register a ship controller when it's instantiated in a scene
        public void RegisterGalaxyShip(ShipController shipController)
        {
            if (shipController?.ShipData != null)
            {
                int shipId = shipController.ShipData.ShipName.GetHashCode();
                galaxyShipControllers[shipId] = shipController;

                // Ensure ShipData is tracked
                if (!AllShipsData.Contains(shipController.ShipData))
                {
                    AllShipsData.Add(shipController.ShipData);
                }

                Debug.Log($"ShipManager: Registered galaxy ship {shipController.name} (ID={shipId})");
            }
        }
        public void RegisterCombatShip(ShipController shipController)
        {
            if (shipController?.ShipData != null)
            {
                int shipId = shipController.ShipData.ShipName.GetHashCode(); ;
                combatShipControllers[shipId] = shipController;
                Debug.Log($"ShipManager: Registered combat ship {shipController.name} (ID={shipId})");
            }
        }
        // Clean up when scenes unload
        public void UnregisterGalaxyShip(int shipId)
        {
            galaxyShipControllers.Remove(shipId);
            Debug.Log($"ShipManager: Unregistered galaxy ship (ID={shipId})");
        }

        public void UnregisterCombatShip(int shipId)
        {
            combatShipControllers.Remove(shipId);
            Debug.Log($"ShipManager: Unregistered combat ship (ID={shipId})");
        }
        // Get ship controller for current scene
        public ShipController GetGalaxyShipController(int shipId)
        {
            galaxyShipControllers.TryGetValue(shipId, out ShipController controller);
            return controller;
        }

        public ShipController GetCombatShipController(int shipId)
        {
            combatShipControllers.TryGetValue(shipId, out ShipController controller);
            return controller;
        }
        // Get ship data (always available)
        public ShipData GetShipData(int shipId)
        {
            return AllShipsData.Find(s => s.ShipName.GetHashCode() == shipId);
        }
        // When transitioning to combat, prepare ship data
        public void TransitionShipsToCombat(List<ShipController> galaxyShips)
        {
            Debug.Log($"ShipManager: Transitioning {galaxyShips.Count} ships to combat");

            // Ships will be instantiated in CombatScene by CombatSceneController
            // ShipData flows through this manager
            foreach (var galaxyShip in galaxyShips)
            {
                if (galaxyShip?.ShipData != null)
                {
                    // Data is already tracked in AllShipsData
                    Debug.Log($"  - Ship {galaxyShip.ShipData.ShipName} ready for combat instantiation");
                }
            }
        }
        // Instantiate combat ship from existing ShipData
        //public ShipController InstantiateCombatShip(ShipData shipData, Vector3 position)
        //{
        //    if (combatShipPrefab == null)
        //    {
        //        Debug.LogError("ShipManager: combatShipPrefab is null!");
        //        return null;
        //    }

        //    ShipController combatShip = Instantiate(combatShipPrefab, position, Quaternion.identity);
        //    combatShip.ShipData = shipData; // Reuse the same data!
        //    combatShip.name = $"{shipData.ShipName}_Combat";

        //    RegisterCombatShip(combatShip);

        //    Debug.Log($"ShipManager: Instantiated combat ship {combatShip.name}");

        //    return combatShip;
        //}
        // Pending UI processing (from your existing code)
        public void AddPendingShipUI(ShipController shipController, GameObject uiParent)
        {
            pendingShipUIs.Add((shipController, uiParent));
        }
        // ToDo: use this version later?
        //public void ProcessPendingShipUIs()
        //{
        //    if (pendingShipUIs.Count == 0) return;

        //    foreach (var pending in pendingShipUIs)
        //    {
        //        if (pending.shipController != null && pending.uiParent != null)
        //        {
        //            // Create ship UI item
        //            CreateShipUIItem(pending.shipController, pending.uiParent);
        //        }
        //    }

        //    pendingShipUIs.Clear();
        //}
        private void CreateShipUIItem(ShipController shipController, GameObject uiParent)
        {
            if (shipListUIPrefab == null) return;

            GameObject shipUI = Instantiate(shipListUIPrefab, uiParent.transform);
            shipController.ShipListUIGameObject = shipUI;

            var shipUIItem = shipUI.GetComponent<ShipListUI_Item>();
            if (shipUIItem != null)
            {
                shipUIItem.ShipController = shipController;
            }

            Debug.Log($"ShipManager: Created UI for ship {shipController.name}");
        }
        // Clean up scene-specific dictionaries when scenes unload
        public void ClearGalaxyShips()
        {
            galaxyShipControllers.Clear();
            Debug.Log("ShipManager: Cleared galaxy ship controllers");
        }

        public void ClearCombatShips()
        {
            combatShipControllers.Clear();
            Debug.Log("ShipManager: Cleared combat ship controllers");
        }
        // Call this when returning from combat to galaxy
        public void SyncCombatResultsToGalaxy()
        {
            Debug.Log("ShipManager: Syncing combat results back to galaxy");

            // Update galaxy ShipData based on combat outcomes
            foreach (var combatShip in combatShipControllers.Values)
            {
                if (combatShip?.ShipData != null)
                {
                    // ShipData is shared, so changes in combat are already reflected
                    // Just need to update or recreate galaxy visual if needed
                    Debug.Log($"  - Ship {combatShip.ShipData.ShipName} HP: {combatShip.ShipData.ShieldHealth}/{combatShip.ShipData.HullHealth}");
                }
            }
        }
        #endregion
        public void OnSelectModel(string selectedShipName)
        {
            Vector3 spawnPos = new Vector3(0, 0, 0);
            SpawnByShipName(selectedShipName, spawnPos);
        }
        public void SpawnByShipName(string shipName, Vector3 position)
        {
            ShipSORegistry shipSORegistry = new ShipSORegistry();
            ShipSO shipSO = shipSORegistry.GetByID(shipName);
            if (shipSO != null && shipSO.ShipFBX_ModelAsGOPrefab != null)
            {
                Instantiate(shipSO.ShipFBX_ModelAsGOPrefab, position, Quaternion.identity);
            }
        }

        public List<ShipController> InstantiateShipControllersWithDataFromSO(List<ShipSO> shipSOList, GameObject parentGO)
        {
            List<ShipController> shipConList = new List<ShipController>();

            Debug.Log($"InstantiateShipControllersWithDataFromSO: Creating {shipSOList?.Count ?? 0} ships for '{parentGO?.name ?? "NULL"}'");

            if (parentGO == null)
            {
                Debug.LogError("InstantiateShipControllersWithDataFromSO: parentGO is NULL!");
                return shipConList;
            }

            for (int i = 0; i < shipSOList.Count; i++)
            {
                if (shipSOList[i] == null)
                {
                    Debug.LogWarning($"  Ship SO at index {i} is null, skipping");
                    continue;
                }

                // ✅ CRITICAL FIX: Use parentGO, NOT CombatManager!
                BOTF3D.Combat.ShipController shipCon = Instantiate(
                    shipConPrefab,
                    new Vector3(0, 0, 0),
                    Quaternion.identity,
                    parentGO.transform); // ✅ FIXED: Use parentGO (fleet/system in galaxy)

                shipCon.Init(this);
                shipCon.ShipData = new ShipData();
                // ✅ CRITICAL FIX: Store the ShipSO reference
                shipCon.ShipData.ShipSO = shipSOList[i];
                shipCon.ShipData.ShipName = shipSOList[i].ShipName;
                shipCon.ShipData.CivEnum = shipSOList[i].CivEnum;
                shipCon.ShipData.TechLevel = shipSOList[i].TechLevel;
                shipCon.ShipData.ShipType = shipSOList[i].ShipType;

                if (shipSOList[i].shipSprite != null)
                    shipCon.ShipData.ShipSprite = shipSOList[i].shipSprite;

                shipCon.ShipData.maxWarpFactor = shipSOList[i].maxWarpFactor;
                shipCon.ShipData.currentWarpFactor = 0f;
                shipCon.ShipData.ShieldHealth = shipSOList[i].ShieldMaxHealth;
                shipCon.ShipData.HullHealth = shipSOList[i].HullMaxHealth;
                shipCon.ShipData.TorpedoDamage = shipSOList[i].TorpedoDamage;
                shipCon.ShipData.BeamDamage = shipSOList[i].BeamDamage;
                shipCon.ShipData.BuildDuration = shipSOList[i].BuildDuration;

                var targetGO = Instantiate(targetGOPrefab, shipCon.transform.position, Quaternion.identity);
                shipCon.ShipData.TargetOnThisShip = targetGO;
                targetGO.transform.SetParent(shipCon.transform, false);
                shipCon.ShipData.TargetOnThisShip.gameObject.transform.Translate(
                    shipCon.transform.position.x,
                    shipCon.transform.position.y,
                    shipCon.transform.position.z + 10);

                shipCon.ShipData.ShipDescription = shipSOList[i].ShipDescription;
                shipCon.gameObject.name = shipCon.ShipData.ShipName;
                shipCon.Order = CombatOrders.None;
                shipCon.gameObject.layer = 9;

                ShipControllerList.Add(shipCon);

                // Determine if parent is fleet or system
                if (parentGO.GetComponent<FleetController>() != null)
                {
                    var fleetCon = parentGO.GetComponent<FleetController>();
                    shipCon.ShipData.CurrentFleetController = fleetCon;

                    if (!fleetCon.FleetData.ShipsList.Contains(shipCon))
                        //// Replace all instances of ShipController with the fully qualified name
                        //List<BOTF3D.Combat.ShipController> shipConList = new List<BOTF3D.Combat.ShipController>();
                        //using CombatShipController = BOTF3D.Combat.ShipController;))
                        fleetCon.FleetData.ShipsList.Add(shipCon);

                    shipCon.ShipData.CurrentStarSysController = null;
                    Debug.Log($"  Ship '{shipCon.ShipData.ShipName}' added to fleet '{fleetCon.name}'");
                }
                else if (parentGO.GetComponent<StarSysController>() != null)
                {
                    var sysCon = parentGO.GetComponent<StarSysController>();
                    shipCon.ShipData.CurrentStarSysController = sysCon;

                    if (!sysCon.StarSysData.ShipsList.Contains(shipCon))
                        sysCon.StarSysData.ShipsList.Add(shipCon);

                    shipCon.ShipData.CurrentFleetController = null;
                    Debug.Log($"  Ship '{shipCon.ShipData.ShipName}' added to system '{sysCon.name}'");
                }
                else
                {
                    Debug.LogWarning($"  ⚠️ Parent '{parentGO.name}' is neither fleet nor system!");
                }

                // Create UI
                InstantiateShipListUIGameObject(shipCon, parentGO);

                // Parent gameplay ship
                shipCon.transform.SetParent(parentGO.transform, false);
                shipConList.Add(shipCon);
            }

            Debug.Log($"  Created {shipConList.Count} ships under '{parentGO.name}'");
            return shipConList;
        }

        public int GetShipBuildDuration(ShipType shipType, TechLevel techLevel, CivEnum civEnum)
        {
            ShipSO aShipSO = new ShipSO();
            int duration = 1;
            aShipSO = GetShipSO(shipType, techLevel, civEnum);
            duration = aShipSO.BuildDuration;
            return duration;
        }
        /// <summary>
        /// Gets a specific ship by type, tech level, and civ
        /// Uses civ-based lists for consistent lookup
        /// </summary>
        public ShipSO GetShipSO(ShipType shipType, TechLevel techLevel, CivEnum civEnum)
        {
            // ✅ Get civ's ship list
            List<ShipSO> civShips = GetShipSOListByCiv(civEnum);

            if (civShips == null || civShips.Count == 0)
            {
                Debug.LogWarning($"GetShipSO: No ships found for {civEnum}");
                return null;
            }

            // ✅ Find ship matching type AND tech level
            ShipSO foundShip = civShips.FirstOrDefault(s =>
                s.ShipType == shipType &&
                s.TechLevel == techLevel);

            if (foundShip == null)
            {
                Debug.LogWarning($"GetShipSO: No {shipType} found for {civEnum} at {techLevel} - searching fallback...");

                // ✅ Fallback: Try to find Scout at EARLY tech
                foundShip = civShips.FirstOrDefault(s =>
                    s.ShipType == ShipType.Scout &&
                    s.TechLevel == TechLevel.EARLY);

                if (foundShip != null)
                {
                    Debug.Log($"  ✅ Using fallback: {foundShip.ShipName}");
                }
            }

            return foundShip;
        }
        public void BuildShipInSystem(ShipType shipType, StarSysController systemCon)
        {
            TechLevel civTechLevel = systemCon.StarSysData.CurrentCivController.CivData.CurrentTechLevel;
            CivEnum civEnum = systemCon.StarSysData.CurrentOwnerCivEnum;

            // ✅ Use new method that finds best available tech version
            ShipSO ourShipSO = GetShipSOAtBestTechLevel(shipType, civTechLevel, civEnum);

            if (ourShipSO == null)
            {
                Debug.LogError($"BuildShipInSystem: Cannot build {shipType} for {civEnum} at tech level {civTechLevel} - ship not available!");
                return; // ✅ Don't create null ship
            }

            Debug.Log($"✅ Building {ourShipSO.ShipName} ({shipType} at {ourShipSO.TechLevel}) for system {systemCon.name}");

            List<ShipSO> shipSOAsList = new List<ShipSO> { ourShipSO };
            List<ShipController> shipConListOfOne = InstantiateShipControllersWithDataFromSO(shipSOAsList, systemCon.gameObject);

            foreach (ShipController shipCon in shipConListOfOne)
            {
                if (shipCon != null)
                {
                    shipCon.transform.SetParent(systemCon.transform);
                    shipCon.ShipData.CurrentStarSysController = systemCon;
                    shipCon.ShipData.CurrentFleetController = null;

                    Debug.Log($"  ✅ Ship '{shipCon.ShipData.ShipName}' added to system '{systemCon.name}'");
                    Debug.Log($"       System now has {systemCon.StarSysData.ShipsList.Count} ships");
                }
            }
        }

        public void InstantiateShipListUIGameObject(ShipController shipCon, GameObject parentGO)
        {
            Debug.Log($"=== InstantiateShipListUIGameObject called for ship '{shipCon?.ShipData?.ShipName}' ===");
            Debug.Log($"  Ship CivEnum: {shipCon?.ShipData?.CivEnum}");
            Debug.Log($"  LocalPlayerCivEnum: {GameController.Instance?.GameData?.LocalPlayerCivEnum}");
            Debug.Log($"  Match: {shipCon?.ShipData?.CivEnum == GameController.Instance?.GameData?.LocalPlayerCivEnum}");

            if (shipCon.ShipData.CivEnum != GameController.Instance.GameData.LocalPlayerCivEnum)
            {
                Debug.Log($"  Ship '{shipCon.ShipData.ShipName}' is NOT local player - no UI created");
                return;
            }

            if (shipCon.ShipListUIGameObject != null)
            {
                Debug.Log($"  Ship '{shipCon.ShipData.ShipName}' already has UI - skipping");
                return;
            }

            Debug.Log($"  Creating UI for ship '{shipCon.ShipData.ShipName}'");

            GameObject thisShipListUIGameObject = (GameObject)Instantiate(shipListUIPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            thisShipListUIGameObject.transform.localRotation = Quaternion.Euler(90f, 0f, 180f);
            thisShipListUIGameObject.SetActive(true);
            thisShipListUIGameObject.name = "ShipListUI_" + shipCon.ShipData.ShipName + "_" + shipIndex;
            shipIndex++;

            UnityEngine.UI.Image[] imageComponents = thisShipListUIGameObject.GetComponentsInChildren<UnityEngine.UI.Image>();
            if (imageComponents.Length > 1 && imageComponents[1] != null)
            {
                imageComponents[1].sprite = shipCon.ShipData.ShipSprite;
            }

            TextMeshProUGUI textComponent = thisShipListUIGameObject.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = shipCon.ShipData.ShipType.ToString();
            }

            CanvasGroup canvasGroup = thisShipListUIGameObject.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.gameObject.SetActive(true);
            }

            thisShipListUIGameObject.layer = 5;
            shipCon.ShipListUIGameObject = thisShipListUIGameObject;

            var shipUiItem = thisShipListUIGameObject.GetComponent<ShipListUI_Item>();
            if (shipUiItem != null)
            {
                shipUiItem.ShipController = shipCon;
            }

            // Try to parent to owner's ShipListUIParent
            bool parented = false;

            if (parentGO.TryGetComponent(out StarSysController sysCon))
            {
                Debug.Log($"  Parent is StarSys: {sysCon.name}");
                shipUiItem.CurrentStarSyst = sysCon;

                if (sysCon.StarSysData.ShipListUIParent != null)
                {
                    shipCon.ShipListUIGameObject.transform.SetParent(sysCon.StarSysData.ShipListUIParent.transform, false);
                    parented = true;
                    Debug.Log($"  ✅ Parented to system ShipListUIParent");
                }
                else
                {
                    Debug.LogWarning($"  ⚠️ System '{sysCon.name}' ShipListUIParent is NULL - adding to pending queue");

                    // Fallback to scene canvas
                    var canvas = FindFirstObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        shipCon.ShipListUIGameObject.transform.SetParent(canvas.transform, false);
                        Debug.Log($"  Temporarily parented to canvas: {canvas.name}");
                    }

                    shipConPendingShipUI.Add(shipCon);
                }
            }
            else if (parentGO.TryGetComponent(out FleetController fleetCon))
            {
                Debug.Log($"  Parent is Fleet: {fleetCon.name}");
                shipUiItem.CurrentFleet = fleetCon;

                if (fleetCon.FleetData.ShipListUIParent != null)
                {
                    shipCon.ShipListUIGameObject.transform.SetParent(fleetCon.FleetData.ShipListUIParent.transform, false);
                    parented = true;
                    Debug.Log($"  ✅ Parented to fleet ShipListUIParent");
                }
                else
                {
                    Debug.LogWarning($"  ⚠️ Fleet '{fleetCon.gameObject.name}' ShipListUIParent is NULL - adding to pending queue");

                    // Fallback to scene canvas
                    var canvas = FindFirstObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        shipCon.ShipListUIGameObject.transform.SetParent(canvas.transform, false);
                        Debug.Log($"  Temporarily parented to canvas: {canvas.name}");
                    }

                    shipConPendingShipUI.Add(shipCon);
                }
            }
            else
            {
                Debug.LogError($"  ❌ Parent '{parentGO.name}' is neither StarSys nor Fleet!");
            }

            Debug.Log($"=== InstantiateShipListUIGameObject complete - Parented: {parented}, Pending count: {shipConPendingShipUI.Count} ===");
        }

        /// <summary>
        /// Process any ships waiting for their UI parent to be assigned (for a specific system).
        /// Called after a system's ShipListUIParent is set.
        /// </summary>
        public void ProcessPendingShipUIs(StarSysController sysCon)
        {
            if (shipConPendingShipUI == null || shipConPendingShipUI.Count == 0)
                return;

            Debug.Log($"ProcessPendingShipUIs: Processing {shipConPendingShipUI.Count} pending ship UIs for system '{sysCon.name}'");

            for (int i = shipConPendingShipUI.Count - 1; i >= 0; i--)
            {
                var shipCon = shipConPendingShipUI[i];

                if (shipCon == null || shipCon.ShipData.CurrentStarSysController != sysCon)
                    continue;

                if (shipCon.ShipListUIGameObject != null && sysCon.StarSysData.ShipListUIParent != null)
                {
                    shipCon.ShipListUIGameObject.transform.SetParent(
                        sysCon.StarSysData.ShipListUIParent.transform, false);

                    shipCon.ShipListUIGameObject.SetActive(true);

                    Debug.Log($"  ✅ Rescued pending ship UI '{shipCon.ShipData.ShipName}' to system ShipListUIParent");

                    shipConPendingShipUI.RemoveAt(i);
                }
            }

            Debug.Log($"ProcessPendingShipUIs: {shipConPendingShipUI.Count} ship UIs still pending");
        }

        /// <summary>
        /// Process ALL pending ship UIs (tries to parent them to their owner's ShipListUIParent).
        /// Called generically when contexts change.
        /// </summary>
        public void ProcessPendingShipUIs()
        {
            if (shipConPendingShipUI == null || shipConPendingShipUI.Count == 0)
                return;

            Debug.Log($"ProcessPendingShipUIs (ALL): Processing {shipConPendingShipUI.Count} pending ship UIs");

            for (int i = shipConPendingShipUI.Count - 1; i >= 0; i--)
            {
                var shipCon = shipConPendingShipUI[i];

                if (shipCon == null)
                {
                    shipConPendingShipUI.RemoveAt(i);
                    continue;
                }

                bool rescued = false;

                // Try system owner
                if (shipCon.ShipData.CurrentStarSysController != null)
                {
                    var sysCon = shipCon.ShipData.CurrentStarSysController;
                    if (shipCon.ShipListUIGameObject != null && sysCon.StarSysData.ShipListUIParent != null)
                    {
                        shipCon.ShipListUIGameObject.transform.SetParent(
                            sysCon.StarSysData.ShipListUIParent.transform, false);

                        shipCon.ShipListUIGameObject.SetActive(true);

                        Debug.Log($"  ✅ Rescued ship UI '{shipCon.ShipData.ShipName}' to system '{sysCon.name}'");

                        shipConPendingShipUI.RemoveAt(i);
                        rescued = true;
                    }
                }
                // Try fleet owner
                else if (shipCon.ShipData.CurrentFleetController != null)
                {
                    var fleetCon = shipCon.ShipData.CurrentFleetController;
                    if (shipCon.ShipListUIGameObject != null && fleetCon.FleetData.ShipListUIParent != null)
                    {
                        shipCon.ShipListUIGameObject.transform.SetParent(
                            fleetCon.FleetData.ShipListUIParent.transform, false);

                        shipCon.ShipListUIGameObject.SetActive(true);

                        Debug.Log($"  ✅ Rescued ship UI '{shipCon.ShipData.ShipName}' to fleet '{fleetCon.name}'");

                        shipConPendingShipUI.RemoveAt(i);
                        rescued = true;
                    }
                }

                if (!rescued)
                {
                    Debug.LogWarning($"  ⚠️ Could not rescue ship UI '{shipCon.ShipData.ShipName}' - still no parent available");
                }
            }

            Debug.Log($"ProcessPendingShipUIs (ALL): {shipConPendingShipUI.Count} ship UIs still pending");
        }

        public void BuildShipsOfFirstFleet(FleetController fleetCon)
        {
            // var shipCon = shipCon.GetComponent<FleetController>();
            CivEnum civEnum = fleetCon.FleetData.CivEnum;
            List<ShipSO> ships = new List<ShipSO>();
            ships = FirstShipDataByTechLevel(CivManager.Instance.GetCivDataByCivEnum(civEnum).CurrentTechLevel, civEnum);
            List<ShipController> shipCons = new List<ShipController>();
            if (ships != null)
            {
                shipCons = InstantiateShipControllersWithDataFromSO(ships, fleetCon.gameObject);
                foreach (ShipController shipCon in shipCons)
                {
                    if (shipCon != null)
                    {
                        shipCon.transform.SetParent(fleetCon.transform);
                        shipCon.ShipData.CurrentFleetController = fleetCon;
                        // already added to ShipsList in InstantiateShipControllersWithDataFromSO and to ShipControllerList
                    }
                }
            }
            fleetCon.UpdateMaxWarp();
        }
        public List<ShipSO> FirstShipDataByTechLevel(TechLevel techLevel, CivEnum civ)
        {
            List<ShipSO> allCivShips = GetShipSOListByCiv(civ);

            if (allCivShips == null || allCivShips.Count == 0)
            {
                Debug.LogWarning($"FirstShipDataByTechLevel: No ships found for {civ}");
                return new List<ShipSO>();
            }

            // ✅ Filter by tech level first (remove nulls too)
            var techLevelShips = allCivShips
                .Where(s => s != null && s.TechLevel == techLevel)
                .ToList();

            if (techLevelShips.Count == 0)
            {
                Debug.LogWarning($"FirstShipDataByTechLevel: No ships found for {civ} at {techLevel}");
                return new List<ShipSO>();
            }

            // ✅ Check if this is a MAJOR race (FED through TERRAN get 3 ships)
            bool isMajorRace = civ >= CivEnum.FED && civ <= CivEnum.TERRAN;

            if (isMajorRace)
            {
                // ✅ MAJOR RACES get THREE ships: Destroyer, Scout, Transport
                List<ShipSO> startingFleetShipList = new List<ShipSO>();

                ShipSO destroyer = techLevelShips.FirstOrDefault(s => s.ShipType == ShipType.Destroyer);
                ShipSO scout = techLevelShips.FirstOrDefault(s => s.ShipType == ShipType.Scout);
                ShipSO transport = techLevelShips.FirstOrDefault(s => s.ShipType == ShipType.Transport);

                if (destroyer != null)
                {
                    startingFleetShipList.Add(destroyer);
                    Debug.Log($"  ✅ Added destroyer '{destroyer.ShipName}' to {civ} starting fleet");
                }
                else
                {
                    Debug.LogWarning($"  ⚠️ No destroyer found for {civ} at {techLevel}");
                }

                if (scout != null)
                {
                    startingFleetShipList.Add(scout);
                    Debug.Log($"  ✅ Added scout '{scout.ShipName}' to {civ} starting fleet");
                }
                else
                {
                    Debug.LogWarning($"  ⚠️ No scout found for {civ} at {techLevel}");
                }

                if (transport != null)
                {
                    startingFleetShipList.Add(transport);
                    Debug.Log($"  ✅ Added transport '{transport.ShipName}' to {civ} starting fleet");
                }
                else
                {
                    Debug.LogWarning($"  ⚠️ No transport found for {civ} at {techLevel}");
                }

                if (startingFleetShipList.Count == 0)
                {
                    Debug.LogError($"❌ FirstShipDataByTechLevel: Could not find ANY starting ships for major race {civ}!");
                }
                else
                {
                    Debug.Log($"✅ FirstShipDataByTechLevel: {civ} starting fleet has {startingFleetShipList.Count} ships");
                }

                return startingFleetShipList;
            }
            else
            {
                // ✅ MINOR RACES get ONE ship: Destroyer (or Scout as fallback)
                ShipSO destroyer = techLevelShips.FirstOrDefault(s => s.ShipType == ShipType.Destroyer);

                if (destroyer != null)
                {
                    Debug.Log($"✅ FirstShipDataByTechLevel: Minor race {civ} gets destroyer '{destroyer.ShipName}'");
                    return new List<ShipSO> { destroyer };
                }

                // Fallback to scout if no destroyer
                ShipSO scout = techLevelShips.FirstOrDefault(s => s.ShipType == ShipType.Scout);
                if (scout != null)
                {
                    Debug.Log($"✅ FirstShipDataByTechLevel: Minor race {civ} gets scout '{scout.ShipName}' (no destroyer found)");
                    return new List<ShipSO> { scout };
                }

                Debug.LogError($"❌ FirstShipDataByTechLevel: Minor race {civ} has no destroyer or scout at {techLevel}!");
                return new List<ShipSO>();
            }
        }

        internal void RemoveShipControllerFromList(ShipController shipCon)
        {
            int foundOne = -1;
            for (int i = 0; i < ShipControllerList.Count; i++)
            {
                if (shipCon == ShipControllerList[i])
                {
                    foundOne = i;
                }
            }
            if (foundOne > -1)
            {
                var toDestroy = ShipControllerList[foundOne];
                ShipControllerList.RemoveAt(foundOne);
                if (toDestroy.ShipListUIGameObject != null) Destroy(toDestroy.ShipListUIGameObject);
                if (toDestroy.gameObject != null) Destroy(toDestroy.gameObject);
            }
        }

        /// <summary>
        /// Creates ships in GALAXY context (parented to fleet/system)
        /// </summary>
        public List<ShipController> CreateGalaxyShips(List<ShipSO> shipSOList, GameObject parentGO)
        {
            List<ShipController> shipConList = new List<ShipController>();

            if (parentGO == null)
            {
                Debug.LogError("CreateGalaxyShips: parentGO is NULL! Cannot create ships.");
                return shipConList;
            }

            Debug.Log($"CreateGalaxyShips: Creating {shipSOList.Count} ships for '{parentGO.name}'");

            for (int i = 0; i < shipSOList.Count; i++)
            {
                if (shipSOList[i] == null) continue;

                // ✅ Parent to fleet/system in galaxy
                ShipController shipCon = Instantiate(
                    shipConPrefab,
                    parentGO.transform.position, // Use parent's position
                    Quaternion.identity,
                    parentGO.transform); // Parent to fleet or system

                InitializeShipData(shipCon, shipSOList[i]);
                shipConList.Add(shipCon);
            }

            Debug.Log($"  Created {shipConList.Count} galaxy ships");
            return shipConList;
        }

        /// <summary>
        /// Creates ships in COMBAT context (parented to combat canvas)
        /// </summary>
        public List<ShipController> CreateCombatShips(List<ShipSO> shipSOList)
        {
            List<ShipController> shipConList = new List<ShipController>();

            if (CombatManager.Instance == null || CombatManager.Instance.CombatUICanvas == null)
            {
                Debug.LogError("CreateCombatShips: CombatManager or CombatUICanvas is NULL!");
                return shipConList;
            }

            Debug.Log($"CreateCombatShips: Creating {shipSOList.Count} ships for combat");

            for (int i = 0; i < shipSOList.Count; i++)
            {
                if (shipSOList[i] == null) continue;

                // ✅ Parent to combat UI canvas
                ShipController shipCon = Instantiate(
                    shipConPrefab,
                    Vector3.zero,
                    Quaternion.identity,
                    CombatManager.Instance.CombatUICanvas.transform);

                InitializeShipData(shipCon, shipSOList[i]);
                shipConList.Add(shipCon);
            }

            Debug.Log($"  Created {shipConList.Count} combat ships");
            return shipConList;
        }

        /// <summary>
        /// Shared initialization logic for ship data
        /// </summary>
        private void InitializeShipData(ShipController shipCon, ShipSO shipSO)
        {
            shipCon.Init(this);
            shipCon.ShipData = new ShipData();

            // ✅ CRITICAL FIX: Store reference to the ShipSO
            shipCon.ShipData.ShipSO = shipSO;

            shipCon.ShipData.ShipName = shipSO.ShipName;
            shipCon.ShipData.CivEnum = shipSO.CivEnum;
            shipCon.ShipData.TechLevel = shipSO.TechLevel;
            shipCon.ShipData.ShipType = shipSO.ShipType;

            if (shipSO.shipSprite != null)
                shipCon.ShipData.ShipSprite = shipSO.shipSprite;

            shipCon.ShipData.maxWarpFactor = shipSO.maxWarpFactor;
            shipCon.ShipData.currentWarpFactor = 0f;
            shipCon.ShipData.ShieldHealth = shipSO.ShieldMaxHealth;
            shipCon.ShipData.HullHealth = shipSO.HullMaxHealth;
            shipCon.ShipData.TorpedoDamage = shipSO.TorpedoDamage;
            shipCon.ShipData.BeamDamage = shipSO.BeamDamage;
            shipCon.ShipData.BuildDuration = shipSO.BuildDuration;

            // Add other initialization...
        }

        /// <summary>
        /// Gets ships for a specific civ and tech level
        /// </summary>
        public List<ShipSO> GetShipSOsForCivAndTech(CivEnum civ, TechLevel techLevel)
        {
            List<ShipSO> allCivShips = GetShipSOListByCiv(civ);

            if (allCivShips == null || allCivShips.Count == 0)
            {
                Debug.LogWarning($"GetShipSOsForCivAndTech: No ships found for {civ}");
                return new List<ShipSO>();
            }

            // ✅ CRITICAL: Remove null entries before filtering
            allCivShips = allCivShips.Where(s => s != null).ToList();

            if (allCivShips.Count == 0)
            {
                Debug.LogError($"GetShipSOsForCivAndTech: All ships in {civ} list are NULL!");
                return new List<ShipSO>();
            }

            // ✅ Now safe to filter by tech level
            var filtered = allCivShips.Where(s => s.TechLevel == techLevel).ToList();

            Debug.Log($"GetShipSOsForCivAndTech: Found {filtered.Count}/{allCivShips.Count} ships for {civ} at {techLevel}");
            //return filtered;
            return new List<ShipSO> { Test };
        }

        /// <summary>
        /// Gets a specific ship for civ, tech level, and type
        /// </summary>
        public ShipSO GetShipSO(CivEnum civ, TechLevel techLevel, ShipType shipType)
        {
            List<ShipSO> civShips = GetShipSOListByCiv(civ);

            return civShips.FirstOrDefault(s =>
                s.TechLevel == techLevel &&
                s.ShipType == shipType);
        }

        internal ShipSO GetFallbackShipSO()
        {
            // Get a default model;
            return GetShipSO(ShipType.Destroyer, TechLevel.EARLY, CivEnum.FED);
        }
#if UNITY_EDITOR
        [ContextMenu("Auto-Populate Ship Lists from Assets")]
        private void AutoPopulateShipLists()
        {
            Debug.Log("=== Auto-Populating Ship Lists ===");

            // Clear existing lists
            FedShipSOList.Clear();
            RomShipSOList.Clear();
            KlingShipSOList.Clear();
            CardShipSOList.Clear();
            DomShipSOList.Clear();
            BorgShipSOList.Clear();
            TerranShipSOList.Clear();
            MinorShipSOList.Clear();

            // ✅ Load ALL ShipSO assets from entire project
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ShipSO");

            Debug.Log($"  Found {guids.Length} ShipSO assets to process");

            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                ShipSO shipSO = UnityEditor.AssetDatabase.LoadAssetAtPath<ShipSO>(path);

                // ✅ CRITICAL: Skip if asset failed to load
                if (shipSO == null)
                {
                    Debug.LogWarning($"  ⚠️ Failed to load ShipSO at path: {path}");
                    continue;
                }

                // Add to appropriate civ list
                switch (shipSO.CivEnum)
                {
                    case CivEnum.FED:
                        if (!FedShipSOList.Contains(shipSO))
                        {
                            FedShipSOList.Add(shipSO);
                            Debug.Log($"  ✅ Added {shipSO.ShipName} to FedShipSOList");
                        }
                        break;
                    case CivEnum.ROM:
                        if (!RomShipSOList.Contains(shipSO))
                        {
                            RomShipSOList.Add(shipSO);
                            Debug.Log($"  ✅ Added {shipSO.ShipName} to RomShipSOList");
                        }
                        break;
                    case CivEnum.KLING:
                        if (!KlingShipSOList.Contains(shipSO))
                        {
                            KlingShipSOList.Add(shipSO);
                            Debug.Log($"  ✅ Added {shipSO.ShipName} to KlingShipSOList");
                        }
                        break;
                    case CivEnum.CARD:
                        if (!CardShipSOList.Contains(shipSO))
                        {
                            CardShipSOList.Add(shipSO);
                            Debug.Log($"  ✅ Added {shipSO.ShipName} to CardShipSOList");
                        }
                        break;
                    case CivEnum.DOM:
                        if (!DomShipSOList.Contains(shipSO))
                        {
                            DomShipSOList.Add(shipSO);
                            Debug.Log($"  ✅ Added {shipSO.ShipName} to DomShipSOList");
                        }
                        break;
                    case CivEnum.BORG:
                        if (!BorgShipSOList.Contains(shipSO))
                        {
                            BorgShipSOList.Add(shipSO);
                            Debug.Log($"  ✅ Added {shipSO.ShipName} to BorgShipSOList");
                        }
                        break;
                    case CivEnum.TERRAN:
                        if (!TerranShipSOList.Contains(shipSO))
                        {
                            TerranShipSOList.Add(shipSO);
                            Debug.Log($"  ✅ Added {shipSO.ShipName} to TerranShipSOList");
                        }
                        break;
                    default:
                        if (!MinorShipSOList.Contains(shipSO))
                        {
                            MinorShipSOList.Add(shipSO);
                            Debug.Log($"  ✅ Added {shipSO.ShipName} to MinorShipSOList");
                        }
                        Debug.LogWarning($"  ⚠️ Unknown CivEnum '{shipSO.CivEnum}' for {shipSO.ShipName}"); break;
                }
            }

            // ✅ Sort lists by tech level, then ship type
            FedShipSOList = FedShipSOList.OrderBy(s => s.TechLevel).ThenBy(s => s.ShipType).ToList();
            RomShipSOList = RomShipSOList.OrderBy(s => s.TechLevel).ThenBy(s => s.ShipType).ToList();
            KlingShipSOList = KlingShipSOList.OrderBy(s => s.TechLevel).ThenBy(s => s.ShipType).ToList();
            CardShipSOList = CardShipSOList.OrderBy(s => s.TechLevel).ThenBy(s => s.ShipType).ToList();
            DomShipSOList = DomShipSOList.OrderBy(s => s.TechLevel).ThenBy(s => s.ShipType).ToList();
            BorgShipSOList = BorgShipSOList.OrderBy(s => s.TechLevel).ThenBy(s => s.ShipType).ToList();
            TerranShipSOList = TerranShipSOList.OrderBy(s => s.TechLevel).ThenBy(s => s.ShipType).ToList();
            MinorShipSOList = MinorShipSOList.OrderBy(s => s.TechLevel).ThenBy(s => s.ShipType).ToList();

            Debug.Log("=== Auto-Populate Complete ===");
            Debug.Log($"  Fed: {FedShipSOList.Count} ships");
            Debug.Log($"  Rom: {RomShipSOList.Count} ships");
            Debug.Log($"  Kling: {KlingShipSOList.Count} ships");
            Debug.Log($"  Card: {CardShipSOList.Count} ships");
            Debug.Log($"  Dom: {DomShipSOList.Count} ships");
            Debug.Log($"  Borg: {BorgShipSOList.Count} ships");
            Debug.Log($"  Terran: {TerranShipSOList.Count} ships");
            Debug.Log($"  Minor: {MinorShipSOList.Count} ships");

            // ✅ Mark as dirty so Unity saves the changes
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }

        [ContextMenu("Debug: Check for Null Entries in Ship Lists")]
        private void DebugCheckNullEntries()
        {
            Debug.Log("=== Checking Ship Lists for Null Entries ===");

            CheckListForNulls("FedShipSOList", FedShipSOList);
            CheckListForNulls("RomShipSOList", RomShipSOList);
            CheckListForNulls("KlingShipSOList", KlingShipSOList);
            CheckListForNulls("CardShipSOList", CardShipSOList);
            CheckListForNulls("DomShipSOList", DomShipSOList);
            CheckListForNulls("BorgShipSOList", BorgShipSOList);
            CheckListForNulls("TerranShipSOList", TerranShipSOList);
            CheckListForNulls("MinorShipSOList", MinorShipSOList);

            Debug.Log("=== Check Complete ===");
        }

        private void CheckListForNulls(string listName, List<ShipSO> list)
        {
            if (list == null)
            {
                Debug.LogError($"  ❌ {listName} is NULL (not initialized)!");
                return;
            }

            int nullCount = 0;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                {
                    Debug.LogError($"  ❌ {listName}[{i}] is NULL!");
                    nullCount++;
                }
            }

            if (nullCount > 0)
            {
                Debug.LogWarning($"  {listName}: {nullCount}/{list.Count} entries are null!");
            }
            else if (list.Count == 0)
            {
                Debug.LogWarning($"  ⚠️ {listName}: List is EMPTY!");
            }
            else
            {
                Debug.Log($"  ✅ {listName}: All {list.Count} entries are valid");
            }
        }
#endif

        /// <summary>
        /// Get all ships available to a civilization at their current tech level (and below)
        /// NOW USES GRANULAR TECH POINTS for unlocking
        /// </summary>
        public List<ShipSO> GetAvailableShipsForCiv(CivEnum civEnum, TechLevel currentTechLevel)
        {
            List<ShipSO> allCivShips = GetShipSOListByCiv(civEnum);

            if (allCivShips == null || allCivShips.Count == 0)
            {
                Debug.LogWarning($"GetAvailableShipsForCiv: No ships found for {civEnum}");
                return new List<ShipSO>();
            }

            // ✅ Remove null entries
            allCivShips = allCivShips.Where(s => s != null).ToList();

            // ✅ Filter: Only include ships at or below current tech level
            List<ShipSO> availableShips = allCivShips
                .Where(s => s.TechLevel <= currentTechLevel)
                .ToList();

            Debug.Log($"GetAvailableShipsForCiv: {civEnum} at {currentTechLevel} has {availableShips.Count}/{allCivShips.Count} ships available");

            foreach (var ship in availableShips)
            {
                Debug.Log($"  ✅ {ship.ShipName} ({ship.ShipType} at {ship.TechLevel})");
            }

            return availableShips;
        }

        /// <summary>
        /// Checks if a specific ship type is available for a civilization at their tech level
        /// </summary>
        public bool IsShipTypeAvailable(ShipType shipType, CivEnum civEnum, TechLevel currentTechLevel)
        {
            ShipSO ship = GetShipSO(shipType, currentTechLevel, civEnum);

            if (ship == null)
            {
                // Try to find ANY version of this ship type for this civ
                List<ShipSO> civShips = GetShipSOListByCiv(civEnum);
                ship = civShips?.FirstOrDefault(s => s != null && s.ShipType == shipType);

                if (ship == null)
                {
                    Debug.Log($"IsShipTypeAvailable: {civEnum} has no {shipType} at any tech level");
                    return false; // This civ doesn't have this ship type at all
                }
            }

            // ✅ Ship is available if its tech level is at or below the civ's current level
            bool available = ship.TechLevel <= currentTechLevel;

            Debug.Log($"IsShipTypeAvailable: {shipType} for {civEnum} - Required: {ship.TechLevel}, Current: {currentTechLevel}, Available: {available}");
            return available;
        }

        /// <summary>
        /// Gets a specific ship by type at the BEST available tech level for the civ
        /// Searches from current tech level DOWN to find the best match
        /// </summary>
        public ShipSO GetShipSOAtBestTechLevel(ShipType shipType, TechLevel maxTechLevel, CivEnum civEnum)
        {
            // ✅ Get civ's ship list
            List<ShipSO> civShips = GetShipSOListByCiv(civEnum);

            if (civShips == null || civShips.Count == 0)
            {
                Debug.LogWarning($"GetShipSOAtBestTechLevel: No ships found for {civEnum}");
                return null;
            }

            // ✅ Find ALL ships of this type at or below max tech level
            var candidateShips = civShips
                .Where(s => s != null && s.ShipType == shipType && s.TechLevel <= maxTechLevel)
                .OrderByDescending(s => s.TechLevel) // Highest tech first
                .ToList();

            if (candidateShips.Count > 0)
            {
                ShipSO bestShip = candidateShips[0];
                Debug.Log($"GetShipSOAtBestTechLevel: Found {shipType} for {civEnum} at {bestShip.TechLevel} (max allowed: {maxTechLevel})");
                return bestShip;
            }

            Debug.LogWarning($"GetShipSOAtBestTechLevel: No {shipType} found for {civEnum} at or below {maxTechLevel}");
            return null;
        }
    }
}
