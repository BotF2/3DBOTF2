
using BOTF3D.GamePlay;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.Core
{
    public class FleetData
    {
        public int CivIndex;
        public int PlayerId; // network player ID, not used in single player
        public Sprite Insignia;
        public CivController CivController;
        public CivEnum CivEnum;
        public Vector3 Position;
        public List<ShipController> ShipsList;
        public float MaxWarpFactor = 3f;
        public float CurrentWarpFactor = 0f;
        public GameObject Destination;
        public GameObject LastDestination;
        public GameObject ShipListUIParent { get; internal set; }
        public string CivLongName;
        public string CivShortName;
        public string FleetName;
        private string description;
        public int FleetInt;
        public List<int> EncounterIDs;
        public Button FleetButtonUp;
        public Button FleetButtonDown;
        public Button FleetButtonUIClose;
        public bool WarpButtonPressed = false;
        private SpriteRenderer[] spriteRenderers;

        public FleetData(FleetSO fleetSO)
        {
            Insignia = fleetSO.Insignia;
            ShipsList = fleetSO.ShipsList;
            MaxWarpFactor = fleetSO.MaxWarpFactor;
            description = fleetSO.Description;
            CivIndex = fleetSO.CivIndex;
            CivEnum = fleetSO.CivOwnerEnum;
            CivLongName = CivManager.Instance.GetCivDataByCivEnum(CivEnum).CivLongName;
            CivShortName = CivManager.Instance.GetCivDataByCivEnum(CivEnum).CivShortName;
            IEnumerable<CivController> ourCivManagers =
                    from x in CivManager.Instance.CivControllersInGame
                    where (x.CivData.CivInt == (int)CivEnum)
                    select x;
            CivController = ourCivManagers.ToList().FirstOrDefault();
        }
        public FleetData(string name)
        {
            FleetName = name;
        }
        public FleetData()
        {

        }
        public List<ShipController> GetShipList()
        {
            return ShipsList;
        }
        public void SetShipList(List<ShipController> newShipList)
        {
            ShipsList = newShipList;
        }
        public void AddToShipList(ShipController shipController)
        {
            ShipsList.Add(shipController);
        }
        public void RemoveFromShipList(ShipController shipController)
        {
            ShipsList.Remove(shipController);
        }
        public float GetMaxWarpFactor()
        {
            return MaxWarpFactor;
        }
        public string GetDescription()
        {
            return description;
        }
        public Vector3 GetPosition()
        {
            return Position;
        }

        public string GetFleetName() { return this.FleetName; }

        internal void PopulateShipsList()
        {
            //throw new NotImplementedException();
        }
        public void SetVisible(bool visible)
        {
            foreach (var sr in spriteRenderers)
                sr.enabled = visible;
        }
    }
}



