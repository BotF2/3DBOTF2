// Ignore Spelling: Nums Revealer

using FischlWorks_FogWar;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;


namespace Assets.Core
{
    /// <summary>
    /// Instantiates the fleets (a FleetController and a FleetData) using FleetSO
    /// </summary>
    public class FleetManager : MonoBehaviour
    {
        public static FleetManager Instance;
        public GameObject scoutBluePrintPrefab;
        public GameObject destroyerBluePrintPrefab;
        public GameObject cruiserBluePrintPrefab;
        public GameObject ltCruiserBluePrintPrefab;
        public GameObject heavyCruiserBluePrintPrefab;
        public GameObject transportBluePrintPrefab;
        [SerializeField]
        private GameObject galaxyCanvasGO;
        [SerializeField]
        private Canvas parentCanavas;
        [SerializeField]
        private csFogWar fogWar;
        [SerializeField]
        private List<FleetSO> fleetSOList;// all possible fleetSO(s)
        [SerializeField]
        private FleetController fleetPrefab;
        [SerializeField]
        private GameObject fleetUIPrefab;
        [SerializeField]
        private GameObject shipManagerMenuPrefab;
        [SerializeField]
        private Material fogPlaneMaterial;
        private GameObject galaxyImage;
        public GameObject GalaxyCenter;
        public List<FleetController> FleetControllerList { get; private set; } = new List<FleetController>();
        [SerializeField]
        private GameObject canvasShipManager;
        [SerializeField]
        private List<int> destinationIntsInUse = new List<int>() { 0 };
        private readonly Dictionary<CivEnum, List<int>> fleetNumsInUse = new Dictionary<CivEnum, List<int>>();
        private static readonly List<FleetController> FleetControllers = new List<FleetController>();
        public List<FleetController> FleetControllersInGame = FleetControllers;
        [SerializeField]
        private GameObject fleetUIGOContentParent; // in Hierarchy at MainMenuScene/CanvasGalaxy/FleetMenuScroll View/Viewport/ContentFleetUIGO.
        [SerializeField]
        private GameObject fleetShipsContentFolderParent;
        private readonly List<CivEnum> localPlayerCanSeeMyInsigniaList = new List<CivEnum>();
        internal GameObject fleetShipUIGOContentParent;
        public csFogWar.FogRevealer TempFogRevealerFleet;
        private float newFleetSpacer = 0f;

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
        public void Start()
        {
            for (int i = 0; i < CivManager.Instance.CivSOListAllPossible.Count; i++)
            {
                fleetNumsInUse.Add(CivManager.Instance.CivSOListAllPossible[i].CivEnum, new List<int>());
            }
            galaxyImage = GameController.Instance.GalaxyImage;
        }
        public void CleanUpDictionaryForFleetNums()
        {
            var civs = CivManager.Instance.CivSOListAllPossible;

            var civsEnumsInGame = CivManager.Instance.CivEnumsInGame;
            for (int i = 0; i < civs.Count; i++)
            {
                if (!civsEnumsInGame.Contains(civs[i].CivEnum))
                {
                    fleetNumsInUse.Remove(civs[i].CivEnum);
                }
            }
        }

        public void BuildFleetsNearSyst(StarSysController systCon)
        {
            // first path here is sent on loading the game for civs with warp, first fleets from Systems/Civs with warp
            FleetSO fleetSO = GetFleetSO_byInt((int)systCon.StarSysData.CurrentOwnerCivEnum);
            var position = systCon.StarSysData.GetPosition();

            // *** This is an option for more fleets/ships with larger galaxy
            //switch (GameManager.current.GalaxySize)
            //{
            //    case GalaxySize.SMALL:
            //        BuildFirstFleets(xyzBump, pairEnumList, position);
            //        break;
            //    case GalaxySize.MEDIUM:
            //        BuildFirstFleets(xyzBump +1, pairEnumList, position);
            //        break;
            //    case GalaxySize.LARGE:
            //        BuildFirstFleets(xyzBump +2, pairEnumList, position);
            //        break;
            //    default:
            //        BuildFirstFleets(xyzBump, pairEnumList, position);
            //        break;
            //

            CivData thisCivData = CivManager.Instance.GetCivDataByCivEnum(fleetSO.CivOwnerEnum); // new CivData();
            FleetData fleetData = new FleetData(fleetSO); // FleetData is not MonoBehavior so new is OK
            fleetData.CurrentWarpFactor = 3f;
            fleetData.CivLongName = thisCivData.CivLongName; //.CivLongName;
            fleetData.CivShortName = thisCivData.CivShortName;
            var emptyFleet = InsatiateEmptyFleetController();
            FleetController aFleet = InstantiateFleet(emptyFleet, systCon, fleetData, position, false);// false, these fleets are created as game loads so not "new fleet"
            //Destroy(emptyFleet);
        }
        public FleetController InsatiateEmptyFleetController()
        {
            FleetController fleetController = Instantiate(fleetPrefab, new Vector3(0, 0, 0),
                    Quaternion.identity);
            return fleetController;
        }

        public FleetController InstantiateFleet(FleetController originalFleetCon, StarSysController systCon, FleetData fleetData, Vector3 position, bool newFleet)
        { // new fleet button = a fleet spawning a new fleet, newFleet(true), or a star system creating a new fleet, newFleet(true) vs a fleet created when the game loads and newfleet(false)
            fleetData.ShipsList.RemoveAll(item => item == null);
            Transform newTrans = null;
            IEnumerable<StarSysController> ourCivSysCons =
            from x in StarSysManager.Instance.StarSysControllerList
            where (x.StarSysData.CurrentOwnerCivEnum == fleetData.CivEnum)
            select x;
            var ourSysCons = ourCivSysCons.ToList();

            FleetController newFleetController = Instantiate(fleetPrefab, new Vector3(0, 0, 0),
                   Quaternion.identity);

            FleetControllerList.Add(newFleetController); // add to list of all fleet controllers
            newFleetController.gameObject.layer = 6; // galaxy layer
            newFleetController.BackgroundGalaxyImage = galaxyImage;
            newFleetController.FleetData = fleetData;
            newFleetController.GalaxyCanvasGo = galaxyCanvasGO;

            var transGalaxyCenter = GalaxyCenter.gameObject.transform;
            if (systCon != null && !newFleet)
            {
                newTrans = systCon.transform; // first fleets near home systems
                Destroy(originalFleetCon.gameObject); // destroy the original fleet controller used as template empty
            }
            else if (originalFleetCon.FleetData != null && newFleet)
                newTrans = originalFleetCon.transform;
            else if (systCon.StarSysData != null && newFleet)
                newTrans = systCon.transform;

            newFleetController.transform.SetParent(transGalaxyCenter, true); // parent is galaxy center, but world position set below

            if (newTrans != null)
            {
                if (!newFleet)
                    newFleetController.transform.Translate(new Vector3(newTrans.position.x + 15f, newTrans.position.y + 15f, newTrans.position.z));
                else
                {
                    if (newFleetSpacer > 8f)
                        newFleetSpacer = 0;
                    newFleetController.transform.Translate(new Vector3(newTrans.position.x - 15f - newFleetSpacer, newTrans.position.y + 15f - newFleetSpacer, newTrans.position.z));
                    newFleetSpacer = newFleetSpacer + 2f;
                }
            }
            fleetData.Position = newFleetController.transform.position;
            if (!newFleet)
                ShipManager.Instance.BuildShipsOfFirstFleet(newFleetController);
            newFleetController.transform.localScale = new Vector3(0.7f, 0.7f, 1); // scale ship insignia here
            int fleetInt = GetNewFleetInt(fleetData.CivEnum);
            newFleetController.gameObject.name = fleetData.CivShortName.ToString() + " " + fleetInt.ToString(); // name game object
            fleetData.Name = newFleetController.gameObject.name;
            newFleetController.FleetData.FleetInt = fleetInt;
            newFleetController.Name = fleetData.Name;
            FleetControllersInGame.Add(newFleetController);
            newFleetController.FleetData.CurrentWarpFactor = 0f;
            TextMeshProUGUI TheText = newFleetController.gameObject.GetComponentInChildren<TextMeshProUGUI>();
            if (TheText != null)
            {
                TheText.text = newFleetController.gameObject.name;
                fleetData.Name = TheText.text;
            }
            FleetChildFields fleetChildFields = newFleetController.GetComponent<FleetChildFields>();
            SpriteRenderer srInsignia = fleetChildFields.InsigniaGO.GetComponent<SpriteRenderer>();
            srInsignia.sprite = newFleetController.FleetData.Insignia;
            SpriteRenderer srInsigniaUnknown = fleetChildFields.InsigniaUnknownGO.GetComponent<SpriteRenderer>();
            if (GameController.Instance.AreWeLocalPlayer(fleetData.CivEnum))
            {
                srInsigniaUnknown.enabled = false;
                srInsignia.enabled = true;
                var ourFogRevealerFleet = new csFogWar.FogRevealer(newFleetController.transform, 200, true);
                fogWar.AddFogRevealer(ourFogRevealerFleet);
                TempFogRevealerFleet = ourFogRevealerFleet;
            }
            else
            {
                // Attach a fog visibility agent so fog system can hide/show sprite renderers of this fleet
                fleetChildFields.FleetNameGO.SetActive(false);
                srInsignia.enabled = false;
                srInsigniaUnknown.enabled = true;
                var ourFogVisibilityAgent = newFleetController.gameObject.AddComponent<csFogVisibilityAgent>();
                ourFogVisibilityAgent.FogWar = fogWar;
            }

            // The line from Fleet to underlying galaxy image and to destination
            MapLineMovable[] ourLineToGalaxyImageScript = newFleetController.gameObject.GetComponentsInChildren<MapLineMovable>();
            for (int i = 0; i < ourLineToGalaxyImageScript.Length; i++)
            {
                if (ourLineToGalaxyImageScript[i].gameObject == fleetChildFields.DropLine)
                {
                    ourLineToGalaxyImageScript[i].GetLineRenderer();
                    ourLineToGalaxyImageScript[i].lineRenderer.startColor = Color.red;
                    ourLineToGalaxyImageScript[i].lineRenderer.endColor = Color.red;
                    ourLineToGalaxyImageScript[i].transform.SetParent(newFleetController.transform, false);
                    Vector3 galaxyPlanePoint = new Vector3(newFleetController.transform.position.x,
                        galaxyImage.transform.position.y, newFleetController.transform.position.z);
                    Vector3[] points = { newFleetController.transform.position, galaxyPlanePoint };
                    ourLineToGalaxyImageScript[i].SetUpLine(points);
                    newFleetController.DropLine = ourLineToGalaxyImageScript[i];
                }
            }
            newFleetController.FleetData.Destination = GalaxyCenter;
            foreach (var civCon in CivManager.Instance.CivControllersInGame)
            {
                if (civCon.CivData.CivEnum == fleetData.CivEnum)
                    fleetData.CivController = civCon;
            }
            newFleetController.gameObject.SetActive(true);

            newFleetController.UpdateMaxWarp();
            InstantiateFleetUIGameObject(newFleetController, newFleet);
            if (newFleet) // new fleet button
            {
                var galaxyMenuUICon = GalaxyMenuUIController.Instance;
                galaxyMenuUICon.FleetConSelectedForShipDeploy = newFleetController;
                if (systCon != null) // no star system, its a fleet
                    galaxyMenuUICon.ShowShipDeployForSystemNewFleet(systCon, newFleetController);
                else if (galaxyMenuUICon.FleetLookingForShipDeploy != null)
                {
                    EnsureFleetShipUIs(galaxyMenuUICon.FleetConSelectedForShipDeploy);
                    galaxyMenuUICon.ShowShipDeployForFleetNewFleet(galaxyMenuUICon.FleetLookingForShipDeploy, newFleetController);
                }
                //ShipDeployMenuUIController.Instance.BottomFleet = newFleetController;
            }
            // hover ui 3d
            //if (!GameController.Instance.AreWeLocalPlayer(newFleetData.CivEnum))
            //{
            //    Transform[] childTransforms = newFleetController.gameObject.GetComponentsInChildren<Transform>(true);
            //    for (int j = 0; j < childTransforms.Length; j++)
            //    {
            //        if (childTransforms[j].gameObject.name == "FleetName")
            //        {
            //            childTransforms[j].gameObject.SetActive(false);
            //        }
            //        var hover = childTransforms[j].GetComponent<HoverUI3D>();
            //        if (hover != null)
            //        {
            //            hover.enabled = false;
            //        }
            //    }
            //}
            return newFleetController;
        }
        private void InstantiateFleetUIGameObject(FleetController fleetCon, bool newFleet)
        {
            if (fleetCon.FleetData.CivEnum == GameController.Instance.GameData.LocalPlayerCivEnum)
            {
                if (fleetCon.FleetUIGameObject == null)
                {
                    GameObject thisFleetUIGameObject = (GameObject)Instantiate(fleetUIPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    thisFleetUIGameObject.SetActive(true);
                    thisFleetUIGameObject.layer = 5;
                    fleetCon.FleetUIGameObject = thisFleetUIGameObject;
                    fleetCon.FleetUIGameObject.SetActive(true);
                    thisFleetUIGameObject.transform.SetParent(fleetUIGOContentParent.transform, false);
                    FleetUI_Fields fleetUI_Fields = thisFleetUIGameObject.GetComponent<FleetUI_Fields>();
                    if (fleetUI_Fields != null && fleetUI_Fields.FleetShipContentGO != null)
                    {
                        fleetCon.FleetData.ShipListUIParent = fleetUI_Fields.FleetShipContentGO;
                        EnsureFleetShipUIs(fleetCon);
                        FleetMenuUIController.Instance.SetupFleetUIElements(fleetCon, thisFleetUIGameObject);
                    }
                    else
                    {
                        Debug.LogWarning($"InstantiateFleetUIGameObject: ShipContent not found in UI prefab for system {fleetCon.name}");
                    }
                }
            }
            var shipManager = ShipManager.Instance;
            if (shipManager != null)
            {
                // Process any pending ship UIs (created earlier before parent existed)
                shipManager.ProcessPendingShipUIs();

                // Ensure each ship in the FleetData has a UI item and that the UI is parented correctly
                EnsureFleetShipUIs(fleetCon);
            }
        }

        private void EnsureFleetShipUIs(FleetController fleetCon)
        {
            if (fleetCon == null || fleetCon.FleetData == null) return;

            var shipManager = ShipManager.Instance;
            if (shipManager == null) return;

            // Preferred parent for ship UI items created by this fleet UI
            GameObject shipListParent = fleetCon.FleetData.ShipListUIParent;

            // If there's no parent yet, give ShipManager a chance to reparent pending UIs and return
            if (shipListParent == null)
            {
                shipManager.ProcessPendingShipUIs();
                return;
            }

            // Iterate fleet ship list and ensure UI exists and is parented to the fleet UI's ShipList container
            var ships = fleetCon.FleetData.ShipsList;
            if (ships == null || ships.Count == 0) return;

            for (int i = 0; i < ships.Count; i++)
            {
                var shipCon = ships[i];
                if (shipCon == null) continue;

                // If UI doesn't exist yet, instantiate it (ShipManager handles queuing/reparenting)
                if (shipCon.ShipListUIGameObject == null)
                {
                    shipManager.InstantiateShipListUIGameObject(shipCon, fleetCon.gameObject);
                }

                // If UI exists but not parented correctly, set the correct parent
                if (shipCon.ShipListUIGameObject != null)
                {
                    var currentParent = shipCon.ShipListUIGameObject.transform.parent;
                    if (currentParent == null || currentParent.gameObject != shipListParent)
                    {
                        shipCon.ShipListUIGameObject.transform.SetParent(shipListParent.transform, false);
                    }
                }
            }

            // Final pass to process any items that were queued by InstantiateShipListUIGameObject
            shipManager.ProcessPendingShipUIs();
        }

        void RemoveFleetConrollerFromAllControllers(FleetController fleetController)
        {
            FleetControllerList.Remove(fleetController);
        }
        void AddFleetConrollerFromAllControllers(FleetController fleetController)
        {
            FleetControllerList.Add(fleetController);
        }
        public void FleetToFleetManagement(FleetController fleetConA, FleetController fleetConB)
        {
            List<ShipController> shipListA = fleetConA.FleetData.GetShipList();
            List<ShipController> shipListB = fleetConB.FleetData.GetShipList();
            // we already know the civ of fleetConA == the civ of fleetConB
            if (GameController.Instance.AreWeLocalPlayer(fleetConA.FleetData.CivEnum))
            {
                //call up UI for civ
            }
            else
            {
                //call up AI for civ fleet management
            }

        }

        public FleetSO GetFleetSO_byInt(int fleetInt)
        {
            FleetSO result = null;
            for (int i = 0; i < fleetSOList.Count; i++)
            //foreach (var fleetSO in fleetSOList)
            {

                if (fleetSOList[i].CivIndex == fleetInt)
                {
                    result = fleetSOList[i];
                    break;
                }
            }
            return result;

        }

        private int GetUniqueIntAsDestination(int destinationInt)
        {
            if (destinationIntsInUse.Contains(destinationInt))
            {
                destinationInt++;
                if (!destinationIntsInUse.Contains(destinationInt))
                    return destinationInt;
            }
            else
            {
                destinationIntsInUse.Add(destinationInt);

            }
            return destinationInt;

        }
        public void RemoveFleet(GameObject go, int asDestinationInt)
        {
            destinationIntsInUse.Remove(asDestinationInt);
            FleetControllerList.Remove(go.GetComponent<FleetController>());
            go.IsDestroyed();
        }
        public int GetNewFleetInt(CivEnum civEnum)
        {
            List<int> ourFleetNumsInUse = fleetNumsInUse[civEnum];
            int numToReturn = 1;
            if (ourFleetNumsInUse.Count == 0)
            {
                ourFleetNumsInUse.Add(numToReturn);
                return numToReturn;
            }
            else
            {
                for (int i = 1; i < ourFleetNumsInUse.Count + 1; i++)
                {
                    if (!ourFleetNumsInUse.Contains(numToReturn))
                    {
                        numToReturn = i;
                    }
                    else
                    {
                        numToReturn = i + 1;
                    }
                }
                ourFleetNumsInUse.Add(numToReturn);
                ourFleetNumsInUse.Sort();
            }
            return numToReturn;
        }
        public void RemoveFleetNumInUse(CivEnum civEnum, int fleetInt)
        {
            fleetNumsInUse[civEnum].Remove(fleetInt);
        }
        public void ExposeAllFleetInsigniaSprites(CivEnum civEnum)
        {
            localPlayerCanSeeMyInsigniaList.Add(civEnum);
            foreach (var fleetController in FleetControllerList)
            {
                if (fleetController.FleetData.CivEnum == civEnum)
                {
                    FleetChildFields fleetChildFields = fleetController.GetComponent<FleetChildFields>();
                    fleetChildFields.InsigniaUnknownGO.SetActive(false);
                    //SpriteRenderer sr2 = fleetChildFields.InsigniaGO.GetComponent<SpriteRenderer>();
                    //sr2.enabled = true;
                }
            }
        }

        internal void RemoveFleetConIfShipListIsEmpty(ShipController shipController)
        {
            int foundOne = -1;
            for (int i = 0; i < FleetControllersInGame.Count; i++)
            {
                if (shipController.ShipData.CurrentFleetController == FleetControllerList[i])
                {
                    if (FleetControllerList[i].FleetData.ShipsList.Count == 0)
                    {
                        foundOne = i;
                    }
                }
            }
            if (foundOne > -1)
            {
                //FleetControllersInGame[foundOne].IsDestroyed();
                //RemoveFleetInt(FleetControllersInGame[foundOne].FleetData.CivEnum, FleetControllersInGame[foundOne].FleetData.FleetInt);
                //FleetControllersInGame.RemoveAt(foundOne);
                //Destroy(FleetControllerList[foundOne].FleetUIGameObject);
                //Destroy(FleetControllerList[foundOne].gameObject);
            }
        }

        internal void DestroyFleetController(FleetController fleetController)
        {
            RemoveFleetNumInUse(fleetController.FleetData.CivEnum, fleetController.FleetData.FleetInt);
            if (FleetControllersInGame.Contains(fleetController))
                FleetControllersInGame.Remove(fleetController);
            RemoveFleetNumInUse(fleetController.FleetData.CivEnum, fleetController.FleetData.FleetInt);
            Destroy(fleetController.FleetUIGameObject);
            Destroy(fleetController.DropLine.gameObject);
            Destroy(fleetController.gameObject);
        }

        internal void RemoveFogWarRevealer(csFogWar.FogRevealer tempFogRevealerFleet)
        {
            fogWar.RemoveFogRevealer(tempFogRevealerFleet);
        }


    }
}