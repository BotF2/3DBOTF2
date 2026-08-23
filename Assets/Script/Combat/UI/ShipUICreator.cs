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
        private readonly GameObject shipListTransportUIPrefab;
        private readonly List<ShipController> shipConPendingShipUI = new List<ShipController>();
        private int shipIndex = 0;

        public ShipUICreator(GameObject uiPrefab, GameObject transportUIPrefab = null)
        {
            shipListUIPrefab = uiPrefab;
            shipListTransportUIPrefab = transportUIPrefab;
        }

        /// <summary>
        /// Create ship list UI for a ship
        /// </summary>
        public void InstantiateShipListUI(ShipController shipCon, GameObject parentGO)
        {
            Debug.Log($"=== InstantiateShipListUI called for ship '{shipCon?.ShipData?.ShipName}' ===");
            Debug.Log($"  Ship CivEnum: {shipCon?.ShipData?.CivEnum}");
            Debug.Log($"  OurCiv: {GameController.Instance?.GetOurCiv()}");
            Debug.Log($"  Match: {shipCon != null && GameController.Instance != null && GameController.Instance.AreWeLocalPlayer(shipCon.ShipData.CivEnum)}");

            // Only create UI for local player ships. GameData.LocalPlayerCivEnum is a racy cache
            // (see GameController.AreWeLocalPlayer's comment) - reading it directly here left every
            // remote client's own ship-list UI stuck comparing against the stale FED default.
            if (!GameController.Instance.AreWeLocalPlayer(shipCon.ShipData.CivEnum))
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

            // Instantiate UI prefab — transports get the wider prefab with cargo indicator
            bool isTransport = shipCon.ShipData.ShipType == ShipType.Transport;
            GameObject prefabToUse = (isTransport && shipListTransportUIPrefab != null)
                ? shipListTransportUIPrefab
                : shipListUIPrefab;
            GameObject thisShipListUIGameObject = Object.Instantiate(prefabToUse, Vector3.zero, Quaternion.identity);

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

            // Setup ship type label and warp number.
            // Prefer ShipListingUI component (add it to the prefab in the Inspector for named-field control).
            // Fallback: find WarpNum child by name; use first other TMP for the type+era label.
            var listingUI = thisShipListUIGameObject.GetComponent<BOTF3D.UI.ShipListingUI>();
            if (listingUI != null)
            {
                listingUI.Populate(shipCon.ShipData);
            }
            else
            {
                var warpNumGO  = thisShipListUIGameObject.transform.Find("WarpNum");
                var warpText   = warpNumGO?.GetComponent<TextMeshProUGUI>();
                if (warpText != null)
                    warpText.text = shipCon.ShipData.maxWarpFactor.ToString("0.##");

                // First TMP child that is NOT the WarpNum object gets the type + era label
                foreach (var tmp in thisShipListUIGameObject.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (warpNumGO != null && tmp.transform.IsChildOf(warpNumGO)) continue;
                    tmp.text = BOTF3D.UI.ShipListingUI.FormatShipTypeLabel(
                        shipCon.ShipData.ShipType, shipCon.ShipData.BuiltAtTechLevel);
                    break;
                }

                // Hull bar fill — nested at HullBar/HullBarFill; HullBar itself is a plain RectTransform
                var hullBarImg = (thisShipListUIGameObject.transform.Find("HullBar/HullBarFill")
                               ?? thisShipListUIGameObject.transform.Find("HullBarFill"))
                                    ?.GetComponent<Image>();
                BOTF3D.UI.ShipListingUI.ApplyHealthBar(hullBarImg, shipCon.ShipData);
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

            // Refresh cargo indicator (transport prefab only; no-op on standard prefab)
            // Pass true to include inactive children — CargoImage may start inactive in the prefab
            thisShipListUIGameObject.GetComponentInChildren<BOTF3D.UI.TransportCargoIndicator>(true)
                ?.Refresh(shipCon.ShipData);

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
