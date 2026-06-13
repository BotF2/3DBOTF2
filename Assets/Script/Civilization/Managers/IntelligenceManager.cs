using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;

namespace BOTF3D.Civilization
{
    public class IntelligenceManager : MonoBehaviour, IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        public static IntelligenceManager Instance;

        [SerializeField] private GameObject intelligenceUIPrefab;
        public List<IntelligenceController> IntelligenceControllerList { get; private set; } = new List<IntelligenceController>();

        // Espionage tuning constants
        private const float BaseSuccessChance   = 0.65f;
        private const float BonusPerExtraIntel  = 0.05f;  // per 25 intel above minimum cost
        private const float SameTechLevelPenalty = 0.10f; // penalty when targeting a playable civ at same tech

        private void Awake()
        {
            ServiceLocator.Register<IntelligenceManager>(this);
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

        // ─── Contact tracking ────────────────────────────────────────────────────

        public void InitializeNewIntelligenceController(CivController civSideOne, FleetController fleetControllerSideOne,
            CivController civSideTwo, FleetController fleetControllerSideTwo, StarSysController sysCon)
        {
            IntelligenceData intelData = new IntelligenceData(fleetControllerSideOne, fleetControllerSideTwo, sysCon);

            if (civSideOne.CivData.CivEnum <= CivEnum.TERRAN || civSideTwo.CivData.CivEnum <= CivEnum.TERRAN)
            {
                intelData.CivSideOne = civSideOne.CivData.CivEnum;
                intelData.LastSeenFleetOfSideOne = fleetControllerSideOne;
                if (fleetControllerSideOne?.FleetData?.ShipsList != null)
                    intelData.LastSeenFleetOfSideOne.FleetData.ShipsList = fleetControllerSideOne.FleetData.ShipsList;

                intelData.CivSideTwo = civSideTwo.CivData.CivEnum;
                intelData.LastSeenFleetOfSideTwo = fleetControllerSideTwo;
                if (fleetControllerSideTwo?.FleetData?.ShipsList != null)
                    intelData.LastSeenFleetOfSideTwo.FleetData.ShipsList = fleetControllerSideTwo.FleetData.ShipsList;

                intelData.LastSeenStarSysController = sysCon;
            }

            var ic = new IntelligenceController(intelData);
            IntelligenceControllerList.Add(ic);
        }

        /// <summary>
        /// Returns all CivEnums the given player civ has made first contact with.
        /// </summary>
        public List<CivEnum> GetContactedCivEnums(CivEnum playerCiv)
        {
            var result = new List<CivEnum>();
            foreach (var ic in IntelligenceControllerList)
            {
                if (ic.IntelligenceData.CivSideOne == playerCiv)
                    result.Add(ic.IntelligenceData.CivSideTwo);
                else if (ic.IntelligenceData.CivSideTwo == playerCiv)
                    result.Add(ic.IntelligenceData.CivSideOne);
            }
            return result;
        }

        public bool FoundAnIntelController(CivController civPartyOne, CivController civPartyTwo)
        {
            foreach (var ic in IntelligenceControllerList)
            {
                if ((ic.IntelligenceData.CivSideOne == civPartyOne.CivData.CivEnum &&
                     ic.IntelligenceData.CivSideTwo == civPartyTwo.CivData.CivEnum)
                 || (ic.IntelligenceData.CivSideTwo == civPartyOne.CivData.CivEnum &&
                     ic.IntelligenceData.CivSideOne == civPartyTwo.CivData.CivEnum))
                    return true;
            }
            return false;
        }

        public IntelligenceController ReturnAnIntelligenceController(CivEnum civPartyOne, CivEnum civPartyTwo)
        {
            foreach (var ic in IntelligenceControllerList)
            {
                if ((ic.IntelligenceData.CivSideOne == civPartyOne && ic.IntelligenceData.CivSideTwo == civPartyTwo)
                 || (ic.IntelligenceData.CivSideOne == civPartyTwo && ic.IntelligenceData.CivSideTwo == civPartyOne))
                    return ic;
            }
            return null;
        }

        public void OpenIntelligenceUI(CivEnum civPartyOne, CivEnum civPartyTwo)
        {
            var ic = ReturnAnIntelligenceController(civPartyOne, civPartyTwo);
            if (ic == null) return;

            if (GameController.Instance.AreWeLocalPlayer(civPartyOne))
            {
                ic.IntelligenceData.CivSideOne = civPartyOne;
                ic.IntelligenceData.CivSideTwo = civPartyTwo;
            }
            else if (GameController.Instance.AreWeLocalPlayer(civPartyTwo))
            {
                ic.IntelligenceData.CivSideOne = civPartyTwo;
                ic.IntelligenceData.CivSideTwo = civPartyOne;
            }
        }

        // ─── Per-turn intel generation (call from turn-end processing) ───────────

        /// <summary>
        /// Called once per game turn for each playable civilization.
        /// Generates intel points and advances/resolves active espionage projects.
        /// WIRE UP: call this from your turn-end system for every CivController in play.
        /// </summary>
        public void ProcessEspionageTurn(CivController civCon)
        {
            if (civCon?.CivData == null || !civCon.CivData.Playable) return;

            GenerateIntelPoints(civCon);
            AdvanceEspionageProjects(civCon);
        }

        private void GenerateIntelPoints(CivController civCon)
        {
            var data = civCon.CivData;
            float rate = data.IntelPointsPerTurn;

            // +1 per active Research Center per turn (intel agencies piggyback on research infrastructure)
            if (data.StarSysWeOwn != null)
            {
                foreach (var sys in data.StarSysWeOwn)
                {
                    if (sys?.StarSysData?.ResearchCenters == null) continue;
                    foreach (var rc in sys.StarSysData.ResearchCenters)
                    {
                        if (rc != null && rc.GetComponent<TMPro.TextMeshProUGUI>()?.text == "1")
                            rate += 1f;
                    }
                }
            }

            data.IntelPoints += rate;
            Debug.Log($"🔍 {data.CivShortName}: +{rate:F1} intel → total {data.IntelPoints:F0}");
        }

        // ─── Espionage project launch ────────────────────────────────────────────

        /// <summary>
        /// Validates and launches a StealTechnology espionage project.
        /// Returns false with a reason string if the project cannot start.
        /// </summary>
        public bool TryLaunchEspionageProject(CivController agentCiv, CivEnum targetCivEnum, out string reason)
        {
            reason = string.Empty;
            var agentData = agentCiv?.CivData;
            if (agentData == null) { reason = "Invalid civilization."; return false; }

            // Intel cost check
            if (agentData.IntelPoints < EspionageProject.IntelCost)
            {
                reason = $"Insufficient Intel Points. Need {EspionageProject.IntelCost}, have {(int)agentData.IntelPoints}.";
                return false;
            }

            // Must have made contact with target
            if (!GetContactedCivEnums(agentData.CivEnum).Contains(targetCivEnum))
            {
                reason = "No contact with that civilization.";
                return false;
            }

            // Resolve target data
            var targetCivCon = CivManager.Instance?.GetCivControllerByCivEnum(targetCivEnum);
            var targetData   = targetCivCon?.CivData;
            if (targetData == null) { reason = "Target civilization data unavailable."; return false; }

            // Non-warp targets yield nothing
            if (!targetData.HasWarp)
            {
                reason = $"{targetData.CivShortName} is not warp-capable. No technology to steal.";
                return false;
            }

            // Playable civ tech-level constraint: must be same level as us
            if (targetData.Playable && targetData.CurrentTechLevel != agentData.CurrentTechLevel)
            {
                reason = $"Cannot steal from {targetData.CivShortName}: they are {targetData.CurrentTechLevel}, we are {agentData.CurrentTechLevel}. Tech levels must match.";
                return false;
            }

            // Deduct intel points and queue project
            agentData.IntelPoints -= EspionageProject.IntelCost;
            var project = new EspionageProject(targetCivEnum, EspionageProjectType.StealTechnology);
            agentData.ActiveEspionageProjects.Add(project);

            reason = $"Project launched against {targetData.CivShortName}. Results in {EspionageProject.DurationTurns} turns.";
            Debug.Log($"🕵 {agentData.CivShortName}: espionage project launched vs {targetData.CivShortName} (cost {EspionageProject.IntelCost} intel)");
            return true;
        }

        // ─── Project resolution ─────────────────────────────────────────────────

        private void AdvanceEspionageProjects(CivController agentCiv)
        {
            var agentData = agentCiv.CivData;
            var completed = new List<EspionageProject>();

            foreach (var project in agentData.ActiveEspionageProjects)
            {
                if (project.IsComplete) { completed.Add(project); continue; }

                project.TurnsRemaining--;
                if (project.TurnsRemaining > 0) continue;

                // Project matures — resolve success or failure
                ResolveProject(agentCiv, project);
                completed.Add(project);
            }

            foreach (var p in completed)
                agentData.ActiveEspionageProjects.Remove(p);
        }

        private void ResolveProject(CivController agentCiv, EspionageProject project)
        {
            var agentData    = agentCiv.CivData;
            var targetCivCon = CivManager.Instance?.GetCivControllerByCivEnum(project.TargetCivEnum);
            var targetData   = targetCivCon?.CivData;

            float successChance = BaseSuccessChance;

            // Bonus intel beyond the base cost improves odds (caps at +30%)
            float extraIntel  = agentData.IntelPoints;  // remaining intel after the cost was paid at launch
            float intelBonus  = Mathf.Min(0.30f, (extraIntel / 25f) * BonusPerExtraIntel);
            successChance    += intelBonus;

            // Playable civ at same tech level is harder (their counter-intel is active)
            if (targetData != null && targetData.Playable)
                successChance -= SameTechLevelPenalty;

            successChance = Mathf.Clamp(successChance, 0.10f, 0.90f);
            bool success  = Random.value <= successChance;

            if (success)
            {
                int yield = ResolveTechYield(targetData);
                project.TechPointsStolen = yield;
                project.State = EspionageProjectState.Succeeded;
                agentData.AddTechPoints(yield);
                Debug.Log($"✅ Espionage SUCCESS: {agentData.CivShortName} stole {yield} tech pts from {targetData?.CivShortName ?? project.TargetCivEnum.ToString()}");
            }
            else
            {
                project.State = EspionageProjectState.Failed;
                Debug.Log($"❌ Espionage FAILED: {agentData.CivShortName} vs {targetData?.CivShortName ?? project.TargetCivEnum.ToString()} (chance was {successChance:P0})");
            }

            // Notify UI if open
            IntelligenceUIController.Instance?.OnProjectResolved(agentCiv.CivData.CivEnum, project);
        }

        private int ResolveTechYield(CivData targetData)
        {
            if (targetData == null) return 0;

            // Use SO-override if set; otherwise use lore table
            int baseYield = targetData.EspionageTechYield > 0
                ? targetData.EspionageTechYield
                : EspionageTechYieldTable.GetYield(targetData.CivShortName, targetData.HasWarp);

            if (baseYield <= 0) return 0;

            // Randomise ±25% around the base
            float roll = Random.Range(0.75f, 1.25f);
            return Mathf.Max(5, Mathf.RoundToInt(baseYield * roll));
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IntelligenceManager>();
        }
    }
}
