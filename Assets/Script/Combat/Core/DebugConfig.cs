namespace BOTF3D.Core
{
    public static class DebugConfig
    {
        // Toggle these in code or create an Inspector window
        public static bool LogFleetCreation = false;
        public static bool LogFogOfWar = false;
        public static bool LogShipDeployment = false;
        public static bool LogSystemCreation = false;
        public static bool LogUIEvents = false;

        // Master switch
        public static bool EnableAllDebugLogs = false;
    }
}

// Instead of:
// Debug.Log($"Fleet '{fleet.name}' visibility: {isVisible}");

// Use:
//if (DebugConfig.LogFogOfWar || DebugConfig.EnableAllDebugLogs)
//    Debug.Log($"Fleet '{fleet.name}' visibility: {isVisible}");

//#if UNITY_EDITOR
//Debug.Log($"FleetManager: Fog grid NOT ready yet...");
//#endif