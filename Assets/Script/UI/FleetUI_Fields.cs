// Ignore Spelling: Minimap

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



public class FleetUI_Fields : MonoBehaviour
{
    [Header("GameObjects")]
    public GameObject FleetShipContentGO;  // Container inside the ship scroll view
    public GameObject ShipScrollView;      // The ScrollRect parent (Ship Scroll View GO)

    [Header("Ship List Expansion")]
    public Button ExpandShipsButton;
    [Tooltip("Height of the Ship Scroll View in collapsed (default) state. " +
             "Default = 6 rows × 29 px (25 cell + 4 spacing) = 174. " +
             "Expand button only appears when ship count exceeds 6 × 8 = 48.")]
    public float CollapsedShipViewHeight = 174f;

    [Header("RectTrans Mini map")]
    public RectTransform MinimapRedDot;

    [Header("Buttons")]
    public Button SelectDestinationCursor;
    public Button DestinationDragTarget;
    public Button CancelDestination;
    public Button SelectDestination;
    public Button InterceptTargetButton; // pursue / intercept a moving enemy fleet
    public Button CancelInterceptButton; // cancel active intercept
    public Button WarpUp;
    public Button WarpDown;
    public Button CloseFleetUI;
    public Button NewFleetButton;
    public Button MergeFleetsButton;
    public Button ShipDeployButton;
    public Button CancelShipManagerButton;
    public Button ColonizeButton; // active when fleet contains a Transport and is in contact with an uninhabited, habitable system
    public Button ClaimSystemButton; // active whenever fleet is in contact with an uninhabited, habitable or terraformable system - no Transport required
    public Button TerraformButton; // active when fleet contains a Transport and is in contact with an uninhabited, terraformable (not yet habitable) system

    [Header("Sliders")]
    public Slider WarpSlider;

    [Header("Text")]
    public TextMeshProUGUI FleetNameText;
    public TextMeshProUGUI DestinationName;
    public TextMeshProUGUI DestinationCoordinates;
    public TextMeshProUGUI WarpValueText;
}
