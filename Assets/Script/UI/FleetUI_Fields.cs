using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FleetUI_Fields : MonoBehaviour
{
    [Header("GameObjects")]
    public GameObject FleetShipContentGO;

    [Header("RectTransforms")]
    public RectTransform RedDot;

    [Header("Buttons")]
    public Button SelectDestinationCursor;
    public Button DestinationDragTarget;
    public Button CancelDestination;
    public Button SelectDestination;
    public Button WarpUp;
    public Button WarpDown;
    public Button CloseFleetUI;
    public Button NewFleet;
    public Button MergeFleets;
    public Button ShipDeploy;
    public Button CancelShipManager;

    [Header("Sliders")]
    public Slider WarpSlider;

    [Header("Text")]
    public TextMeshProUGUI FleetNameText;
    public TextMeshProUGUI DestinationName;
    public TextMeshProUGUI DestinationCoordinates;
    public TextMeshProUGUI WarpValueText;
}
