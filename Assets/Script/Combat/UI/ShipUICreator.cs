using BOTF3D.Core;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Creates and manages ship list UI elements.
    /// Handles UI instantiation, parenting, and pending UI queue.
    /// </summary>
    public class ShipUICreator
    {
        private readonly GameObject shipListUIPrefab;
        private readonly List<ShipController> shipConPendingShipUI = new List<ShipController>();
        private int shipIndex = 0;

        public ShipUICreator(GameObject uiPrefab)
        {
            shipListUIPrefab = uiPrefab;
        }

        /// <summary>
        /// Create ship list UI for a ship
        /// </summary>
        public void InstantiateShipListUI(ShipController shipCon, GameObject parentGO)
        {
            Debug.Log($"=== InstantiateShipListUI called for ship '{shipCon?.ShipData?.ShipName}' ===");
            Debug.Log($"  Ship CivEnum: {shipCon?.ShipData?.CivEnum}");
            Debug.Log($"  LocalPlayerCivEnum: {GameController.Instance?.GameData?.LocalPlayerCivEnum}");
            Debug.Log($"  Match: {shipCon?.ShipData?.CivEnum == GameController.Instance?.GameData?.LocalPlayerCivEnum}");

            // Only create UI for local player ships
            if (shipCon.ShipData.CivEnum != GameController.Instance.GameData.LocalPlayerCivEnum)
            {
                Debug.Log($"  Ship '{shipCon.ShipData.ShipName}' is NOT local player - no UI created");
                return;
            }

            // Skip if UI already exists
            if (shipCon.ShipListUIGameObject != null)
            {
                Debug.Log($"  Ship '{shipCon.ShipData.ShipName}' already has UI - skipping");
                return;
            }

            Debug.Log($"  Creating UI for ship '{shipCon.ShipData.ShipName}'");

            // Instantiate UI prefab
            GameObject thisShipListUIGameObject = Object.Instantiate(
                shipListUIPrefab,
                Vector3.zero,
                Quaternion.identity
            );

            thisShipListUIGameObject.transform.localRotation = Quaternion.identity;
            // Stays inactive until TryParentShipUI confirms a real ShipListUIParent - see the
            // FallbackToCanvas removal below for why this must not be visible while unparented.
            thisShipListUIGameObject.SetActive(false);
            thisShipListUIGameObject.name = "ShipListUI_" + shipCon.ShipData.ShipName + "_" + shipIndex;
            shipIndex++;

            // Setup UI images
            Image[] imageComponents = thisShipListUIGameObject.GetComponentsInChildren<Image>();
            if (imageComponents.Length > 1 && imageComponents[1] != null)
            {
                imageComponents[1].sprite = shipCon.ShipData.ShipSprite;
            }

            // Setup UI text
            TextMeshProUGUI textComponent = thisShipListUIGameObject.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = shipCon.ShipData.ShipType.ToString();
            }

            // Setup canvas group
            CanvasGroup canvasGroup = thisShipListUIGameObject.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.gameObject.SetActive(true);
            }

            thisShipListUIGameObject.layer = 5;
            shipCon.ShipListUIGameObject = thisShipListUIGameObject;

            // Setup UI item component
            var shipUiItem = thisShipListUIGameObject.GetComponent<ShipListUI_Item>();
            if (shipUiItem != null)
            {
                shipUiItem.ShipController = shipCon;
            }

            // Try to parent UI to owner's ShipListUIParent
            TryParentShipUI(shipCon, parentGO, shipUiItem);

            Debug.Log($"=== InstantiateShipListUI complete - Pending count: {shipConPendingShipUI.Count} ===");
        }

        /// <summary>
        /// Try to parent ship UI to fleet/system UI parent
        /// </summary>
        private void TryParentShipUI(ShipController shipCon, GameObject parentGO, ShipListUI_Item shipUiItem)
        {
            if (parentGO.TryGetComponent(out StarSysController sysCon))
            {
                Debug.Log($"  Parent is StarSys: {sysCon.name}");
                shipUiItem.CurrentStarSyst = sysCon;

                if (sysCon.StarSysData.ShipListUIParent != null)
                {
                    shipCon.ShipListUIGameObject.transform.SetParent(
                        sysCon.StarSysData.ShipListUIParent.transform,
                        false
                    );
                    shipCon.ShipListUIGameObject.SetActive(true);
                    Debug.Log($"  ✅ Parented to system ShipListUIParent");
                }
                else
                {
                    Debug.LogWarning($"  ⚠️ System '{sysCon.name}' ShipListUIParent is NULL - queued pending, staying inactive");
                    shipConPendingShipUI.Add(shipCon);
                }
            }
            else if (parentGO.TryGetComponent(out FleetController fleetCon))
            {
                Debug.Log($"  Parent is Fleet: {fleetCon.name}");
                shipUiItem.CurrentFleet = fleetCon;

                if (fleetCon.FleetData.ShipListUIParent != null)
                {
                    shipCon.ShipListUIGameObject.transform.SetParent(
                        fleetCon.FleetData.ShipListUIParent.transform,
                        false
                    );
                    shipCon.ShipListUIGameObject.SetActive(true);
                    Debug.Log($"  ✅ Parented to fleet ShipListUIParent");
                }
                else
                {
                    Debug.LogWarning($"  ⚠️ Fleet '{fleetCon.gameObject.name}' ShipListUIParent is NULL - queued pending, staying inactive");
                    shipConPendingShipUI.Add(shipCon);
                }
            }
            else
            {
                GameLogger.Log(GameLogger.LogCategory.Combat, $"  ❌ Parent '{parentGO.name}' is neither StarSys nor Fleet!");
            }
        }

        /// <summary>
        /// Process pending ship UIs for a specific system
        /// </summary>
        public void ProcessPendingShipUIs(StarSysController sysCon)
        {
            if (shipConPendingShipUI == null || shipConPendingShipUI.Count == 0)
                return;

            Debug.Log($"ProcessPendingShipUIs: Processing {shipConPendingShipUI.Count} pending ship UIs for system '{sysCon.name}'");

            for (int i = shipConPendingShipUI.Count - 1; i >= 0; i--)
            {
                var shipCon = shipConPendingShipUI[i];

                if (shipCon == null || shipCon.ShipData.CurrentStarSysController != sysCon)
                    continue;

                if (shipCon.ShipListUIGameObject != null && sysCon.StarSysData.ShipListUIParent != null)
                {
                    shipCon.ShipListUIGameObject.transform.SetParent(
                        sysCon.StarSysData.ShipListUIParent.transform,
                        false
                    );

                    shipCon.ShipListUIGameObject.SetActive(true);

                    Debug.Log($"  ✅ Rescued pending ship UI '{shipCon.ShipData.ShipName}' to system ShipListUIParent");

                    shipConPendingShipUI.RemoveAt(i);
                }
            }

            Debug.Log($"ProcessPendingShipUIs: {shipConPendingShipUI.Count} ship UIs still pending");
        }

        /// <summary>
        /// Process ALL pending ship UIs (tries all owners)
        /// </summary>
        public void ProcessAllPendingShipUIs()
        {
            if (shipConPendingShipUI == null || shipConPendingShipUI.Count == 0)
                return;

            Debug.Log($"ProcessPendingShipUIs (ALL): Processing {shipConPendingShipUI.Count} pending ship UIs");

            for (int i = shipConPendingShipUI.Count - 1; i >= 0; i--)
            {
                var shipCon = shipConPendingShipUI[i];

                if (shipCon == null)
                {
                    shipConPendingShipUI.RemoveAt(i);
                    continue;
                }

                bool rescued = false;

                // Try system owner
                if (shipCon.ShipData.CurrentStarSysController != null)
                {
                    var sysCon = shipCon.ShipData.CurrentStarSysController;
                    if (shipCon.ShipListUIGameObject != null && sysCon.StarSysData.ShipListUIParent != null)
                    {
                        shipCon.ShipListUIGameObject.transform.SetParent(
                            sysCon.StarSysData.ShipListUIParent.transform,
                            false
                        );

                        shipCon.ShipListUIGameObject.SetActive(true);

                        Debug.Log($"  ✅ Rescued ship UI '{shipCon.ShipData.ShipName}' to system '{sysCon.name}'");

                        shipConPendingShipUI.RemoveAt(i);
                        rescued = true;
                    }
                }
                // Try fleet owner
                else if (shipCon.ShipData.CurrentFleetController != null)
                {
                    var fleetCon = shipCon.ShipData.CurrentFleetController;
                    if (shipCon.ShipListUIGameObject != null && fleetCon.FleetData.ShipListUIParent != null)
                    {
                        shipCon.ShipListUIGameObject.transform.SetParent(
                            fleetCon.FleetData.ShipListUIParent.transform,
                            false
                        );

                        shipCon.ShipListUIGameObject.SetActive(true);

                        Debug.Log($"  ✅ Rescued ship UI '{shipCon.ShipData.ShipName}' to fleet '{fleetCon.name}'");

                        shipConPendingShipUI.RemoveAt(i);
                        rescued = true;
                    }
                }

                if (!rescued)
                {
                    Debug.LogWarning($"  ⚠️ Could not rescue ship UI '{shipCon.ShipData.ShipName}' - still no parent available");
                }
            }

            Debug.Log($"ProcessPendingShipUIs (ALL): {shipConPendingShipUI.Count} ship UIs still pending");
        }
    }
}
