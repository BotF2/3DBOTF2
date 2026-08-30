// Ignore Spelling: Sys

using BOTF3D.UI;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Audio;




namespace BOTF3D.Galaxy
{
    public class StarSysBuildManager : IManager
    {
        public void Initialize() { }
        public void Cleanup() { }
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

        // SliderBuildProgress/ShipSliderBuildProgress on StarSysMenuUIController are rewired to whichever
        // system's build panel is currently open (see StarSysManager.InstantiateSysBuildUI); they are not
        // scoped per system. Without this check, a system building in the background (e.g. an AI civ)
        // would overwrite the currently-viewed system's slider with its own unrelated progress every frame.
        private bool IsShowingThisSystemsBuildUI => StarSysManager.Instance?.CurrentBuildUISysCon == controller;

        private IEnumerator BuildFacilityCoroutine(Transform buildItem)
        {
            Debug.Log($"=== BuildFacilityCoroutine: START ===");

            if (buildItem == null)
            {
                Debug.LogError("BuildFacilityCoroutine: buildItem is NULL!");
                yield break;
            }
            var buildDrag = buildItem.GetComponentInChildren<FactoryBuildItemDrag>();
            if (buildDrag == null) // && buildItem.name != "BuildingBackground")
            {
                Debug.LogError($"BuildFacilityCoroutine: No FactoryBuildItemDrag found on '{buildItem.name}'!");
                yield break;
            }

            int buildTime = GetBuildTimeDuration(buildDrag.FacilityType);
            if (buildTime <= 0) buildTime = 1;

            // Deduct dilithium stockpile when a power plant build starts
            if (buildDrag.FacilityType == StarSysFacilityType.PowerPlanet)
            {
                controller.StarSysData.DeductDilithium(
                    ShipStatCalculator.GetPowerPlantDilithiumCost(controller.StarSysData.CurrentOwnerCivEnum));
                RefreshCompactHeaderDilithium();
            }

            int startDate = TimeManager.Instance.CurrentStarDate();
            int endDate = startDate + buildTime;

            Debug.Log($"BuildFacilityCoroutine: Building {buildDrag.FacilityType}");
            Debug.Log($"  Build time: {buildTime} stardates");
            Debug.Log($"  Start date: {startDate}");
            Debug.Log($"  End date: {endDate}");

            // ✅ Reset slider to 0%
            if (IsShowingThisSystemsBuildUI && StarSysMenuUIController.Instance != null)
            {
                StarSysMenuUIController.Instance.SetBuildProgress(0f);
                Debug.Log("  Slider reset to 0%");
            }

            int loopCount = 0;
            while (TimeManager.Instance.CurrentStarDate() < endDate)
            {
                // ✅ Calculate progress based on STARDATE advancement
                int currentDate = TimeManager.Instance.CurrentStarDate();
                int elapsedStardates = currentDate - startDate;
                float progress = Mathf.Clamp01((float)elapsedStardates / buildTime);

                // ✅ Log every 100 frames
                if (loopCount % 100 == 0)
                {
                    Debug.Log($"  Progress: {progress:P0} ({elapsedStardates}/{buildTime} stardates, current={currentDate})");
                }

                // ✅ Update the slider
                if (IsShowingThisSystemsBuildUI && StarSysMenuUIController.Instance != null)
                {
                    StarSysMenuUIController.Instance.SetBuildProgress(progress);
                }

                loopCount++;
                yield return null; // wait one frame
            }

            // ✅ Complete - set slider to 100%
            if (IsShowingThisSystemsBuildUI && StarSysMenuUIController.Instance != null)
            {
                StarSysMenuUIController.Instance.SetBuildProgress(1f);
            }

            Debug.Log($"BuildFacilityCoroutine: {buildDrag.FacilityType} COMPLETE at stardate {TimeManager.Instance.CurrentStarDate()}");
            Debug.Log($"  Total frames: {loopCount}");

            // ✅ Build complete
            CompleteFacilityBuild(buildItem);

            // ✅ Reset slider
            if (IsShowingThisSystemsBuildUI && StarSysMenuUIController.Instance != null)
            {
                StarSysMenuUIController.Instance.SetBuildProgress(0f);
            }

            // Null the coroutine FIRST so IsBuildingFacility becomes false before
            // StartNextFacilityBuildIfAny sets it back to true for the next item.
            // (CompleteFacilityBuild no longer does this, to avoid clobbering the
            //  new coroutine reference that StartNextFacilityBuildIfAny assigns.)
            buildCoroutine = null;
            StartNextFacilityBuildIfAny();
            StarSysMenuUIController.Instance?.RefreshQueueForSystem(controller);
        }
        internal void CompleteFacilityBuild(Transform buildItem)
        {
            if (buildItem == null) return;

            // Explicit removal mirrors the ship-queue pattern and ensures the list is
            // accurate before RefreshFacilityQueue reads it (startIndex depends on it).
            controller.sysBuildQueueList.Remove(buildItem);

            var buildDrag = buildItem.GetComponentInChildren<FactoryBuildItemDrag>();
            if (buildDrag == null)
            {
                Debug.LogWarning("CompleteFacilityBuild: FactoryBuildItemDrag missing on buildItem.");
                UnityEngine.Object.Destroy(buildItem.gameObject);
                return;
            }

            int civInt = (int)controller.StarSysData.CurrentOwnerCivEnum;
            GameObject newFacilityGO = null;

            switch (buildDrag.FacilityType)
            {
                case StarSysFacilityType.PowerPlanet:
                    newFacilityGO = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.PowerPlantPrefab, civInt, 0, controller)[0];
                    controller.StarSysData.PowerPlants = controller.StarSysData.PowerPlants ?? new System.Collections.Generic.List<GameObject>();
                    // ❌ REMOVED: controller.StarSysData.PowerPlants.Add(newFacilityGO); // Let AddSysFacility handle this
                    break;

                case StarSysFacilityType.Factory:
                    newFacilityGO = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.FactoryPrefab, civInt, 0, controller)[0];
                    controller.StarSysData.Factories = controller.StarSysData.Factories ?? new System.Collections.Generic.List<GameObject>();
                    // ❌ REMOVED: controller.StarSysData.Factories.Add(newFacilityGO); // Let AddSysFacility handle this
                    break;

                case StarSysFacilityType.Shipyard:
                    newFacilityGO = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.ShipyardPrefab, civInt, 0, controller)[0];
                    controller.StarSysData.Shipyards = controller.StarSysData.Shipyards ?? new System.Collections.Generic.List<GameObject>();
                    // ❌ REMOVED: controller.StarSysData.Shipyards.Add(newFacilityGO); // Let AddSysFacility handle this
                    break;

                case StarSysFacilityType.ShieldGenerator:
                    newFacilityGO = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.ShieldGeneratorPrefab, civInt, 0, controller)[0];
                    controller.StarSysData.ShieldGenerators = controller.StarSysData.ShieldGenerators ?? new System.Collections.Generic.List<GameObject>();
                    // ❌ REMOVED: controller.StarSysData.ShieldGenerators.Add(newFacilityGO); // Let AddSysFacility handle this
                    break;

                case StarSysFacilityType.OrbitalBattery:
                    newFacilityGO = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.OrbitalBatteryPrefab, civInt, 0, controller)[0];
                    controller.StarSysData.OrbitalBatteries = controller.StarSysData.OrbitalBatteries ?? new System.Collections.Generic.List<GameObject>();
                    // ❌ REMOVED: controller.StarSysData.OrbitalBatteries.Add(newFacilityGO); // Let AddSysFacility handle this
                    break;

                case StarSysFacilityType.ResearchCenter:
                    newFacilityGO = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.ResearchCenterPrefab, civInt, 0, controller)[0];
                    controller.StarSysData.ResearchCenters = controller.StarSysData.ResearchCenters ?? new System.Collections.Generic.List<GameObject>();
                    // ❌ REMOVED: controller.StarSysData.ResearchCenters.Add(newFacilityGO); // Let AddSysFacility handle this
                    break;

                default:
                    Debug.LogWarning($"CompleteFacilityBuild: Unknown facility type {buildDrag.FacilityType}");
                    break;
            }

            // Phase 2 & 3: adjust dilithium mining rate based on what was just built
            UpdateMiningRateOnBuild(controller, buildDrag.FacilityType);

            // Parent it under the star system so transforms/hierarchy are correct
            if (newFacilityGO != null)
                newFacilityGO.transform.SetParent(controller.gameObject.transform, false);

            // Remove/cleanup temp build UI item used to represent the building process
            if (buildItem != null)
            {
                buildItem.SetParent(buildDrag.originalParent, false);
                UnityEngine.Object.Destroy(buildItem.gameObject);
            }

            // ✅ AddSysFacility will now handle adding to the list AND updating UI
            if (StarSysMenuUIController.Instance != null && newFacilityGO != null)
            {
                StarSysMenuUIController.Instance.AddSysFacility(
                    controller,
                    newFacilityGO,
                    string.Empty,
                    string.Empty,
                    buildDrag.FacilityType
                );
            }
            else
            {
                Debug.LogWarning($"CompleteFacilityBuild: StarSysMenuUIController.Instance is null or newFacilityGO is null for system {controller.name}.");
            }

        }

        // ── Dilithium mining rate growth on facility completion ───────────────────
        // Phase 2: each power plant built at a colony or recently-conquered system
        //          grows the mining rate by 1, up to ColonyRate + 1. Represents
        //          expanding extraction infrastructure as the system develops.
        //
        // Phase 3: the first shipyard removes the conquest disruption penalty by
        //          raising the rate to the full colony rate. Represents the
        //          conquering civ establishing proper resource administration.
        //
        // Homeworlds are excluded automatically — their rate is always above
        // ColonyRate + 1, so neither condition fires for them.
        // Pre-warp / non-habitable systems (rate == 0) are also excluded.
        private void UpdateMiningRateOnBuild(StarSysController controller, StarSysFacilityType facilityType)
        {
            var sysData = controller.StarSysData;
            if (sysData.DilithiumMiningRate <= 0) return;

            int colonyRate = StarSysManager.GetColonyMiningRate(sysData.CurrentOwnerCivEnum);

            switch (facilityType)
            {
                case StarSysFacilityType.PowerPlanet:
                    if (sysData.DilithiumMiningRate < colonyRate + 1)
                    {
                        sysData.DilithiumMiningRate = Mathf.Min(sysData.DilithiumMiningRate + 1, colonyRate + 1);
                        Debug.Log($"[Dilithium] {controller.name}: power plant built — mining rate now {sysData.DilithiumMiningRate}");
                    }
                    break;

                case StarSysFacilityType.Shipyard:
                    if (sysData.DilithiumMiningRate < colonyRate)
                    {
                        sysData.DilithiumMiningRate = colonyRate;
                        Debug.Log($"[Dilithium] {controller.name}: first shipyard online — disruption penalty cleared, mining rate now {sysData.DilithiumMiningRate}");
                    }
                    break;
            }
        }

        /// <summary>
        /// Pushes the current StarSysData.DilithiumStockpile to the always-visible
        /// compact header, keeping it in sync whenever dilithium is spent.
        /// </summary>
        private void RefreshCompactHeaderDilithium()
        {
            // AI-controlled systems may never have a UI GameObject instantiated, so
            // _starSysUIGameObject can be Unity's "fake null" — `?.` bypasses Unity's
            // overloaded null check and still calls GetComponent, throwing. Use an
            // explicit `== null` check instead.
            if (controller.StarSysUIGameObject == null) return;

            var fields = controller.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
            if (fields == null || fields.compactHeader == null) return;

            fields.compactHeader.RefreshDilithium();
        }
        public void StartNextFacilityBuildIfAny()
        {
            Debug.Log($"StartNextFacilityBuildIfAny: IsBuildingFacility={IsBuildingFacility}, Queue count={controller.sysBuildQueueList.Count}");

            if (buildCoroutine != null)
            {
                Debug.Log("  Stopping existing build coroutine");
                controller.StopCoroutine(buildCoroutine);
            }

            // Drop any leading entries whose backing GameObject was destroyed (e.g. queued while
            // combat was active elsewhere and lost when CombatScene unloaded — see the scene-pin
            // fix in QueueFacilityBuild) instead of getting permanently stuck logging an error on
            // every call from here on.
            while (controller.sysBuildQueueList.Count > 0 && controller.sysBuildQueueList[0] == null)
            {
                Debug.LogWarning("  ⚠️ Dropping destroyed build queue entry (was likely queued during combat elsewhere)");
                controller.sysBuildQueueList.RemoveAt(0);
            }

            if (controller.sysBuildQueueList.Count == 0)
            {
                Debug.Log("  ❌ Build queue is EMPTY - nothing to build");
                return;
            }

            // The whole GalaxyScene (every star system, including ones with nothing to do with
            // the fight) gets deactivated for the duration of combat (see
            // SceneController.LoadCombatSceneAdditive), but TimeManager's tick keeps running in
            // the background and can still reach here for an inactive system. StartCoroutine on
            // an inactive GameObject logs "Coroutine couldn't be started" and returns null, which
            // silently leaves the build stuck at IsBuildingFacility=false. Skip and retry on the
            // next tick — the queued item is left in place, so nothing is lost, just deferred
            // until this system's GameObject is active again (i.e. combat elsewhere has ended).
            if (!controller.gameObject.activeInHierarchy)
            {
                Debug.Log($"  ⏸ {controller.gameObject.name} is inactive (likely mid-combat elsewhere) — deferring build start to next tick");
                return;
            }

            Debug.Log($"  ✅ Starting build for item: {controller.sysBuildQueueList[0].name}");

            buildCoroutine = controller.StartCoroutine(
                BuildFacilityCoroutine(controller.sysBuildQueueList[0])
            );
        }
        private IEnumerator BuildShipCoroutine(Transform shipBuildItem)
        {
            var drag = shipBuildItem.GetComponentInChildren<ShipBuildDrag>();
            if (drag == null) yield break;

            // Validate and deduct dilithium before build begins
            var civ = controller.StarSysData.CurrentCivController;
            var techLevel = civ?.CivData?.CurrentTechLevel ?? TechLevel.EARLY;
            var civEnum = controller.StarSysData.CurrentOwnerCivEnum;
            int quality = civ?.CivData?.QualityScore ?? 5;
            int shipDilithium = ShipStatCalculator.Calculate(drag.ShipType, techLevel, civEnum, quality).DilithiumCost;

            if (!controller.StarSysData.HasDilithium(shipDilithium))
            {
                Debug.LogWarning($"BuildShipCoroutine: Not enough dilithium to build {drag.ShipType} (need {shipDilithium}, have {controller.StarSysData.DilithiumStockpile})");
                shipBuildCoroutine = null;
                yield break;
            }
            controller.StarSysData.DeductDilithium(shipDilithium);
            RefreshCompactHeaderDilithium();

            int buildTime = ShipManager.Instance.GetShipBuildDuration(
                drag.ShipType,
                controller.StarSysData.CurrentCivController.CivData.CurrentTechLevel,
                controller.StarSysData.CurrentOwnerCivEnum
            );

            // Apply any pending build-time reduction earned from captured ships
            if (civ?.CivData?.PendingBuildTimeReduction > 0)
            {
                int reduction = Mathf.Min(civ.CivData.PendingBuildTimeReduction, buildTime - 1);
                buildTime -= reduction;
                civ.CivData.PendingBuildTimeReduction -= reduction;
                Debug.Log($"⚙️ {civ.CivData.CivShortName}: build time reduced by {reduction} (capture bonus). New duration: {buildTime}");
            }

            int startDate = TimeManager.Instance.CurrentStarDate();
            int endDate = startDate + buildTime;

            Debug.Log($"BuildShipCoroutine: Building {drag.ShipType} for {buildTime} stardates (start={startDate}, end={endDate})");

            // ✅ Reset slider to 0%
            if (IsShowingThisSystemsBuildUI && StarSysMenuUIController.Instance != null)
            {
                StarSysMenuUIController.Instance.SetShipBuildProgress(0f);
            }

            while (TimeManager.Instance.CurrentStarDate() < endDate)
            {
                // ✅ Calculate progress based on STARDATE advancement
                int currentDate = TimeManager.Instance.CurrentStarDate();
                int elapsedStardates = currentDate - startDate;
                float progress = Mathf.Clamp01((float)elapsedStardates / buildTime);

                // ✅ Update the ship slider
                if (IsShowingThisSystemsBuildUI && StarSysMenuUIController.Instance != null)
                {
                    StarSysMenuUIController.Instance.SetShipBuildProgress(progress);
                }

                yield return null;
            }

            // ✅ Complete - set slider to 100%
            if (IsShowingThisSystemsBuildUI && StarSysMenuUIController.Instance != null)
            {
                StarSysMenuUIController.Instance.SetShipBuildProgress(1f);
            }

            Debug.Log($"BuildShipCoroutine: {drag.ShipType} complete at stardate {TimeManager.Instance.CurrentStarDate()}");

            ShipManager.Instance.BuildShipInSystem(drag.ShipType, controller);

            controller.sysShipBuildQueueList.Remove(shipBuildItem);
            if (shipBuildItem != null)
                UnityEngine.Object.Destroy(shipBuildItem.gameObject);

            StarSysMenuUIController.Instance?.RefreshQueueForSystem(controller);

            // ✅ Reset slider
            if (IsShowingThisSystemsBuildUI && StarSysMenuUIController.Instance != null)
            {
                StarSysMenuUIController.Instance.SetShipBuildProgress(0f);
            }

            shipBuildCoroutine = null;
        }
        public void StartNextShipBuildIfAny()
        {
            if (shipBuildCoroutine != null)
                controller.StopCoroutine(shipBuildCoroutine);

            // See StartNextFacilityBuildIfAny for why this can happen and why we drop rather than
            // get stuck.
            while (controller.sysShipBuildQueueList.Count > 0 && controller.sysShipBuildQueueList[0] == null)
                controller.sysShipBuildQueueList.RemoveAt(0);

            if (controller.sysShipBuildQueueList.Count == 0)
                return;

            // See StartNextFacilityBuildIfAny for why this guard is needed: the whole GalaxyScene
            // is deactivated for the duration of combat anywhere in the galaxy, but the AI economy
            // tick keeps running in the background. Defer to next tick rather than let
            // StartCoroutine fail on an inactive GameObject.
            if (!controller.gameObject.activeInHierarchy)
                return;

            shipBuildCoroutine = controller.StartCoroutine(
                BuildShipCoroutine(controller.sysShipBuildQueueList[0])
            );
        }

        // ── Code-driven queue API (used by AI; bypasses drag-and-drop UI) ──────────

        /// <summary>
        /// Enqueues a facility build without requiring UI drag-and-drop.
        /// Creates a minimal headless GameObject carrying the FactoryBuildItemDrag
        /// data the build coroutine expects.  Returns false if the queue is full.
        /// </summary>
        public bool QueueFacilityBuild(StarSysFacilityType type)
        {
            if (controller.sysBuildQueueList.Count >= 5) return false;

            // Belt-and-suspenders: StarSysAIManager's picker functions already check this
            // system's own build ceiling (StarSysManager.GetFacilityCap) before ever calling
            // here, but this is the single choke point every AI queue request passes through,
            // so it's the safe place to enforce it too rather than trust every future caller to
            // remember.
            if (StarSysManager.Instance != null)
            {
                int builtAndQueued = StarSysManager.Instance.GetBuiltAndQueuedFacilityCount(controller, type);
                if (builtAndQueued >= StarSysManager.Instance.GetFacilityCap(controller, type))
                    return false;
            }

            var go   = new GameObject($"AIBuild_{type}");
            // New GameObjects land in whichever scene is currently "active" (SceneManager.SetActiveScene),
            // which is CombatScene for the duration of any fight anywhere in the galaxy. Without this,
            // a build queued while combat is active gets destroyed when CombatScene unloads, leaving a
            // dangling null in this system's queue (count intact) — see StartNextFacilityBuildIfAny's
            // "First item in queue is NULL" error. Pin it to the star system's own scene explicitly.
            SceneManager.MoveGameObjectToScene(go, controller.gameObject.scene);
            var drag = go.AddComponent<FactoryBuildItemDrag>();
            drag.FacilityType      = type;
            drag.StarSysController = controller;

            controller.sysBuildQueueList.Add(go.transform);

            if (!IsBuildingFacility)
                StartNextFacilityBuildIfAny();

            return true;
        }

        /// <summary>
        /// Enqueues a ship build without requiring UI drag-and-drop.
        /// Creates a minimal headless GameObject carrying the ShipBuildDrag
        /// data the build coroutine expects.  Returns false if the queue is full.
        /// </summary>
        public bool QueueShipBuild(ShipType type)
        {
            if (controller.sysShipBuildQueueList.Count >= 5) return false;

            var go   = new GameObject($"AIBuild_{type}");
            // See QueueFacilityBuild for why this is needed — avoids the marker object landing in
            // CombatScene (and being destroyed on unload) if this queues while combat is active.
            SceneManager.MoveGameObjectToScene(go, controller.gameObject.scene);
            var drag = go.AddComponent<ShipBuildDrag>();
            drag.ShipType          = type;
            drag.StarSysController = controller;

            controller.sysShipBuildQueueList.Add(go.transform);

            if (!IsBuildingShip)
                StartNextShipBuildIfAny();

            return true;
        }

        public int GetBuildTimeDuration(StarSysFacilityType starSysFacilities)
        {
            int timeDuration = 1;
            TechLevel ourTechLevel = controller.StarSysData.CurrentCivController.CivData.CurrentTechLevel;

            // ✅ Get base build time
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

            // ✅ Apply tech multipliers to reduce build time
            if (TechManager.Instance != null)
            {
                float speedMultiplier = 1f;

                // Use appropriate multiplier based on facility type
                switch (starSysFacilities)
                {
                    case StarSysFacilityType.Factory:
                        speedMultiplier = TechManager.Instance.GetFactorySpeedMultiplier(ourTechLevel);
                        break;
                    case StarSysFacilityType.Shipyard:
                        speedMultiplier = TechManager.Instance.GetShipyardSpeedMultiplier(ourTechLevel);
                        break;
                    default:
                        // Other facilities use factory speed bonus
                        speedMultiplier = TechManager.Instance.GetFactorySpeedMultiplier(ourTechLevel);
                        break;
                }

                // ✅ Reduce build time based on speed multiplier
                timeDuration = Mathf.Max(1, Mathf.RoundToInt(timeDuration / speedMultiplier));

                Debug.Log($"GetBuildTimeDuration: {starSysFacilities} at {ourTechLevel} - Base: {timeDuration * speedMultiplier}, Adjusted: {timeDuration} (x{speedMultiplier:F2})");
            }

            return timeDuration;
        }

        internal void RegisterStarSysController(StarSysController starSysCon)
        {
            starSysCon.StarSysBuildManager = this;
        }
    }
}

