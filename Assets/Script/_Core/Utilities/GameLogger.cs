using UnityEngine;
using System.Collections.Generic;

namespace BOTF3D.Core
{
    /// <summary>
    /// Centralized logging system with category-based filtering
    /// Allows enabling/disabling logs per feature for easier debugging
    /// </summary>
    public static class GameLogger
    {
        public enum LogCategory
        {
            Combat,
            Diplomacy,
            Fleet,
            StarSystem,
            UI,
            Audio,
            Networking,
            Save,
            AI,
            General
        }

        private static Dictionary<LogCategory, bool> enabledCategories = new Dictionary<LogCategory, bool>
        {
            { LogCategory.Combat, true },
            { LogCategory.Diplomacy, true },
            { LogCategory.Fleet, true },
            { LogCategory.StarSystem, true },
            { LogCategory.UI, true },
            { LogCategory.Audio, false },
            { LogCategory.Networking, true },
            { LogCategory.Save, true },
            { LogCategory.AI, false },
            { LogCategory.General, true }
        };

        private static Dictionary<LogCategory, Color> categoryColors = new Dictionary<LogCategory, Color>
        {
            { LogCategory.Combat, Color.red },
            { LogCategory.Diplomacy, Color.blue },
            { LogCategory.Fleet, Color.green },
            { LogCategory.StarSystem, Color.yellow },
            { LogCategory.UI, Color.cyan },
            { LogCategory.Audio, Color.magenta },
            { LogCategory.Networking, Color.white },
            { LogCategory.Save, new Color(1f, 0.5f, 0f) }, // orange
            { LogCategory.AI, new Color(0.5f, 0f, 0.5f) }, // purple
            { LogCategory.General, Color.white }
        };

        /// <summary>
        /// Enable or disable logging for a specific category
        /// </summary>
        public static void SetCategoryEnabled(LogCategory category, bool enabled)
        {
            enabledCategories[category] = enabled;
        }

        /// <summary>
        /// Check if a category is enabled
        /// </summary>
        public static bool IsCategoryEnabled(LogCategory category)
        {
            return enabledCategories.TryGetValue(category, out bool enabled) && enabled;
        }

        /// <summary>
        /// Log a regular message with category
        /// </summary>
        public static void Log(LogCategory category, string message, Object context = null)
        {
            if (!IsCategoryEnabled(category)) return;

            string coloredMessage = FormatMessage(category, message);
            Debug.Log(coloredMessage, context);
        }

        /// <summary>
        /// Log a warning message with category
        /// </summary>
        public static void LogWarning(LogCategory category, string message, Object context = null)
        {
            if (!IsCategoryEnabled(category)) return;

            string coloredMessage = FormatMessage(category, message);
            Debug.LogWarning(coloredMessage, context);
        }

        /// <summary>
        /// Log an error message with category - always shown regardless of category setting
        /// </summary>
        public static void LogError(LogCategory category, string message, Object context = null)
        {
            string coloredMessage = FormatMessage(category, message);
            Debug.LogError(coloredMessage, context);
        }

        private static string FormatMessage(LogCategory category, string message)
        {
            Color color = categoryColors[category];
            string hexColor = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{hexColor}>[{category}]</color> {message}";
        }

        /// <summary>
        /// Enable all logging categories
        /// </summary>
        public static void EnableAll()
        {
            foreach (LogCategory category in System.Enum.GetValues(typeof(LogCategory)))
            {
                enabledCategories[category] = true;
            }
        }

        /// <summary>
        /// Disable all logging categories except errors
        /// </summary>
        public static void DisableAll()
        {
            foreach (LogCategory category in System.Enum.GetValues(typeof(LogCategory)))
            {
                enabledCategories[category] = false;
            }
        }
    }
}
