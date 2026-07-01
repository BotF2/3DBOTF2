using BOTF3D.Core;

using System.Linq;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.UI
{
    /// <summary>
    /// Manages ship deployment operations between fleets and systems.
    /// Handles showing/hiding deploy menus, tracking deploy state.
    /// </summary>
    public class GalaxyShipDeployManager : IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        // Ship deploy state
        public FleetController FleetLookingForShipDeploy { get; set; }
        public FleetController FleetSelectedForShipDeploy { get; set; }
        public StarSysController StarSystLookingForShipDeploy { get; set; }
        public StarSysController StarSystSelectedForShipDeploy { get; set; }

        // Ship merge state
        public FleetController FleetLookingForShipMerge { get; set; }
        public FleetController FleetSelectedForShipMerge { get; set; }
        public StarSysController StarSystLookingForShipMerge { get; set; }
        public StarSysController StarSystSelectedForShipMerge { get; set; }

        // Fleet destination state
        public FleetController FleetLookingForDestination { get; set; }

        // ✅ FIX: Resolve lazily via Instance instead of capturing at construction time
        private ShipDeployMenuUIController ShipDeployUI => ShipDeployMenuUIController.Instance;

        public GalaxyShipDeployManager(ShipDeployMenuUIController unused)
        {
            // Param kept for backward-compat; we now use the singleton lazily.
        }

        /// <summary>
        /// Show ship deploy menu for a fleet
        /// </summary>
        public void ShowShipDeployMenuForFleet(FleetController fleet)
        {
            if (fleet == null)
            {
                Debug.LogError("GalaxyShipDeployManager: Cannot show deploy menu - fleet is null");
                return;
            }

            Debug.Log($"GalaxyShipDeployManager: Showing ship deploy menu for fleet {fleet.name}");

            // Reset cursor
            if (MousePointerChanger.Instance != null)
            {
                MousePointerChanger.Instance.ResetCursor();
            }

            // Handle UI parenting to appropriate view
            HandleFleetUIParenting(fleet);

            if (ShipDeployUI != null)
            {
                // Activate the controller gameObject
                ShipDeployUI.gameObject.SetActive(true);

                // Show the view first so slots are active
                ShipDeployUI.ShowShipDeployMenuView();

                // ✅ FIX: Populate TOP slot with the source (looking) fleet's ships
                if (FleetLookingForShipDeploy != null && FleetLookingForShipDeploy.FleetData?.ShipsList != null)
                {
                    ShipDeployUI.SetUpTopShipLists(
                        FleetLookingForShipDeploy.FleetData.ShipsList
                            .Cast<BOTF3D.Combat.ShipController>().ToList()
                    );
                    Debug.Log($"  Top slot populated with {FleetLookingForShipDeploy.FleetData.ShipsList.Count} ships from '{FleetLookingForShipDeploy.name}'");
                }
                else
                {
                    Debug.LogWarning("  ⚠️ No FleetLookingForShipDeploy set - top slot will be empty");
                }

                // Populate BOTTOM slot with the selected/target fleet's ships
                ShipDeployUI.SetUpBottomShipLists(fleet, deployNotMerge: true);
                Debug.Log($"  Bottom slot populated for target fleet '{fleet.name}'");
            }
            else
            {
                Debug.LogError("GalaxyShipDeployManager: ShipDeployMenuUIController.Instance is null!");
            }

            // Set click mode for ship deployment
            if (GalaxyMenuUIController.Instance != null)
            {
                GalaxyMenuUIController.Instance.SetClickMode(GalaxyClickMode.SelectForShipDeploy);
            }
        }

        /// <summary>
        /// Handle parenting fleet UI to the appropriate menu view
        /// </summary>
        private void HandleFleetUIParenting(FleetController fleet)
        {
            if (fleet?.FleetUIGameObject == null) return;

            // If a fleet is looking for deploy, parent to AFleetMenuView
            if (FleetLookingForShipDeploy != null)
            {
                var fleetMenuView = FleetMenuUIController.Instance?.AFleetMenuView?.gameObject;
                if (fleetMenuView != null)
                {
                    fleet.FleetUIGameObject.transform.SetParent(fleetMenuView.transform, false);
                    fleet.FleetUIGameObject.transform.SetAsLastSibling();
                    fleet.FleetUIGameObject.SetActive(true);
                    Debug.Log($"  Fleet UI '{fleet.FleetUIGameObject.name}' parented to AFleetMenuView");
                }
            }
            // If a system is looking for deploy, parent to ASystemMenuView
            else if (StarSystLookingForShipDeploy != null)
            {
                var sysMenuView = StarSysMenuUIController.Instance?.ASystemMenuView?.gameObject;
                if (sysMenuView != null)
                {
                    fleet.FleetUIGameObject.transform.SetParent(sysMenuView.transform, false);
                    fleet.FleetUIGameObject.transform.SetAsLastSibling();
                    fleet.FleetUIGameObject.SetActive(true);

                    // Ensure system menu view is active
                    if (!sysMenuView.activeSelf)
                    {
                        sysMenuView.SetActive(true);
                        Debug.Log($"  ASystemMenuView activated");
                    }

                    Debug.Log($"  Fleet UI '{fleet.FleetUIGameObject.name}' parented to ASystemMenuView");
                }
            }
        }

        /// <summary>
        /// Show ship deploy menu for system creating new fleet
        /// Sets up top slot with system's ships, bottom slot with new fleet
        /// </summary>
        public void ShowShipDeployForSystemNewFleet(StarSysController system, FleetController newFleet)
        {
            if (system == null || newFleet == null)
            {
                Debug.LogError("GalaxyShipDeployManager: Cannot show deploy menu - system or newFleet is null");
                return;
            }

            Debug.Log($"GalaxyShipDeployManager: Showing ship deploy for system {system.name} -> new fleet {newFleet.name}");

            // Reset cursor
            if (MousePointerChanger.Instance != null)
            {
                MousePointerChanger.Instance.ResetCursor();
            }

            // Set up the deployment context
            StarSystLookingForShipDeploy = system;
            FleetSelectedForShipDeploy = newFleet;

            // Ensure system has ShipListUIParent
            EnsureSystemShipListUIParent(system);

            // Ensure new fleet has ShipListUIParent
            EnsureFleetShipListUIParent(newFleet);

            if (ShipDeployUI != null)
            {
                // Activate the controller
                ShipDeployUI.gameObject.SetActive(true);

                // Show the view
                ShipDeployUI.ShowShipDeployMenuView();

                // Set up top slot with system's ships
                ShipDeployUI.SetUpTopShipLists(
                    system.StarSysData.ShipsList.Cast<BOTF3D.Combat.ShipController>().ToList()
                );

                // Set up bottom slot with new fleet (empty)
                ShipDeployUI.SetUpBottomShipLists(newFleet, deployNotMerge: true);

                // Parent system UI to ASystemMenuView
                var sysMenuView = StarSysMenuUIController.Instance?.ASystemMenuView?.gameObject;
                if (sysMenuView != null && system.StarSysUIGameObject != null)
                {
                    system.StarSysUIGameObject.transform.SetParent(sysMenuView.transform, false);
                    system.StarSysUIGameObject.transform.SetAsLastSibling();
                    system.StarSysUIGameObject.SetActive(true);
                }

                // Parent new fleet UI to ASystemMenuView
                if (sysMenuView != null && newFleet.FleetUIGameObject != null)
                {
                    newFleet.FleetUIGameObject.transform.SetParent(sysMenuView.transform, false);
                    newFleet.FleetUIGameObject.transform.SetAsLastSibling();
                    newFleet.FleetUIGameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("GalaxyShipDeployManager: ShipDeployMenuUIController.Instance is null!");
            }
        }

        /// <summary>
        /// Ensure system has ShipListUIParent set up
        /// </summary>
        private void EnsureSystemShipListUIParent(StarSysController system)
        {
            if (system?.StarSysData == null) return;

            if (system.StarSysData.ShipListUIParent == null)
            {
                Debug.LogWarning($"System '{system.name}' missing ShipListUIParent - setting it up now");

                var uiFields = system.StarSysUIGameObject?.GetComponent<StarSysUI_Fields>();
                if (uiFields?.shipContent != null)
                {
                    system.StarSysData.ShipListUIParent = uiFields.shipContent.gameObject;
                    Debug.Log($"✅ Set ShipListUIParent for system '{system.name}'");
                }
                else
                {
                    Debug.LogError($"❌ Cannot find shipContent for system '{system.name}'!");
                }
            }
        }

        /// <summary>
        /// Ensure fleet has ShipListUIParent set up
        /// </summary>
        private void EnsureFleetShipListUIParent(FleetController fleet)
        {
            if (fleet?.FleetData == null) return;

            if (fleet.FleetData.ShipListUIParent == null)
            {
                Debug.LogWarning($"Fleet '{fleet.name}' missing ShipListUIParent - setting it up now");

                var uiFields = fleet.FleetUIGameObject?.GetComponent<FleetUI_Fields>();
                if (uiFields?.FleetShipContentGO != null)
                {
                    fleet.FleetData.ShipListUIParent = uiFields.FleetShipContentGO;
                    Debug.Log($"✅ Set ShipListUIParent for fleet '{fleet.name}'");
                }
                else
                {
                    Debug.LogError($"❌ Cannot find FleetShipContentGO for fleet '{fleet.name}'!");
                }
            }
        }

        /// <summary>
        /// Show ship deploy menu for fleet to fleet transfer
        /// Sets up top slot with original fleet's ships, bottom slot with new fleet
        /// </summary>
        public void ShowShipDeployForFleetNewFleet(FleetController originalFleet, FleetController newFleet)
        {
            if (originalFleet == null || newFleet == null)
            {
                Debug.LogError("GalaxyShipDeployManager: Cannot show deploy menu - originalFleet or newFleet is null");
                return;
            }

            Debug.Log($"GalaxyShipDeployManager: Showing ship deploy for fleet {originalFleet.name} -> new fleet {newFleet.name}");

            // Reset cursor
            if (MousePointerChanger.Instance != null)
            {
                MousePointerChanger.Instance.ResetCursor();
            }

            // Set up the deployment context
            FleetLookingForShipDeploy = originalFleet;
            FleetSelectedForShipDeploy = newFleet;

            // Ensure both fleets have ShipListUIParent
            EnsureFleetShipListUIParent(originalFleet);
            EnsureFleetShipListUIParent(newFleet);

            if (ShipDeployUI != null)
            {
                // Activate the controller
                ShipDeployUI.gameObject.SetActive(true);

                // Show the view
                ShipDeployUI.ShowShipDeployMenuView();

                // Set up top slot with original fleet's ships
                ShipDeployUI.SetUpTopShipLists(
                    originalFleet.FleetData.ShipsList.Cast<BOTF3D.Combat.ShipController>().ToList()
                );

                // Set up bottom slot with new fleet (empty)
                ShipDeployUI.SetUpBottomShipLists(newFleet, deployNotMerge: true);

                // Parent fleet UIs to AFleetMenuView
                var fleetMenuView = FleetMenuUIController.Instance?.AFleetMenuView?.gameObject;
                if (fleetMenuView != null)
                {
                    if (originalFleet.FleetUIGameObject != null)
                    {
                        originalFleet.FleetUIGameObject.transform.SetParent(fleetMenuView.transform, false);
                        originalFleet.FleetUIGameObject.transform.SetAsLastSibling();
                        originalFleet.FleetUIGameObject.SetActive(true);
                    }

                    if (newFleet.FleetUIGameObject != null)
                    {
                        newFleet.FleetUIGameObject.transform.SetParent(fleetMenuView.transform, false);
                        newFleet.FleetUIGameObject.transform.SetAsLastSibling();
                        newFleet.FleetUIGameObject.SetActive(true);
                    }
                }
            }
            else
            {
                Debug.LogError("GalaxyShipDeployManager: ShipDeployMenuUIController.Instance is null!");
            }
        }

        /// <summary>
        /// Hide ship deploy menu
        /// </summary>
        public void HideShipDeployMenu()
        {
            Debug.Log("GalaxyShipDeployManager: Hiding ship deploy menu");

            if (ShipDeployUI != null)
            {
                ShipDeployUI.HideShipDeployMenuView();
            }
            else
            {
                Debug.LogWarning("GalaxyShipDeployManager: HideShipDeployMenu - ShipDeployMenuUIController.Instance is null");
            }
        }

        /// <summary>
        /// Set which fleet is looking for ship deploy
        /// </summary>
        public void SetFleetLookingForShipDeploy(FleetController fleet)
        {
            FleetLookingForShipDeploy = fleet;
            Debug.Log($"GalaxyShipDeployManager: Fleet looking for ship deploy set to {fleet?.name}");
        }

        /// <summary>
        /// Set which fleet is selected for ship deploy
        /// </summary>
        public void SetFleetSelectedForShipDeploy(FleetController fleet)
        {
            FleetSelectedForShipDeploy = fleet;
            Debug.Log($"GalaxyShipDeployManager: Fleet selected for ship deploy set to {fleet?.name}");
        }

        /// <summary>
        /// Set which system is looking for ship deploy
        /// </summary>
        public void SetSystemLookingForShipDeploy(StarSysController system)
        {
            StarSystLookingForShipDeploy = system;
            Debug.Log($"GalaxyShipDeployManager: System looking for ship deploy set to {system?.name}");
        }

        /// <summary>
        /// Set which system is selected for ship deploy
        /// </summary>
        public void SetSystemSelectedForShipDeploy(StarSysController system)
        {
            StarSystSelectedForShipDeploy = system;
            Debug.Log($"GalaxyShipDeployManager: System selected for ship deploy set to {system?.name}");
        }

        /// <summary>
        /// Set which fleet is looking for merge
        /// </summary>
        public void SetFleetLookingForMerge(FleetController fleet)
        {
            FleetLookingForShipMerge = fleet;
            Debug.Log($"GalaxyShipDeployManager: Fleet looking for merge set to {fleet?.name}");
        }

        /// <summary>
        /// Set which fleet is selected for merge
        /// </summary>
        public void SetFleetSelectedForMerge(FleetController fleet)
        {
            FleetSelectedForShipMerge = fleet;
            Debug.Log($"GalaxyShipDeployManager: Fleet selected for merge set to {fleet?.name}");
        }

        /// <summary>
        /// Set which system is looking for merge
        /// </summary>
        public void SetSystemLookingForMerge(StarSysController system)
        {
            StarSystLookingForShipMerge = system;
            Debug.Log($"GalaxyShipDeployManager: System looking for merge set to {system?.name}");
        }

        /// <summary>
        /// Set which system is selected for merge
        /// </summary>
        public void SetSystemSelectedForMerge(StarSysController system)
        {
            StarSystSelectedForShipMerge = system;
            Debug.Log($"GalaxyShipDeployManager: System selected for merge set to {system?.name}");
        }

        /// <summary>
        /// Begin setting destination for a fleet
        /// </summary>
        public void BeginSetDestination(FleetController fleet)
        {
            FleetLookingForDestination = fleet;
        }

        /// <summary>
        /// Complete ship exchange operation
        /// </summary>
        public void CompleteShipExchange()
        {
            Debug.Log("GalaxyShipDeployManager: Completing ship exchange");

            // Clear all deploy state
            FleetLookingForShipDeploy = null;
            FleetSelectedForShipDeploy = null;
            StarSystLookingForShipDeploy = null;
            StarSystSelectedForShipDeploy = null;

            HideShipDeployMenu();
        }

        /// <summary>
        /// Cancel ship deploy operation
        /// </summary>
        public void CancelShipDeploy()
        {
            // Clear all state
            FleetLookingForShipDeploy = null;
            FleetSelectedForShipDeploy = null;
            StarSystLookingForShipDeploy = null;
            StarSystSelectedForShipDeploy = null;
            FleetLookingForShipMerge = null;
            FleetSelectedForShipMerge = null;
            StarSystLookingForShipMerge = null;
            StarSystSelectedForShipMerge = null;
            FleetLookingForDestination = null;

            HideShipDeployMenu();
        }

        /// <summary>
        /// Complete set destination operation
        /// </summary>
        public void CompleteSetDestination()
        {
            FleetLookingForDestination = null;
        }
    }
}
