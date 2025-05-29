using Assets.Core;
using NUnit.Framework.Internal.Execution;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShipManager : MonoBehaviour
{
    public static ShipManager Instance;

    [SerializeField]
    private ShipController shipConPrefab;
    public List<ShipController> ShipControllerGameList;
    public List<ShipSO> ShipSOListTech0;
    public List<ShipSO> ShipSOListTech1;
    public List<ShipSO> ShipSOListTech2;
    public List<ShipSO> ShipSOListTech3;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public List<ShipController> ShipControllerWithDataFromSO(List<ShipSO> shipSOList)
    {
        List<ShipController> shipConList = new List<ShipController>();
        for (int i = 0; i < shipSOList.Count; i++)
        {

            if (shipSOList[i] != null)
            {
                ShipController shipCon = Instantiate(shipConPrefab, new Vector3(0, 0, 0),
                Quaternion.identity);
                shipCon.Init(this);
                shipCon.ShipData = new ShipData();
                shipCon.ShipData.ShipName = shipSOList[i].ShipName;
                shipCon.ShipData.CivEnum = shipSOList[i].CivEnum;
                shipCon.ShipData.TechLevel = shipSOList[i].TechLevel;
                shipCon.ShipData.ShipType = shipSOList[i].ShipType;
                if (shipSOList[i].shipSprite != null)
                    shipCon.ShipData.ShipSprite = shipSOList[i].shipSprite;
                shipCon.ShipData.maxWarpFactor = shipSOList[i].maxWarpFactor;
                shipCon.ShipData.currentWarpFactor = 0f;
                shipCon.ShipData.ShieldMaxHealth = shipSOList[i].ShieldMaxHealth;
                shipCon.ShipData.HullMaxHealth = shipSOList[i].HullMaxHealth;
                shipCon.ShipData.TorpedoDamage = shipSOList[i].TorpedoDamage;
                shipCon.ShipData.BeamDamage = shipSOList[i].BeamDamage;
                shipCon.ShipData.BuildDuration = shipSOList[i].BuildDuration;
                shipCon.gameObject.name = shipCon.ShipData.ShipName;
                ShipControllerGameList.Add(shipCon);
                shipConList.Add(shipCon);
            }
        }
        return shipConList;
    }

    public int GetShipBuildDuration(ShipType shipType, TechLevel techLevel, CivEnum civEnum)
    {
        ShipSO aShipSO = new ShipSO();
        int duration = 1;
        aShipSO = GetShipSO(shipType, techLevel, civEnum);
        duration = aShipSO.BuildDuration;
        return duration;
    }
    private ShipSO GetShipSO(ShipType shipType, TechLevel techLevel, CivEnum civEnum)
    {
        ShipSO ourShipSO = new ShipSO();
        switch (techLevel)
        {
            case TechLevel.EARLY:
                var shipSOIEnumEarly = ShipSOListTech0.Where(x => x.ShipType == shipType && x.CivEnum == civEnum);
                var shipSOe = shipSOIEnumEarly.ToList().FirstOrDefault();
                ourShipSO = shipSOe;
                break;
            case TechLevel.DEVELOPED:
                var shipSOIEnumDeveloped = ShipSOListTech1.Where(x => x.ShipType == shipType && x.CivEnum == civEnum);
                var shipSOd = shipSOIEnumDeveloped.ToList().FirstOrDefault();
                ourShipSO = shipSOd;
                break;
            case TechLevel.ADVANCED:
                var shipSOIEnumAdvanced = ShipSOListTech1.Where(x => x.ShipType == shipType && x.CivEnum == civEnum);
                var shipSOa = shipSOIEnumAdvanced.ToList().FirstOrDefault();
                ourShipSO = shipSOa;
                break;
            case TechLevel.SUPREME:
                var shipSOIEnumSup = ShipSOListTech1.Where(x => x.ShipType == shipType && x.CivEnum == civEnum);
                var shipSOs = shipSOIEnumSup.ToList().FirstOrDefault();
                ourShipSO = shipSOs;
                break;
            default:
                break;
        }
        return ourShipSO;
    }
    public void ShipsFromFleetsForCombat() // GameObject fleetGOA, GameObject fleetGOB)
    {

        //ShipSO ourShipSO = GetShipSO(shipType, sysCon.StarSysData.CurrentCivController.CivData.TechLevel, sysCon.StarSysData.CurrentOwnerCivEnum);
        //List<ShipSO> shipSOAsList = new List<ShipSO> { ourShipSO };
        //var shipConListOfOne = ShipControllerWithDataFromSO(shipSOAsList); // takes a list of ShipSO
        //for (int i = 0; i < shipConListOfOne.Count; i++)
        //{
        //    shipConListOfOne[i].transform.SetParent(shipCon.transform);
        //    shipCon.GetComponent<FleetController>().FleetData.AddToShipList(shipConListOfOne[i].GetComponent<ShipController>());
        //}
    }
    public void BuildShipInSystem(ShipType shipType, StarSysController sysCon)
    {
        ShipSO ourShipSO = GetShipSO(shipType, sysCon.StarSysData.CurrentCivController.CivData.TechLevel, sysCon.StarSysData.CurrentOwnerCivEnum);
        List<ShipSO> shipSOAsList = new List<ShipSO> { ourShipSO };
        var shipConListOfOne = ShipControllerWithDataFromSO(shipSOAsList); // takes a list of ShipSO
        ShipControllerGameList.Add(shipConListOfOne[0]);
        sysCon.StarSysData.ShipsList.Add(shipConListOfOne[0]);
        if (GalaxyMenuUIController.Instance != null)
            GalaxyMenuUIController.Instance.UpdateSystemShipList(sysCon);
    }
    public void BuildShipsOfFirstFleet(FleetController fleetCon)
    {
       // var fleetCon = fleetCon.GetComponent<FleetController>();
        CivEnum civEnum = fleetCon.FleetData.CivEnum;
        List<ShipSO> ships = new List<ShipSO>();
        ships = FirstShipDateByTechlevel((int)CivManager.Instance.GetCivDataByCivEnum(civEnum).TechLevel, civEnum);
        //if (ships != null)
        List<ShipController> shipCons = new List<ShipController>();
        if (ships != null)
        {
            shipCons = ShipControllerWithDataFromSO(ships);
            foreach (ShipController shipGO in shipCons)
            {
                if (shipGO != null)
                {
                    shipGO.transform.SetParent(fleetCon.transform);
                    fleetCon.FleetData.ShipsList.Add(shipGO.GetComponent<ShipController>());
                }
            }
        }

        fleetCon.UpdateMaxWarp();
        //fleetCon.FleetData.CurrentWarpFactor = 0f;
    }
    public List<ShipSO> FirstShipDateByTechlevel(int techLevel, CivEnum civ)
    {
        List<ShipSO> listOfShipSOs = new List<ShipSO>();
        switch (techLevel)
        {
            case 100:// early
                foreach (var shipSO in ShipSOListTech0)
                {
                    if (shipSO.CivEnum == civ)
                    {
                        listOfShipSOs.Add(shipSO);
                    }
                }
                break;
            case 300: // developed
                foreach (var shipSO in ShipSOListTech1)
                {
                    if (shipSO.CivEnum == civ)
                    {
                        listOfShipSOs.Add(shipSO);
                    }
                }
                break;
            case 600: // advanced
                foreach (var shipSO in ShipSOListTech2)
                {
                    if (shipSO.CivEnum == civ)
                    {
                        listOfShipSOs.Add(shipSO);
                    }
                }
                break;
            case 900: // supreme
                foreach (var shipSO in ShipSOListTech3)
                {
                    if (shipSO.CivEnum == civ)
                    {
                        listOfShipSOs.Add(shipSO);
                    }
                }
                break;
            default:
                foreach (var shipSO in ShipSOListTech0)
                {
                    if (shipSO.CivEnum == civ)
                    {
                        listOfShipSOs.Add(shipSO);
                    }
                };
                break;
        }
        return listOfShipSOs;
    }
}
