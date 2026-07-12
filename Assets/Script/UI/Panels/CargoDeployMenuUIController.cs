using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Core;
using BOTF3D.Galaxy;
using BOTF3D.Combat;

namespace BOTF3D.UI
{
    /// <summary>
    /// Panel for loading/unloading population units onto transport ships docked at a star system.
    /// Structurally parallel to ShipDeployMenuUIController but scoped to a single system at a time
    /// (cargo transfer is local: population never leaves the system except aboard a ship that warps out).
    /// </summary>
    public class CargoDeployMenuUIController : MonoBehaviour
    {
        public static CargoDeployMenuUIController Instance;

        [Header("References (assign in Inspector)")]
        public GameObject CargoDeployPanel;
        public GameObject PopulationSlot; // holds the single draggable PopulationCargoTokenUI
        public GameObject GroundForceSlot; // holds the single draggable GroundForceCargoTokenUI
        public GameObject TransportListSlot; // parent that CargoTransportRowUI rows are instantiated under
        public GameObject transportRowPrefab; // prefab with CargoTransportRowUI + CargoDropSlot
        public Button saveCloseButton;

        // Cached directly rather than re-found via GetComponentInChildren on every refresh: the token
        // is temporarily reparented to the canvas root while being dragged (see PopulationCargoTokenUI.
        // OnBeginDrag), and OnDrop fires before OnEndDrag restores it under PopulationSlot — a hierarchy
        // search at that moment would silently miss it and the count text would never update.
        private PopulationCargoTokenUI populationToken;
        private GroundForceCargoTokenUI groundForceToken;

        public StarSysController CurrentStarSys;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ShowCargoMenuView(StarSysController sysCon)
        {
            CurrentStarSys = sysCon;

            if (CargoDeployPanel != null)
                CargoDeployPanel.SetActive(true);

            if (saveCloseButton == null && CargoDeployPanel != null)
                saveCloseButton = CargoDeployPanel.transform.Find("SaveClose")?.GetComponent<Button>();

            if (saveCloseButton != null)
            {
                saveCloseButton.onClick.RemoveAllListeners();
                saveCloseButton.onClick.AddListener(CloseCargoMenu);
            }

            RefreshView();
        }

        public void CloseCargoMenu()
        {
            if (CargoDeployPanel != null)
                CargoDeployPanel.SetActive(false);
            CurrentStarSys = null;
        }

        public void RefreshView()
        {
            RefreshPopulationToken();
            RefreshGroundForceToken();
            RefreshTransportRows();
            RefreshSystemPanel();
        }

        /// <summary>Pushes the current StarSysData back into the system's own SystemUI_Prefab (numPopulation,
        /// GroundForceGrid icons, etc). CargoDeployPanel edits StarSysData directly, so without this the
        /// system panel would show stale numbers/icons until it's closed and reopened.</summary>
        private void RefreshSystemPanel()
        {
            var sysData = CurrentStarSys?.StarSysData;
            if (sysData == null || CurrentStarSys.StarSysUIGameObject == null) return;

            var sysUIFields = CurrentStarSys.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
            if (sysUIFields != null)
                sysUIFields.InitializeFromStarSysData(sysData);
        }

        private void RefreshPopulationToken()
        {
            if (populationToken == null)
            {
                if (PopulationSlot == null) return;
                populationToken = PopulationSlot.GetComponentInChildren<PopulationCargoTokenUI>(true);
            }

            populationToken?.SetAvailable(CurrentStarSys?.StarSysData?.Population ?? 0);
        }

        private void RefreshGroundForceToken()
        {
            if (groundForceToken == null)
            {
                if (GroundForceSlot == null) return;
                groundForceToken = GroundForceSlot.GetComponentInChildren<GroundForceCargoTokenUI>(true);
            }

            groundForceToken?.SetAvailable(CurrentStarSys?.StarSysData?.GroundForces?.Count ?? 0);
        }

        private void RefreshTransportRows()
        {
            if (TransportListSlot == null || transportRowPrefab == null) return;

            foreach (Transform child in TransportListSlot.transform)
                Destroy(child.gameObject);

            var ships = CurrentStarSys?.StarSysData?.ShipsList;
            if (ships == null) return;

            foreach (var ship in ships)
            {
                if (ship?.ShipData == null || ship.ShipData.ShipType != ShipType.Transport) continue;

                var rowGO = Instantiate(transportRowPrefab, TransportListSlot.transform);
                var row = rowGO.GetComponent<CargoTransportRowUI>();
                row?.Setup(ship);
            }
        }

        /// <summary>Called by CargoDropSlot when a population token is dropped on a transport row.</summary>
        public bool TryLoadPopulationOnto(ShipController transport, int units = 1)
        {
            var sysData = CurrentStarSys?.StarSysData;
            if (transport?.ShipData == null || sysData == null) return false;

            var shipData = transport.ShipData;
            if (shipData.LoadedGroundForces > 0) return false; // a transport carries one cargo type at a time

            int room = shipData.CargoCapacity - (shipData.LoadedPopulation + shipData.LoadedGroundForces);
            int move = Mathf.Min(units, room, sysData.Population);
            if (move <= 0) return false;

            sysData.Population -= move;
            shipData.LoadedPopulation += move;
            UpdateTransportDesignation(shipData);
            RefreshView();
            return true;
        }

        /// <summary>Called by the unload button on a transport row to return cargo to the system's population.</summary>
        public bool TryUnloadPopulationFrom(ShipController transport, int units = 1)
        {
            var sysData = CurrentStarSys?.StarSysData;
            if (transport?.ShipData == null || sysData == null) return false;

            var shipData = transport.ShipData;
            int move = Mathf.Min(units, shipData.LoadedPopulation);
            if (move <= 0) return false;

            shipData.LoadedPopulation -= move;
            sysData.Population += move;
            UpdateTransportDesignation(shipData);
            RefreshView();
            return true;
        }

        /// <summary>Called by CargoDropSlot when a ground force token is dropped on a transport row.
        /// Ground forces share CargoCapacity with population, so room is computed against both.</summary>
        public bool TryLoadGroundForceOnto(ShipController transport, int units = 1)
        {
            var sysData = CurrentStarSys?.StarSysData;
            if (transport?.ShipData == null || sysData == null) return false;

            var shipData = transport.ShipData;
            if (shipData.LoadedPopulation > 0) return false; // a transport carries one cargo type at a time

            int room = shipData.CargoCapacity - (shipData.LoadedPopulation + shipData.LoadedGroundForces);
            int move = Mathf.Min(units, room, sysData.GroundForces.Count);
            if (move <= 0) return false;

            for (int i = 0; i < move; i++)
            {
                int last = sysData.GroundForces.Count - 1;
                var unitGO = sysData.GroundForces[last];
                sysData.GroundForces.RemoveAt(last);
                if (unitGO != null)
                    Destroy(unitGO);
            }

            shipData.LoadedGroundForces += move;
            UpdateTransportDesignation(shipData);
            RefreshView();
            return true;
        }

        /// <summary>Called by the unload button on a transport row to return loaded ground forces to the system.</summary>
        public bool TryUnloadGroundForceFrom(ShipController transport, int units = 1)
        {
            var sysData = CurrentStarSys?.StarSysData;
            if (transport?.ShipData == null || sysData == null || CurrentStarSys == null) return false;

            var shipData = transport.ShipData;
            int move = Mathf.Min(units, shipData.LoadedGroundForces);
            if (move <= 0) return false;

            shipData.LoadedGroundForces -= move;
            for (int i = 0; i < move; i++)
                StarSysManager.Instance.AddGroundForceUnit(CurrentStarSys);

            UpdateTransportDesignation(shipData);
            RefreshView();
            return true;
        }

        /// <summary>Renames a transport for at-a-glance identification based on what it's currently carrying:
        /// Colonyship while population is aboard, Dropship while ground forces are aboard, otherwise its
        /// original name. BaseShipName is captured the first time a rename is needed so it can be restored.</summary>
        private void UpdateTransportDesignation(ShipData shipData)
        {
            if (string.IsNullOrEmpty(shipData.BaseShipName))
                shipData.BaseShipName = shipData.ShipName;

            if (shipData.LoadedPopulation > 0)
                shipData.ShipName = "Colonyship";
            else if (shipData.LoadedGroundForces > 0)
                shipData.ShipName = "Dropship";
            else
                shipData.ShipName = shipData.BaseShipName;
        }
    }
}
