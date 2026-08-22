using BOTF3D.Combat;
using BOTF3D.Core;
using BOTF3D.Galaxy;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    /// <summary>
    /// Manages the Scrap Ship panel. Open via the system UI Scrap button (shipyard required).
    ///
    /// Unity Editor setup — attach this script to a panel GameObject under the GalaxyScene canvas.
    /// Required child references (assign in Inspector):
    ///   panelRoot          — the root GameObject to show/hide (can be this.gameObject)
    ///   rowContainer       — Vertical Layout Group content transform (inside a Scroll View)
    ///   scrapRowPrefab     — ScrapShipRow prefab with ScrapShipRowUI component
    ///   confirmButton      — "Scrap Selected Ship" button; disabled when nothing selected
    ///   closeButton        — closes the panel without scrapping
    ///   dilithiumLabel     — shows "Recover X Li2" for the selected ship
    ///   noShipsLabel       — shown when no scrappable ships are at this system
    ///   systemNameLabel    — displays the current system name in the panel header
    /// </summary>
    public class ScrapPanelUIController : MonoBehaviour
    {
        public static ScrapPanelUIController Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform rowContainer;
        [SerializeField] private GameObject scrapRowPrefab;

        [Header("Buttons & Labels")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI dilithiumLabel;
        [SerializeField] private TextMeshProUGUI noShipsLabel;
        [SerializeField] private TextMeshProUGUI systemNameLabel;

        private StarSysController _currentSystem;
        private ScrapShipRowUI _selectedRow;
        private readonly List<ScrapShipRowUI> _rows = new List<ScrapShipRowUI>();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(ClickConfirm);
                confirmButton.interactable = false;
            }
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void Open(StarSysController sysCon)
        {
            if (sysCon == null) return;
            _currentSystem = sysCon;
            _selectedRow = null;

            if (panelRoot != null) panelRoot.SetActive(true);
            if (systemNameLabel != null) systemNameLabel.text = sysCon.StarSysData.SysName;
            if (dilithiumLabel != null) dilithiumLabel.text = string.Empty;
            if (confirmButton != null) confirmButton.interactable = false;

            PopulateRows();
        }

        public void Close()
        {
            _currentSystem = null;
            _selectedRow = null;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void PopulateRows()
        {
            ClearRows();

            if (_currentSystem == null) return;

            // Gather all scrappable ships: system garrison + any fleet docked here
            var ships = GatherScrappableShips(_currentSystem);

            bool hasAny = ships.Count > 0;
            if (noShipsLabel != null) noShipsLabel.gameObject.SetActive(!hasAny);
            if (!hasAny) return;

            TechLevel civTech = _currentSystem.StarSysData.CurrentCivController?.CivData?.CurrentTechLevel
                                ?? TechLevel.EARLY;

            // Sort: widest tech gap first (most obsolete), then highest dilithium recovery
            ships.Sort((a, b) =>
            {
                int gapA = (int)civTech - (int)a.ShipData.BuiltAtTechLevel;
                int gapB = (int)civTech - (int)b.ShipData.BuiltAtTechLevel;
                if (gapB != gapA) return gapB.CompareTo(gapA);
                return b.ShipData.DilithiumCost.CompareTo(a.ShipData.DilithiumCost);
            });

            foreach (var ship in ships)
            {
                var rowGO = Instantiate(scrapRowPrefab, rowContainer);
                var row = rowGO.GetComponent<ScrapShipRowUI>();
                if (row == null) continue;
                row.Populate(ship, civTech, OnRowSelected);
                _rows.Add(row);
            }
        }

        private void ClearRows()
        {
            foreach (var row in _rows)
                if (row != null) Destroy(row.gameObject);
            _rows.Clear();
        }

        private List<ShipController> GatherScrappableShips(StarSysController sysCon)
        {
            var result = new List<ShipController>();
            CivEnum owner = sysCon.StarSysData.CurrentOwnerCivEnum;

            // Ships garrisoned at the system
            foreach (var ship in sysCon.StarSysData.ShipsList)
            {
                if (IsEligible(ship, owner)) result.Add(ship);
            }

            // Ships in any fleet docked at this system
            if (FleetManager.Instance != null)
            {
                foreach (var fleet in FleetManager.Instance.FleetControllerList)
                {
                    if (fleet == null || fleet.FleetData == null) continue;
                    if (fleet.FleetData.DockedStarSys != sysCon) continue;
                    if (fleet.FleetData.CivEnum != owner) continue;
                    foreach (var ship in fleet.FleetData.ShipsList)
                    {
                        if (IsEligible(ship, owner)) result.Add(ship);
                    }
                }
            }

            return result;
        }

        private bool IsEligible(ShipController ship, CivEnum owner) =>
            ship != null && ship.ShipData != null
            && !ship.ShipData.Distroyed
            && ship.ShipData.CivEnum == owner
            && ship.ShipData.ShipType != ShipType.OrbitalBattery; // batteries are facility-linked, not scrappable

        private void OnRowSelected(ScrapShipRowUI row)
        {
            // Deselect previous
            if (_selectedRow != null) _selectedRow.SetSelected(false);

            _selectedRow = row;
            _selectedRow.SetSelected(true);

            if (confirmButton != null)
                confirmButton.interactable = _selectedRow != null;

            if (dilithiumLabel != null && row?.ShipController != null)
                dilithiumLabel.text = $"Recover  {row.ShipController.ShipData.DilithiumCost}  Li2";
        }

        private void ClickConfirm()
        {
            if (_selectedRow == null || _currentSystem == null) return;
            var ship = _selectedRow.ShipController;
            if (ship == null || ship.ShipData == null || ship.ShipData.Distroyed) return;

            StarSysManager.Instance?.ScrapShip(ship, _currentSystem);

            // Refresh so the scrapped ship no longer appears
            _selectedRow = null;
            if (confirmButton != null) confirmButton.interactable = false;
            if (dilithiumLabel != null) dilithiumLabel.text = string.Empty;
            PopulateRows();

            // Refresh the compact header dilithium counter in the system UI
            if (_currentSystem.StarSysUIGameObject != null)
                _currentSystem.StarSysUIGameObject.GetComponent<StarSysUI_Fields>()
                    ?.compactHeader?.RefreshDilithium();
        }
    }
}
