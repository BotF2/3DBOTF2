using TMPro;
using UnityEngine;

public class HoverUI3DSystem : MonoBehaviour
{
    public static HoverUI3DSystem Instance;

    // Existing tooltip pieces
    public GameObject toolTipObject;
    public TextMeshProUGUI toolTipText;

    // New: the interactive panel that can be shown (assign your HoverUI3DObjectPanel here)
    [Header("Interactive Panel (assign)")]
    public GameObject HoverUI3DObjectPanel;

    // New: the Button prefab / instance used for "Merge Fleet" (assign the ButtonMergeFleet object from the panel)
    // This GameObject will be reparented to the instantiated Fleet UI when requested.
    public GameObject ButtonMergeFleet;

    void Awake()
    {
        Instance = this;
        Hide();
        // keep panel hidden initially
        if (HoverUI3DObjectPanel != null) HoverUI3DObjectPanel.SetActive(false);
    }

    public void Show(string message, Vector3 position)
    {
        if (toolTipText != null) toolTipText.text = message;
        if (toolTipObject != null)
        {
            toolTipObject.transform.position = position;
            toolTipObject.SetActive(true);
        }
    }

    public void Hide()
    {
        if (toolTipObject != null) toolTipObject.SetActive(false);
    }

    // Show the HoverUI3DObjectPanel at world position (use Canvas/Screen space or world-space panel appropriately)
    public void ShowPanel(Vector3 worldPosition)
    {
        if (HoverUI3DObjectPanel == null) return;
        HoverUI3DObjectPanel.transform.position = worldPosition;
        HoverUI3DObjectPanel.SetActive(true);
    }

    public void HidePanel()
    {
        if (HoverUI3DObjectPanel == null) return;
        HoverUI3DObjectPanel.SetActive(false);
    }

    // Attach the configured ButtonMergeFleet to the Fleet UI instance.
    // If a childName is supplied, the button will be parented under that child transform of the fleet UI.
    // Keeps RectTransform layout by using worldPositionStays = false.
    public void AttachMergeButtonToFleet(GameObject fleetGO, Vector3 localScale)
    {
        if (fleetGO == null || ButtonMergeFleet == null) return;
        {
            Transform targetParent = fleetGO.transform;
            var buttonRt = ButtonMergeFleet.GetComponent<RectTransform>();
            ButtonMergeFleet.transform.SetParent(targetParent, worldPositionStays: false);
            buttonRt.anchoredPosition = Vector2.zero;
            buttonRt.localScale = localScale;
        }
        //}

        //if (!string.IsNullOrEmpty(childName))
        //{
        //    var found = targetParent.Find(childName);
        //    if (found != null) targetParent = found;

        //}

        //    // move the button GameObject under the fleet UI
        //else
        //{
        //    buttonRt.SetParent(targetParent, worldPositionStays: false);
        //    // optionally reset anchors/pos so it sits nicely in layout
        //    buttonRt.anchoredPosition = Vector2.zero;
        //    buttonRt.localScale = Vector3.one;
        //}
    }

    // Optional helper: detach (put back under panel) — useful for scene cleanup or reuse
    public void DetachMergeButtonToPanel()
    {
        if (ButtonMergeFleet == null || HoverUI3DObjectPanel == null) return;
        ButtonMergeFleet.transform.SetParent(HoverUI3DObjectPanel.transform, worldPositionStays: false);
    }
}
