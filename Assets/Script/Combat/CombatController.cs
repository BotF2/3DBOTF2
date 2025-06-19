using System.Collections.Generic;
using UnityEngine;
using Assets.Core;
using UnityEngine.UI;

public class CombatController : MonoBehaviour
{
    private CombatData combatData;
    public CombatData CombatData { get { return combatData; } set { combatData = value; } }


    public void SetCombatOrder(Orders theOrder)
    {
        for (int i = 0; i < CombatManager.Instance.CombatControllers.Count; i++)
        {
            if (CombatUIController.Instance.CombatController == CombatManager.Instance.CombatControllers[i])
            {
                CombatManager.Instance.CombatControllers[i].CombatData.Order = theOrder;
            }
        }
       
    }
}
