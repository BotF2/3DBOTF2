using BOTF3D.GamePlay;
using BOTF3D.UI;
using UnityEngine;

namespace BOTF3D.Core
{
    public class TechManager : MonoBehaviour
    {
        public static TechManager Instance;

        [SerializeField] private int techPointsPerResearchCenterPerTurn = 1;

        private void Awake()
        {
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
            // Each stardate, add tech points from all research centers
            foreach (var civ in CivManager.Instance.CivControllersInGame)
            {
                if (civ?.CivData == null) continue;

                int activeResearchCenters = CountActiveResearchCenters(civ);

                // ✅ Apply tech level multiplier to research output
                float researchMultiplier = GetResearchOutputMultiplier(civ.CivData.CurrentTechLevel);
                int basePoints = activeResearchCenters * techPointsPerResearchCenterPerTurn;
                int techPointsGained = Mathf.RoundToInt(basePoints * researchMultiplier);

                if (techPointsGained > 0)
                {
                    civ.CivData.TechPoints += techPointsGained;
                    Debug.Log($"{civ.CivData.CivShortName}: +{techPointsGained} tech points " +
                             $"({activeResearchCenters} centers × {researchMultiplier:F1}x = total: {civ.CivData.TechPoints})");

                    // Check for tech level advancement
                    CheckTechLevelAdvancement(civ);
                }
            }
        }
        private void OnTechLevelAdvanced(CivController civ, TechLevel newLevel)
        {
            Debug.Log($"🎉 {civ.CivData.CivLongName} advanced to {newLevel}!");

            // Trigger event for UI and other systems
            OnTechAdvanced?.Invoke(civ, newLevel);

            // ✅ Add null check for TechNotificationUI
            if (GameController.Instance.AreWeLocalPlayer(civ.CivData.CivEnum))
            {
                // Option A: If TechNotificationUI exists
                if (TechNotificationUI.Instance != null)
                {
                    TechNotificationUI.Instance.ShowTechAdvancement(civ, newLevel);
                }

                // Option B: If TechNotificationUI doesn't exist yet, comment out
                // TechNotificationUI.Instance?.ShowTechAdvancement(civ, newLevel);
                Debug.Log($"🎉 {civ.CivData.CivLongName} advanced to {newLevel}!");

                // Trigger event for UI and other systems
                OnTechAdvanced?.Invoke(civ, newLevel);

                // Show notification to local player
                if (GameController.Instance.AreWeLocalPlayer(civ.CivData.CivEnum))
                {
                    TechNotificationUI.Instance?.ShowTechAdvancement(civ, newLevel);
                }
                Debug.Log($"📢 PLAYER NOTIFICATION: You advanced to {newLevel}!");
            }
        }
        private int CountActiveResearchCenters(CivController civ)
        {
            int count = 0;

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

        private void CheckTechLevelAdvancement(CivController civ)
        {
            TechLevel newLevel = GetTechLevelForPoints(civ.CivData.TechPoints);

            if (newLevel > civ.CivData.CurrentTechLevel)
            {
                Debug.Log($"🎉 {civ.CivData.CivLongName} advanced to {newLevel}!");
                civ.CivData.CurrentTechLevel = newLevel;

                // Trigger UI notification
                OnTechLevelAdvanced(civ, newLevel);
            }
        }

        private TechLevel GetTechLevelForPoints(int points)
        {
            if (points >= 600) return TechLevel.SUPREME;
            if (points >= 300) return TechLevel.ADVANCED;
            if (points >= 100) return TechLevel.DEVELOPED;
            return TechLevel.EARLY;
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

            civ.CivData.TechPoints += points;
            Debug.Log($"{civ.CivData.CivShortName}: +{points} tech points (total: {civ.CivData.TechPoints})");

            CheckTechLevelAdvancement(civ);
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

                int activeResearchCenters = CountActiveResearchCenters(civ);

                if (activeResearchCenters > 0)
                {
                    // Apply tech level multiplier to research output
                    float researchMultiplier = GetResearchOutputMultiplier(civ.CivData.CurrentTechLevel);
                    int basePoints = activeResearchCenters * techPointsPerResearchCenterPerTurn;
                    int techPointsGained = Mathf.RoundToInt(basePoints * researchMultiplier);

                    civ.CivData.TechPoints += techPointsGained;
                    Debug.Log($"{civ.CivData.CivShortName}: +{techPointsGained} tech points " +
                             $"({activeResearchCenters} centers × {researchMultiplier:F1}x = total: {civ.CivData.TechPoints})");

                    CheckTechLevelAdvancement(civ);
                }
            }
        }
    }
}
