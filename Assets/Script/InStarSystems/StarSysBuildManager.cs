// Ignore Spelling: Sys

using Assets.Core;
using System.Collections;
using UnityEngine;

public class StarSysBuildManager
{
    /// <summary>
    /// build system items and ships production logic
    /// </summary>
    private readonly StarSysController controller;
    private Coroutine buildCoroutine;
    private Coroutine shipBuildCoroutine;

    public bool IsBuildingFacility
    {
        get { return buildCoroutine != null; }
    }
    public bool IsBuildingShip
    {
        get { return shipBuildCoroutine != null; }
    }
    public StarSysBuildManager(StarSysController owner)
    {
        controller = owner;
    }
    private IEnumerator BuildFacilityCoroutine(Transform buildItem)
    {
        if (buildItem == null)
            yield break;

        var buildDrag = buildItem.GetComponentInChildren<FactoryBuildItemDrag>();
        if (buildDrag == null)
            yield break;

        int buildTime = GetBuildTimeDuration(buildDrag.FacilityType);
        if (buildTime <= 0) buildTime = 1;

        int startDate = TimeManager.Instance.CurrentStarDate();
        int endDate = startDate + buildTime;

        while (TimeManager.Instance.CurrentStarDate() < endDate)
        {
            float progress =
                (TimeManager.Instance.CurrentStarDate() - startDate) / (float)buildTime;

            StarSysMenuUIController.Instance.SetBuildProgress(Mathf.Clamp01(progress));

            yield return null; // wait one frame
        }

        // ✅ Build complete
        CompleteFacilityBuild(buildItem);

        StarSysMenuUIController.Instance.SetBuildProgress(0f);
    }
    internal void CompleteFacilityBuild(Transform buildItem)
    {
        if (buildItem == null) return;

        var buildDrag = buildItem.GetComponentInChildren<FactoryBuildItemDrag>();
        if (buildDrag == null)
        {
            Debug.LogWarning("CompleteFacilityBuild: FactoryBuildItemDrag missing on buildItem.");
            return;
        }

        int civInt = (int)controller.StarSysData.CurrentOwnerCivEnum;
        GameObject newFacilityGO = null;

        switch (buildDrag.FacilityType)
        {
            case StarSysFacilityType.PowerPlanet:
                newFacilityGO = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.PowerPlantPrefab, civInt, 0, controller)[0];
                controller.StarSysData.PowerPlants = controller.StarSysData.PowerPlants ?? new System.Collections.Generic.List<GameObject>();
                controller.StarSysData.PowerPlants.Add(newFacilityGO);
                break;

            case StarSysFacilityType.Factory:
                newFacilityGO = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.FactoryPrefab, civInt, 0, controller)[0];
                controller.StarSysData.Factories = controller.StarSysData.Factories ?? new System.Collections.Generic.List<GameObject>();
                controller.StarSysData.Factories.Add(newFacilityGO);
                break;

            case StarSysFacilityType.Shipyard:
                newFacilityGO = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.ShipyardPrefab, civInt, 0, controller)[0];
                controller.StarSysData.Shipyards = controller.StarSysData.Shipyards ?? new System.Collections.Generic.List<GameObject>();
                controller.StarSysData.Shipyards.Add(newFacilityGO);
                break;

            case StarSysFacilityType.ShieldGenerator:
                newFacilityGO = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.ShieldGeneratorPrefab, civInt, 0, controller)[0];
                controller.StarSysData.ShieldGenerators = controller.StarSysData.ShieldGenerators ?? new System.Collections.Generic.List<GameObject>();
                controller.StarSysData.ShieldGenerators.Add(newFacilityGO);
                break;

            case StarSysFacilityType.OrbitalBattery:
                newFacilityGO = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.OrbitalBatteryPrefab, civInt, 0, controller)[0];
                controller.StarSysData.OrbitalBatteries = controller.StarSysData.OrbitalBatteries ?? new System.Collections.Generic.List<GameObject>();
                controller.StarSysData.OrbitalBatteries.Add(newFacilityGO);
                break;

            case StarSysFacilityType.ResearchCenter:
                newFacilityGO = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.ResearchCenterPrefab, civInt, 0, controller)[0];
                controller.StarSysData.ResearchCenters = controller.StarSysData.ResearchCenters ?? new System.Collections.Generic.List<GameObject>();
                controller.StarSysData.ResearchCenters.Add(newFacilityGO);
                break;

            default:
                Debug.LogWarning($"CompleteFacilityBuild: Unknown facility type {buildDrag.FacilityType}");
                break;
        }

        // Parent it under the star system so transforms/hierarchy are correct
        if (newFacilityGO != null)
            newFacilityGO.transform.SetParent(controller.gameObject.transform, false);

        // Remove/cleanup temp build UI item used to represent the building process
        buildItem.SetParent(buildDrag.originalParent, false);
        UnityEngine.Object.Destroy(buildItem.gameObject);

        // --- UI update: prefer typed UI that reads from StarSysData lists ---
        var uiElement = controller.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
        if (uiElement != null)
        {
            // InitializeFromStarSysData reads the lists and updates counts/icons/loads
            uiElement.InitializeFromStarSysData(controller.StarSysData);
        }
        else
        {
            // Fallback for older string-based menu: attempt to update via menu controller (keeps backwards compatibility)
            if (StarSysMenuUIController.Instance != null)
            {
                StarSysMenuUIController.Instance.AddSysFacility(
                    controller,
                    newFacilityGO,
                    "ResearchCenterLoad",       // loadName (legacy)
                    "NumResearchCenterRatio",   // ratioName (legacy)
                    buildDrag.FacilityType
                );
            }
            else
            {
                Debug.LogWarning($"CompleteFacilityBuild: No UI available to update for system {controller.name}.");
            }
        }

        // Recompute power load in menu/controller UI (defensive)
        var uiContoller = StarSysMenuUIController.Instance;
        if (uiContoller != null)
        {
            uiContoller.UpdateSystemPowerBalance(controller);
        }
        // Mark coroutine done and start next queued build
        buildCoroutine = null;
        StartNextFacilityBuildIfAny();
    }
    public void StartNextFacilityBuildIfAny()
    {
        if (buildCoroutine != null)
            controller.StopCoroutine(buildCoroutine);

        if (controller.sysBuildQueueList.Count == 0 || controller.sysBuildQueueList[0] == null)
            return;

        buildCoroutine = controller.StartCoroutine(
            BuildFacilityCoroutine(controller.sysBuildQueueList[0])
        );
    }
    private IEnumerator BuildShipCoroutine(Transform shipBuildItem)
    {
        var drag = shipBuildItem.GetComponentInChildren<ShipBuildDrag>();
        if (drag == null) yield break;

        int buildTime = ShipManager.Instance.GetShipBuildDuration(
            drag.ShipType,
            controller.StarSysData.CurrentCivController.CivData.TechLevel,
            controller.StarSysData.CurrentOwnerCivEnum
        );

        int startDate = TimeManager.Instance.CurrentStarDate();
        int endDate = startDate + buildTime;

        while (TimeManager.Instance.CurrentStarDate() < endDate)
        {
            float progress =
                (TimeManager.Instance.CurrentStarDate() - startDate) / (float)buildTime;
            StarSysMenuUIController.Instance.SetShipBuildProgress(Mathf.Clamp01(progress));
            yield return null;
        }

        ShipManager.Instance.BuildShipInSystem(drag.ShipType, controller);

        UnityEngine.Object.Destroy(shipBuildItem.gameObject);
        controller.shipBuildQueueList.Remove(shipBuildItem);

        StarSysMenuUIController.Instance.SetShipBuildProgress(0f);
        shipBuildCoroutine = null;
        StartNextShipBuildIfAny();
    }
    public void StartNextShipBuildIfAny()
    {
        if (shipBuildCoroutine != null)
            controller.StopCoroutine(shipBuildCoroutine);

        if (controller.shipBuildQueueList.Count == 0 || controller.shipBuildQueueList[0] == null)
            return;

        shipBuildCoroutine = controller.StartCoroutine(
            BuildShipCoroutine(controller.shipBuildQueueList[0])
        );
    }
    public int GetBuildTimeDuration(StarSysFacilityType starSysFacilities)
    {
        int timeDuration = 1;
        TechLevel ourTechLevel = controller.StarSysData.CurrentCivController.CivData.TechLevel;
        // ToD use tech level to set features of system production, defense....q
        switch (starSysFacilities)
        {
            case StarSysFacilityType.PowerPlanet:
                timeDuration = controller.StarSysData.PowerPlantData.BuildDuration;
                break;
            case StarSysFacilityType.Factory:
                timeDuration = controller.StarSysData.FactoryData.BuildDuration;
                break;
            case StarSysFacilityType.Shipyard:
                timeDuration = controller.StarSysData.ShipyardData.BuildDuration;
                break;
            case StarSysFacilityType.ShieldGenerator:
                timeDuration = controller.StarSysData.ShieldGeneratorData.BuildDuration;
                break;
            case StarSysFacilityType.OrbitalBattery:
                timeDuration = controller.StarSysData.OrbitalBatteryData.BuildDuration;
                break;
            case StarSysFacilityType.ResearchCenter:
                timeDuration = controller.StarSysData.ResearchCenterData.BuildDuration;
                break;
            default:
                break;
        }
        return timeDuration;
        //ToD use tech level to set features of system production, defense....
    }

    internal void RegisterStarSysController(StarSysController starSysCon)
    {
        starSysCon.SysBuildManager = this;
    }
}

