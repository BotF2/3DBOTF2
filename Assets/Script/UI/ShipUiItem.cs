using Assets.Core;
using UnityEngine;

public class ShipUiItem : MonoBehaviour
{
    public ShipController ShipController;       // Reference to underlying ship
    public FleetController CurrentFleet; // Who currently owns the ship UI
    public StarSysController CurrentStarSys; // Which star system currently owns the ship UI
}
