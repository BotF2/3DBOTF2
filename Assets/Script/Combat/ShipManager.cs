using Assets.Core;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ShipManager : MonoBehaviour
{
    public static ShipManager Instance;

    [SerializeField]
    private ShipController shipConPrefab;

    [SerializeField]
    private GameObject shipListUIPrefab; // prefab for the ship list UI in the galaxy menu
    public List<ShipController> ShipControllerList = new List<ShipController>();
    public List<ShipSO> ShipSOListTech0 = new List<ShipSO>();
    public List<ShipSO> ShipSOListTech1 = new List<ShipSO>();
    public List<ShipSO> ShipSOListTech2 = new List<ShipSO>();
    public List<ShipSO> ShipSOListTech3 = new List<ShipSO>();
    public ShipSORegistry ShipSORegistry;
    public GameObject targetGOPrefab;
    public GameObject[] torpedoPrefabs;
    public GameObject[] beamWeaponPrefabs;
    int shipIndex = 0;

    // Pending UI items that couldn't be properly parented when created
    private readonly List<ShipController> shipConPendingShipUI = new List<ShipController>();

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
                Quaternion.identity, CombatManager.Instance.CombatUICanvasGO.transform);
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
                shipCon.ShipData.ShieldHealth = shipSOList[i].ShieldMaxHealth;
                shipCon.ShipData.HullHealth = shipSOList[i].HullMaxHealth;
                shipCon.ShipData.TorpedoDamage = shipSOList[i].TorpedoDamage;
                shipCon.ShipData.BeamDamage = shipSOList[i].BeamDamage;
                shipCon.ShipData.BuildDuration = shipSOList[i].BuildDuration;
                var targetGO = Instantiate(targetGOPrefab, shipCon.transform.position, Quaternion.identity); // where other ship weapons target 
                shipCon.ShipData.TargetOnThisShip = targetGO;
                targetGO.transform.SetParent(shipCon.transform, false); // set target GO as child of ship GO
                shipCon.ShipData.TargetOnThisShip.gameObject.transform.Translate(shipCon.transform.position.x, shipCon.transform.position.y, shipCon.transform.position.z + 10); // move target location back along spine of ship
                shipCon.ShipData.ShipDescription = shipSOList[i].ShipDescription;
                shipCon.gameObject.name = shipCon.ShipData.ShipName;
                shipCon.Order = CombatOrders.None;
                shipCon.gameObject.layer = 9; // set to "ships" layer
                ShipControllerList.Add(shipCon);
                if (parentGO.GetComponentInChildren<FleetController>() == null)
                {
                    var sysCon = parentGO.GetComponent<StarSysController>();
                    shipCon.ShipData.CurrentStarSysController = sysCon;
                    if (sysCon.StarSysData.ShipsList.Contains(shipCon.GetComponent<ShipController>()) == false)
                        sysCon.StarSysData.ShipsList.Add(shipCon.GetComponent<ShipController>());
                    shipCon.ShipData.CurrentFleetController = null;
                }
                else if (parentGO.GetComponentInChildren<StarSysController>() == null)
                {
                    var fleetCon = parentGO.GetComponent<FleetController>();
                    shipCon.ShipData.CurrentFleetController = fleetCon;
                    if (fleetCon.FleetData.ShipsList.Contains(shipCon.GetComponent<ShipController>()) == false)
                        fleetCon.FleetData.ShipsList.Add(shipCon.GetComponent<ShipController>());
                    shipCon.ShipData.CurrentStarSysController = null;
                }

                // Create the UI object and try to parent it to the owner's ShipListUIParent.
                InstantiateShipListUIGameObject(shipCon, parentGO);

                // Put gameplay ship under parent in scene
                shipCon.transform.SetParent(parentGO.transform, false); // load into List of ships in the galaxy menu
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
    public ShipSO GetShipSO(ShipType shipType, TechLevel techLevel, CivEnum civEnum)
    {
        ShipSO ourShipSO = null;
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
                var shipSOIEnumAdvanced = ShipSOListTech2.Where(x => x.ShipType == shipType && x.CivEnum == civEnum);
                var shipSOa = shipSOIEnumAdvanced.ToList().FirstOrDefault();
                ourShipSO = shipSOa;
                break;
            case TechLevel.SUPREME:
                var shipSOIEnumSup = ShipSOListTech3.Where(x => x.ShipType == shipType && x.CivEnum == civEnum);
                var shipSOs = shipSOIEnumSup.ToList().FirstOrDefault();
                ourShipSO = shipSOs;
                break;
            default:
                break;
        }
        if (ourShipSO == null)
        {
            Debug.Log("No shipSO found for " + shipType.ToString() + " at tech level " + techLevel.ToString() + " for civ " + civEnum.ToString() + ". Returning default scout ship.");
            var shipSOIEnumDefault = ShipSOListTech0.Where(x => x.ShipType == ShipType.Scout && x.CivEnum == civEnum);
            var shipSOdft = shipSOIEnumDefault.ToList().FirstOrDefault();
            ourShipSO = shipSOdft;
        }
        return ourShipSO;
    }
    public void BuildShipInSystem(ShipType shipType, StarSysController sysCon) // a destroyer for warp capable systems on game loading and shipyard during game
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
                ShipControllerList.Add(shipCon);
                shipCon.ShipData.CurrentStarSysController = sysCon;
                shipCon.ShipData.CurrentFleetController = null;
            }
        }
    }

    public void InstantiateShipListUIGameObject(ShipController shipCon, GameObject parentGO)
    {
        if (shipCon.ShipData.CivEnum == GameController.Instance.GameData.LocalPlayerCivEnum)
        {
            if (shipCon.ShipListUIGameObject == null)
            {
                GameObject thisShipListUIGameObject = (GameObject)Instantiate(shipListUIPrefab, new Vector3(0, 0, 0),
                    Quaternion.identity);
                thisShipListUIGameObject.SetActive(true);
                thisShipListUIGameObject.name = "ShipListUI_" + shipCon.ShipData.ShipName + "_" + shipIndex;
                UnityEngine.UI.Image[] imageComponents = thisShipListUIGameObject.GetComponentsInChildren<UnityEngine.UI.Image>();
                if (imageComponents.Length > 1 && imageComponents[1] != null)
                {
                    imageComponents[1].sprite = shipCon.ShipData.ShipSprite;
                }

                TextMeshProUGUI textComponent = thisShipListUIGameObject.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = shipCon.ShipData.ShipType.ToString();
                }
                CanvasGroup canvasGroup = thisShipListUIGameObject.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.gameObject.SetActive(true);
                }
                thisShipListUIGameObject.layer = 5;
                shipCon.ShipListUIGameObject = thisShipListUIGameObject;
                var shipUiItem = thisShipListUIGameObject.GetComponent<ShipListUI_Item>();
                shipUiItem.ShipController = shipCon;

                // Try to parent to owner's ShipListUIParent.  If missing, parent to scene Canvas and queue for reparenting.
                if (parentGO.TryGetComponent(out StarSysController sysCon))
                {
                    shipUiItem.CurrentStarSyst = sysCon;
                    if (sysCon.StarSysData.ShipListUIParent != null)
                    {
                        shipCon.ShipListUIGameObject.transform.SetParent(sysCon.StarSysData.ShipListUIParent.transform, false);
                    }
                    else
                    {
                        // fallback to scene canvas so UI is visible; add to pending for reparenting later
                        var canvas = FindFirstObjectByType<Canvas>();
                        if (canvas != null)
                            shipCon.ShipListUIGameObject.transform.SetParent(canvas.transform, false);

                        shipConPendingShipUI.Add(shipCon);
                        Debug.LogWarning($"Ship UI created before system ShipListUIParent for {sysCon.name}; queued for reparenting.");
                    }
                }
                else if (parentGO.TryGetComponent(out FleetController fleetCon))
                {
                    shipUiItem.CurrentFleet = fleetCon;
                    if (fleetCon.FleetData.ShipListUIParent != null)
                    {
                        shipCon.ShipListUIGameObject.transform.SetParent(fleetCon.FleetData.ShipListUIParent.transform, false);
                    }
                    else
                    {
                        // fallback to scene canvas and queue
                        var canvas = FindFirstObjectByType<Canvas>();
                        if (canvas != null)
                            shipCon.ShipListUIGameObject.transform.SetParent(canvas.transform, false);

                        shipConPendingShipUI.Add(shipCon);
                        Debug.LogWarning($"Ship UI created before fleet ShipListUIParent for {fleetCon.gameObject.name}; queued for reparenting.");
                    }
                }
            }
        }
    }

    // Call this after you instantiate system/fleet UI so any pending ship UI gets reparented correctly.
    public void ProcessPendingShipUIs()
    {
        if (shipConPendingShipUI.Count == 0) return;

        for (int i = shipConPendingShipUI.Count - 1; i >= 0; i--)
        {
            var shipCon = shipConPendingShipUI[i];
            if (shipCon == null || shipCon.ShipListUIGameObject == null)
            {
                shipConPendingShipUI.RemoveAt(i);
                continue;
            }

            var uiItem = shipCon.ShipListUIGameObject.GetComponent<ShipListUI_Item>();
            if (uiItem == null)
            {
                shipConPendingShipUI.RemoveAt(i);
                continue;
            }

            bool reparented = false;

            if (uiItem.CurrentStarSyst != null)
            {
                var sys = uiItem.CurrentStarSyst;
                if (sys.StarSysData != null && sys.StarSysData.ShipListUIParent != null)
                {
                    shipCon.ShipListUIGameObject.transform.SetParent(sys.StarSysData.ShipListUIParent.transform, false);
                    reparented = true;
                }
            }
            else if (uiItem.CurrentFleet != null)
            {
                var fleet = uiItem.CurrentFleet;
                if (fleet.FleetData != null && fleet.FleetData.ShipListUIParent != null)
                {
                    shipCon.ShipListUIGameObject.transform.SetParent(fleet.FleetData.ShipListUIParent.transform, false);
                    reparented = true;
                }
            }

            if (reparented)
            {
                shipConPendingShipUI.RemoveAt(i);
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
                    shipCon.ShipData.CurrentFleetController = fleetCon;
                    // already added to ShipsList in InstantiateShipControllersWithDataFromSO and to ShipControllerList
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
                }
                ;
                break;
        }
        return listOfShipSOs;
    }

    internal void RemoveShipControllerFromList(ShipController shipCon)
    {
        int foundOne = -1;
        for (int i = 0; i < ShipControllerList.Count; i++)
        {
            if (shipCon == ShipControllerList[i])
            {
                foundOne = i;
            }
        }
        if (foundOne > -1)
        {
            var toDestroy = ShipControllerList[foundOne];
            ShipControllerList.RemoveAt(foundOne);
            if (toDestroy.ShipListUIGameObject != null) Destroy(toDestroy.ShipListUIGameObject);
            if (toDestroy.gameObject != null) Destroy(toDestroy.gameObject);
        }
    }
}
