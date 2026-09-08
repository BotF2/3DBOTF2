using BOTF3D.Civilization;
using BOTF3D.Core;
using UnityEngine;

namespace BOTF3D.Galaxy
{
    /// <summary>
    /// Borg Transwarp Hub Network (TechEffectHook.TranswarpHubNetwork, Branch F Tier 5) and the
    /// shared Branch A Transwarp Access (TechEffectHook.AccessTranswarpHub, Tier 5) - "instant
    /// point-to-point travel between Borg-held systems" (TechTree_Phase2_Design.md §4 Branch A
    /// Tier-5 note, §8 II.3's "new TranswarpHubController for Borg").
    ///
    /// Scope note: a full hub-to-hub destination PICKER is future UI work (see
    /// FleetMenuUIController's own note on this) - every owned Borg system already doubles as a hub
    /// node (no separate hub facility object exists), so this first pass wires the one destination
    /// that needs no picker at all: instant recall to the civ's own home system. Any fleet sitting at
    /// a Borg-owned system can jump straight to Borg's own (also Borg-owned) home system. A non-Borg
    /// civ with Transwarp Access could use the same call once it's standing at a Borg system that
    /// isn't its own home - left for a later pass once a real hub-picker UI exists, since "jump to
    /// MY home system" doesn't generalize to "jump between someone ELSE's hub network" without one.
    /// </summary>
    public static class TranswarpHubController
    {
        /// <summary>
        /// True if fleet is eligible to instantly transwarp right now: its owning civ has researched
        /// Transwarp Hub Network, the fleet is currently docked/contacted at a Borg-owned system, and
        /// that civ has its own separate Borg-owned home system to jump to.
        /// </summary>
        public static bool CanTranswarpHome(FleetController fleet, out StarSysController homeSysCon)
        {
            homeSysCon = null;
            if (fleet?.FleetData == null) return false;

            CivController civ = CivManager.Instance?.GetCivControllerByCivEnum(fleet.FleetData.CivEnum);
            if (civ?.CivData?.Effects == null || !civ.CivData.Effects.TranswarpHubNetwork) return false;
            if (fleet.FleetData.CivEnum != CivEnum.BORG) return false; // §4 scope note above

            homeSysCon = civ.CivData.StarSysWeOwn?.Find(s =>
                s != null && s.StarSysData.SysName == civ.CivData.CivHomeSystemName);
            if (homeSysCon == null) return false;

            // Already there - nothing to jump to.
            if (fleet.FleetData.DockedStarSys == homeSysCon) return false;

            // Must currently be docked at a Borg-owned system - the network only carries a fleet
            // between hub nodes, it doesn't pull one in from open space.
            StarSysController currentSys = fleet.FleetData.DockedStarSys;
            if (currentSys == null || currentSys.StarSysData.CurrentOwnerCivEnum != CivEnum.BORG) return false;

            return true;
        }

        /// <summary>
        /// Performs the jump: repositions the fleet's transform directly at the destination and
        /// clears its warp state, the same end-state a normal arrival leaves it in. Server-authoritative
        /// caller's responsibility (see FleetMenuUIController.ClickTranswarpButton's own comment on the
        /// Cmd relay every other fleet-order button in this file already uses).
        /// </summary>
        public static bool TryTranswarpHome(FleetController fleet)
        {
            if (!CanTranswarpHome(fleet, out StarSysController homeSysCon)) return false;

            fleet.FleetData.ReleaseDockSlotIfAny();
            fleet.transform.position = homeSysCon.StarSysData.GetPosition();
            fleet.FleetData.Position = fleet.transform.position;
            fleet.FleetData.CurrentWarpFactor = 0f;
            fleet.FleetData.Destination = null;

            Debug.Log($"🌀 TranswarpHubController: '{fleet.name}' ({fleet.FleetData.CivEnum}) jumped to '{homeSysCon.StarSysData.SysName}'.");
            return true;
        }
    }
}
