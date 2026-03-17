// Ignore Spelling: Minimap

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FleetUI_Fields : MonoBehaviour
{
    [Header("GameObjects")]
    public GameObject FleetShipContentGO;

    [Header("RectTrans Mini map")]
    public RectTransform MinimapRedDot;

    [Header("Buttons")]
    public Button SelectDestinationCursor;
    public Button DestinationDragTarget;
    public Button CancelDestination;
    public Button SelectDestination;
    public Button WarpUp;
    public Button WarpDown;
    public Button CloseFleetUI;
    public Button NewFleetButton;
    public Button MergeFleetsButton;
    public Button ShipDeployButton;
    public Button CancelShipManagerButton;

    [Header("Sliders")]
    public Slider WarpSlider;

    [Header("Text")]
    public TextMeshProUGUI FleetNameText;
    public TextMeshProUGUI DestinationName;
    public TextMeshProUGUI DestinationCoordinates;
    public TextMeshProUGUI WarpValueText;
}
