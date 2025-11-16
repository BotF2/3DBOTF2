using FischlWorks_FogWar;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System;


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
        public GameObject hvyCruiserBluePrintPrefab;
        public GameObject transportBluePrintPrefab;
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
        [SerializeField]
        private GameObject galaxyImage;
        public GameObject GalaxyCenter;
        public List<FleetController> FleetControllerList { get; private set; } = new List<FleetController>();
        [SerializeField]
        private Sprite unknownfleet;
        [SerializeField]
        private GameObject canvasShipManager;
        [SerializeField]
        private List<int> destinationIntsInUse = new List<int>() { 0 };
        private Dictionary<CivEnum, List<int>> fleetNumsInUse  = new Dictionary<CivEnum, List<int>>();
        public List<FleetController> FleetControllersInGame = new List<FleetController>();
        [SerializeField]
        private GameObject fleetUIGOContentParent;
        [SerializeField]
        private GameObject fleetShipsContentFolderParent;
        private List<CivEnum> localPlayerCanSeeMyInsigniaList = new List<CivEnum>();


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
        }
        public void CleanUpDictinaryForFleetNums()
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

        public void BuildFirstFleetsNearSys(StarSysController sysCon, bool inSystem)
        {
            // first path here is sent on loading the game for civs with warp, first fleets from Systems/Civs with warp
            FleetSO fleetSO = GetFleetSObyInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
            var position = sysCon.StarSysData.GetPosition();

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
            FleetController aFleet = InstantiateFleet(sysCon, fleetData, position, inSystem);  
        }
        public FleetController InstatiateEmptyFleetController()
        { 
            FleetController fleetController = Instantiate(fleetPrefab, new Vector3(0, 0, 0),
                    Quaternion.identity);
            return fleetController;
        }

        public FleetController InstantiateFleet(StarSysController sysCon, FleetData fleetData, Vector3 position, bool inSystem)
        {
            IEnumerable<StarSysController> ourCivSysCons =
            from x in StarSysManager.Instance.StarSysControllerList
            where (x.StarSysData.CurrentOwnerCivEnum == fleetData.CivEnum)
            select x;
            var ourSysCons = ourCivSysCons.ToList();

            FleetController fleetController = Instantiate(fleetPrefab, new Vector3(0, 0, 0),
                    Quaternion.identity);
            FleetControllerList.Add(fleetController); // add to list of all fleet controllers
            fleetController.gameObject.layer = 6; // galaxy layer
            fleetController.BackgroundGalaxyImage = galaxyImage;
            fleetController.FleetData = fleetData;
            fleetController.FleetData.ShipsList.Clear();
            //// Tool Tip?
            //Canvas[] canvasArray = fleetController.gameObject.GetComponentsInChildren<Canvas>();
            ////for (int j = 0; j < canvasArray.Length; j++)
            ////{
            ////    if (canvasArray[j].name == "CanvasToolTip")
            ////    {
            ////        fleetController.CanvasToolTip = canvasArray[j];
            ////    }
            ////}
            if (!inSystem)
            {
                var transGalaxyCenter = GalaxyCenter.gameObject.transform;
                var trans = sysCon.gameObject.transform;
                fleetController.transform.SetParent(transGalaxyCenter, true); // parent is galaxy center, it is not in a star system
                                                                              // now put it near the home world and visible/seen on the galaxy map, in galaxy space. It is not 'hidden' in the system
                fleetController.transform.Translate(new Vector3(trans.position.x + 20f, trans.position.y + 20f, trans.position.z));
                fleetData.Position = fleetController.transform.position;
                ShipManager.Instance.BuildShipsOfFirstFleet(fleetController);
            }
            else // it is in the system shipyard so 'hidden' on the galaxy map inside the system
            {
                fleetController.transform.SetParent(sysCon.gameObject.transform, false);
            }
            fleetController.transform.localScale = new Vector3(0.7f, 0.7f, 1); // scale ship insignia here
            int fleetInt = GetNewFleetInt(fleetData.CivEnum);
            fleetController.gameObject.name = fleetData.CivShortName.ToString() + " Fleet " + fleetInt.ToString(); // name game object
            fleetData.Name = fleetController.gameObject.name;
            //if (!inSystem)
            //    ShipManager.Instance.BuildShipsOfFirstFleet(fleetController);
            fleetController.FleetData.FleetInt = fleetInt;
            fleetController.Name = fleetData.Name;
            FleetControllersInGame.Add(fleetController);
            fleetController.FleetData.CurrentWarpFactor = 0f;
            TextMeshProUGUI TheText = fleetController.gameObject.GetComponentInChildren<TextMeshProUGUI>();

            if (GameController.Instance.AreWeLocalPlayer(fleetData.CivEnum))
            {
                var ourFogRevealerFleet = new csFogWar.FogRevealer(fleetController.transform, 200, true);
                fogWar.AddFogRevealer(ourFogRevealerFleet);
            }
            else
            {
                fleetController.gameObject.AddComponent<csFogVisibilityAgent>();
                var ourFogVisibilityAgent = fleetController.gameObject.GetComponent<csFogVisibilityAgent>();
                ourFogVisibilityAgent.FogWar = fogWar;
                ourFogVisibilityAgent.enabled = true;
            }

            TheText.text = fleetController.gameObject.name;
            fleetData.Name = TheText.text;
            var Renderers = fleetController.gameObject.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < Renderers.Length; i++)
            {
                if (Renderers[i] != null)
                {
                    if (Renderers[i].name == "InsigniaSprite")
                    {
                        Renderers[i].sprite = fleetController.FleetData.Insignia;
                        if (!GameController.Instance.AreWeLocalPlayer(fleetController.FleetData.CivEnum) && !localPlayerCanSeeMyInsigniaList.Contains(fleetData.CivEnum))
                        {
                            Renderers[i].gameObject.SetActive(false);
                        }
                        else Renderers[i].gameObject.SetActive(true);
                    }
                    if (Renderers[i].name == "InsigniaUnknown" && (GameController.Instance.AreWeLocalPlayer(fleetController.FleetData.CivEnum) || localPlayerCanSeeMyInsigniaList.Contains(fleetData.CivEnum)))
                    {
                        Renderers[i].gameObject.SetActive(false);
                    }
                }
            }
            // The line from Fleet to underlying galaxy image and to destination
            MapLineMovable[] ourLineToGalaxyImageScript = fleetController.gameObject.GetComponentsInChildren<MapLineMovable>();
            for (int i = 0; i < ourLineToGalaxyImageScript.Length; i++)
            {
                if (ourLineToGalaxyImageScript[i].name == "DropLine")
                {
                    ourLineToGalaxyImageScript[i].GetLineRenderer();
                    ourLineToGalaxyImageScript[i].lineRenderer.startColor = Color.red;
                    ourLineToGalaxyImageScript[i].lineRenderer.endColor = Color.red;
                    ourLineToGalaxyImageScript[i].transform.SetParent(fleetController.transform, false);
                    Vector3 galaxyPlanePoint = new Vector3(fleetController.transform.position.x,
                        galaxyImage.transform.position.y, fleetController.transform.position.z);
                    Vector3[] points = { fleetController.transform.position, galaxyPlanePoint };
                    ourLineToGalaxyImageScript[i].SetUpLine(points);
                    fleetController.DropLine = ourLineToGalaxyImageScript[i];
                }

            }
            fleetController.FleetData.Destination = GalaxyCenter;
            foreach (var civCon in CivManager.Instance.CivControllersInGame)
            {
                if (civCon.CivData.CivEnum == fleetData.CivEnum)
                    fleetData.CivController = civCon;
            }
            fleetController.gameObject.SetActive(true);

            fleetController.UpdateMaxWarp();
            InstantiateFleetUIGameObject(fleetController);
            return fleetController;          
        }
        private void InstantiateFleetUIGameObject(FleetController fleetCon)
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
                    //var originalParent = fleetCon.FleetUIGameObject.GetComponent<FleetAndSystemChildController>();
                    //if (originalParent != null)
                    //{
                    //    //MainMenuUIController.Instance.GalaxyMenuGO.SetActive(true);
                    //    FleetMenuUIController.Instance.FleetMenuView.gameObject.SetActive(true);
                    //    originalParent.OriginalParentTransform = FleetMenuUIController.Instance.FleetListContainer.transform;
                    //    FleetMenuUIController.Instance.FleetMenuView.gameObject.SetActive(false);
                    //    //MainMenuUIController.Instance.GalaxyMenuGO.SetActive(false);
                    //}
                    thisFleetUIGameObject.transform.SetParent(fleetUIGOContentParent.transform, false);
                    var transforms = thisFleetUIGameObject.GetComponentsInChildren<Transform>();
                    for (int i = 0; i < transforms.Length; i++)
                    {
                        if (transforms[i].name == "FleetShipContent")
                        {
                            fleetCon.FleetData.ShipListUIParent = transforms[i].gameObject;
                            return;
                        }
                    }
                }
            }
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

        public FleetSO GetFleetSObyInt(int fleetInt)
        {
            FleetSO result = null;
            for (int i = 0;i< fleetSOList.Count; i++)
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
        public static GameObject FindGameObjectInChildrenWithTag(GameObject parent, string tag)
        {
            Transform t = parent.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                if (t.GetChild(i).gameObject.tag == tag)
                    return t.GetChild(i).gameObject;
            }
            return null;
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
                        break;
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
        public void RemoveFleetInt(CivEnum civEnum, int fleetInt)
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
                    Transform[] transforms = fleetController.gameObject.GetComponentsInChildren<Transform>();
                    foreach (Transform t in transforms)
                    {
                        if (t.name == "InsigniaHolder")
                        {
                            t.GetChild(0).gameObject.SetActive(true);// activate the child of holder so the sprite renderer can be found
                            break;
                        }
                    }
                    var Renderers = fleetController.gameObject.GetComponentsInChildren<SpriteRenderer>();
                    for (int i = 0; i < Renderers.Length; i++)
                    {
                        if (Renderers[i] != null)
                        {
                            if (Renderers[i].name == "InsigniaSprite")
                            {
                                Renderers[i].gameObject.SetActive(true);
                                var fog = fleetController.gameObject.GetComponent<csFogVisibilityAgent>();
                                if (fog != null)
                                    fog.spriteRenderers.Add(Renderers[i]);
                            }
                            else if (Renderers[i].name == "InsigniaUnknown")
                                Renderers[i].gameObject.SetActive(false);
                        }
                    }
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

        internal void NewImageInShipInventory(ShipType scout)
        {
            
        }
    }
}