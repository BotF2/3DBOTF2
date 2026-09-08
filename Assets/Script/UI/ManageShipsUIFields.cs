using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the root of ManageShipsUI_Prefab - the shared, on-demand ship/fleet/cargo management
/// overlay for one star system at a time. Mirrors BuildUIFields/StarSysManager.
/// InstantiateSysBuildUI's pattern exactly: one prefab instance, reused (re-populated, not
/// destroyed) if the same system reopens it, destroyed and recreated if a different system does.
///
/// This holds everything StarSysMenuUIController.PopulateManageShipsUI needs to move a system's
/// ship list into and wire up: the ship grid (moved out of SystemUI_Prefab/ExpandedContent's
/// shipContent when this opens, and moved back via StarSysMenuUIController.
/// ReturnShipsFromManageShipsUI when it closes - ships are single live GameObjects that only ever
/// live in one place at a time), the fleet-management buttons, and the three action dropdowns
/// (Scrap Ships / Load / Unload).
/// </summary>
public class ManageShipsUIFields : MonoBehaviour
{
    [Header("Header")]
    public TextMeshProUGUI systemNameText;
    [Tooltip("Any number of buttons that close this panel (mirrors BuildUIFields.closeButtons) - wired by StarSysManager.InstantiateManageShipsUI to HideManageShipsUI.")]
    public Button[] closeButtons;

    [Header("Ship List")]
    [Tooltip("Content transform ships are parented under - same GridLayoutGroup/ContentSizeFitter setup StarSysMenuUIController.SetupShipGrid applies to StarSysUI_Fields.shipContent.")]
    public RectTransform shipContent;
    [Tooltip("The ScrollView GameObject wrapping shipContent (needs a Viewport child and a ScrollRect, same as StarSysUI_Fields.ShipScrollView).")]
    public GameObject shipScrollView;

    [Header("Fleet Buttons")]
    public Button newFleetButton;
    public Button mergeFleetButton;
    public Button shipDeployButton;

    [Header("Action Dropdowns")]
    [Tooltip("Option 0 is the fixed \"Scrap Ships\" label; options 1+ are eligible ships, oldest TechLevel first.")]
    public TMP_Dropdown scrapShipsDropdown;
    [Tooltip("Option 0 is the fixed \"Load\" label; options 1+ are Colony/Troops/Terraform.")]
    public TMP_Dropdown loadDropdown;
    [Tooltip("Option 0 is the fixed \"Unload\" label; options 1+ are docked transports carrying cargo.")]
    public TMP_Dropdown unloadDropdown;
}
