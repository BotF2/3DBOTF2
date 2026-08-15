using BOTF3D.Core;
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Tracks all ships in the game across both galaxy and combat contexts.
    /// Maintains scene-specific dictionaries for fast lookups.
    /// </summary>
    public class ShipRegistry
    {
        // Global tracking
        public List<ShipData> AllShipsData { get; private set; } = new List<ShipData>();
        public List<ShipController> AllShipControllers { get; private set; } = new List<ShipController>();

        // Scene-specific tracking (cleared when scenes unload)
        private Dictionary<int, ShipController> galaxyShipControllers = new Dictionary<int, ShipController>();
        private Dictionary<int, ShipController> combatShipControllers = new Dictionary<int, ShipController>();

        /// <summary>
        /// Register a ship controller when it's instantiated in galaxy
        /// </summary>
        public void RegisterGalaxyShip(ShipController shipController)
        {
            if (shipController?.ShipData != null)
            {
                int shipId = shipController.ShipData.ShipName.GetHashCode();
                galaxyShipControllers[shipId] = shipController;

                // Ensure ShipData is tracked globally
                if (!AllShipsData.Contains(shipController.ShipData))
                {
                    AllShipsData.Add(shipController.ShipData);
                }

                if (!AllShipControllers.Contains(shipController))
                {
                    AllShipControllers.Add(shipController);
                }

                Debug.Log($"ShipRegistry: Registered galaxy ship {shipController.name} (ID={shipId})");
            }
        }

        /// <summary>
        /// Register a ship controller when it's instantiated in combat
        /// </summary>
        public void RegisterCombatShip(ShipController shipController)
        {
            if (shipController?.ShipData != null)
            {
                int shipId = shipController.ShipData.ShipName.GetHashCode();
                combatShipControllers[shipId] = shipController;
                Debug.Log($"ShipRegistry: Registered combat ship {shipController.name} (ID={shipId})");
            }
        }

        /// <summary>
        /// Unregister a ship from galaxy tracking
        /// </summary>
        public void UnregisterGalaxyShip(int shipId)
        {
            galaxyShipControllers.Remove(shipId);
            Debug.Log($"ShipRegistry: Unregistered galaxy ship (ID={shipId})");
        }

        /// <summary>
        /// Unregister a ship from combat tracking
        /// </summary>
        public void UnregisterCombatShip(int shipId)
        {
            combatShipControllers.Remove(shipId);
            Debug.Log($"ShipRegistry: Unregistered combat ship (ID={shipId})");
        }

        /// <summary>
        /// Get galaxy ship controller by ID
        /// </summary>
        public ShipController GetGalaxyShipController(int shipId)
        {
            galaxyShipControllers.TryGetValue(shipId, out ShipController controller);
            return controller;
        }

        /// <summary>
        /// Get combat ship controller by ID
        /// </summary>
        public ShipController GetCombatShipController(int shipId)
        {
            combatShipControllers.TryGetValue(shipId, out ShipController controller);
            return controller;
        }

        /// <summary>
        /// Get ship data by ID (always available)
        /// </summary>
        public ShipData GetShipData(int shipId)
        {
            return AllShipsData.Find(s => s.ShipName.GetHashCode() == shipId);
        }

        /// <summary>
        /// Find a ship controller by its stable ShipData.ShipID (see ShipFactory - deterministically
        /// assigned from civ/fleet/system + a per-owner creation sequence, so the same ship resolves
        /// to the same ID on every peer). Unlike GetGalaxyShipController/GetCombatShipController above
        /// (keyed by ShipName.GetHashCode(), which collides for same-named ships), this is safe to use
        /// for cross-peer roster reconciliation - e.g. FleetController.RpcSyncShipRoster resolving a
        /// list of ShipIDs sent from the peer that just created/split a fleet into this peer's own
        /// local ShipController references. Searches AllShipControllers rather than the scene-specific
        /// dictionaries so a ship is still found here even mid-transfer between two fleets/systems.
        /// </summary>
        public ShipController GetShipControllerByShipID(int shipID)
        {
            return AllShipControllers.Find(s => s != null && s.ShipData != null && s.ShipData.ShipID == shipID);
        }

        /// <summary>
        /// Remove a ship controller from all tracking
        /// </summary>
        public void RemoveShipController(ShipController shipController)
        {
            if (shipController != null)
            {
                AllShipControllers.Remove(shipController);
                Debug.Log($"ShipRegistry: Removed {shipController.name} from global tracking");
            }
        }

        /// <summary>
        /// Clear galaxy ship controllers (called when galaxy scene unloads)
        /// </summary>
        public void ClearGalaxyShips()
        {
            galaxyShipControllers.Clear();
            Debug.Log("ShipRegistry: Cleared galaxy ship controllers");
        }

        /// <summary>
        /// Clear combat ship controllers (called when combat scene unloads)
        /// </summary>
        public void ClearCombatShips()
        {
            combatShipControllers.Clear();
            Debug.Log("ShipRegistry: Cleared combat ship controllers");
        }

        /// <summary>
        /// Transition ships to combat context
        /// </summary>
        public void TransitionShipsToCombat(List<ShipController> galaxyShips)
        {
            Debug.Log($"ShipRegistry: Transitioning {galaxyShips.Count} ships to combat");

            foreach (var galaxyShip in galaxyShips)
            {
                if (galaxyShip?.ShipData != null)
                {
                    Debug.Log($"  - Ship {galaxyShip.ShipData.ShipName} ready for combat instantiation");
                }
            }
        }

        /// <summary>
        /// Sync combat results back to galaxy
        /// </summary>
        public void SyncCombatResultsToGalaxy()
        {
            Debug.Log("ShipRegistry: Syncing combat results back to galaxy");

            foreach (var combatShip in combatShipControllers.Values)
            {
                if (combatShip?.ShipData != null)
                {
                    Debug.Log($"  - Ship {combatShip.ShipData.ShipName} HP: {combatShip.ShipData.ShieldHealth}/{combatShip.ShipData.HullHealth}");
                }
            }
        }
    }
}
