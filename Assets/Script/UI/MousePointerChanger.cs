using Assets.Core;
using System;
using UnityEngine;

public class MousePointerChanger : MonoBehaviour
{
    public static MousePointerChanger Instance;
    // Reference to the new cursor texture
    [SerializeField]
    private Texture2D galaxyMapCursorForFedDestination;
    [SerializeField]
    private Texture2D galaxyMapCursorForRomDestination;
    [SerializeField]
    private Texture2D galaxyMapCursorForKlingDestination;
    [SerializeField]
    private Texture2D galaxyMapCursorForCardDestination;
    [SerializeField]
    private Texture2D galaxyMapCursorForDomDestination;
    [SerializeField]
    private Texture2D galaxyMapCursorForBorgDestination;
    [SerializeField]
    private Texture2D galaxyMapCursorShipHandGrab;
    [SerializeField]
    private Texture2D galaxyMapCursorTerran;
   // public FleetController fleetConBehindGalaxyMapDestinationCursor = null;
    //public FleetController fleetConBehindGalaxyMapShipCursor = null; 
    //public StarSysController sysConBehindGalaxyMapShipCursor = null; 
    // Define the hot spot of the cursor (the point that will be the "clicking" point)
    private Vector2 hotSpot = Vector2.zero;
    // uses Unity's software cursor mode
    public CursorMode cursorMode = CursorMode.Auto;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    public void SetShipExchangeCursor(FleetController fleetCon)
    {
        ChangeCursor(galaxyMapCursorShipHandGrab, hotSpot, cursorMode, true);
        //GalaxyMenuUIController.Instance.FleetLookingForShipExchange = fleetCon;
        GalaxyMenuUIController.Instance.CurrentClickMode = GalaxyClickMode.SelectForShipExchange;
    }
    public void SetShipExchangeCursor(StarSysController sysCon)
    {
        ChangeCursor(galaxyMapCursorShipHandGrab, hotSpot, cursorMode, true);
        //GalaxyMenuUIController.Instance.StarSysLookingForShipExchange = sysCon;
        GalaxyMenuUIController.Instance.CurrentClickMode = GalaxyClickMode.SelectForShipExchange;
    }
    public void SetShipExchangeCursor()
    {
        ChangeCursor(galaxyMapCursorShipHandGrab, hotSpot, cursorMode, true);
        GalaxyMenuUIController.Instance.CurrentClickMode = GalaxyClickMode.SelectForShipExchange;

    }
    public void ChangeToCivSpacificGalaxyMapCursor()
    {
        if (GameController.Instance.AreWeLocalPlayer(CivEnum.FED))
            ChangeCursor(galaxyMapCursorForFedDestination, hotSpot, cursorMode, false);
        else if (GameController.Instance.AreWeLocalPlayer(CivEnum.ROM))
            ChangeCursor(galaxyMapCursorForRomDestination, hotSpot, cursorMode, false);
        else if (GameController.Instance.AreWeLocalPlayer(CivEnum.KLING))
            ChangeCursor(galaxyMapCursorForKlingDestination, hotSpot, cursorMode, false);
        else if (GameController.Instance.AreWeLocalPlayer(CivEnum.CARD))
            ChangeCursor(galaxyMapCursorForCardDestination, hotSpot, cursorMode, false);
        else if (GameController.Instance.AreWeLocalPlayer(CivEnum.DOM))
            ChangeCursor(galaxyMapCursorForDomDestination, hotSpot, cursorMode, false);
        else if (GameController.Instance.AreWeLocalPlayer(CivEnum.BORG))
            ChangeCursor(galaxyMapCursorForBorgDestination, hotSpot, cursorMode, false);
        else ChangeCursor(galaxyMapCursorTerran, hotSpot, cursorMode, false);
    }

    // Function to change the cursor
    private void ChangeCursor(Texture2D cursorTexture, Vector2 hotSpot, CursorMode cursorMode, bool fromShipManager)
    {
        Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);
    }

    // Reset to default cursor
    public void ResetCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, cursorMode);
    }

    internal void SetDestinationCursor(FleetController fleetCon)
    {
        ChangeToCivSpacificGalaxyMapCursor();
        GalaxyMenuUIController.Instance.CurrentClickMode = GalaxyClickMode.SetDestination;
    }
    internal void SetDestinationCursor()
    {
        ChangeToCivSpacificGalaxyMapCursor();
        GalaxyMenuUIController.Instance.CurrentClickMode = GalaxyClickMode.SetDestination;
    }
}

