using BOTF3D.Core;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BOTF3D.Combat.Testing
{
    /// <summary>
    /// Records combat state for replay, debugging, and testing.
    /// Saves combat data as JSON for later analysis or bug reproduction.
    /// </summary>
    public class CombatRecorder : MonoBehaviour
    {
        [Header("Recording Settings")]
        public bool IsRecording = true;
        public bool AutoSaveOnEnd = true;
        public string RecordingName = "combat_recording";

        private CombatRecording currentRecording;
        private CombatController combatController;

        public void Initialize(CombatController controller)
        {
            combatController = controller;
            currentRecording = new CombatRecording
            {
                RecordingName = RecordingName,
                StartTime = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),
                InitialState = CaptureInitialState()
            };

            GameLogger.Log(GameLogger.LogCategory.Combat, $"📹 CombatRecorder initialized: {RecordingName}");
        }

        /// <summary>
        /// Record a turn result
        /// </summary>
        public void RecordTurn(TurnResult result)
        {
            if (!IsRecording || currentRecording == null) return;

            var turnSnapshot = new TurnSnapshot
            {
                TurnNumber = result.TurnNumber,
                SideOneOrder = result.SideOneOrder,
                SideTwoOrder = result.SideTwoOrder,
                SideOneDamageDealt = result.SideOneDamageDealt,
                SideTwoDamageDealt = result.SideTwoDamageDealt,
                SideOneRetreated = result.SideOneRetreated,
                SideTwoRetreated = result.SideTwoRetreated,
                ShipsDestroyed = new List<string>(result.ShipsDestroyed),
                SideOneShips = CaptureShipStates(combatController.CombatData.SideOneShipCons),
                SideTwoShips = CaptureShipStates(combatController.CombatData.SideTwoShipCons)
            };

            currentRecording.TurnSnapshots.Add(turnSnapshot);
            GameLogger.Log(GameLogger.LogCategory.Combat, $"📹 Recorded Turn {result.TurnNumber}");
        }

        /// <summary>
        /// Save recording to disk
        /// </summary>
        public void SaveRecording(string customName = null)
        {
            if (currentRecording == null)
            {
                GameLogger.Log(GameLogger.LogCategory.Combat, "⚠️ No recording to save");
                return;
            }

            string fileName = customName ?? $"{RecordingName}_{currentRecording.StartTime}.json";
            string path = GetRecordingPath(fileName);

            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonUtility.ToJson(currentRecording, true);
                File.WriteAllText(path, json);
                GameLogger.Log(GameLogger.LogCategory.Combat, $"✅ Combat recording saved: {path}");
            }
            catch (System.Exception e)
            {
                GameLogger.Log(GameLogger.LogCategory.Combat, $"❌ Failed to save recording: {e.Message}");
            }
        }

        /// <summary>
        /// Load recording from disk
        /// </summary>
        public static CombatRecording LoadRecording(string fileName)
        {
            string path = GetRecordingPath(fileName);

            if (!File.Exists(path))
            {
                GameLogger.Log(GameLogger.LogCategory.Combat, $"❌ Recording not found: {path}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                CombatRecording recording = JsonUtility.FromJson<CombatRecording>(json);
                GameLogger.Log(GameLogger.LogCategory.Combat, $"✅ Combat recording loaded: {path}");
                return recording;
            }
            catch (System.Exception e)
            {
                GameLogger.Log(GameLogger.LogCategory.Combat, $"❌ Failed to load recording: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get all available recordings
        /// </summary>
        public static List<string> GetAvailableRecordings()
        {
            string directory = GetRecordingsDirectory();
            List<string> recordings = new List<string>();

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                return recordings;
            }

            string[] files = Directory.GetFiles(directory, "*.json");
            foreach (string file in files)
            {
                recordings.Add(Path.GetFileName(file));
            }

            return recordings;
        }

        private CombatInitialState CaptureInitialState()
        {
            if (combatController?.CombatData == null) return new CombatInitialState();

            return new CombatInitialState
            {
                CombatID = combatController.CombatID,
                SideOneCiv = combatController.CombatData.CivEnumSideOne.ToString(),
                SideTwoCiv = combatController.CombatData.CivEnumSideTwo.ToString(),
                CombatType = combatController.CombatData.CombatType.ToString(),
                SideOneInitialOrder = combatController.CombatData.SideOneOrder,
                SideTwoInitialOrder = combatController.CombatData.SideTwoOrder,
                SideOneShips = CaptureShipStates(combatController.CombatData.SideOneShipCons),
                SideTwoShips = CaptureShipStates(combatController.CombatData.SideTwoShipCons)
            };
        }

        private List<ShipSnapshot> CaptureShipStates(List<ShipController> ships)
        {
            List<ShipSnapshot> snapshots = new List<ShipSnapshot>();

            foreach (var ship in ships)
            {
                if (ship == null || ship.ShipData == null) continue;

                snapshots.Add(new ShipSnapshot
                {
                    ShipName = ship.ShipData.ShipName,
                    ShipType = ship.ShipData.ShipType.ToString(),
                    ShieldHealth = ship.ShipData.ShieldHealth,
                    HullHealth = ship.ShipData.HullHealth,
                    Position = ship.transform.position,
                    Rotation = ship.transform.rotation.eulerAngles,
                    IsDestroyed = ship.ShipData.Distroyed,
                    CurrentOrder = ship.Order,
                    TargetName = ship.ShipData.TargetThisShipController?.ShipData?.ShipName ?? "None"
                });
            }

            return snapshots;
        }

        private static string GetRecordingPath(string fileName)
        {
            return Path.Combine(GetRecordingsDirectory(), fileName);
        }

        private static string GetRecordingsDirectory()
        {
            return Path.Combine(Application.persistentDataPath, "CombatRecordings");
        }

        private void OnDestroy()
        {
            if (AutoSaveOnEnd && IsRecording && currentRecording != null)
            {
                SaveRecording();
            }
        }
    }

    // ===== DATA STRUCTURES FOR SERIALIZATION =====

    [System.Serializable]
    public class CombatRecording
    {
        public string RecordingName;
        public string StartTime;
        public CombatInitialState InitialState;
        public List<TurnSnapshot> TurnSnapshots = new List<TurnSnapshot>();
    }

    [System.Serializable]
    public class CombatInitialState
    {
        public int CombatID;
        public string SideOneCiv;
        public string SideTwoCiv;
        public string CombatType;
        public CombatOrders SideOneInitialOrder;
        public CombatOrders SideTwoInitialOrder;
        public List<ShipSnapshot> SideOneShips;
        public List<ShipSnapshot> SideTwoShips;
    }

    [System.Serializable]
    public class TurnSnapshot
    {
        public int TurnNumber;
        public CombatOrders SideOneOrder;
        public CombatOrders SideTwoOrder;
        public int SideOneDamageDealt;
        public int SideTwoDamageDealt;
        public bool SideOneRetreated;
        public bool SideTwoRetreated;
        public List<string> ShipsDestroyed;
        public List<ShipSnapshot> SideOneShips;
        public List<ShipSnapshot> SideTwoShips;
    }

    [System.Serializable]
    public class ShipSnapshot
    {
        public string ShipName;
        public string ShipType;
        public int ShieldHealth;
        public int HullHealth;
        public Vector3 Position;
        public Vector3 Rotation;
        public bool IsDestroyed;
        public CombatOrders CurrentOrder;
        public string TargetName;
    }
}
