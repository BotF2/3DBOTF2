
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

        // Star system this fleet currently occupies a dock slot at (see FleetDockLayout /
        // StarSysData.ClaimFleetDockSlot), null once the fleet has moved away. Set by
        // FleetManager.InstantiateFleet when the fleet is created at a system; released by
        // ReleaseDockSlotIfAny the moment the fleet actually starts moving.
        public StarSysController DockedStarSys;
        public int DockedSlotIndex = -1;

        // Uninhabited, habitable system this fleet is currently in contact with, or null. Set by
        // FleetController.OnTriggerEnter's arrival-trigger the moment a traveling fleet reaches such
        // a system (the same spot that already opens HabitableSysUIController's informational popup);
        // cleared on arrival at any other system. Read by FleetMenuUIController to enable the Colonize
        // order button - see StarSysController.ColonizeWithTransport for what clicking it does.
        public StarSysController ColonizableSystem;

        public void ReleaseDockSlotIfAny()
        {
            if (DockedStarSys == null) return;
            DockedStarSys.StarSysData.ReleaseFleetDockSlot(DockedSlotIndex);
            DockedStarSys = null;
            DockedSlotIndex = -1;
        }

        // Simpler positioning used when THIS fleet spawns a new fleet off itself (splitting,
        // convoys anchored on a fleet rather than a system) instead of the star-system dock slots
        // above. No occupancy tracking - just nudge each successive split a bit further out, and
        // restart from the first nudge as soon as this fleet's own position has moved since the
        // last split, since a moving fleet already can't leave newly split fleets overlapping it.
        private Vector3 lastSplitAnchorPosition;
        private bool hasSplitAnchor;
        private int splitSpacerCount;

        public Vector3 GetNextSplitOffset(Vector3 anchorPosition)
        {
            if (!hasSplitAnchor || anchorPosition != lastSplitAnchorPosition)
            {
                splitSpacerCount = 0;
                lastSplitAnchorPosition = anchorPosition;
                hasSplitAnchor = true;
            }

            float step = 15f + splitSpacerCount * 5f;
            splitSpacerCount++;
            return new Vector3(-step, step, 0f);
        }

        // True for a temporary fleet spawned to ferry redeployed ships to a distant fleet/system.
        public bool IsConvoy;
        // Set when IsConvoy and the redeploy target is another fleet; convoy merges into it on arrival.
        public FleetController ConvoyMergeTarget;
        // Set when IsConvoy and the redeploy target is a star system; convoy deposits ships there on arrival.
        public StarSysController ConvoyMergeSystem;
        private SpriteRenderer[] spriteRenderers;

        // Backs ShipController.ShipData.ShipID assignment in ShipFactory.LinkShipToParent. Ships have
        // no NetworkIdentity of their own (see FleetController's comments on why ship data isn't
        // networked), so a server-authoritative Command that transfers one between fleets (see
        // FleetManager.ServerTransferShip) needs some other stable way to say "this specific ship" -
        // a sequence number scoped to this fleet's own (already-synced) FleetInt is guaranteed to match
        // between the server and any client, since a fleet's starting ships are always created together,
        // in the same deterministic order, by the same call (ShipManager.BuildShipsOfFirstFleet /
        // GalaxySceneInitializer.AddTestShipsToFleet) on both sides - unlike a single global counter,
        // which could desync if fleets happen to reconstruct in a different order on a given client.
        private int nextShipCreationSeq = 1; // 0 is reserved to mean "unassigned"
        public int GetNextShipCreationSeq() => nextShipCreationSeq++;

        public FleetData(FleetSO fleetSO)
        {
            Insignia = fleetSO.Insignia;
            // Starts empty - real starting ships are built by ShipManager.BuildShipsOfFirstFleet
            // (from each civ's own ShipSO list, see ShipSOProvider.GetStartingFleetShips), or by
            // the ship-deploy UI for player-formed fleets. ShipController is a scene MonoBehaviour,
            // so a ScriptableObject like FleetSO was never able to hold real ship references here.
            ShipsList = new List<BOTF3D.Combat.ShipController>();
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



