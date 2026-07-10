using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;

namespace BOTF3D.Core
{
    /// <summary>
    /// Grows each owned system's Population every stardate based on its active Factories and
    /// ResearchCenters, then converts accumulated Population into GroundForce units up to the
    /// system's MaxGroundForceUnits cap (see StarSysManager.DetermineMaxGroundForceUnits).
    /// Replenishment falls out of the same conversion step: if GroundForces were lost in combat,
    /// the next tick re-fields units toward the population-supported target automatically.
    /// </summary>
    public class PopulationManager : MonoBehaviour, IManager
    {
        public void Initialize() { }
        public void Cleanup() { }
        public static PopulationManager Instance;

        [SerializeField] private int baseGrowthPerTurn = 1;
        [SerializeField] private int growthPerActiveFactory = 1;
        [SerializeField] private int growthPerActiveResearchCenter = 1;

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

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnStardateChanged += OnStardateChanged;
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnStardateChanged -= OnStardateChanged;
        }

        private void OnStardateChanged()
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
            int growth = baseGrowthPerTurn
                + activeFactories * growthPerActiveFactory
                + activeResearchCenters * growthPerActiveResearchCenter;

            sysData.Population = Mathf.Min(sysData.MaxPopulation, sysData.Population + growth);

            int targetGroundForces = Mathf.Clamp(
                sysData.Population / GroundForceData.PopulationPerUnit, 0, sysData.MaxGroundForceUnits);

            while (sysData.GroundForces.Count < targetGroundForces)
                StarSysManager.Instance.AddGroundForceUnit(sysCon);
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
