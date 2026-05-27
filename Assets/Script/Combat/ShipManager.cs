// Ignore Spelling: BOTF Kling Unregister sys

using BOTF3D.Combat;
using BOTF3D.GamePlay;
using BOTF3D.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BOTF3D.Core
{
    /// <summary>
    /// Refactored ShipManager - orchestrates specialized managers for ship operations.
    /// Delegates to: ShipRegistry, ShipFactory, ShipDataInitializer, ShipUICreator, ShipSOProvider
    /// </summary>
    public class ShipManager : MonoBehaviour
    {
        public static ShipManager Instance;

        [Header("Ship Prefabs")]
        [SerializeField] private ShipController shipConPrefab;
        [SerializeField] private ShipController galaxyShipPrefab;

        [Header("Ship UI")]
        [SerializeField] private GameObject shipListUIPrefab;

        [Header("Weapon Prefabs")]
        public GameObject targetGOPrefab;
        public GameObject[] torpedoPrefabs;
        public GameObject[] beamWeaponPrefabs;

        [Header("Weapon Audio Clips")]
        public AudioClip[] beamFireClips;
        public AudioClip[] torpedoFireClips;

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

        // Specialized managers (handle all ship operations)
        private ShipRegistry shipRegistry;
        private ShipFactory shipFactory;
        private ShipDataInitializer shipDataInitializer;
        private ShipUICreator shipUICreator;
        private ShipSOProvider shipSOProvider;

        // Legacy tracking (maintained for backward compatibility)
        public List<ShipController> ShipControllerList = new List<ShipController>();

        // Expose registry data for backward compatibility
        public List<ShipData> AllShipsData => shipRegistry?.AllShipsData;
        public List<ShipController> AllShipControllers => shipRegistry?.AllShipControllers;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeManagers();

#if UNITY_EDITOR
            // Auto-populate civ lists if empty (Editor only)
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
            // In builds, verify lists are populated
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

        /// <summary>
        /// Initialize all specialized managers
        /// </summary>
        private void InitializeManagers()
        {
            Debug.Log("ShipManager: Initializing specialized managers...");

            // Create ShipSOProvider with all civ ship lists
            shipSOProvider = new ShipSOProvider(
                FedShipSOList,
                RomShipSOList,
                KlingShipSOList,
                CardShipSOList,
                DomShipSOList,
                BorgShipSOList,
                TerranShipSOList,
                MinorShipSOList
            );

            // Create other managers
            shipRegistry = new ShipRegistry();
            shipFactory = new ShipFactory(shipConPrefab, targetGOPrefab);
            shipDataInitializer = new ShipDataInitializer();
            shipUICreator = new ShipUICreator(shipListUIPrefab);

            Debug.Log("✅ ShipManager: All managers initialized");
        }
        #region Ship SO Queries (Delegate to ShipSOProvider)

        public ShipSO GetShipSO(CivEnum civEnum, ShipType shipType)
        {
            return shipSOProvider.GetShipSOListByCiv(civEnum)?.FirstOrDefault(s => s.ShipType == shipType);
        }

        public List<ShipSO> GetShipSOListByCiv(CivEnum civEnum)
        {
            return shipSOProvider.GetShipSOListByCiv(civEnum);
        }

        public ShipSO GetShipSO(ShipType shipType, TechLevel techLevel, CivEnum civEnum)
        {
            return shipSOProvider.GetShipSO(shipType, techLevel, civEnum);
        }

        public ShipSO GetShipSOAtBestTechLevel(ShipType shipType, TechLevel maxTechLevel, CivEnum civEnum)
        {
            return shipSOProvider.GetShipSOAtBestTechLevel(shipType, maxTechLevel, civEnum);
        }

        public List<ShipSO> GetAvailableShipsForCiv(CivEnum civEnum, TechLevel currentTechLevel)
        {
            return shipSOProvider.GetAvailableShipsForCiv(civEnum, currentTechLevel);
        }

        public bool IsShipTypeAvailable(ShipType shipType, CivEnum civEnum, TechLevel currentTechLevel)
        {
            return shipSOProvider.IsShipTypeAvailable(shipType, civEnum, currentTechLevel);
        }

        public ShipSO GetFallbackShipSO()
        {
            return shipSOProvider.GetFallbackShipSO();
        }

        public List<ShipSO> FirstShipDataByTechLevel(TechLevel techLevel, CivEnum civ)
        {
            return shipSOProvider.GetStartingFleetShips(techLevel, civ);
        }

        #endregion

        #region Ship Registry Operations (Delegate to ShipRegistry)

        public void RegisterGalaxyShip(ShipController shipController)
        {
            shipRegistry.RegisterGalaxyShip(shipController);
        }

        public void RegisterCombatShip(ShipController shipController)
        {
            shipRegistry.RegisterCombatShip(shipController);
        }

        public void UnregisterGalaxyShip(int shipId)
        {
            shipRegistry.UnregisterGalaxyShip(shipId);
        }

        public void UnregisterCombatShip(int shipId)
        {
            shipRegistry.UnregisterCombatShip(shipId);
        }

        public ShipController GetGalaxyShipController(int shipId)
        {
            return shipRegistry.GetGalaxyShipController(shipId);
        }

        public ShipController GetCombatShipController(int shipId)
        {
            return shipRegistry.GetCombatShipController(shipId);
        }

        public ShipData GetShipData(int shipId)
        {
            return shipRegistry.GetShipData(shipId);
        }

        public void ClearGalaxyShips()
        {
            shipRegistry.ClearGalaxyShips();
        }

        public void ClearCombatShips()
        {
            shipRegistry.ClearCombatShips();
        }

        public void TransitionShipsToCombat(List<ShipController> galaxyShips)
        {
            shipRegistry.TransitionShipsToCombat(galaxyShips);
        }

        public void SyncCombatResultsToGalaxy()
        {
            shipRegistry.SyncCombatResultsToGalaxy();
        }

        #endregion

        #region Ship UI Operations (Delegate to ShipUICreator)

        public void InstantiateShipListUIGameObject(ShipController shipCon, GameObject parentGO)
        {
            shipUICreator.InstantiateShipListUI(shipCon, parentGO);
        }

        public void ProcessPendingShipUIs(StarSysController sysCon)
        {
            shipUICreator.ProcessPendingShipUIs(sysCon);
        }

        public void ProcessPendingShipUIs()
        {
            shipUICreator.ProcessAllPendingShipUIs();
        }

        #endregion
        #region Legacy Ship Operations (Refactored to use Managers)

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

        #endregion

        #region Ship Creation (Refactored to use Managers)

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

                // Create ship using ShipFactory
                ShipController shipCon = shipFactory.CreateGalaxyShip(shipSOList[i], Vector3.zero, parentGO);

                if (shipCon == null)
                    continue;

                // Initialize ship data using ShipDataInitializer
                shipCon.Init(this);
                shipDataInitializer.InitializeShipData(shipCon, shipSOList[i]);

                // Create target GameObject
                var targetGO = shipFactory.CreateTargetForShip(shipCon);
                shipCon.ShipData.TargetOnThisShip = targetGO;

                shipCon.Order = CombatOrders.None;
                shipCon.gameObject.layer = 9;

                // Legacy tracking
                ShipControllerList.Add(shipCon);

                // Link ship to parent using ShipFactory
                shipFactory.LinkShipToParent(shipCon, parentGO);

                // Register with ShipRegistry
                shipRegistry.RegisterGalaxyShip(shipCon);

                // Create UI using ShipUICreator
                shipUICreator.InstantiateShipListUI(shipCon, parentGO);

                shipConList.Add(shipCon);
            }

            Debug.Log($"  Created {shipConList.Count} ships under '{parentGO.name}'");
            return shipConList;
        }

        #endregion

        #region Ship Building Operations

        public int GetShipBuildDuration(ShipType shipType, TechLevel techLevel, CivEnum civEnum)
        {
            ShipSO shipSO = GetShipSO(shipType, techLevel, civEnum);
            return shipSO?.BuildDuration ?? 1;
        }

        #endregion
        public void BuildShipInSystem(ShipType shipType, StarSysController systemCon)
        {
            TechLevel civTechLevel = systemCon.StarSysData.CurrentCivController.CivData.CurrentTechLevel;
            CivEnum civEnum = systemCon.StarSysData.CurrentOwnerCivEnum;

            ShipSO ourShipSO = shipSOProvider.GetShipSOAtBestTechLevel(shipType, civTechLevel, civEnum);

            if (ourShipSO == null)
            {
                Debug.LogError($"BuildShipInSystem: Cannot build {shipType} for {civEnum} at tech level {civTechLevel} - ship not available!");
                return;
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


        public void BuildShipsOfFirstFleet(FleetController fleetCon)
        {
            CivEnum civEnum = fleetCon.FleetData.CivEnum;
            TechLevel techLevel = CivManager.Instance.GetCivDataByCivEnum(civEnum).CurrentTechLevel;

            List<ShipSO> ships = shipSOProvider.GetStartingFleetShips(techLevel, civEnum);
            List<ShipController> shipCons = new List<ShipController>();

            if (ships != null && ships.Count > 0)
            {
                shipCons = InstantiateShipControllersWithDataFromSO(ships, fleetCon.gameObject);
                foreach (ShipController shipCon in shipCons)
                {
                    if (shipCon != null)
                    {
                        shipCon.transform.SetParent(fleetCon.transform);
                        shipCon.ShipData.CurrentFleetController = fleetCon;
                    }
                }
            }

            fleetCon.UpdateMaxWarp();
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

                // Remove from registry
                shipRegistry.RemoveShipController(toDestroy);

                if (toDestroy.ShipListUIGameObject != null) Destroy(toDestroy.ShipListUIGameObject);
                if (toDestroy.gameObject != null) Destroy(toDestroy.gameObject);
            }
        }

        public List<ShipSO> GetShipSOsForCivAndTech(CivEnum civ, TechLevel techLevel)
        {
            List<ShipSO> allCivShips = shipSOProvider.GetShipSOListByCiv(civ);

            if (allCivShips == null || allCivShips.Count == 0)
            {
                Debug.LogWarning($"GetShipSOsForCivAndTech: No ships found for {civ}");
                return new List<ShipSO>();
            }

            allCivShips = allCivShips.Where(s => s != null).ToList();

            if (allCivShips.Count == 0)
            {
                Debug.LogError($"GetShipSOsForCivAndTech: All ships in {civ} list are NULL!");
                return new List<ShipSO>();
            }

            var filtered = allCivShips.Where(s => s.TechLevel == techLevel).ToList();

            Debug.Log($"GetShipSOsForCivAndTech: Found {filtered.Count}/{allCivShips.Count} ships for {civ} at {techLevel}");
            return new List<ShipSO> { Test };
        }
#if UNITY_EDITOR
        [ContextMenu("Auto-Populate Ship Lists from Assets")]
        private void AutoPopulateShipLists()
        {
            Debug.Log("=== Auto-Populating Ship Lists ===");

            FedShipSOList.Clear();
            RomShipSOList.Clear();
            KlingShipSOList.Clear();
            CardShipSOList.Clear();
            DomShipSOList.Clear();
            BorgShipSOList.Clear();
            TerranShipSOList.Clear();
            MinorShipSOList.Clear();

            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ShipSO");
            Debug.Log($"  Found {guids.Length} ShipSO assets to process");

            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                ShipSO shipSO = UnityEditor.AssetDatabase.LoadAssetAtPath<ShipSO>(path);

                if (shipSO == null)
                {
                    Debug.LogWarning($"  ⚠️ Failed to load ShipSO at path: {path}");
                    continue;
                }

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
                        Debug.LogWarning($"  ⚠️ Unknown CivEnum '{shipSO.CivEnum}' for {shipSO.ShipName}");
                        break;
                }
            }

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
    }
}
