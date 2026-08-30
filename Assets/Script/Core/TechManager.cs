
using BOTF3D.UI;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;
using FischlWorks_FogWar;



namespace BOTF3D.Core
{
    public class TechManager : MonoBehaviour, IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        public static TechManager Instance;

        [SerializeField] private int techPointsPerResearchCenterPerTurn = 1;

        // Fog-of-war sight range grows in 7 discrete stages as a civ earns TechPoints, to read
        // as stepped sensor/scanner breakthroughs rather than smooth growth. Stage 1 (0 points,
        // start of EARLY) sits at ~1/3 of FleetManager.LocalPlayerFogSightRange; stage 7 (900
        // points, within SUPREME) sits at 2x - deliberately short of the ~1000-point effective
        // cap CivData.TechRating clamps to, so the final stage still reads as an earned reward
        // rather than an automatic end-state. Boundaries otherwise line up with
        // CivData.TechThresholds (0/100/300/600), each level split into one extra mid-level step.
        private static readonly (int TechPointsRequired, float Multiplier)[] fogSightRangeStages =
        {
            (0,   1f / 3f),
            (100, 0.6111f),
            (200, 0.8889f),
            (300, 1.1667f),
            (450, 1.4444f),
            (600, 1.7222f),
            (900, 2f),
        };

        /// <summary>
        /// Returns the fog-of-war sight range multiplier for the given TechPoints total,
        /// stepping up at each of the 7 stages defined in fogSightRangeStages.
        /// </summary>
        public float GetFogSightRangeMultiplier(int techPoints)
        {
            float multiplier = fogSightRangeStages[0].Multiplier;

            foreach (var stage in fogSightRangeStages)
            {
                if (techPoints >= stage.TechPointsRequired)
                    multiplier = stage.Multiplier;
            }

            return multiplier;
        }

        /// <summary>
        /// Returns the actual fog-of-war sight range (world units) for the given TechPoints
        /// total, scaling FleetManager.LocalPlayerFogSightRange by the current stage multiplier.
        /// </summary>
        public int GetFogSightRange(int techPoints)
        {
            return Mathf.RoundToInt(FleetManager.LocalPlayerFogSightRange * GetFogSightRangeMultiplier(techPoints));
        }

        // Cached so RefreshLocalPlayerFogSightRangeIfChanged only touches csFogWar (and forces
        // a recompute) on an actual stage change, not on every single TechPoints-mutating call.
        private int lastAppliedLocalFogSightRange = -1;

        /// <summary>
        /// Re-applies the local player's current fog sight range to every registered FogRevealer
        /// if their stage has changed since the last check. Every FogRevealer in the game belongs
        /// to the local player (see FleetManager.RegisterFleetControllerAndSetupVisuals and
        /// StarSysManager.InstantiateSystem - only local-player fleets/systems ever get one), so
        /// a stage change applies uniformly to all of them. Call this after any code path that
        /// may have changed the local player's TechPoints.
        /// </summary>
        private void RefreshLocalPlayerFogSightRangeIfChanged()
        {
            CivController localCiv = CivManager.Instance?.LocalPlayerCivController;
            if (localCiv?.CivData == null) return;

            int desiredSightRange = GetFogSightRange(localCiv.CivData.TechPoints);
            if (desiredSightRange == lastAppliedLocalFogSightRange) return;

            csFogWar fogWar = csFogWar.Instance;
            // Not ready yet (e.g. still mid-scene-load) - leave lastAppliedLocalFogSightRange
            // untouched so this retries on the next mutation instead of silently giving up.
            if (fogWar == null || !fogWar.FogReady) return;

            lastAppliedLocalFogSightRange = desiredSightRange;

            foreach (var revealer in fogWar._FogRevealers)
            {
                revealer.SetSightRange(desiredSightRange);
            }

            fogWar.ForceUpdateFog();
        }

        private void Awake()
        {
            ServiceLocator.Register<TechManager>(this);
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Tech is processed once per turn via TimeManager.ProcessTurnEvents → ProcessResearchForAllCivs.
        // No per-stardate hook needed; subscribing per-stardate would add points 10× per turn.

        // Minor races have no player driving their research choices, so - unlike the majors above -
        // they always earn at least a small trickle of TechPoints every turn regardless of whether
        // the (AI-managed) system ever gets around to building a Research Center. This is what keeps
        // a minor race's tech progressing on its own timeline instead of stalling at 0 forever if
        // StarSysAIManager's economy priorities never reach ResearchCenter for it (see
        // ResearchCenterGateFactories/ResearchCenterGateShipyards in StarSysAIManager - a fresh
        // colony-tier minor can go a long time before qualifying).
        private const int BaseMinorTechPerTurn = 1;

        /// <summary>
        /// Advances every non-playable (minor) civ's TechPoints by its own turn's research output -
        /// entirely independent of the local human player's own tech gain (that coupling used to
        /// exist here and made every minor race's tech rise and fall with the human's pace instead
        /// of its own economy). Two additive sources, both scaled by <see cref="CivSO.QualityScore"/>
        /// via <see cref="ShipStatCalculator.GetQualityScaleFactor"/> (the same 0.70x-1.30x curve
        /// used for that minor's ships) so a canonically advanced minor (e.g. Breen, Tholian)
        /// researches faster than a canonically primitive one (e.g. Pakled, Kazon) even before
        /// either has built a single facility:
        ///   • BaseMinorTechPerTurn - a flat passive trickle, always earned.
        ///   • Its own active Research Centers - same per-center rate and tech-level multiplier the
        ///     majors use (CountActiveResearchCenters/GetResearchOutputMultiplier), since minors are
        ///     AI-managed (StarSysAIManager) and can build Research Centers within their own
        ///     QualityScore-scaled facility cap just like any AI-run system.
        /// Pre-warp civs and uninhabited placeholders never progress (HasWarp gate, unchanged from
        /// before).
        /// </summary>
        private void ProcessMinorRaceResearch()
        {
            foreach (var civ in CivManager.Instance.CivControllersInGame)
            {
                if (civ?.CivData == null) continue;
                if (civ.CivData.Playable) continue;   // playable civs handled in the pass above
                if (!civ.CivData.HasWarp) continue;   // pre-warp and uninhabited placeholders do not progress

                TechLevel levelBefore = civ.CivData.CurrentTechLevel;
                float qualityScale = ShipStatCalculator.GetQualityScaleFactor(civ.CivData.QualityScore);
                float researchMultiplier = GetResearchOutputMultiplier(levelBefore);
                int activeResearchCenters = CountActiveResearchCenters(civ);

                int techPointsGained = Mathf.Max(1, Mathf.RoundToInt(
                    (BaseMinorTechPerTurn + activeResearchCenters * techPointsPerResearchCenterPerTurn * researchMultiplier)
                    * qualityScale));

                civ.CivData.TechPoints += techPointsGained;

                if (civ.CivData.CurrentTechLevel > levelBefore)
                    OnTechLevelAdvanced(civ, civ.CivData.CurrentTechLevel);
            }
        }
        private void OnTechLevelAdvanced(CivController civ, TechLevel newLevel)
        {
            Debug.Log($"{civ.CivData.CivLongName} advanced to {newLevel}!");
            OnTechAdvanced?.Invoke(civ, newLevel);

            // Immediately swap the shipyard palette (icons/build-times/locked state) if this
            // civ's build menu happens to be open right now - otherwise it wouldn't refresh
            // until the player closed and reopened it.
            TimeManager.Instance?.RefreshBuildUIsForCiv(civ.CivData.CivEnum);

            if (GameController.Instance.AreWeLocalPlayer(civ.CivData.CivEnum))
            {
                TechNotificationUI.Instance?.ShowTechAdvancement(civ, newLevel);
                Debug.Log($"PLAYER NOTIFICATION: You advanced to {newLevel}!");
            }
        }
        private int CountActiveResearchCenters(CivController civ)
        {
            int count = 0;

            // StarSysWeOwn is only assigned once a civ is given a home system
            // (CivManager.AddSystemToOwnSystemListAndHomeSys) - it stays null otherwise,
            // so guard it here the same way every other reader of this list does.
            if (civ.CivData.StarSysWeOwn == null) return count;

            foreach (var system in civ.CivData.StarSysWeOwn)
            {
                if (system?.StarSysData == null) continue;

                // Count research centers that are turned ON (text = "1")
                foreach (var researchCenter in system.StarSysData.ResearchCenters)
                {
                    if (researchCenter?.GetComponent<TMPro.TextMeshProUGUI>()?.text == "1")
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Returns build speed multiplier for factories based on tech level.
        /// Higher tech = faster building (lower build time).
        /// </summary>
        public float GetFactorySpeedMultiplier(TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.EARLY:
                    return 1.0f; // Base speed
                case TechLevel.DEVELOPED:
                    return 1.25f; // 25% faster
                case TechLevel.ADVANCED:
                    return 1.5f; // 50% faster
                case TechLevel.SUPREME:
                    return 2.0f; // 2x faster
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Returns build speed multiplier for shipyards based on tech level.
        /// </summary>
        public float GetShipyardSpeedMultiplier(TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.EARLY:
                    return 1.0f;
                case TechLevel.DEVELOPED:
                    return 1.3f; // 30% faster
                case TechLevel.ADVANCED:
                    return 1.6f; // 60% faster
                case TechLevel.SUPREME:
                    return 2.2f; // 2.2x faster (shipyards benefit more from tech)
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Returns build speed multiplier for other facilities (shields, batteries, research).
        /// </summary>
        public float GetFacilitySpeedMultiplier(TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.EARLY:
                    return 1.0f;
                case TechLevel.DEVELOPED:
                    return 1.2f; // 20% faster
                case TechLevel.ADVANCED:
                    return 1.4f; // 40% faster
                case TechLevel.SUPREME:
                    return 1.8f; // 80% faster
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Returns research output multiplier based on tech level.
        /// Higher tech = more efficient research centers.
        /// </summary>
        public float GetResearchOutputMultiplier(TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.EARLY:
                    return 1.0f;
                case TechLevel.DEVELOPED:
                    return 1.5f; // 50% more research output
                case TechLevel.ADVANCED:
                    return 2.0f; // 2x output
                case TechLevel.SUPREME:
                    return 3.0f; // 3x output
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Generic tech multiplier for stats (ship speed, weapons, shields, etc.)
        /// </summary>
        public float GetTechMultiplier(TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.EARLY:
                    return 1.0f; // Base stats
                case TechLevel.DEVELOPED:
                    return 1.15f; // +15%
                case TechLevel.ADVANCED:
                    return 1.35f; // +35%
                case TechLevel.SUPREME:
                    return 1.6f; // +60%
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Returns power efficiency multiplier based on tech level.
        /// Higher tech = more efficient power usage (lower consumption).
        /// </summary>
        public float GetPowerEfficiencyMultiplier(TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.EARLY:
                    return 1.0f; // Base power consumption
                case TechLevel.DEVELOPED:
                    return 0.9f; // 10% less power needed
                case TechLevel.ADVANCED:
                    return 0.8f; // 20% less power
                case TechLevel.SUPREME:
                    return 0.7f; // 30% less power
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Add research points to a civilization and check for advancement.
        /// Called by UI or other systems.
        /// </summary>
        public void AddResearchPoints(CivController civ, int points)
        {
            if (civ?.CivData == null) return;

            TechLevel levelBefore = civ.CivData.CurrentTechLevel;
            civ.CivData.TechPoints += points;
            Debug.Log($"{civ.CivData.CivShortName}: +{points} tech points (total: {civ.CivData.TechPoints})");

            if (civ.CivData.CurrentTechLevel > levelBefore)
                OnTechLevelAdvanced(civ, civ.CivData.CurrentTechLevel);

            RefreshLocalPlayerFogSightRangeIfChanged();
        }

        /// <summary>
        /// Returns progress (0-1) to the next tech level.
        /// </summary>
        public float GetProgressToNextLevel(CivData civData)
        {
            if (civData == null) return 0f;

            TechLevel currentLevel = civData.CurrentTechLevel;
            TechLevel nextLevel = GetNextTechLevel(currentLevel);

            if (nextLevel == currentLevel) // Already at max
                return 1f;

            int currentThreshold = CivData.TechThresholds[currentLevel];
            int nextThreshold = CivData.TechThresholds[nextLevel];
            int pointsNeeded = nextThreshold - currentThreshold;
            int pointsEarned = civData.TechPoints - currentThreshold;

            return Mathf.Clamp01((float)pointsEarned / pointsNeeded);
        }

        /// <summary>
        /// Gets the next tech level after the given one.
        /// </summary>
        private TechLevel GetNextTechLevel(TechLevel current)
        {
            switch (current)
            {
                case TechLevel.EARLY:
                    return TechLevel.DEVELOPED;
                case TechLevel.DEVELOPED:
                    return TechLevel.ADVANCED;
                case TechLevel.ADVANCED:
                    return TechLevel.SUPREME;
                case TechLevel.SUPREME:
                    return TechLevel.SUPREME; // Already at max
                default:
                    return TechLevel.EARLY;
            }
        }

        /// <summary>
        /// Event triggered when a civilization advances to a new tech level.
        /// Make this public so UI can subscribe.
        /// </summary>
        public event System.Action<CivController, TechLevel> OnTechAdvanced;

        /// <summary>
        /// Called when tech level advances - trigger event and notifications.
        /// </summary>
        //private void OnTechLevelAdvanced(CivController civ, TechLevel newLevel)
        //{
        //    Debug.Log($"🎉 {civ.CivData.CivLongName} advanced to {newLevel}!");

        //    // Trigger event for UI and other systems
        //    OnTechAdvanced?.Invoke(civ, newLevel);

        //    // Show notification to local player
        //    if (GameController.Instance.AreWeLocalPlayer(civ.CivData.CivEnum))
        //    {
        //        TechNotificationUI.Instance?.ShowTechAdvancement(civ, newLevel);
        //    }
        //}

        /// <summary>
        /// Manually process research for all civilizations (called each turn/stardate).
        /// </summary>
        public void ProcessResearchForAllCivs()
        {
            foreach (var civ in CivManager.Instance.CivControllersInGame)
            {
                if (civ?.CivData == null) continue;
                if (!civ.CivData.Playable) continue;

                int activeResearchCenters = CountActiveResearchCenters(civ);
                if (activeResearchCenters == 0) continue;

                TechLevel levelBefore    = civ.CivData.CurrentTechLevel;
                float researchMultiplier = GetResearchOutputMultiplier(levelBefore);
                int techPointsGained     = Mathf.RoundToInt(activeResearchCenters * techPointsPerResearchCenterPerTurn * researchMultiplier);

                civ.CivData.TechPoints += techPointsGained;
                Debug.Log($"{civ.CivData.CivShortName}: +{techPointsGained} tech points " +
                         $"({activeResearchCenters} centers × {researchMultiplier:F1}x = total: {civ.CivData.TechPoints})");

                if (civ.CivData.CurrentTechLevel > levelBefore)
                    OnTechLevelAdvanced(civ, civ.CivData.CurrentTechLevel);
            }

            // Simulated independently of the local player's own tech gain - see
            // ProcessMinorRaceResearch's comment for why that coupling was removed.
            ProcessMinorRaceResearch();

            RefreshLocalPlayerFogSightRangeIfChanged();
        }


        private void OnDestroy()
        {
            ServiceLocator.Unregister<TechManager>();
        }
}
}