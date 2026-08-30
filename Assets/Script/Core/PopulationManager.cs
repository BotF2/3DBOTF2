using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;

namespace BOTF3D.Core
{
    /// <summary>
    /// Grows each owned system's Population once per TURN (see ProcessPopulationGrowthForAllCivs,
    /// called from TimeManager.ProcessTurnEvents alongside research/dilithium/antimatter/repairs -
    /// every other turn-boundary system already lives there, so population growth follows the same
    /// "ProcessTurnEvents explicitly calls each system" shape instead of self-subscribing to a
    /// different, more frequent clock) based on its active Factories and ResearchCenters, then
    /// converts accumulated Population into GroundForce units up to the system's
    /// MaxGroundForceUnits cap (see StarSysManager.DetermineMaxGroundForceUnits). Replenishment
    /// falls out of the same conversion step: if GroundForces were lost in combat, the next turn
    /// re-fields units toward the population-supported target automatically.
    /// See Docs/Design/TurnAndStardateFlow.md for the full stardate/turn model this fits into.
    /// </summary>
    public class PopulationManager : MonoBehaviour, IManager
    {
        public void Initialize() { }
        public void Cleanup() { }
        public static PopulationManager Instance;

        // Values are fractional population units per TURN; sub-1 growth accumulates in
        // StarSysData.PopulationGrowthAccumulator until it crosses a whole unit. With one active
        // Factory + one active ResearchCenter this defaults to 0.3/turn, i.e. just under 1
        // population unit every 3 turns - the same overall pace as the old per-stardate values
        // (0.02/0.005/0.005) multiplied by TimeManager.StarDatesPerTurn (10), since this now ticks
        // 10x less often than it used to.
        [SerializeField] private float baseGrowthPerTurn = 0.2f;
        [SerializeField] private float growthPerActiveFactory = 0.05f;
        [SerializeField] private float growthPerActiveResearchCenter = 0.05f;

        private void Awake()
        {
            ServiceLocator.Register<PopulationManager>(this);
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

        /// <summary>
        /// Grows every civ's owned systems by one turn's worth of population. Called from
        /// TimeManager.ProcessTurnEvents at the turn boundary - not self-subscribed to any event,
        /// so it runs exactly once per turn in lockstep with research/dilithium/antimatter/repairs.
        /// </summary>
        public void ProcessPopulationGrowthForAllCivs()
        {
            foreach (var civ in CivManager.Instance.CivControllersInGame)
            {
                if (civ?.CivData?.StarSysWeOwn == null) continue;

                foreach (var sysCon in civ.CivData.StarSysWeOwn)
                {
                    if (sysCon?.StarSysData == null) continue;
                    GrowSystem(sysCon);
                }
            }
        }

        private void GrowSystem(StarSysController sysCon)
        {
            var sysData = sysCon.StarSysData;
            if (sysData.MaxPopulation <= 0) return;

            int activeFactories = CountActive(sysData.Factories);
            int activeResearchCenters = CountActive(sysData.ResearchCenters);
            float growthRate = baseGrowthPerTurn
                + activeFactories * growthPerActiveFactory
                + activeResearchCenters * growthPerActiveResearchCenter;

            // Growth rates are fractional (see field comments above), so accumulate the remainder between
            // turns rather than truncating it away each tick.
            sysData.PopulationGrowthAccumulator += growthRate;
            int growth = Mathf.FloorToInt(sysData.PopulationGrowthAccumulator);
            if (growth <= 0) return;
            sysData.PopulationGrowthAccumulator -= growth;

            // Population + GroundForces.Count together make up the system's total population (see
            // StarSysUI_Fields.InitializeFromStarSysData), so growth and the MaxPopulation cap apply to
            // that combined total, not to civilian Population alone.
            int totalPopulation = sysData.Population + sysData.GroundForces.Count;
            totalPopulation = Mathf.Min(sysData.MaxPopulation, totalPopulation + growth);
            sysData.Population = totalPopulation - sysData.GroundForces.Count;

            int targetGroundForces = Mathf.Clamp(
                totalPopulation / GroundForceData.PopulationPerUnit, 0, sysData.MaxGroundForceUnits);

            // Newly fielded ground forces are carved out of civilian Population, not added on top of
            // it, so the combined total stays exactly equal to totalPopulation above.
            while (sysData.GroundForces.Count < targetGroundForces
                && sysData.Population >= GroundForceData.PopulationPerUnit)
            {
                StarSysManager.Instance.AddGroundForceUnit(sysCon);
                sysData.Population -= GroundForceData.PopulationPerUnit;
            }
        }

        private int CountActive(List<GameObject> facilities)
        {
            if (facilities == null) return 0;

            int count = 0;
            foreach (var facility in facilities)
            {
                if (facility?.GetComponent<TMPro.TextMeshProUGUI>()?.text == "1")
                    count++;
            }
            return count;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<PopulationManager>();
        }
    }
}
