
using Assets.GamePlay;
using Assets.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Assets.Core
{
    public class ShipManager : MonoBehaviour
    {
        public static ShipManager Instance;

        [SerializeField]
        private ShipController shipConPrefab;
        [Header("Ship UI")]
        [SerializeField]
        private GameObject shipListUIPrefab; // prefab for the ship list UI in the galaxy menu

        public List<ShipController> ShipControllerList = new List<ShipController>();

        [Header("Ship ScriptableObjects by Tech Level")]
        public List<ShipSO> ShipSOListTech0 = new List<ShipSO>();
        public List<ShipSO> ShipSOListTech1 = new List<ShipSO>();
        public List<ShipSO> ShipSOListTech2 = new List<ShipSO>();
        public List<ShipSO> ShipSOListTech3 = new List<ShipSO>();
        public ShipSORegistry ShipSORegistry;
        [Header("Weapon Prefabs")]
        public GameObject targetGOPrefab;
        public GameObject[] torpedoPrefabs;
        public GameObject[] beamWeaponPrefabs;
        int shipIndex = 0;
        // ToDo these later?
        // Pending UI items (from your existing code)
        private List<(ShipController shipController, GameObject uiParent)> pendingShipUIs = new List<(ShipController, GameObject)>();
        // ToDo these later?
        [Header("Ship ScriptableObjects - Ship Templates")]
        [SerializeField] public List<ShipSO> FedShipSOList;
        [SerializeField] public List<ShipSO> RomShipSOList;
        [SerializeField] public List<ShipSO> KlingShipSOList;
        [SerializeField] public List<ShipSO> CardShipSOList;
        [SerializeField] public List<ShipSO> DomShipSOList;
        [SerializeField] public List<ShipSO> BorgShipSOList;
        [SerializeField] public List<ShipSO> TerranShipSOList;
        // ToDo these later?
        [Header("Ship Prefabs")]
        [SerializeField] private ShipController galaxyShipPrefab; // Visual representation in galaxy
        [SerializeField] private ShipController combatShipPrefab; // Visual representation in combat (can be same or different)
                                                                  // RUNTIME DATA - ship instances and state
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
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
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
                //ToDo: set up these lists 
                //    case CivEnum.FED: return FedShipSOList;
                //case CivEnum.ROM: return RomShipSOList;
                //case CivEnum.KLING: return KlingShipSOList;
                //case CivEnum.CARD: return CardShipSOList;
                //case CivEnum.DOM: return DomShipSOList;
                //case CivEnum.BORG: return BorgShipSOList;
                //case CivEnum.TERRAN: return TerranShipSOList;
                default: return null;
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
        public ShipController InstantiateCombatShip(ShipData shipData, Vector3 position)
        {
            if (combatShipPrefab == null)
            {
                Debug.LogError("ShipManager: combatShipPrefab is null!");
                return null;
            }

            ShipController combatShip = Instantiate(combatShipPrefab, position, Quaternion.identity);
            combatShip.ShipData = shipData; // Reuse the same data!
            combatShip.name = $"{shipData.ShipName}_Combat";

            RegisterCombatShip(combatShip);

            Debug.Log($"ShipManager: Instantiated combat ship {combatShip.name}");

            return combatShip;
        }
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
            ShipSO shipSO = ShipSORegistry.GetByID(shipName);
            if (shipSO != null && shipSO.Prefab != null)
            {
                Instantiate(shipSO.Prefab, position, Quaternion.identity);
            }
        }

        public List<ShipController> InstantiateShipControllersWithDataFromSO(List<ShipSO> shipSOList, GameObject parentGO)
        {
            List<ShipController> shipConList = new List<ShipController>();
            for (int i = 0; i < shipSOList.Count; i++)
            {
                if (shipSOList[i] != null)
                {
                    ShipController shipCon = Instantiate(shipConPrefab, new Vector3(0, 0, 0),
                    Quaternion.identity, CombatManager.Instance.CombatUICanvasGO.transform);
                    shipCon.Init(this);
                    shipCon.ShipData = new ShipData();
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
                    var targetGO = Instantiate(targetGOPrefab, shipCon.transform.position, Quaternion.identity); // where other ship weapons target 
                    shipCon.ShipData.TargetOnThisShip = targetGO;
                    targetGO.transform.SetParent(shipCon.transform, false); // set target GO as child of ship GO
                    shipCon.ShipData.TargetOnThisShip.gameObject.transform.Translate(shipCon.transform.position.x, shipCon.transform.position.y, shipCon.transform.position.z + 10); // move target location back along spine of ship
                    shipCon.ShipData.ShipDescription = shipSOList[i].ShipDescription;
                    shipCon.gameObject.name = shipCon.ShipData.ShipName;
                    shipCon.Order = CombatOrders.None;
                    shipCon.gameObject.layer = 9; // set to "ships" layer
                    ShipControllerList.Add(shipCon);
                    if (parentGO.GetComponentInChildren<FleetController>() == null)
                    {
                        var sysCon = parentGO.GetComponent<StarSysController>();
                        shipCon.ShipData.CurrentStarSysController = sysCon;
                        if (sysCon.StarSysData.ShipsList.Contains(shipCon.GetComponent<ShipController>()) == false)
                            sysCon.StarSysData.ShipsList.Add(shipCon.GetComponent<ShipController>());
                        shipCon.ShipData.CurrentFleetController = null;
                    }
                    else if (parentGO.GetComponentInChildren<StarSysController>() == null)
                    {
                        var fleetCon = parentGO.GetComponent<FleetController>();
                        shipCon.ShipData.CurrentFleetController = fleetCon;
                        if (fleetCon.FleetData.ShipsList.Contains(shipCon.GetComponent<ShipController>()) == false)
                            fleetCon.FleetData.ShipsList.Add(shipCon.GetComponent<ShipController>());
                        shipCon.ShipData.CurrentStarSysController = null;
                    }

                    // Create the UI object and try to parent it to the owner's ShipListUIParent.
                    InstantiateShipListUIGameObject(shipCon, parentGO);

                    // Put gameplay ship under parent in scene
                    shipCon.transform.SetParent(parentGO.transform, false); // load into List of ships in the galaxy menu
                    shipConList.Add(shipCon);
                }
            }
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
        public ShipSO GetShipSO(ShipType shipType, TechLevel techLevel, CivEnum civEnum)
        {
            ShipSO ourShipSO = null;
            switch (techLevel)
            {
                case TechLevel.EARLY:
                    var shipSOIEnumEarly = ShipSOListTech0.Where(x => x.ShipType == shipType && x.CivEnum == civEnum);
                    var shipSOe = shipSOIEnumEarly.ToList().FirstOrDefault();
                    ourShipSO = shipSOe;
                    break;
                case TechLevel.DEVELOPED:
                    var shipSOIEnumDeveloped = ShipSOListTech1.Where(x => x.ShipType == shipType && x.CivEnum == civEnum);
                    var shipSOd = shipSOIEnumDeveloped.ToList().FirstOrDefault();
                    ourShipSO = shipSOd;
                    break;
                case TechLevel.ADVANCED:
                    var shipSOIEnumAdvanced = ShipSOListTech2.Where(x => x.ShipType == shipType && x.CivEnum == civEnum);
                    var shipSOa = shipSOIEnumAdvanced.ToList().FirstOrDefault();
                    ourShipSO = shipSOa;
                    break;
                case TechLevel.SUPREME:
                    var shipSOIEnumSup = ShipSOListTech3.Where(x => x.ShipType == shipType && x.CivEnum == civEnum);
                    var shipSOs = shipSOIEnumSup.ToList().FirstOrDefault();
                    ourShipSO = shipSOs;
                    break;
                default:
                    break;
            }
            if (ourShipSO == null)
            {
                Debug.Log("No shipSO found for " + shipType.ToString() + " at tech level " + techLevel.ToString() + " for civ " + civEnum.ToString() + ". Returning default scout ship.");
                var shipSOIEnumDefault = ShipSOListTech0.Where(x => x.ShipType == ShipType.Scout && x.CivEnum == civEnum);
                var shipSOdft = shipSOIEnumDefault.ToList().FirstOrDefault();
                ourShipSO = shipSOdft;
            }
            return ourShipSO;
        }
        public void BuildShipInSystem(ShipType shipType, StarSysController systemCon) // a destroyer for warp capable systems on game loading and shipyard during game
        {
            ShipSO ourShipSO = GetShipSO(shipType, systemCon.StarSysData.CurrentCivController.CivData.TechLevel, systemCon.StarSysData.CurrentOwnerCivEnum);
            List<ShipSO> shipSOAsList = new List<ShipSO> { ourShipSO };
            List<ShipController> shipConListOfOne = InstantiateShipControllersWithDataFromSO(shipSOAsList, systemCon.gameObject);
            foreach (ShipController shipCon in shipConListOfOne)
            {
                if (shipCon != null)
                {
                    shipCon.transform.SetParent(systemCon.transform);
                    // NOTE: InstantiateShipControllersWithDataFromSO already:
                    //  - adds the ShipController to ShipControllerList
                    //  - adds the ShipController to sysCon.StarSysData.ShipsList (when owner is a StarSysController)
                    // so we must not add them again to avoid duplicates.
                    shipCon.ShipData.CurrentStarSysController = systemCon;
                    shipCon.ShipData.CurrentFleetController = null;
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
                Debug.LogWarning($"  Ship '{shipCon.ShipData.ShipName}' is NOT local player - no UI created");
                return;
            }

            if (shipCon.ShipListUIGameObject != null)
            {
                Debug.Log($"  Ship '{shipCon.ShipData.ShipName}' already has UI - skipping");
                return;
            }

            Debug.Log($"  Creating UI for ship '{shipCon.ShipData.ShipName}'");

            GameObject thisShipListUIGameObject = (GameObject)Instantiate(shipListUIPrefab, new Vector3(0, 0, 0), Quaternion.identity);
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
            ships = FirstShipDateByTechLevel((int)CivManager.Instance.GetCivDataByCivEnum(civEnum).TechLevel, civEnum);
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
        public List<ShipSO> FirstShipDateByTechLevel(int techLevel, CivEnum civ)
        {
            List<ShipSO> listOfShipSOs = new List<ShipSO>();
            switch (techLevel)
            {
                case 100:// early
                    foreach (var shipSO in ShipSOListTech0)
                    {
                        if (shipSO.CivEnum == civ)
                        {
                            listOfShipSOs.Add(shipSO);
                        }
                    }
                    break;
                case 300: // developed
                    foreach (var shipSO in ShipSOListTech1)
                    {
                        if (shipSO.CivEnum == civ)
                        {
                            listOfShipSOs.Add(shipSO);
                        }
                    }
                    break;
                case 600: // advanced
                    foreach (var shipSO in ShipSOListTech2)
                    {
                        if (shipSO.CivEnum == civ)
                        {
                            listOfShipSOs.Add(shipSO);
                        }
                    }
                    break;
                case 900: // supreme
                    foreach (var shipSO in ShipSOListTech3)
                    {
                        if (shipSO.CivEnum == civ)
                        {
                            listOfShipSOs.Add(shipSO);
                        }
                    }
                    break;
                default:
                    foreach (var shipSO in ShipSOListTech0)
                    {
                        if (shipSO.CivEnum == civ)
                        {
                            listOfShipSOs.Add(shipSO);
                        }
                    }
                    ;
                    break;
            }
            return listOfShipSOs;
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
    }
}
