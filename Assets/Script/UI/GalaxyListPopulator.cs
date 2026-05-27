using BOTF3D.Core;
using BOTF3D.GamePlay;
using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.UI
{
    /// <summary>
    /// Populates and manages UI lists for star systems, fleets, and diplomacy.
    /// Handles creating, updating, and clearing list items.
    /// </summary>
    public class GalaxyListPopulator
    {
        // UI list references
        private readonly List<GameObject> starSysUIList;
        private readonly List<GameObject> fleetUIList;
        private readonly List<GameObject> diplomacyUIList;
        private readonly List<GameObject> sysShipUIList;

        // Controller lists
        private readonly List<StarSysController> sysControllers;
        private readonly List<FleetController> fleetControllers;
        private readonly List<DiplomacyController> diplomacyControllers;

        // Prefabs
        private readonly GameObject fleetUIPrefab;

        public GalaxyListPopulator(
            List<GameObject> starSysUIList,
            List<GameObject> fleetUIList,
            List<GameObject> diplomacyUIList,
            List<GameObject> sysShipUIList,
            List<StarSysController> sysControllers,
            List<FleetController> fleetControllers,
            List<DiplomacyController> diplomacyControllers,
            GameObject fleetUIPrefab)
        {
            this.starSysUIList = starSysUIList;
            this.fleetUIList = fleetUIList;
            this.diplomacyUIList = diplomacyUIList;
            this.sysShipUIList = sysShipUIList;
            this.sysControllers = sysControllers;
            this.fleetControllers = fleetControllers;
            this.diplomacyControllers = diplomacyControllers;
            this.fleetUIPrefab = fleetUIPrefab;
        }

        /// <summary>
        /// Populate star systems list
        /// </summary>
        public void PopulateStarSystemsList()
        {
            Debug.Log("GalaxyListPopulator: Populating star systems list");

            // Clear existing UI
            ClearStarSystemsList();

            if (sysControllers == null || sysControllers.Count == 0)
            {
                Debug.LogWarning("GalaxyListPopulator: No star systems to populate");
                return;
            }

            // Get local player's systems
            CivEnum localPlayerCiv = GameController.Instance.GameData.LocalPlayerCivEnum;

            foreach (var sysCon in sysControllers)
            {
                if (sysCon == null) continue;

                // Only show player's own systems
                if (sysCon.StarSysData.CurrentOwnerCivEnum == localPlayerCiv)
                {
                    // UI is already created by StarSysManager, track the StarSysUIGameObject
                    if (sysCon.StarSysUIGameObject != null)
                    {
                        starSysUIList.Add(sysCon.StarSysUIGameObject);
                    }
                }
            }

            Debug.Log($"GalaxyListPopulator: Populated {starSysUIList.Count} star systems");
        }

        /// <summary>
        /// Populate fleets list
        /// </summary>
        public void PopulateFleetsList()
        {
            Debug.Log("GalaxyListPopulator: Populating fleets list");

            // Clear existing UI
            ClearFleetsList();

            if (fleetControllers == null || fleetControllers.Count == 0)
            {
                Debug.LogWarning("GalaxyListPopulator: No fleets to populate");
                return;
            }

            // Get local player's fleets
            CivEnum localPlayerCiv = GameController.Instance.GameData.LocalPlayerCivEnum;

            foreach (var fleetCon in fleetControllers)
            {
                if (fleetCon == null) continue;

                // Only show player's own fleets
                if (fleetCon.FleetData.CivEnum == localPlayerCiv)
                {
                    // UI is already created by FleetManager, track the FleetUIGameObject
                    if (fleetCon.FleetUIGameObject != null)
                    {
                        fleetUIList.Add(fleetCon.FleetUIGameObject);
                    }
                }
            }

            Debug.Log($"GalaxyListPopulator: Populated {fleetUIList.Count} fleets");
        }

        /// <summary>
        /// Populate diplomacy list - newest encounter at the top
        /// </summary>
        public void PopulateDiplomacyList()
        {
            Debug.Log("GalaxyListPopulator: Populating diplomacy list");

            ClearDiplomacyList();

            if (diplomacyControllers == null || diplomacyControllers.Count == 0)
            {
                Debug.LogWarning("GalaxyListPopulator: No diplomacy contacts to populate");
                return;
            }

            // ✅ FIX: Iterate in reverse so the oldest entry ends up at the top (SetAsFirstSibling
            //         pushes each item to index 0, so iterating oldest-first means newest lands on top).
            for (int i = diplomacyControllers.Count - 1; i >= 0; i--)
            {
                var dipCon = diplomacyControllers[i];
                if (dipCon == null) continue;

                if (dipCon.DiplomacyUIGameObject != null)
                {
                    diplomacyUIList.Add(dipCon.DiplomacyUIGameObject);
                    dipCon.DiplomacyUIGameObject.SetActive(true);
                    dipCon.DiplomacyUIGameObject.transform.SetAsFirstSibling(); // ✅ newest ends up at top
                }
            }

            Debug.Log($"GalaxyListPopulator: Populated {diplomacyUIList.Count} diplomacy contacts");
        }

        /// <summary>
        /// Clear star systems list
        /// </summary>
        public void ClearStarSystemsList()
        {
            if (starSysUIList != null)
            {
                foreach (var uiGO in starSysUIList)
                {
                    if (uiGO != null)
                    {
                        uiGO.SetActive(false);
                    }
                }
                starSysUIList.Clear();
            }
        }

        /// <summary>
        /// Clear fleets list
        /// </summary>
        public void ClearFleetsList()
        {
            if (fleetUIList != null)
            {
                foreach (var uiGO in fleetUIList)
                {
                    if (uiGO != null)
                    {
                        uiGO.SetActive(false);
                    }
                }
                fleetUIList.Clear();
            }
        }

        /// <summary>
        /// Clear diplomacy list
        /// </summary>
        public void ClearDiplomacyList()
        {
            if (diplomacyUIList != null)
            {
                foreach (var uiGO in diplomacyUIList)
                {
                    if (uiGO != null)
                    {
                        uiGO.SetActive(false);
                    }
                }
                diplomacyUIList.Clear();
            }
        }

        /// <summary>
        /// Clear ship UI list
        /// </summary>
        public void ClearShipUIList()
        {
            if (sysShipUIList != null)
            {
                foreach (var uiGO in sysShipUIList)
                {
                    if (uiGO != null)
                    {
                        uiGO.SetActive(false);
                    }
                }
                sysShipUIList.Clear();
            }
        }

        /// <summary>
        /// Clear all lists
        /// </summary>
        public void ClearAllLists()
        {
            Debug.Log("GalaxyListPopulator: Clearing all lists");
            ClearStarSystemsList();
            ClearFleetsList();
            ClearDiplomacyList();
            ClearShipUIList();
        }

        /// <summary>
        /// Refresh all lists (clear and repopulate)
        /// </summary>
        public void RefreshAllLists()
        {
            Debug.Log("GalaxyListPopulator: Refreshing all lists");
            PopulateStarSystemsList();
            PopulateFleetsList();
            PopulateDiplomacyList();
        }

        /// <summary>
        /// Get star system controller by UI GameObject
        /// </summary>
        public StarSysController GetStarSystemControllerByUI(GameObject uiGO)
        {
            if (sysControllers == null) return null;

            foreach (var sysCon in sysControllers)
            {
                if (sysCon?.StarSysUIGameObject == uiGO)
                {
                    return sysCon;
                }
            }

            return null;
        }

        /// <summary>
        /// Get fleet controller by UI GameObject
        /// </summary>
        public FleetController GetFleetControllerByUI(GameObject uiGO)
        {
            if (fleetControllers == null) return null;

            foreach (var fleetCon in fleetControllers)
            {
                if (fleetCon?.FleetUIGameObject == uiGO)
                {
                    return fleetCon;
                }
            }

            return null;
        }
    }
}
