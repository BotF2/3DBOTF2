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
    // Borg Transwarp Hub Network (TranswarpHubController.CanTranswarpHome, §8 II.3) - active when
    // this fleet is docked at a Borg-owned system with a separate Borg home system to jump to.
    // Assign a Button GameObject to this slot in the Editor (same pattern as ClaimSystemButton/
    // TerraformButton above) before it appears in-game - see FleetMenuUIController.ClickTranswarpButton.
    public Button TranswarpButton;
    // Romulan/Klingon cloak arc (CloakingController.CanToggleCloak, §8 II.3) - shown once this
    // fleet's civ has completed Basic Cloaking Field/Battle Cloak, regardless of tier (every other
    // civ never sees this button at all). A player-toggled on/off, not an automatic always-on state -
    // see FleetMenuUIController.ClickCloakToggleButton and FleetData.IsCloakActive's own comment.
    // Same Editor-wiring pattern as TranswarpButton above.
    public Button CloakToggleButton;
    // System Invasion Phase 1 siege (Docs/Design/SystemInvasion_Phase1_Design.md §4) - shown only
    // while this fleet is actively besieging a system (FleetController.IsBesiegingSystem), letting
    // the player voluntarily give up the siege and free the fleet to move again instead of it being
    // possible only once Invasion.3/4's real Bombard/Invade panel ships. See
    // FleetMenuUIController.ClickBreakOffSiegeButton. Same Editor-wiring pattern as TranswarpButton
    // above - assign a Button GameObject to this slot before it appears in-game.
    public Button BreakOffSiegeButton;

    [Header("Sliders")]
    public Slider WarpSlider;

    [Header("Text")]
    public TextMeshProUGUI FleetNameText;
    public TextMeshProUGUI DestinationName;
    public TextMeshProUGUI DestinationCoordinates;
    public TextMeshProUGUI WarpValueText;
}
