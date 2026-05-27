using BOTF3D.Audio;
using BOTF3D.Core;
using BOTF3D.GamePlay;
using BOTF3D.UI;
using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Handles instantiation and initialization of CombatController instances.
    /// Creates combat data, assigns prefabs, and configures the combat controller.
    /// </summary>
    public class CombatInstantiator
    {
        private readonly CombatController combatConPrefab;
        private readonly Transform parentTransform;
        private readonly SoundData warpInSound;
        private readonly CombatSceneLoader sceneLoader;
        private readonly WeaponAssetProvider weaponProvider;

        private List<CombatController> allCombatControllers = new List<CombatController>();

        public CombatInstantiator(
            CombatController prefab,
            Transform parent,
            SoundData warpSound,
            CombatSceneLoader loader,
            WeaponAssetProvider weapons)
        {
            combatConPrefab = prefab;
            parentTransform = parent;
            warpInSound = warpSound;
            sceneLoader = loader;
            weaponProvider = weapons;
        }

        /// <summary>
        /// Instantiate a new CombatController
        /// </summary>
        public CombatController InstantiateCombatController(
            List<ShipController> sideOneShipCons,
            List<ShipController> sideTwoShipCons)
        {
            if (combatConPrefab == null)
            {
                Debug.LogError("InstantiateCombatController: combatConPrefab is null! Assign it in Inspector.");
                return null;
            }

            if (sceneLoader.CombatUICanvas == null)
            {
                Debug.LogError("❌ Cannot instantiate combat - CombatUICanvas not found!");
                return null;
            }

            Debug.Log("📦 Instantiating NEW CombatController...");

            // Create combat data
            CombatData combatData = CreateCombatData(sideOneShipCons, sideTwoShipCons);

            // Instantiate controller
            CombatController combatController = Object.Instantiate(
                combatConPrefab,
                Vector3.zero,
                Quaternion.identity,
                parentTransform);

            combatController.name = $"CombatController_{combatController.CombatID}";

            // Configure controller
            ConfigureCombatController(combatController, combatData);

            // Assign weapon assets
            AssignWeaponAssets(combatController, sideOneShipCons[0].ShipData.CivEnum, sideTwoShipCons[0].ShipData.CivEnum);

            Debug.Log($"✅ CombatController instantiated: {combatController.name}");

            // Populate ship data and setup positions
            combatController.PopulateShipData(combatController);

            // Track controller
            allCombatControllers.Add(combatController);

            return combatController;
        }

        /// <summary>
        /// Create combat data for the two sides
        /// </summary>
        private CombatData CreateCombatData(
            List<ShipController> sideOneShipCons,
            List<ShipController> sideTwoShipCons)
        {
            return new CombatData
            {
                SideOneShipCons = sideOneShipCons,
                SideTwoShipCons = sideTwoShipCons,
                CivEnumSideOne = sideOneShipCons[0].ShipData.CivEnum,
                CivEnumSideTwo = sideTwoShipCons[0].ShipData.CivEnum,
                OrderSideOne = CombatOrders.Engage,
                OrderSideTwo = CombatOrders.Engage
            };
        }

        /// <summary>
        /// Configure the combat controller with data and settings
        /// </summary>
        private void ConfigureCombatController(CombatController controller, CombatData combatData)
        {
            combatData.CombatID = controller.CombatID;
            controller.CombatData = combatData;
            controller.isMoving = false;
            controller.isClosing = false;
            controller.WarpingIn = true;
            controller.WarpingAnimationOver = false;
            controller.ShipCombatCanvas = sceneLoader.Combat3DCanvas.GetComponent<Canvas>();
            controller.warpInSound = warpInSound;
        }

        /// <summary>
        /// Assign weapon prefabs and audio to the combat controller
        /// </summary>
        private void AssignWeaponAssets(CombatController controller, CivEnum sideOneCiv, CivEnum sideTwoCiv)
        {
            // Request weapon assignment from provider
            weaponProvider.AssignWeaponsForCombat(sideOneCiv, sideTwoCiv);

            // Assign to controller
            controller.SideOneTorpedoPrefab = weaponProvider.SideOneTorpedoPrefab;
            controller.SideTwoTorpedoPrefab = weaponProvider.SideTwoTorpedoPrefab;
            controller.SideOneBeamPrefab = weaponProvider.SideOneBeamPrefab;
            controller.SideTwoBeamPrefab = weaponProvider.SideTwoBeamPrefab;

            controller.SideOneBeamFireClip = weaponProvider.SideOneBeamFireClip;
            controller.SideTwoBeamFireClip = weaponProvider.SideTwoBeamFireClip;
            controller.SideOneTorpedoFireClip = weaponProvider.SideOneTorpedoFireClip;
            controller.SideTwoTorpedoFireClip = weaponProvider.SideTwoTorpedoFireClip;
        }

        /// <summary>
        /// Remove a combat controller from tracking
        /// </summary>
        public void RemoveCombatController(CombatController controller)
        {
            allCombatControllers.Remove(controller);
        }

        /// <summary>
        /// Get all active combat controllers
        /// </summary>
        public List<CombatController> GetAllCombatControllers()
        {
            return new List<CombatController>(allCombatControllers);
        }
    }
}
