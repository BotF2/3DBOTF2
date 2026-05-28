using BOTF3D.Combat;
using BOTF3D.Core;

using BOTF3D.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



public class GalaxySceneInitializer : MonoBehaviour
{
        public void Initialize() { }
        public void UpdateState() { }
    [Header("Assign All Galaxy Scene References")]
    public GameObject galaxyCenter;
    public GameObject galaxyImage;
    public Canvas canvasGalaxy;
    public GameObject fleetListContainer;
    public GameObject systemListContainer;
    public GameObject galaxyCameraDragMoveZoom;
    public Camera mainCamera;

    [Header("Test Fleet Configuration")]
    [SerializeField] private bool createTestFleets = true;
    [SerializeField] private int testScouts = 6;
    [SerializeField] private int testDestroyers = 6;
    [SerializeField] private int testTransports = 2;

    private bool isInitialized = false;

    private void Awake()
    {
        Debug.Log("GalaxySceneInitializer: Awake - starting initialization sequence...");

        // ✅ Start coroutine to set references after 2 frames
        // Per copilot-instructions: wait for UI ownership normalization
        StartCoroutine(InitializeGalaxyReferencesAfterDelay());
    }

    /// <summary>
    /// Wait 2 frames before setting galaxy references to ensure scene is fully loaded
    /// Per copilot-instructions.md: "wait two frames (yield return null twice) 
    /// before normalizing UI ownership and rebuilding lists"
    /// </summary>
    private IEnumerator InitializeGalaxyReferencesAfterDelay()
    {
        if (isInitialized)
        {
            Debug.Log("GalaxySceneInitializer: Already initialized - skipping");
            yield break;
        }

        Debug.Log("GalaxySceneInitializer: Waiting 2 frames for scene stabilization...");

        // ✅ Wait two frames (from copilot-instructions.md)
        yield return null;
        yield return null;

        Debug.Log("GalaxySceneInitializer: Scene stabilized - setting galaxy references...");

        // Get MainCamera if not assigned
        if (mainCamera == null)
        {
            var mainCameraGO = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCameraGO != null)
                mainCamera = mainCameraGO.GetComponent<Camera>();
        }

        // Pass to FleetManager
        if (FleetManager.Instance != null)
        {
            FleetManager.Instance.SetGalaxyReferences(galaxyCenter, galaxyImage, canvasGalaxy, fleetListContainer);
            Debug.Log("  ✅ FleetManager references set");
        }
        else
        {
            Debug.LogError("  ❌ FleetManager.Instance is NULL!");
        }

        // Pass to StarSysManager
        if (StarSysManager.Instance != null)
        {
            StarSysManager.Instance.SetGalaxyReferences(galaxyCenter, systemListContainer);
            Debug.Log("  ✅ StarSysManager references set");
        }
        else
        {
            Debug.LogError("  ❌ StarSysManager.Instance is NULL!");
        }

        // Pass to StarSysMenuUIController
        if (StarSysMenuUIController.Instance != null)
        {
            StarSysMenuUIController.Instance.SetUIReferences(systemListContainer, canvasGalaxy.gameObject);
            Debug.Log("  ✅ StarSysMenuUIController references set");
        }
        else
        {
            Debug.LogError("  ❌ StarSysMenuUIController.Instance is NULL!");
        }

        // Pass to SceneController
        if (SceneController.Instance != null)
        {
            SceneController.Instance.SetGalaxyReferences(galaxyCameraDragMoveZoom);
            Debug.Log("  ✅ SceneController references set");
        }
        else
        {
            Debug.LogError("  ❌ SceneController.Instance is NULL!");
        }

        isInitialized = true;
        Debug.Log("✅ GalaxySceneInitializer: Initialization complete!");

        // ✅ Create test fleets after systems and default fleets are created
        if (createTestFleets)
        {
            // Wait longer for all systems and fleets to be created
            yield return new WaitForSeconds(2.0f);
            AddShipsToExistingFleets();
        }
    }

    /// <summary>
    /// Adds test ships to the existing default fleets for Federation and Klingon
    /// </summary>
    private void AddShipsToExistingFleets()
    {
        Debug.Log("🧪 Adding test ships to existing fleets...");

        if (FleetManager.Instance == null || ShipManager.Instance == null)
        {
            Debug.LogError("❌ FleetManager or ShipManager is null!");
            return;
        }

        // Find Federation and Klingon fleets
        FleetController fedFleet = FindFleetByCiv(CivEnum.FED);
        FleetController klingFleet = FindFleetByCiv(CivEnum.KLING);

        if (fedFleet != null)
        {
            AddTestShipsToFleet(fedFleet, CivEnum.FED, "Federation");
        }
        else
        {
            Debug.LogWarning("⚠️ No Federation fleet found to add test ships to");
        }

        if (klingFleet != null)
        {
            AddTestShipsToFleet(klingFleet, CivEnum.KLING, "Klingon");
        }
        else
        {
            Debug.LogWarning("⚠️ No Klingon fleet found to add test ships to");
        }

        Debug.Log("✅ Test ships added to existing fleets!");
    }

    /// <summary>
    /// Finds the first fleet belonging to a specific civilization
    /// </summary>
    private FleetController FindFleetByCiv(CivEnum civEnum)
    {
        if (FleetManager.Instance == null || FleetManager.Instance.FleetControllerList == null)
        {
            Debug.LogError("FindFleetByCiv: FleetManager.Instance or FleetControllerList is null!");
            return null;
        }

        Debug.Log($"FindFleetByCiv: Searching {FleetManager.Instance.FleetControllerList.Count} fleets for {civEnum}");

        foreach (var fleet in FleetManager.Instance.FleetControllerList)
        {
            if (fleet != null && fleet.FleetData != null)
            {
                Debug.Log($"  Checking fleet '{fleet.name}' - CivEnum: {fleet.FleetData.CivEnum}");

                if (fleet.FleetData.CivEnum == civEnum)
                {
                    Debug.Log($"  ✅ Found {civEnum} fleet: '{fleet.name}'");
                    return fleet;
                }
            }
        }

        Debug.LogWarning($"FindFleetByCiv: No fleet found for {civEnum}");
        return null;
    }

    /// <summary>
    /// Adds test ships to an existing fleet using the full ship creation pipeline
    /// </summary>
    private void AddTestShipsToFleet(FleetController fleet, CivEnum civEnum, string civName)
    {
        if (fleet == null || fleet.FleetData == null)
        {
            Debug.LogError($"AddTestShipsToFleet: Fleet or FleetData is null for {civName}");
            return;
        }

        Debug.Log($"📦 Adding test ships to {civName} fleet '{fleet.FleetData.FleetName}'...");
        Debug.Log($"   Current ship count: {fleet.FleetData.ShipsList.Count}");

        // ✅ Get current tech level from GameController
        TechLevel currentTechLevel = TechLevel.EARLY;
        if (GameController.Instance?.GameData != null)
        {
            currentTechLevel = GameController.Instance.GameData.StartingTechLevel;
            Debug.Log($"   Using tech level: {currentTechLevel}");
        }

        // Get ship SOs from ShipManager for the CURRENT tech level
        ShipSO scoutSO = GetShipSOForTechLevel(civEnum, ShipType.Scout, currentTechLevel);
        ShipSO destroyerSO = GetShipSOForTechLevel(civEnum, ShipType.Destroyer, currentTechLevel);
        ShipSO transportSO = GetShipSOForTechLevel(civEnum, ShipType.Transport, currentTechLevel);

        // Build list of ships to create
        List<ShipSO> shipsToCreate = new List<ShipSO>();

        // Add scouts
        if (scoutSO != null)
        {
            for (int i = 0; i < testScouts; i++)
                shipsToCreate.Add(scoutSO);
            Debug.Log($"   Adding {testScouts} Scouts");
        }
        else
        {
            Debug.LogWarning($"⚠️ No Scout ShipSO found for {civEnum} at {currentTechLevel}");
        }

        // Add destroyers
        if (destroyerSO != null)
        {
            for (int i = 0; i < testDestroyers; i++)
                shipsToCreate.Add(destroyerSO);
            Debug.Log($"   Adding {testDestroyers} Destroyers");
        }
        else
        {
            Debug.LogWarning($"⚠️ No Destroyer ShipSO found for {civEnum} at {currentTechLevel}");
        }

        // Add transports
        if (transportSO != null)
        {
            for (int i = 0; i < testTransports; i++)
                shipsToCreate.Add(transportSO);
            Debug.Log($"   Adding {testTransports} Transports");
        }
        else
        {
            Debug.LogWarning($"⚠️ No Transport ShipSO found for {civEnum} at {currentTechLevel}");
        }

        if (shipsToCreate.Count == 0)
        {
            Debug.LogError($"❌ No ship SOs found for {civEnum} at {currentTechLevel}! Cannot add test ships.");
            return;
        }

        Debug.Log($"   Creating {shipsToCreate.Count} ships using FULL pipeline...");

        // ✅ Use the FULL ship creation method that includes UI
        List<ShipController> createdShips = ShipManager.Instance.InstantiateShipControllersWithDataFromSO(
            shipsToCreate,
            fleet.gameObject
        );

        if (createdShips == null || createdShips.Count == 0)
        {
            Debug.LogError($"❌ InstantiateShipControllersWithDataFromSO returned no ships for {civName}!");
            return;
        }

        Debug.Log($"   Ships created via full pipeline: {createdShips.Count}");

        // Update fleet max warp
        fleet.UpdateMaxWarp();

        Debug.Log($"✅ Added {createdShips.Count} test ships to {civName} fleet:");
        Debug.Log($"   - {testScouts} Scouts");
        Debug.Log($"   - {testDestroyers} Destroyers");
        Debug.Log($"   - {testTransports} Transports");
        Debug.Log($"   Total ships in fleet: {fleet.FleetData.ShipsList.Count}");
    }

    /// <summary>
    /// Get a ship SO for a specific civ, ship type, and tech level
    /// </summary>
    private ShipSO GetShipSOForTechLevel(CivEnum civEnum, ShipType shipType, TechLevel techLevel)
    {
        List<ShipSO> civShips = ShipManager.Instance.GetShipSOListByCiv(civEnum);

        if (civShips == null || civShips.Count == 0)
        {
            Debug.LogWarning($"GetShipSOForTechLevel: No ships found for {civEnum}");
            return null;
        }

        // Find ship matching both type AND tech level
        ShipSO matchingShip = civShips.Find(s =>
            s != null &&
            s.ShipType == shipType &&
            s.TechLevel == techLevel
        );

        if (matchingShip != null)
        {
            Debug.Log($"   Found {shipType} for {civEnum} at {techLevel}: {matchingShip.ShipName}");
        }
        else
        {
            Debug.LogWarning($"   No {shipType} found for {civEnum} at {techLevel}");
        }

        return matchingShip;
    }
}
