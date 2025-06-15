using System.Collections.Generic;
using UnityEngine;
using Assets.Core;
using UnityEngine.UI;

public class CombatController : MonoBehaviour
{
    private CombatData combatData;
    public CombatData CombatData { get { return combatData; } set { combatData = value; } }
    private Orders order;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    internal void SetCombatOrder(Orders theOrder, CombatController combatCon)
    {
        order = theOrder;
    }
    public void ActivePlayerToggle(Toggle activeToggleOrder)
    {

        switch (activeToggleOrder.name.ToUpper())
        {
            case "TOGGLE_ENGAGE":
                SetCombatOrder(Orders.Engage, this);
                order = Orders.Engage;
                Debug.Log("Active Engage.");
                break;
            case "TOGGLE_RUSH":
                Debug.Log("Active Rush.");
                SetCombatOrder(Orders.Rush, this);
                order = Orders.Rush;
                break;
            case "TOGGLE_RETREAT":
                Debug.Log("Active Retreat.");
                SetCombatOrder(Orders.Retreat, this);
                order = Orders.Retreat;
                break;
            case "TOGGLE_FORMATION":
                Debug.Log("Active Formation.");
                SetCombatOrder(Orders.Formation, this);
                order = Orders.Formation;
                break;
            case "TOGGLE_PROTECT_TRANSPORTS":
                Debug.Log("Active Protect Transports.");
                SetCombatOrder(Orders.ProtectTransports, this);
                order = Orders.ProtectTransports;
                break;
            case "TOGGLE_TARGET_TRANSPORTS":
                Debug.Log("Active Target Transports.");
                SetCombatOrder(Orders.TargetTransports, this);
                order = Orders.TargetTransports;
                break;
            default:
                break;
        }
    }

}
