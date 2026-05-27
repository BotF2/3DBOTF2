using BOTF3D.Core;
using BOTF3D.GamePlay;
using UnityEngine;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Factory for creating ShipController instances in different contexts.
    /// Handles instantiation for both galaxy and combat scenes.
    /// </summary>
    public class ShipFactory
    {
        private readonly ShipController shipConPrefab;
        private readonly GameObject targetGOPrefab;

        public ShipFactory(ShipController prefab, GameObject targetPrefab)
        {
            shipConPrefab = prefab;
            targetGOPrefab = targetPrefab;
        }

        /// <summary>
        /// Create a galaxy ship (parented to fleet/system)
        /// </summary>
        public ShipController CreateGalaxyShip(ShipSO shipSO, Vector3 position, GameObject parent)
        {
            if (shipConPrefab == null)
            {
                Debug.LogError("ShipFactory: shipConPrefab is null!");
                return null;
            }

            if (parent == null)
            {
                Debug.LogError("ShipFactory: parent GameObject is null!");
                return null;
            }

            // Instantiate ship controller
            ShipController shipController = Object.Instantiate(
                shipConPrefab,
                position,
                Quaternion.identity,
                parent.transform
            );

            shipController.name = $"{shipSO.ShipName}_Galaxy";
            shipController.gameObject.layer = 9;

            Debug.Log($"ShipFactory: Created galaxy ship '{shipController.name}' under '{parent.name}'");

            return shipController;
        }

        /// <summary>
        /// Create a combat ship (parented to combat canvas)
        /// </summary>
        public ShipController CreateCombatShip(ShipSO shipSO, Vector3 position)
        {
            if (shipConPrefab == null)
            {
                Debug.LogError("ShipFactory: shipConPrefab is null!");
                return null;
            }

            if (CombatManager.Instance == null || CombatManager.Instance.CombatUICanvas == null)
            {
                Debug.LogError("ShipFactory: CombatManager or CombatUICanvas is null!");
                return null;
            }

            // Instantiate ship controller
            ShipController shipController = Object.Instantiate(
                shipConPrefab,
                position,
                Quaternion.identity,
                CombatManager.Instance.CombatUICanvas.transform
            );

            shipController.name = $"{shipSO.ShipName}_Combat";

            Debug.Log($"ShipFactory: Created combat ship '{shipController.name}'");

            return shipController;
        }

        /// <summary>
        /// Create target GameObject for a ship
        /// </summary>
        public GameObject CreateTargetForShip(ShipController shipController)
        {
            if (targetGOPrefab == null)
            {
                Debug.LogWarning("ShipFactory: targetGOPrefab is null, skipping target creation");
                return null;
            }

            GameObject targetGO = Object.Instantiate(
                targetGOPrefab,
                shipController.transform.position,
                Quaternion.identity
            );

            targetGO.transform.SetParent(shipController.transform, false);
            targetGO.transform.Translate(
                shipController.transform.position.x,
                shipController.transform.position.y,
                shipController.transform.position.z + 10
            );

            return targetGO;
        }

        /// <summary>
        /// Link ship to its parent (fleet or system)
        /// </summary>
        public void LinkShipToParent(ShipController shipController, GameObject parent)
        {
            if (parent.TryGetComponent<FleetController>(out var fleetCon))
            {
                shipController.ShipData.CurrentFleetController = fleetCon;

                if (!fleetCon.FleetData.ShipsList.Contains(shipController))
                {
                    fleetCon.FleetData.ShipsList.Add(shipController);
                }

                shipController.ShipData.CurrentStarSysController = null;
                Debug.Log($"  Ship '{shipController.ShipData.ShipName}' linked to fleet '{fleetCon.name}'");
            }
            else if (parent.TryGetComponent<StarSysController>(out var sysCon))
            {
                shipController.ShipData.CurrentStarSysController = sysCon;

                if (!sysCon.StarSysData.ShipsList.Contains(shipController))
                {
                    sysCon.StarSysData.ShipsList.Add(shipController);
                }

                shipController.ShipData.CurrentFleetController = null;
                Debug.Log($"  Ship '{shipController.ShipData.ShipName}' linked to system '{sysCon.name}'");
            }
            else
            {
                Debug.LogWarning($"  ⚠️ Parent '{parent.name}' is neither fleet nor system!");
            }
        }
    }
}
