
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Galaxy
{
    public class FleetData
    {
        public int CivIndex;
        public int PlayerId; // network player ID, not used in single player
        public Sprite Insignia;
        public CivController CivController;
        public CivEnum CivEnum;
        public Vector3 Position;
        public List<BOTF3D.Combat.ShipController> ShipsList;
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
        public FleetController InterceptTarget; // non-null while this fleet is in intercept/pursuit mode
        // True from SetInterceptTarget until CancelIntercept, independent of whether InterceptTarget
        // still references a live object. Needed because a destroyed FleetController compares equal to
        // null (UnityEngine.Object's == override), so "InterceptTarget == null" alone can't distinguish
        // "never had a target" from "target was just destroyed".
        public bool IsPursuingIntercept;

        // True for a temporary fleet spawned to ferry redeployed ships to a distant fleet/system.
        public bool IsConvoy;
        // Set when IsConvoy and the redeploy target is another fleet; convoy merges into it on arrival.
        public FleetController ConvoyMergeTarget;
        // Set when IsConvoy and the redeploy target is a star system; convoy deposits ships there on arrival.
        public StarSysController ConvoyMergeSystem;
        private SpriteRenderer[] spriteRenderers;

        public FleetData(FleetSO fleetSO)
        {
            Insignia = fleetSO.Insignia;
            ShipsList = new List<BOTF3D.Combat.ShipController>(fleetSO.ShipsList);
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
        public List<BOTF3D.Combat.ShipController> GetShipList()
        {
            return ShipsList;
        }
        public void SetShipList(List<BOTF3D.Combat.ShipController> newShipList)
        {
            ShipsList = newShipList;
        }
        public void AddToShipList(BOTF3D.Combat.ShipController shipController)
        {
            // Guard here (not just in FleetController's wrapper) since several call sites add
            // directly to ShipsList — a duplicate reference here silently causes a "skipped
            // duplicate add" warning later, whenever this fleet is merged into another.
            if (shipController != null && !ShipsList.Contains(shipController))
                ShipsList.Add(shipController);
        }
        public void RemoveFromShipList(BOTF3D.Combat.ShipController shipController)
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



