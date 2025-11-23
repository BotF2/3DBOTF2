using Assets.Core;
using UnityEngine;
/// <summary>
/// This class is attached to each ship UI item in the ship list UI 
/// along with the ShipListItemDrag script to hold references to the underlying ship (ShipController),
/// </summary>
public class ShipListUI_Item : MonoBehaviour
{
    public ShipController ShipController; // Reference to underlying ship
    public FleetController CurrentFleet; // Who currently owns the ship UI
    public StarSysController CurrentStarSyst; // Which star system currently owns the ship UI
}
