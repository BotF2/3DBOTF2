using UnityEngine;
using BOTF3D.Core;

namespace BOTF3D.Config
{
    /// <summary>
    /// Central game configuration ScriptableObject
    /// Stores all tunable game parameters in one place for easy balancing
    /// Create via: Assets > Create > BOTF3D > Game Config
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "BOTF3D/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Debug Settings")]
        [Tooltip("Enable/disable all debug logging")]
        public bool enableDebugLogs = true;

        [Tooltip("Enable detailed combat logging")]
        public bool enableCombatDebug = false;

        [Tooltip("Enable AI decision logging")]
        public bool enableAIDebug = false;

        [Tooltip("Show fog of war debug visualization")]
        public bool showFogOfWarDebug = false;

        [Header("Performance Settings")]
        [Tooltip("Maximum ships allowed per combat encounter")]
        [Range(10, 50)]
        public int maxShipsPerCombat = 20;

        [Tooltip("Combat simulation time scale")]
        [Range(0.5f, 3f)]
        public float combatTimeScale = 1f;

        [Tooltip("Enable object pooling for projectiles")]
        public bool useProjectilePooling = true;

        [Tooltip("Max pooled projectiles per weapon type")]
        public int maxPooledProjectiles = 50;

        [Header("Combat Balance")]
        [Tooltip("Base weapon damage multiplier")]
        [Range(0.5f, 2f)]
        public float weaponDamageMultiplier = 1f;

        [Tooltip("Base shield strength multiplier")]
        [Range(0.5f, 2f)]
        public float shieldStrengthMultiplier = 1f;

        [Tooltip("Combat retreat allowed")]
        public bool allowCombatRetreat = true;

        [Tooltip("Disable random orders for Side Two in combat (for testing)")]
        public bool disableRandomSideTwoOrders = false;

        [Header("Galaxy Settings")]
        [Tooltip("Research points per turn base value")]
        public int baseResearchPerTurn = 10;

        [Tooltip("Fleet maintenance cost multiplier")]
        [Range(0.5f, 2f)]
        public float fleetMaintenanceMultiplier = 1f;

        [Header("UI Settings")]
        [Tooltip("Show tooltips on hover")]
        public bool enableTooltips = true;

        [Tooltip("Tooltip delay in seconds")]
        [Range(0f, 2f)]
        public float tooltipDelay = 0.5f;

        [Tooltip("Enable tutorial hints")]
        public bool enableTutorials = true;

        [Header("Audio Settings")]
        [Tooltip("Master volume")]
        [Range(0f, 1f)]
        public float masterVolume = 1f;

        [Tooltip("Music volume")]
        [Range(0f, 1f)]
        public float musicVolume = 0.7f;

        [Tooltip("SFX volume")]
        [Range(0f, 1f)]
        public float sfxVolume = 0.8f;

        /// <summary>
        /// Apply logging settings to GameLogger
        /// </summary>
        public void ApplyLogSettings()
        {
            GameLogger.SetCategoryEnabled(GameLogger.LogCategory.Combat, enableCombatDebug);
            GameLogger.SetCategoryEnabled(GameLogger.LogCategory.AI, enableAIDebug);

            if (!enableDebugLogs)
            {
                GameLogger.DisableAll();
            }
        }
    }
}
