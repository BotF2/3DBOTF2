using Assets.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToolTipController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
   // public static ToolTipController current;
    /// <summary>
    /// Only tool tips on our systems and fleet and System UI so far
    /// It gets complicated.
    /// </summary>

    public TextMeshProUGUI TextComponent;
    //private void Awake()
    //{
    //    if (current != null)
    //    {
    //        Destroy(gameObject);
    //    }
    //    else
    //    {
    //        current = this;
    //        DontDestroyOnLoad(gameObject);
    //    }
    //}
    
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        if (TextComponent != null && HoverManager.Instance != null)
        {
            var localPlayerCivCon = CivManager.Instance.GetLocalPlayerCivController();
            foreach (StarSysController starSysCon in localPlayerCivCon.CivData.StarSysOwned)
            {
                if (starSysCon.StarSysData.GetSysName() == TextComponent.text)
                {
                    HoverManager.Instance.ShowTip(TextComponent.text);
                    break;
                }
            }

            if (TextComponent.text.Contains(localPlayerCivCon.CivShortName))
            {
                HoverManager.Instance.ShowTip(TextComponent.text);
            }
            ///***** ToDo maybe - also see civs we know?
            else
            {
                var starSysCon = eventData.selectedObject.GetComponent<StarSysController>();
                //foreach (CivController civCon in localPlayerCivCon.CivData.CivControllersWeKnow)
                if (starSysCon != null && DiplomacyManager.Instance.FoundADiplomacyController(localPlayerCivCon, starSysCon.StarSysData.CurrentCivController))
                {
                    HoverManager.Instance.ShowTip(TextComponent.text);
                }
            }
        }
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        if (HoverManager.Instance != null)
            HoverManager.Instance.HidTip();
    }
}
