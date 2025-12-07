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
    private Transform buildingItem;
    private Transform shipBuildingItem;
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
        if (buildDrag == null) return;
        switch (buildDrag.FacilityType)
        {
            case StarSysFacilities.PowerPlanet:
                controller.StarSysData.PowerPlants.Add(
                    StarSysManager.Instance.AddSystemFacilities(
                        1,
                        StarSysManager.Instance.PowerPlantPrefab,
                        (int)controller.StarSysData.CurrentOwnerCivEnum,
                        controller.StarSysData,
                        0
                    )[0]
                );
                break;

            case StarSysFacilities.Factory:
                var factory =
                    StarSysManager.Instance.AddSystemFacilities(
                        1,
                        StarSysManager.Instance.FactoryPrefab,
                        (int)controller.StarSysData.CurrentOwnerCivEnum,
                        controller.StarSysData,
                        0
                    )[0];
                StarSysMenuUIController.Instance.AddSysFacility(controller, factory, "FactoryLoad", "NumFactoryRatio", StarSysFacilities.Factory);
                break;
                // (shipyard, shield, OB, research...)
        }

        // Remove UI element
        buildItem.SetParent(buildDrag.originalParent, false);
        UnityEngine.Object.Destroy(buildItem.gameObject);

        controller.sysBuildQueueList.Remove(buildItem);

        StarSysMenuUIController.Instance.UpdateSystemPowerLoad(controller);
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
    public int GetBuildTimeDuration(StarSysFacilities starSysFacilities)
    {
        int timeDuration = 1;
        TechLevel ourTechLevel = controller.StarSysData.CurrentCivController.CivData.TechLevel;
        switch (starSysFacilities)
        {
            case StarSysFacilities.PowerPlanet:
                timeDuration = controller.StarSysData.PowerPlantData.BuildDuration;
                break;
            case StarSysFacilities.Factory:
                timeDuration = controller.StarSysData.FactoryData.BuildDuration;
                break;
            case StarSysFacilities.Shipyard:
                timeDuration = controller.StarSysData.ShipyardData.BuildDuration;
                break;
            case StarSysFacilities.ShieldGenerator:
                timeDuration = controller.StarSysData.ShieldGeneratorData.BuildDuration;
                break;
            case StarSysFacilities.OrbitalBattery:
                timeDuration = controller.StarSysData.OrbitalBatteryData.BuildDuration;
                break;
            case StarSysFacilities.ResearchCenter:
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

