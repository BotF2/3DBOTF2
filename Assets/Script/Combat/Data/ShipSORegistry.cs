namespace BOTF3D.Combat
{
using System;
using UnityEngine;
using System.Collections.Generic;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



[CreateAssetMenu(fileName = "ModelRegistry", menuName = "Models/Model Registry")]
public class ShipSORegistry : ScriptableObject
{
    public ShipSO[] shipSOs;

    public ShipSO GetByID(string name)
    {
        foreach (var shipSO in shipSOs)
        {
            if (shipSO.ShipName == name)
                return shipSO;
        }
        Debug.LogWarning("Ship SO ID not found: " + name);
        return null;
    }

    public void AddShipSO(ShipSO newShipSO)
    {
        Array.Resize(ref shipSOs, shipSOs.Length + 1);
        shipSOs[shipSOs.Length - 1] = newShipSO;
    }

    public void RemoveShipSO(string name)
    {
        int index = Array.FindIndex(shipSOs, shipSO => shipSO.ShipName == name);
        if (index >= 0)
        {
            for (int i = index; i < shipSOs.Length - 1; i++)
            {
                shipSOs[i] = shipSOs[i + 1];
            }
            Array.Resize(ref shipSOs, shipSOs.Length - 1);
        }
        else
        {
            Debug.LogWarning("Ship SO ID not found for removal: " + name);
        }
    }
}

}