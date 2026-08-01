using BOTF3D.Core;
using System.Linq;
using UnityEngine;

namespace BOTF3D.Combat.Testing
{
    /// <summary>
    /// Helper class that prints combat testing information to console.
    /// Shows when any combat starts in the editor.
    /// </summary>
    public static class CombatTestingHelper
    {
        private static bool hasShownWelcome = false;

        /// <summary>
        /// Show welcome message with debug tools info (call from CombatController.Awake)
        /// </summary>
        public static void ShowWelcomeMessage()
        {
            if (hasShownWelcome) return;
            hasShownWelcome = true;

            GameLogger.Log(GameLogger.LogCategory.Combat,
                "╔════════════════════════════════════════════════════════╗\n" +
                "║         COMBAT DEBUG TOOLS ACTIVE                      ║\n" +
                "╠════════════════════════════════════════════════════════╣\n" +
                "║ • Press F1 → Toggle Debug UI                          ║\n" +
                "║ • Combat automatically recorded to JSON               ║\n" +
                "║ • Check Window > General > Test Runner for tests      ║\n" +
                "║ • BOTF > Combat Scenario Editor for quick setup       ║\n" +
                "╠════════════════════════════════════════════════════════╣\n" +
                "║ Debug UI Controls:                                     ║\n" +
                "║   - Skip Turn: Fast-forward to next turn              ║\n" +
                "║   - Side X Win: Instantly win for testing             ║\n" +
                "║   - End Combat: Return to galaxy immediately          ║\n" +
                "╠════════════════════════════════════════════════════════╣\n" +
                "║ Recordings Location:                                   ║\n" +
                "║   " + GetRecordingsPath() + "\n" +
                "╚════════════════════════════════════════════════════════╝"
            );
        }

        /// <summary>
        /// Print combat setup summary
        /// </summary>
        public static void PrintCombatSetup(CombatData combatData)
        {
            if (combatData == null) return;

            int s1Ships = combatData.SideOneShipCons?.Count ?? 0;
            int s2Ships = combatData.SideTwoShipCons?.Count ?? 0;

            GameLogger.Log(GameLogger.LogCategory.Combat,
                $"⚔️ COMBAT STARTING\n" +
                $"   Side 1: {combatData.CivEnumSideOne} ({s1Ships} ships) - Order: {combatData.SideOneOrder}\n" +
                $"   Side 2: {combatData.CivEnumSideTwo} ({s2Ships} ships) - Order: {combatData.SideTwoOrder}\n" +
                $"   Press F1 for debug overlay"
            );
        }

        /// <summary>
        /// Print turn summary
        /// </summary>
        public static void PrintTurnSummary(TurnResult result)
        {
            if (result == null) return;

            string destroyedList = result.ShipsDestroyed.Count > 0
                ? string.Join(", ", result.ShipsDestroyed)
                : "none";

            GameLogger.Log(GameLogger.LogCategory.Combat,
                $"📊 TURN {result.TurnNumber} RESULTS\n" +
                $"   Orders: {result.SideOneOrder} vs {result.SideTwoOrder}\n" +
                $"   Side 1 dealt: {result.SideOneDamageDealt} damage\n" +
                $"   Side 2 dealt: {result.SideTwoDamageDealt} damage\n" +
                $"   Ships destroyed ({result.ShipsDestroyed.Count}): {destroyedList}"
            );

            if (result.Shots != null && result.Shots.Count > 0)
            {
                var byCivWeapon = result.Shots
                    .GroupBy(s => (s.ShooterCiv, s.WeaponType))
                    .OrderBy(g => g.Key.ShooterCiv).ThenBy(g => g.Key.WeaponType);

                GameLogger.Log(GameLogger.LogCategory.Combat, $"🎯 SHOT LOG ({result.Shots.Count} total shots):");
                foreach (var group in byCivWeapon)
                {
                    int shotCount = group.Count();
                    int totalDamage = group.Sum(s => s.Damage);
                    int kills = group.Count(s => s.TargetDestroyed);
                    float firstShotTime = group.Min(s => s.TimeInTurn);
                    GameLogger.Log(GameLogger.LogCategory.Combat,
                        $"   {group.Key.ShooterCiv} {group.Key.WeaponType}: {shotCount} shots, {totalDamage} dmg, {kills} kills, first shot @ {firstShotTime:F1}s");
                }
            }
        }

        /// <summary>
        /// Print test recommendations based on current state
        /// </summary>
        public static void SuggestTestScenarios(CombatData combatData)
        {
            if (combatData == null) return;

            bool s1HasTransports = combatData.SideOneShipCons.Exists(s => s?.ShipData?.ShipType == ShipType.Transport);
            bool s2HasTransports = combatData.SideTwoShipCons.Exists(s => s?.ShipData?.ShipType == ShipType.Transport);

            GameLogger.Log(GameLogger.LogCategory.Combat, "💡 TEST SUGGESTIONS:");

            if (s1HasTransports || s2HasTransports)
            {
                GameLogger.Log(GameLogger.LogCategory.Combat, "   • Test AttackTransports vs Formation (transport protection)");
            }

            if (combatData.SideOneOrder == combatData.SideTwoOrder)
            {
                GameLogger.Log(GameLogger.LogCategory.Combat, "   • Try asymmetric orders (Rush vs Formation, etc.)");
            }

            GameLogger.Log(GameLogger.LogCategory.Combat, "   • Use 'Skip Turn' (F1 menu) to test multiple rounds quickly");
            GameLogger.Log(GameLogger.LogCategory.Combat, "   • Check combat recording after to analyze damage over time");
        }

        /// <summary>
        /// Get recordings folder path
        /// </summary>
        private static string GetRecordingsPath()
        {
            return System.IO.Path.Combine(Application.persistentDataPath, "CombatRecordings");
        }

        /// <summary>
        /// Open recordings folder in file explorer
        /// </summary>
        public static void OpenRecordingsFolder()
        {
            string path = GetRecordingsPath();

            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            System.Diagnostics.Process.Start("explorer.exe", path.Replace("/", "\\"));
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            System.Diagnostics.Process.Start("open", path);
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            System.Diagnostics.Process.Start("xdg-open", path);
#endif

            GameLogger.Log(GameLogger.LogCategory.Combat, $"📂 Opened recordings folder: {path}");
        }

        /// <summary>
        /// List all available recordings
        /// </summary>
        public static void ListRecordings()
        {
            var recordings = CombatRecorder.GetAvailableRecordings();

            if (recordings.Count == 0)
            {
                GameLogger.Log(GameLogger.LogCategory.Combat, "📁 No combat recordings found");
                return;
            }

            GameLogger.Log(GameLogger.LogCategory.Combat, $"📁 Found {recordings.Count} combat recordings:");
            foreach (var recording in recordings)
            {
                GameLogger.Log(GameLogger.LogCategory.Combat, $"   • {recording}");
            }
            GameLogger.Log(GameLogger.LogCategory.Combat, $"   Location: {GetRecordingsPath()}");
        }

        /// <summary>
        /// Print quick reference guide
        /// </summary>
        public static void PrintQuickGuide()
        {
            GameLogger.Log(GameLogger.LogCategory.Combat,
                "🎮 COMBAT TESTING QUICK GUIDE\n\n" +
                "IN-GAME CONTROLS:\n" +
                "  F1                  → Toggle debug overlay\n" +
                "  Debug UI: Skip Turn → Fast-forward testing\n" +
                "  Debug UI: Force Win → Test victory conditions\n\n" +
                "UNITY EDITOR TOOLS:\n" +
                "  BOTF > Combat Scenario Editor → Quick combat setup\n" +
                "  Window > Test Runner          → Run unit tests\n\n" +
                "RECORDINGS:\n" +
                "  Auto-saved to: " + GetRecordingsPath() + "\n" +
                "  Load/analyze: CombatRecorder.LoadRecording(filename)\n\n" +
                "TESTING WORKFLOW:\n" +
                "  1. Make code change\n" +
                "  2. Run unit tests (Test Runner)\n" +
                "  3. Create scenario (Scenario Editor)\n" +
                "  4. Use Debug UI (F1) to monitor\n" +
                "  5. Check recording for detailed analysis\n\n" +
                "For full documentation, see:\n" +
                "  Assets/Script/Combat/Testing/README_COMBAT_TESTING.md"
            );
        }
    }
}
