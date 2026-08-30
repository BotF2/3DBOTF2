// Ignore Spelling: BOTF Sys

using BOTF3D.Core;
using BOTF3D.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.UI
{
    public class FactoryBuildItemDrag : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        public void Initialize() { }
        public void UpdateState() { }
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
            // ✅ Don't overwrite if already set! Only set if null
            if (StarSysController == null)
            {
                StarSysController = StarSysMenuUIController.Instance?.ActiveStarSysController;

                if (StarSysController == null)
                {
                    Debug.LogError("❌ FactoryBuildItemDrag.OnBeginDrag: Cannot get StarSysController! Both references are null.");
                }
            }

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
            canvasGroup.blocksRaycasts = false;
            transform.SetParent(transform.root);
            transform.SetAsLastSibling();
            Debug.Log($"FactoryBuildItemDrag.OnBeginDrag: {FacilityType}, StarSysController={(StarSysController != null ? StarSysController.name : "NULL")}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            // follow the mouse cursor
            rectTransform.anchoredPosition += eventData.delta / rectTransform.lossyScale;
            Debug.Log("onDraging");
        }

        // ✅ FIXED: Only ONE OnEndDrag method, no "override", includes validation
        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;

            // ✅ CRITICAL: Validate StarSysController before using!
            if (StarSysController == null)
            {
                Debug.LogError("FactoryBuildItemDrag.OnEndDrag: StarSysController is NULL! Cannot complete drag operation.");
                Debug.LogError("  This means the build UI wasn't properly initialized or the system reference was lost.");

                // Return to original parent
                transform.SetParent(originalParent);
                rectTransform.anchoredPosition = Vector2.zero;
                return; // ✅ Exit early
            }

            // ✅ Also validate StarSysData
            if (StarSysController.StarSysData == null)
            {
                Debug.LogError($"FactoryBuildItemDrag.OnEndDrag: StarSysController '{StarSysController.name}' has null StarSysData!");
                transform.SetParent(originalParent);
                rectTransform.anchoredPosition = Vector2.zero;
                return;
            }

            if (eventData.pointerEnter != null && eventData.pointerEnter.CompareTag("FactoryBuildSlot"))
            {
                // Enforce max 5 queued items (count only actual drag items, not slot placeholders)
                int queued = 0;
                foreach (var t in StarSysController.sysBuildQueueList)
                    if (t != null && t.GetComponent<FactoryBuildItemDrag>() != null) queued++;
                if (queued >= 5)
                {
                    Debug.Log("FactoryBuildItemDrag: queue full (max 5)");
                    transform.SetParent(originalParent);
                    rectTransform.anchoredPosition = Vector2.zero;
                    return;
                }

                // Enforce this system's own per-facility-type build ceiling (built + already
                // queued) - see StarSysManager.GetFacilityCap. This is the player-facing
                // enforcement point; StarSysAIManager checks the same cap for AI-driven builds.
                if (StarSysManager.Instance != null)
                {
                    int builtAndQueued = StarSysManager.Instance.GetBuiltAndQueuedFacilityCount(StarSysController, FacilityType);
                    int cap = StarSysManager.Instance.GetFacilityCap(StarSysController, FacilityType);
                    if (builtAndQueued >= cap)
                    {
                        Debug.Log($"FactoryBuildItemDrag: '{StarSysController.name}' is already at its {FacilityType} build ceiling ({cap})");
                        transform.SetParent(originalParent);
                        rectTransform.anchoredPosition = Vector2.zero;
                        return;
                    }
                }

                // ✅ CRITICAL: Parent to the ACTUAL build queue, not the drop slot!
                if (StarSysController.BuildListGridLayoutGroup != null)
                {
                    transform.SetParent(StarSysController.BuildListGridLayoutGroup.transform);
                    Debug.Log($"  ✅ Added '{FacilityType}' to BuildListGridLayoutGroup");
                }
                else
                {
                    Debug.LogError("  ❌ BuildListGridLayoutGroup is NULL on StarSysController!");
                    transform.SetParent(originalParent);
                    rectTransform.anchoredPosition = Vector2.zero;
                    return;
                }

                var theDragedScript = eventData.pointerDrag.GetComponent<FactoryBuildItemDrag>();

                // ✅ Create visual icon in inventory
                if (StarSysManager.Instance != null)
                {
                    StarSysManager.Instance.NewImageInEmptyBuildAbleInventory(theDragedScript.FacilityType, this.StarSysController);
                }
                else
                {
                    Debug.LogError("FactoryBuildItemDrag.OnEndDrag: StarSysManager.Instance is NULL!");
                }

                // ✅ CRITICAL: Manually trigger queue update since OnTransformChildrenChanged might not fire
                StarSysController.GridFactoryQueueUpdate();
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
