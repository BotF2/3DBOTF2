using BOTF3D.Core;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Manages creation and tracking of health bars for all ships in combat.
    /// Handles world-space UI health bar setup and configuration.
    /// </summary>
    public class HealthBarManager : IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        private readonly CombatData combatData;
        private readonly List<GameObject> healthbarRenderers = new List<GameObject>();

        public HealthBarManager(CombatData data)
        {
            combatData = data;
        }

        /// <summary>
        /// Create health bars for all ships AFTER warp animation completes
        /// </summary>
        public void CreateHealthBarsForAllShips()
        {
            Debug.Log("🏥 Creating health bars for all ships...");

            int healthbarCount = 0;

            // Create health bars for Side One ships
            foreach (var ship in combatData.SideOneShipCons)
            {
                if (ship != null && !ship.ShipData.Distroyed)
                {
                    CreateHealthBarForShip(ship, -1); // -1 for side one (left side)
                    healthbarCount++;
                }
            }

            // Create health bars for Side Two ships
            foreach (var ship in combatData.SideTwoShipCons)
            {
                if (ship != null && !ship.ShipData.Distroyed)
                {
                    CreateHealthBarForShip(ship, 1); // 1 for side two (right side)
                    healthbarCount++;
                }
            }

            Debug.Log($"✅ Created {healthbarCount} health bars");
        }

        /// <summary>
        /// Create a health bar for a single ship
        /// </summary>
        private void CreateHealthBarForShip(ShipController ship, int side1negSide2pos)
        {
            if (ship == null || CombatManager.Instance == null || CombatManager.Instance.HealthbarPrefab == null)
            {
                Debug.LogWarning($"Cannot create health bar - missing ship or prefab");
                return;
            }

            GameObject healthbarGO = Object.Instantiate(CombatManager.Instance.HealthbarPrefab);
            healthbarGO.SetActive(true);

            // Parent directly to ship (world-space UI)
            healthbarGO.transform.SetParent(ship.transform, false);
            healthbarGO.transform.localPosition = new Vector3(5 * side1negSide2pos, -3f, -10f);
            healthbarGO.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            healthbarGO.transform.localRotation = Quaternion.Euler(0, -90 * side1negSide2pos, 0);

            // Ensure health bar Canvas is on World Space
            Canvas healthbarCanvas = healthbarGO.GetComponent<Canvas>();
            if (healthbarCanvas == null)
            {
                healthbarCanvas = healthbarGO.AddComponent<Canvas>();
            }

            healthbarCanvas.renderMode = RenderMode.WorldSpace;
            healthbarCanvas.worldCamera = ShipCombatCameraController.Instance?.GetComponentInChildren<Camera>();

            // Add CanvasScaler for proper sizing
            if (!healthbarGO.TryGetComponent<CanvasScaler>(out var canvasScaler))
            {
                canvasScaler = healthbarGO.AddComponent<CanvasScaler>();
            }
            canvasScaler.dynamicPixelsPerUnit = 10;

            // Set health bar layer to Default (NOT UI layer for world-space)
            healthbarGO.layer = LayerMask.NameToLayer("Default");
            SetLayerRecursively(healthbarGO, LayerMask.NameToLayer("Default"));

            // Set up health bar images
            Image[] healthbarImages = healthbarGO.GetComponentsInChildren<Image>();
            foreach (var img in healthbarImages)
            {
                if (img.gameObject.name == "HealthFill")
                {
                    ship.HealthFillImage = img;
                    ship.HealthFillImage.fillAmount = 1f;
                    ship.HealthFillImage.color = Color.green;
                }
                else if (img.gameObject.name == "HealthBackground")
                {
                    ship.HealthBackgroundImage = img;
                    ship.HealthBackgroundImage.fillAmount = 1f;
                    ship.HealthBackgroundImage.color = Color.red;
                }
            }

            // Add to tracking list
            healthbarRenderers.Add(healthbarGO);

            // Add billboard component to face camera
            var billboard = healthbarGO.GetComponent<BillboardCameraCombat>();
            if (billboard == null)
            {
                billboard = healthbarGO.AddComponent<BillboardCameraCombat>();
            }
        }

        /// <summary>
        /// Clean up all health bars
        /// </summary>
        public void DestroyAllHealthBars()
        {
            foreach (var hb in healthbarRenderers)
            {
                if (hb != null) Object.Destroy(hb);
            }
            healthbarRenderers.Clear();
        }

        /// <summary>
        /// Set layer of GameObject and all children recursively
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;

            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
