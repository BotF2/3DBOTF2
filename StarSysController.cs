// Add this method to StarSysController to fix CS1061
public void RemoveShipFromSystem(ShipController shipController)
{
    if (StarSysData != null && StarSysData.ShipsList != null && shipController != null)
    {
        StarSysData.ShipsList.Remove(shipController);
    }
}