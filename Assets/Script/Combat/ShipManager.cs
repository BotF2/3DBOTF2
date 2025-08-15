using Assets.Core;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.UIElements;
using System.Data.Common;

public class ShipManager : MonoBehaviour
{
    public static ShipManager Instance;

    [SerializeField]
    private ShipController shipConPrefab;
    public GameObject ShipPrefab;

    public GameObject PrefabSphere;
    [SerializeField]
    private GameObject shipListUIPrefab; // prefab for the ship list UI in the galaxy menu
    public List<ShipController> ShipControllerGameList = new List<ShipController>();
    public List<ShipSO> ShipSOListTech0 = new List<ShipSO>();
    public List<ShipSO> ShipSOListTech1 = new List<ShipSO>();
    public List<ShipSO> ShipSOListTech2 = new List<ShipSO>();
    public List<ShipSO> ShipSOListTech3 = new List<ShipSO>();
    public ShipSORegistry ShipSORegistry;
    public GameObject targetGOPrefab;
    public GameObject[] torpedoPrefabs;
    public GameObject[] beamWeaponPrefabs;
    int shipIndex = 0;

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
    public void OnSelectModel(string selectedShipName)
    {
        Vector3 spawnPos = new Vector3(0, 0, 0);
        SpawnByShipName(selectedShipName, spawnPos);
    }
    public void SpawnByShipName(string shipName, Vector3 position)
    {
        ShipSO shipSO = ShipSORegistry.GetByID(shipName);
        if (shipSO != null && shipSO.Prefab != null)
        {
            Instantiate(shipSO.Prefab, position, Quaternion.identity);
        }
    }

    public List<ShipController> InstantiateShipControllersWithDataFromSO(List<ShipSO> shipSOList, GameObject parentGO)
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
                var position = shipCon.transform.position;
                shipCon.ShipData.TargetMeHere = Instantiate(targetGOPrefab, new Vector3(position.x, position.y, position.z+ 10f), Quaternion.identity);
                shipCon.ShipData.TargetMeHere.transform.SetParent(shipCon.transform, false);
                shipCon.gameObject.name = shipCon.ShipData.ShipName;
                ShipControllerGameList.Add(shipCon);
                shipConList.Add(shipCon);
                InstantiateShipListUIGameObject(shipCon, parentGO); // create the ship list UI g.o. for this ship
                 
                shipCon.transform.SetParent(parentGO.transform, false); // load into List of ships in the galaxy menu 
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
    public ShipSO GetShipSO(ShipType shipType, TechLevel techLevel, CivEnum civEnum)
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

    public void BuildShipInSystem(ShipType shipType, StarSysController sysCon)
    {
        ShipSO ourShipSO = GetShipSO(shipType, sysCon.StarSysData.CurrentCivController.CivData.TechLevel, sysCon.StarSysData.CurrentOwnerCivEnum);
        List<ShipSO> shipSOAsList = new List<ShipSO> { ourShipSO };
        List<ShipController> shipConListOfOne = InstantiateShipControllersWithDataFromSO(shipSOAsList, sysCon.gameObject);
        foreach (ShipController shipCon in shipConListOfOne)
        {
            if (shipCon != null)
            {
                shipCon.transform.SetParent(sysCon.transform);
                sysCon.StarSysData.ShipsList.Add(shipCon.GetComponent<ShipController>());
                ShipControllerGameList.Add(shipCon);
            }
        }
    }
    private void InstantiateShipListUIGameObject(ShipController shipCon, GameObject parentGO)
    {
        if (shipCon.ShipData.CivEnum == GameController.Instance.GameData.LocalPlayerCivEnum)
        {
            if (shipCon.ShipListUIGameObject == null)
            {
                GameObject thisShipListUIGameObject = (GameObject)Instantiate(shipListUIPrefab, new Vector3(0, 0, 0),
                    Quaternion.identity);
                thisShipListUIGameObject.SetActive(true);
                UnityEngine.UI.Image[] imageComponents = thisShipListUIGameObject.GetComponentsInChildren<UnityEngine.UI.Image>();
                if (imageComponents[1] != null)
                {
                    imageComponents[1].sprite = shipCon.ShipData.ShipSprite;
                }

                TextMeshProUGUI textComponent = thisShipListUIGameObject.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = shipCon.ShipData.ShipType.ToString();
                }
                
                thisShipListUIGameObject.layer = 5;
                shipCon.ShipListUIGameObject = thisShipListUIGameObject;         

                if (parentGO.TryGetComponent(out StarSysController sysCon))
                {
                    if (sysCon.StarSysData.ShipListUIParent != null)
                    {
                        shipCon.ShipListUIGameObject.transform.SetParent(sysCon.StarSysData.ShipListUIParent.transform, false);
                    }
                }
                if (parentGO.TryGetComponent(out FleetController fleetCon))
                {
                    if (fleetCon.FleetData.ShipListUIParent != null)
                    {
                        shipCon.ShipListUIGameObject.transform.SetParent(fleetCon.FleetData.ShipListUIParent.transform, false);
                    }
                }
            }
        }
    }
    public void BuildShipsOfFirstFleet(FleetController fleetCon)
    {
       // var shipCon = shipCon.GetComponent<FleetController>();
        CivEnum civEnum = fleetCon.FleetData.CivEnum;
        List<ShipSO> ships = new List<ShipSO>();
        ships = FirstShipDateByTechlevel((int)CivManager.Instance.GetCivDataByCivEnum(civEnum).TechLevel, civEnum);
        List<ShipController> shipCons = new List<ShipController>();
        if (ships != null)
        {
            shipCons = InstantiateShipControllersWithDataFromSO(ships, fleetCon.gameObject);
            foreach (ShipController shipCon in shipCons)
            {
                if (shipCon != null)
                {
                    shipCon.transform.SetParent(fleetCon.transform);
                    fleetCon.FleetData.ShipsList.Add(shipCon.GetComponent<ShipController>());
                    ShipControllerGameList.Add(shipCon);
                }
            }
        }
        fleetCon.UpdateMaxWarp();
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
