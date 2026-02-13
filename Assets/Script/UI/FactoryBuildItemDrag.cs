using Assets.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.GamePlay
{
    public class FactoryBuildItemDrag : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        public Transform originalParent;

        public StarSysController StarSysController;
        public StarSysFacilityType FacilityType;
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
                    theDragedScript.FacilityType = StarSysFacilityType.PowerPlanet;
                    break;
                case "ItemFactory":
                    theDragedScript.FacilityType = StarSysFacilityType.Factory;
                    break;
                case "ItemShipyard":
                    theDragedScript.FacilityType = StarSysFacilityType.Shipyard;
                    break;
                case "ItemShieldGenerator":
                    theDragedScript.FacilityType = StarSysFacilityType.ShieldGenerator;
                    break;
                case "ItemOrbitalBattery":
                    theDragedScript.FacilityType = StarSysFacilityType.OrbitalBattery;
                    break;
                case "ItemResearchCenter":
                    theDragedScript.FacilityType = StarSysFacilityType.ResearchCenter;
                    break;
                default:
                    break;
            }
            originalParent = transform.parent;
            canvasGroup.blocksRaycasts = false; // allow click to hit for drag
            transform.SetParent(transform.root);// parent to canvas, root parent
            transform.SetAsLastSibling();// down list to top layer to be seen
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

                switch (theDragedScript.FacilityType)
                {
                    case StarSysFacilityType.PowerPlanet:
                        StarSysManager.Instance.NewImageInEmptyBuildAbleInventory(theDragedScript.FacilityType, this.StarSysController);
                        break;
                    case StarSysFacilityType.Factory:
                        StarSysManager.Instance.NewImageInEmptyBuildAbleInventory(theDragedScript.FacilityType, this.StarSysController);
                        break;
                    case StarSysFacilityType.Shipyard:
                        StarSysManager.Instance.NewImageInEmptyBuildAbleInventory(theDragedScript.FacilityType, this.StarSysController);
                        break;
                    case StarSysFacilityType.ShieldGenerator:
                        StarSysManager.Instance.NewImageInEmptyBuildAbleInventory(theDragedScript.FacilityType, this.StarSysController);
                        break;
                    case StarSysFacilityType.OrbitalBattery:
                        StarSysManager.Instance.NewImageInEmptyBuildAbleInventory(theDragedScript.FacilityType, this.StarSysController);
                        break;
                    case StarSysFacilityType.ResearchCenter:
                        StarSysManager.Instance.NewImageInEmptyBuildAbleInventory(theDragedScript.FacilityType, this.StarSysController);
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
}
