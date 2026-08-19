using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Civilization;
using BOTF3D.UI;
using FischlWorks_FogWar;

namespace BOTF3D.Galaxy
{
    /// <summary>
    /// Watches enemy fleets that are currently visible through fog of war and reports when one
    /// is closing in on the local player's home system. Self-contained: attach to any active
    /// GameObject in GalaxyScene, no Inspector wiring required.
    /// </summary>
    public class HomeSystemThreatWatcher : MonoBehaviour
    {
        [SerializeField] private float checkIntervalSeconds = 2f;
        // Best-guess default - needs tuning in-Editor to the scene's actual world-space scale.
        [SerializeField] private float alertRadius = 250f;

        private readonly Dictionary<FleetController, float> lastDistance = new Dictionary<FleetController, float>();
        private readonly HashSet<FleetController> alreadyReported = new HashSet<FleetController>();
        private readonly List<FleetController> toForget = new List<FleetController>();

        private void Start()
        {
            InvokeRepeating(nameof(CheckApproachingFleets), checkIntervalSeconds, checkIntervalSeconds);
        }

        private void CheckApproachingFleets()
        {
            if (GameController.Instance == null || FleetManager.Instance == null || CivManager.Instance == null)
                return;

            CivEnum local = GameController.Instance.GetOurCiv();
            CivController localCiv = CivManager.Instance.GetCivControllerByCivEnum(local);
            if (localCiv?.CivData == null || string.IsNullOrEmpty(localCiv.CivData.CivHomeSystemName))
                return;

            Vector3 homePos = localCiv.CivData.HomeStarSystemPosition;
            string homeSystemName = localCiv.CivData.CivHomeSystemName;

            toForget.Clear();
            foreach (var tracked in lastDistance.Keys)
            {
                if (tracked == null || !FleetManager.Instance.FleetControllerList.Contains(tracked))
                    toForget.Add(tracked);
            }
            foreach (var forget in toForget)
            {
                lastDistance.Remove(forget);
                alreadyReported.Remove(forget);
            }

            foreach (FleetController fleetCon in FleetManager.Instance.FleetControllerList)
            {
                if (fleetCon == null || fleetCon.FleetData == null) continue;
                if (GameController.Instance.AreWeLocalPlayer(fleetCon.FleetData.CivEnum)) continue;

                csFogVisibilityAgent fogAgent = fleetCon.GetComponent<csFogVisibilityAgent>();
                bool visible = fogAgent != null && fogAgent.Visibility;
                if (!visible)
                {
                    lastDistance.Remove(fleetCon);
                    alreadyReported.Remove(fleetCon);
                    continue;
                }

                float distance = Vector3.Distance(fleetCon.FleetData.Position, homePos);
                bool hasPrevious = lastDistance.TryGetValue(fleetCon, out float previousDistance);
                lastDistance[fleetCon] = distance;

                bool withinAlertRadius = distance <= alertRadius;
                bool closingIn = hasPrevious && distance < previousDistance - 0.01f;

                if (withinAlertRadius && closingIn && !alreadyReported.Contains(fleetCon))
                {
                    alreadyReported.Add(fleetCon);
                    ReportApproach(fleetCon, homeSystemName, homePos);
                }
                else if (!withinAlertRadius)
                {
                    alreadyReported.Remove(fleetCon);
                }
            }
        }

        private static void ReportApproach(FleetController fleetCon, string homeSystemName, Vector3 homePos)
        {
            GalaxyQuadrant quadrant = ReportEntry.QuadrantFromPosition(homePos);
            string shortName = fleetCon.FleetData.CivShortName;
            string longName = fleetCon.FleetData.CivLongName;
            int stardate = TimeManager.Instance != null ? TimeManager.Instance.currentStardate : 0;

            ReportEntryUI.PushReport(new ReportEntry(ReportCategory.Combat, stardate,
                $"{shortName} fleet approaching {homeSystemName}",
                $"A {longName} fleet has been sighted closing on {homeSystemName}, your home system.",
                homeSystemName, quadrant, ReportSeverity.Warning));
        }
    }
}
