using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Assets.Core;


public class FactoryBuildItemDrag : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    public Transform originalParent;
    
    public StarSysController StarSysController;
    public StarSysFacilities FacilityType;
    public Sprite ShipSprite;
    public int BuildDuration;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        var theDragedScript = eventData.pointerDrag.GetComponent<FactoryBuildItemDrag>();
        switch (eventData.pointerDrag.name)
        {
            case "ItemPowerPlant":
                theDragedScript.FacilityType = StarSysFacilities.PowerPlanet;
                    break;
            case "ItemFactory":
                theDragedScript.FacilityType = StarSysFacilities.Factory;
                break;
            case "ItemShipyard":
                theDragedScript.FacilityType = StarSysFacilities.Shipyard;
                break;
            case "ItemShieldGenerator":
                theDragedScript.FacilityType = StarSysFacilities.ShieldGenerator;
                break;
            case "ItemOrbitalBattery":
                theDragedScript.FacilityType = StarSysFacilities.OrbitalBattery;
                break;
            case "ItemResearchCenter":
                theDragedScript.FacilityType = StarSysFacilities.ResearchCenter;
                break;
            default:
                break;
        }
        originalParent = transform.parent;
        canvasGroup.blocksRaycasts = false; // allow drag
        transform.SetParent(transform.root); // down list to top layer to be seen
        transform.SetAsLastSibling();
        Debug.Log("onBeginDrag");
    }

    public void OnDrag(PointerEventData eventData)
    {
        // follow the mouse cursor
        rectTransform.anchoredPosition += eventData.delta / rectTransform.lossyScale;
        Debug.Log("onDraging");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        if (eventData.pointerEnter != null && eventData.pointerEnter.CompareTag("FactoryBuildSlot"))
        {
            transform.SetParent(eventData.pointerEnter.transform);
            var theDragedScript = eventData.pointerDrag.GetComponent<FactoryBuildItemDrag>();
            switch (eventData.pointerDrag.name)
            {
                case "ItemPowerPlant":
                case "ItemPowerPlant Variant(Clone)":
                    theDragedScript.FacilityType = StarSysFacilities.PowerPlanet;
                    break;
                case "ItemFactory":
                case "ItemFactory Variant(Clone)":
                    theDragedScript.FacilityType = StarSysFacilities.Factory;
                    break;
                case "ItemShipyard":
                case "ItemShipyard Variant(Clone)":
                    theDragedScript.FacilityType = StarSysFacilities.Shipyard;
                    break;
                case "ItemShieldGenerator":
                case "ItemShieldGenerator Variant(Clone)":
                    theDragedScript.FacilityType = StarSysFacilities.ShieldGenerator;
                    break;
                case "ItemOrbitalBattery":
                case "ItemOrbitalBattery Variant(Clone)":
                    theDragedScript.FacilityType = StarSysFacilities.OrbitalBattery;
                    break;
                case "ItemResearchCenter":
                case "ItemResearchCenter Variant(Clone)":
                    theDragedScript.FacilityType = StarSysFacilities.ResearchCenter;
                    break;
                default:
                    break;
            }
            switch (theDragedScript.FacilityType)
            {
                case StarSysFacilities.PowerPlanet:
                    StarSysManager.Instance.NewImageInEmptyBuildableInventory(StarSysManager.Instance.PowerPlantPrefab, this.StarSysController);
                    break;
                case StarSysFacilities.Factory:
                    StarSysManager.Instance.NewImageInEmptyBuildableInventory(StarSysManager.Instance.FactoryPrefab, this.StarSysController);
                    break;
                case StarSysFacilities.Shipyard:
                    StarSysManager.Instance.NewImageInEmptyBuildableInventory(StarSysManager.Instance.ShipyardPrefab, this.StarSysController);
                    break;
                case StarSysFacilities.ShieldGenerator:
                    StarSysManager.Instance.NewImageInEmptyBuildableInventory(StarSysManager.Instance.ShieldGeneratorPrefab, this.StarSysController);
                    break;
                case StarSysFacilities.OrbitalBattery:
                    StarSysManager.Instance.NewImageInEmptyBuildableInventory(StarSysManager.Instance.OrbitalBatteryPrefab, this.StarSysController);
                    break;
                case StarSysFacilities.ResearchCenter:
                    StarSysManager.Instance.NewImageInEmptyBuildableInventory(StarSysManager.Instance.ResearchCenterPrefab, this.StarSysController);
                    break;
                default:
                    break;
            }
        }
        else
        {
            transform.SetParent(originalParent);
        }
        rectTransform.anchoredPosition = Vector2.zero;
        Debug.Log("onEndDrag");
    }

}
