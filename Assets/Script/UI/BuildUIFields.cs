using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Represents the collection of UI elements for a star system build panel (sliders, text, grids, slots, images, buttons).
/// Attached to the SysBuildUIListPrefab and ShipSliderPrefab and wired references in the Inspector.
/// StarSysManager will read these fields to wire runtime references instead of searching the hierarchy by name.
/// </summary>
public class BuildUIFields : MonoBehaviour
{
    [Header("Sliders")]
    public Slider shipBuildProgress;
    public Slider factoryBuildProgress;

    [Header("Text")]
    public TextMeshProUGUI systemNameTMP;

    [Header("Grid Layouts")]
    public GridLayoutGroup queueHoldingBuildables;
    public GridLayoutGroup queueHoldingBuildableShips;

    [Header("Inventory Slot Parents (assign matching GameObjects in prefab)")]
    public GameObject powerPlantInventorySlot;
    public GameObject factoryInventorySlot;
    public GameObject shipyardInventorySlot;
    public GameObject shieldGenInventorySlot;
    public GameObject orbitalBatteryInventorySlot;
    public GameObject researchCenterInventorySlot;

    [Header("Ship Inventory Slots")]
    public GameObject scoutInventorySlot;
    public GameObject destroyerInventorySlot;
    public GameObject cruiserInventorySlot;
    public GameObject ltCruiserInventorySlot;
    public GameObject hvyCruiserInventorySlot;
    public GameObject transportInventorySlot;

    //[Header("Facility Buttons")]
    //public Button factoryButtonOn;
    //public Button factoryButtonOff;
    //public Button yardButtonOn;
    //public Button yardButtonOff;
    //public Button shieldButtonOn;
    //public Button shieldButtonOff;
    //public Button oBButtonOn;
    //public Button oBButtonOff;
    //public Button researchButtonOn;
    //public Button researchButtonOff;

    //[Header("System Action Buttons")]
    //public Button buildButton;
    //public Button shipButton;
    //public Button newFleetButton;
    //public Button mergeFleetButton;
    //public Button shipDeployButton;
    //public Button cancelShipManagerButton;

    //[Header("Images")]
    //public Image powerUnitImage;
    //public Image factoryImage;
    //public Image shipyardImage;
    //public Image shieldPlantImage;
    //public Image orbitalBatteriesImage;
    //public Image researchImage;
    //public Image powerOverloadImage;

    [Header("Close / Misc Buttons")]
    public Button[] closeButtons;

    // Optional: allow access to other important child transforms if needed
    [Header("Optional slots / helpers")]
    public Transform[] slots;
}
