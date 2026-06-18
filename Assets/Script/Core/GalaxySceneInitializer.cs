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
    [SerializeField] private bool createTestFleets = false;
    [SerializeField] private int testScouts = 6;
    [SerializeField] private int testDestroyers = 6;
    [SerializeField] private int testTransports = 2;

    private bool isInitialized = false;

    private void Awake()
    {
        Debug.Log("GalaxySceneInitializer: Awake - starting initialization sequence...");
        StartCoroutine(InitializeGalaxyReferencesAfterDelay());
    }

    /// <summary>
    /// Wait 2 frames before setting galaxy references to ensure scene is fully loaded.
    /// </summary>
    private IEnumerator InitializeGalaxyReferencesAfterDelay()
    {
        if (isInitialized)
        {
            Debug.Log("GalaxySceneInitializer: Already initialized - skipping");
            yield break;
        }

        Debug.Log("GalaxySceneInitializer: Waiting 2 frames for scene stabilization...");
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

        // createTestFleets requires ShipManager (CombatScene only) — leave false in builds.
        if (createTestFleets)
        {
            yield return new WaitForSeconds(2.0f);
            AddShipsToExistingFleets();
        }
    }

    /// <summary>
    /// Adds test ships to the existing default fleets for Federation and Klingon.
    /// Requires ShipManager which only exists when CombatScene is loaded.
    /// </summary>
    private void AddShipsToExistingFleets()
    {
        if (FleetManager.Instance == null || ShipManager.Instance == null)
        {
            Debug.LogWarning("⚠️ AddShipsToExistingFleets: ShipManager or FleetManager is null - skipping. " +
                             "Set createTestFleets=false in Inspector for normal builds.");
            return;
        }

        Debug.Log("🧪 Adding test ships to existing fleets...");

        FleetController fedFleet = FindFleetByCiv(CivEnum.FED);
        FleetController klingFleet = FindFleetByCiv(CivEnum.KLING);

        if (fedFleet != null)
            AddTestShipsToFleet(fedFleet, CivEnum.FED, "Federation");
        else
            Debug.LogWarning("⚠️ No Federation fleet found to add test ships to");

        if (klingFleet != null)
            AddTestShipsToFleet(klingFleet, CivEnum.KLING, "Klingon");
        else
            Debug.LogWarning("⚠️ No Klingon fleet found to add test ships to");

        Debug.Log("✅ Test ships added to existing fleets!");
    }

    private FleetController FindFleetByCiv(CivEnum civEnum)
    {
        if (FleetManager.Instance == null || FleetManager.Instance.FleetControllerList == null)
        {
            Debug.LogError("FindFleetByCiv: FleetManager.Instance or FleetControllerList is null!");
            return null;
        }

        foreach (var fleet in FleetManager.Instance.FleetControllerList)
        {
            if (fleet != null && fleet.FleetData != null && fleet.FleetData.CivEnum == civEnum)
                return fleet;
        }

        Debug.LogWarning($"FindFleetByCiv: No fleet found for {civEnum}");
        return null;
    }

    private void AddTestShipsToFleet(FleetController fleet, CivEnum civEnum, string civName)
    {
        if (fleet == null || fleet.FleetData == null)
        {
            Debug.LogError($"AddTestShipsToFleet: Fleet or FleetData is null for {civName}");
            return;
        }

        Debug.Log($"📦 Adding test ships to {civName} fleet '{fleet.FleetData.FleetName}'...");

        TechLevel currentTechLevel = TechLevel.EARLY;
        if (GameController.Instance?.GameData != null)
            currentTechLevel = GameController.Instance.GameData.StartingTechLevel;

        ShipSO scoutSO     = GetShipSOForTechLevel(civEnum, ShipType.Scout,     currentTechLevel);
        ShipSO destroyerSO = GetShipSOForTechLevel(civEnum, ShipType.Destroyer, currentTechLevel);
        ShipSO transportSO = GetShipSOForTechLevel(civEnum, ShipType.Transport, currentTechLevel);

        List<ShipSO> shipsToCreate = new List<ShipSO>();
        for (int i = 0; i < testScouts;      i++) if (scoutSO     != null) shipsToCreate.Add(scoutSO);
        for (int i = 0; i < testDestroyers;  i++) if (destroyerSO != null) shipsToCreate.Add(destroyerSO);
        for (int i = 0; i < testTransports;  i++) if (transportSO != null) shipsToCreate.Add(transportSO);

        if (shipsToCreate.Count == 0)
        {
            Debug.LogError($"❌ No ship SOs found for {civEnum} at {currentTechLevel}!");
            return;
        }

        List<ShipController> createdShips = ShipManager.Instance.InstantiateShipControllersWithDataFromSO(
            shipsToCreate, fleet.gameObject);

        if (createdShips != null && createdShips.Count > 0)
        {
            fleet.UpdateMaxWarp();
            Debug.Log($"✅ Added {createdShips.Count} test ships to {civName} fleet. Total: {fleet.FleetData.ShipsList.Count}");
        }
        else
        {
            Debug.LogError($"❌ InstantiateShipControllersWithDataFromSO returned no ships for {civName}!");
        }
    }

    private ShipSO GetShipSOForTechLevel(CivEnum civEnum, ShipType shipType, TechLevel techLevel)
    {
        List<ShipSO> civShips = ShipManager.Instance.GetShipSOListByCiv(civEnum);
        if (civShips == null || civShips.Count == 0) return null;
        return civShips.Find(s => s != null && s.ShipType == shipType && s.TechLevel == techLevel);
    }
}
