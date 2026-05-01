using BOTF3D.Core;
using BOTF3D.GamePlay;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// The UI controller owns hierarchy and presentation.
/// </summary>
/// 
namespace BOTF3D.UI
{
    public class DiplomacyMenuUIController : MonoBehaviour
    {
        public static DiplomacyMenuUIController Instance;
        //private Camera galaxyEventCamera;
        //[SerializeField]
        //private Canvas parentCanvas;
        public DiplomacyController DiplomacyController;
        public GameObject DiplomacyMenuView;
        public GameObject ADiplomacyMenuView;
        [SerializeField]
        private GameObject diplomacyNoContacts;
        [SerializeField]
        private GameObject diplomacyListContainter;
        [SerializeField]
        private GameObject firstContact;
        [SerializeField]
        private TMP_Text theirNameTMP;
        [SerializeField]
        private Image theirInsignia;
        [SerializeField]
        private Image theirRaceImage;
        [SerializeField]
        private TMP_Text relationTMP;
        [SerializeField]
        private TMP_Text relationPointsTMP;
        [SerializeField]
        private TMP_Text traitOneTMP;
        [SerializeField]
        private TMP_Text traitTwoTMP;
        [SerializeField]
        private TMP_Text traitThreeTMP;
        [SerializeField]
        private TMP_Text traitFourTMP;
        [SerializeField]
        private TMP_Text ourTraitOneTMP;
        [SerializeField]
        private TMP_Text ourTraitTwoTMP;
        [SerializeField]
        private TMP_Text ourTraitThreeTMP;
        [SerializeField]
        private TMP_Text ourTraitFourTMP;
        [SerializeField]
        private TMP_Text transmissionTMP;
        [SerializeField]
        private TMP_Text descriptionTMP;
        [SerializeField]
        private GameObject descriptionPanel;
        [SerializeField]
        private GameObject[] UI_PanelGOs;
        [SerializeField]
        private Image[] tabButtonMasks;
        [SerializeField]
        private GameObject InteractionButtonGO;
        [SerializeField]
        private GameObject tradeButtonGO;
        [SerializeField]
        private GameObject engagementButtonGO;
        [SerializeField]
        private GameObject techButtonGO;
        [SerializeField]
        private GameObject aidButtonGO;
        [SerializeField]
        private GameObject allianceButtonGO;
        [SerializeField]
        private GameObject gatherIntelButtonGO;
        [SerializeField]
        private GameObject theftButtonGO;
        [SerializeField]
        private GameObject disinformationButtonGO;
        [SerializeField]
        private GameObject sabatogeButtonGO;
        [SerializeField]
        private GameObject combatButtonGO;
        [SerializeField]
        private GameObject closeDiplomacyButtonGO;
        int _scouts;
        int _destroyers;
        int _cruisers;
        int _ltCruisers;
        int _hvyCruisers;
        int _transports;
        [Header("Runtime lists")]
        [SerializeField] public List<GameObject> ListOfDiplomacyUiGos = new List<GameObject>();
        public bool IsVisibleA_DiplomacyMenuView => ADiplomacyMenuView.activeSelf; // for pause time
        public bool IsVisibleDiplomacyMenuView => DiplomacyMenuView.activeSelf; // for pause time

        private void Awake()
        {
            // ? Scene-based singleton
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("? DiplomacyMenuUIController: Instance assigned");
            }
            else if (Instance != this)
            {
                Debug.LogWarning($"? Duplicate DiplomacyMenuUIController found! Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Initially hide fleet menu views
            if (DiplomacyMenuView != null)
                DiplomacyMenuView.SetActive(false);
            if (ADiplomacyMenuView != null)
                ADiplomacyMenuView.SetActive(false);
            //galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;
            //parentCanvas.worldCamera = galaxyEventCamera;

        }
        public void ShowDiplomacyMenuView()
        {
            if (DiplomacyMenuView == null) return;

            DiplomacyMenuView.SetActive(true);

            // ✅ Re-enable raycasts when showing
            var canvasGroups = DiplomacyMenuView.GetComponentsInChildren<CanvasGroup>(true);
            foreach (var group in canvasGroups)
            {
                group.blocksRaycasts = true;
            }

            // ✅ Activate scroll views
            var scrollRects = DiplomacyMenuView.GetComponentsInChildren<ScrollRect>(true);
            foreach (var scroll in scrollRects)
            {
                scroll.gameObject.SetActive(true);
            }

            Debug.Log("ShowDiplomacyMenuView: Activated with raycasts enabled");
        }

        public void ShowA_DiplomacyMenuView()
        {
            if (ADiplomacyMenuView == null) return;

            ADiplomacyMenuView.SetActive(true);

            // ✅ Re-enable raycasts when showing
            var canvasGroups = ADiplomacyMenuView.GetComponentsInChildren<CanvasGroup>(true);
            foreach (var group in canvasGroups)
            {
                group.blocksRaycasts = true;
            }

            // ✅ Activate scroll views
            var scrollRects = ADiplomacyMenuView.GetComponentsInChildren<ScrollRect>(true);
            foreach (var scroll in scrollRects)
            {
                scroll.gameObject.SetActive(true);
            }

            Debug.Log("ShowA_DiplomacyMenuView: Activated with raycasts enabled");
        }
        public void HideDiplomacyMenuView()
        {
            if (DiplomacyMenuView == null) return;

            // ✅ Deactivate all child scroll views and canvas groups
            var scrollRects = DiplomacyMenuView.GetComponentsInChildren<ScrollRect>(true);
            foreach (var scroll in scrollRects)
            {
                scroll.gameObject.SetActive(false);
                Debug.Log($"  Deactivated scroll view: {scroll.name}");
            }

            // ✅ Disable raycast blocking on canvas groups
            var canvasGroups = DiplomacyMenuView.GetComponentsInChildren<CanvasGroup>(true);
            foreach (var group in canvasGroups)
            {
                group.blocksRaycasts = false;
                Debug.Log($"  Disabled raycasts on: {group.name}");
            }

            DiplomacyMenuView.SetActive(false);
            Debug.Log("HideDiplomacyMenuView: Complete - all raycasts disabled");
        }
        public void HideA_DiplomacyMenuView()
        {
            if (ADiplomacyMenuView == null) return;

            // ✅ Deactivate all child scroll views and canvas groups
            var scrollRects = ADiplomacyMenuView.GetComponentsInChildren<ScrollRect>(true);
            foreach (var scroll in scrollRects)
            {
                scroll.gameObject.SetActive(false);
            }

            // ✅ Disable raycast blocking on canvas groups
            var canvasGroups = ADiplomacyMenuView.GetComponentsInChildren<CanvasGroup>(true);
            foreach (var group in canvasGroups)
            {
                group.blocksRaycasts = false;
            }

            ADiplomacyMenuView.SetActive(false);
            Debug.Log("HideA_DiplomacyMenuView: Complete");
        }

        public void SetActiveSetParentADiplomacyUIData(DiplomacyController diploCon)
        {
            diploCon.DiplomacyUIGameObject.SetActive(true);
            diploCon.DiplomacyUIGameObject.transform.SetParent(DiplomacyMenuView.transform, false);
        }
        // ToDo: Build out Diplomacy to work with traits for AI and human players
        public void MoveTheDiplomacyUIGO(GameObject diploMenu)
        {
            for (int i = 0; i < ListOfDiplomacyUiGos.Count; i++)
            {
                if (ListOfDiplomacyUiGos[i] == diploMenu)
                {
                    ListOfDiplomacyUiGos[i].SetActive(true);
                    ListOfDiplomacyUiGos[i].transform.SetParent(ADiplomacyMenuView.transform, false);
                    return;
                }
            }
        }
        public void MoveBackAnyDiplomacyUIGO()
        {
            //for (int i = 0; i < aDiplomacyMenuView.transform.childCount; i++)
            //{
            //    if (aDiplomacyMenuView.transform.GetChild(i).gameObject != null)
            //        aDiplomacyMenuView.transform.GetChild(i).gameObject.transform.SetParent(diplomacyListContainter.transform, false); ;
            //}
            while (ADiplomacyMenuView.transform.childCount > 0)
            {
                var child = ADiplomacyMenuView.transform.GetChild(0).gameObject;
                child.transform.SetParent(diplomacyListContainter.transform, false);
            }
        }
        public void SetUpDiplomacyUIElements(GameObject diplomacyUIGO, GameObject diplomacyControllerGO, List<ShipController> shipList)
        {
            DiplomacyController diplomacyCon = diplomacyControllerGO.GetComponent<DiplomacyController>();
            Debug.Log($"SetUpDiplomacyUIElements called for DiplomacyController: {(diplomacyCon == null ? "null" : diplomacyCon.DiplomacyData.CivOne?.CivData.CivShortName ?? "unknown")}");

            if (diplomacyUIGO != null)
                diplomacyCon.DiplomacyUIGameObject = diplomacyUIGO;
            GalaxyMenuUIController.Instance.HideNoContactUI();
            ADiplomacyMenuView.gameObject.SetActive(true);
            DiplomacyMenuView.gameObject.SetActive(true);
            diplomacyListContainter.gameObject.SetActive(true);
            // diplomacyUIGO.GetComponent<DiplomacyMenuUIController>() = this;
            CivController partyOne = CivManager.Instance.GetCivControllerByCivEnum(diplomacyCon.DiplomacyData.CivEnumSideOne);
            CivController partyTwo = CivManager.Instance.GetCivControllerByCivEnum(diplomacyCon.DiplomacyData.CivEnumSideTwo);
            CivController notLocalPlayerCiv;
            CivController localPlayerCiv;
            StarSysController homeSysController;
            diplomacyCon.DiplomacyUIGameObject.SetActive(true);
            diplomacyCon.DiplomacyUIGameObject.transform.SetParent(diplomacyListContainter.transform, false);


            if (!DiplomacyManager.Instance.DiplomacyControllers.Contains(diplomacyCon))
                DiplomacyManager.Instance.DiplomacyControllers.Add(diplomacyCon);// add to list so GalaxyMenuUI has it
            if (!ListOfDiplomacyUiGos.Contains(diplomacyCon.DiplomacyUIGameObject))
                ListOfDiplomacyUiGos.Add(diplomacyCon.DiplomacyUIGameObject); // add to list of DiplomacyUI Game Objects for GalaxyMenuUI
            if (GameController.Instance.AreWeLocalPlayer(partyOne.CivData.CivEnum))
            {
                notLocalPlayerCiv = partyTwo;
                localPlayerCiv = partyOne;
                GalaxyMenuUIController.Instance.FindTheirHomeSystem(partyTwo, out homeSysController);
                //LoadCivDataInUI(ourDiplomacyController.DiplomacyData.CivOther, ourDiplomacyController);
            }
            else
            {
                notLocalPlayerCiv = partyOne;
                localPlayerCiv = partyTwo;
                GalaxyMenuUIController.Instance.FindTheirHomeSystem(partyOne, out homeSysController);
                //LoadCivDataInUI(ourDiplomacyController.DiplomacyData.CivMajor, ourDiplomacyController);
            }
            Image[] listOfImages = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<Image>();
            for (int q = 0; q < listOfImages.Length; q++)
            {
                // int techLevelInt = (int)CivManager.Instance.LocalPlayerCivContoller.CivData.StartingTechLevel / 100; // Early Tech level = 100, Supreme = 900;
                bool foundRaceImage = false;
                bool foundInsigniaImage = false;
                listOfImages[q].enabled = true;
                var name = listOfImages[q].name.ToString();
                switch (name)
                {
                    case "RaceImage":
                        listOfImages[q].sprite = notLocalPlayerCiv.CivData.CivRaceSprite;
                        foundRaceImage = true;
                        break;
                    case "InsigniaTheirCiv (TMP)":
                        listOfImages[q].sprite = notLocalPlayerCiv.CivData.InsigniaSprite;
                        foundInsigniaImage = true;
                        break;
                    default:
                        break;
                }
                if (foundRaceImage && foundInsigniaImage)
                {
                    break;
                }
            }
            RectTransform[] rectTransforms = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<RectTransform>();
            for (int i = 0; i < rectTransforms.Length; i++)
            {
                switch (rectTransforms[i].name)
                {
                    case "MiniMap":
                        rectTransforms[i].gameObject.SetActive(true);
                        float x = homeSysController.StarSysData.GetPosition().x * 0.12f;
                        //float y = 0f;
                        float z = homeSysController.StarSysData.GetPosition().z * 0.12f;

                        // Get the first child's RectTransform
                        if (rectTransforms[i].childCount > 0)
                        {
                            RectTransform dot = rectTransforms[i].GetChild(0).GetComponent<RectTransform>();
                            dot.anchoredPosition = new Vector2(x, z);
                        }
                        else
                        {
                            Debug.LogWarning($"MiniMap has no children at {rectTransforms[i].name}");
                        }
                        break;
                    case "InteractionButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        InteractionButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "TradeButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        tradeButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "EngagementButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        engagementButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "TechButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        techButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "AidButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        aidButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "AllianceButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        allianceButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "GatherIntel":
                        rectTransforms[i].gameObject.SetActive(true);
                        gatherIntelButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "Theft":
                        rectTransforms[i].gameObject.SetActive(true);
                        theftButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "Disinformation":
                        rectTransforms[i].gameObject.SetActive(true);
                        disinformationButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "SabatogeButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        sabatogeButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "CombatButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        combatButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "ButtonCloseDiplomacytUI":
                        rectTransforms[i].gameObject.SetActive(true);
                        closeDiplomacyButtonGO = rectTransforms[i].gameObject;
                        break;
                    default:
                        break;
                }
            }
            TextMeshProUGUI[] ourTMPs = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0; i < ourTMPs.Length; i++)
            {
                int techLevelInt = (int)notLocalPlayerCiv.CivData.CurrentTechLevel / 100; // Early Tech level = 100, Supreme = 900;
                ourTMPs[i].enabled = true;
                var aName = ourTMPs[i].name;
                var sysCiv = homeSysController.StarSysData.CurrentOwnerCivEnum;
                string nameOfWhatWeSee = notLocalPlayerCiv.CivData.CivShortName;
                if (shipList != null)
                    CountShips(shipList);
                //   ourTMPs[i].text = "No Intel";
                //    continue;
                switch (aName)
                {
                    case "ThierNameText":
                        ourTMPs[i].text = notLocalPlayerCiv.CivData.CivLongName;
                        break;
                    case "RelationText":
                        ourTMPs[i].text = diplomacyCon.DiplomacyData.DiplomacyStatusEnumOfCivs.ToString();
                        break;
                    case "Text Points (TMP)":
                        ourTMPs[i].text = diplomacyCon.DiplomacyData.DiplomacyPointsOfCivs.ToString();
                        break;
                    case "TraitText (1)":
                        ourTMPs[i].text = notLocalPlayerCiv.CivData.Warlike.ToString();
                        break;
                    case "TraitText (2)":
                        ourTMPs[i].text = notLocalPlayerCiv.CivData.Xenophbia.ToString();
                        break;
                    case "TraitText (3)":
                        ourTMPs[i].text = notLocalPlayerCiv.CivData.Ruthelss.ToString();
                        break;
                    case "TraitText (4)":
                        ourTMPs[i].text = notLocalPlayerCiv.CivData.Greedy.ToString();
                        break;
                    case "OurTraitText (1)":
                        ourTMPs[i].text = localPlayerCiv.CivData.Warlike.ToString();
                        break;
                    case "OurTraitText (2)":
                        ourTMPs[i].text = localPlayerCiv.CivData.Xenophbia.ToString();
                        break;
                    case "OurTraitText (3)":
                        ourTMPs[i].text = localPlayerCiv.CivData.Ruthelss.ToString();
                        break;
                    case "OurTraitText (4)":
                        ourTMPs[i].text = localPlayerCiv.CivData.Greedy.ToString();
                        break;
                    case "WhoWeHit":
                        ourTMPs[i].text = nameOfWhatWeSee;
                        break;
                    case "NumS":
                        ourTMPs[i].text = _scouts.ToString();
                        break;
                    case "NumD":
                        ourTMPs[i].text = _destroyers.ToString();
                        break;
                    case "NumC":
                        ourTMPs[i].text = _cruisers.ToString();
                        break;
                    case "NumLC":
                        ourTMPs[i].text = _ltCruisers.ToString();
                        break;
                    case "NumHC":
                        ourTMPs[i].text = _hvyCruisers.ToString();
                        break;
                    case "NumT":
                        ourTMPs[i].text = _transports.ToString();
                        break;
                    default:
                        break;
                }
            }
            Button[] listButtons = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<Button>();
            foreach (var listButton in listButtons)
            {
                switch (listButton.name)
                {
                    case "InteractionButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.ProposeTrade(diplomacyCon));
                        break;
                    case "OpenDiscriptionButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.ProposeTrade(diplomacyCon));
                        break;
                    case "TradeButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.ProposeTrade(diplomacyCon));
                        break;
                    case "EngagementButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.Engagement(diplomacyCon));
                        break;
                    case "TechButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.ProposeTech(diplomacyCon));
                        break;
                    case "AidButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.SendAid(diplomacyCon));
                        break;
                    case "AllianceButton":
                        //fleetCon.FleetData.FleetButtonUp = listButton;
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.OfferAlliance(diplomacyCon));
                        break;
                    case "GatherIntelButton":
                        // fleetCon.FleetData.FleetButtonDown = listButton;
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.GatherIntel(diplomacyCon));
                        break;
                    case "TheftButton":
                        //fleetCon.FleetData.FleetButtonUIClose = listButton;
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.Theft(diplomacyCon));
                        break;
                    case "DisinformationButton":
                        //fleetCon.FleetData.FleetButtonUIClose = listButton;
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.Disinformation(diplomacyCon));
                        break;
                    case "SabatogeButton":
                        //fleetCon.FleetData.FleetButtonUIClose = listButton;
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.Sabatoge(diplomacyCon));
                        break;
                    case "CombatButton":
                        //fleetCon.FleetData.FleetButtonUIClose = listButton;
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.Combat(diplomacyCon));
                        break;
                    default:
                        break;
                }
            }
        }
        //public void ListDiplomacyUIGO(GameObject diploGO)
        //{
        //    if (diploGO == null)
        //    {
        //        Debug.LogError("RegisterDiplomacyUIGO called with null GameObject.");
        //        return;
        //    }

        //    //// If container is missing, fallback to attaching to this controller
        //    //Transform parentTransform = aDiplomacyMenuView != null
        //    //    ? aDiplomacyMenuView.transform
        //    //    : diplomacyListContainter.transform;

        //    //diploGO.transform.SetParent(parentTransform, false);
        //    //diploGO.SetActive(true);
        //    //diploGO.layer = 5;

        //    // Prevent duplicates
        //    if (!ListOfDiplomacyUiGos.Contains(diploGO))
        //    {
        //        ListOfDiplomacyUiGos.Add(diploGO);
        //        Debug.Log($"Registered diplomacy UI '{diploGO.name}' under '{parentTransform.name}'.");
        //    }
        //    else
        //    {
        //        Debug.Log($"Diplomacy UI '{diploGO.name}' is already registered — skipping duplicate add.");
        //    }
        //}

        public void UnregisterDiplomacyUIGO(GameObject diploGO)
        {
            if (diploGO == null)
                return;

            if (ListOfDiplomacyUiGos.Remove(diploGO))
            {
                Debug.Log($"Unregistered diplomacy UI '{diploGO.name}'.");
            }
        }

        public void ClearAllDiplomacyUIs()
        {
            foreach (var go in ListOfDiplomacyUiGos)
            {
                if (go != null)
                    Destroy(go);
            }
            ListOfDiplomacyUiGos.Clear();
            Debug.Log("Cleared all diplomacy UI GameObjects.");
        }

        private void CountShips(List<ShipController> ships)
        {
            _scouts = ships.Count(s => s.ShipData.ShipType == ShipType.Scout);
            _destroyers = ships.Count(s => s.ShipData.ShipType == ShipType.Destroyer);
            _cruisers = ships.Count(s => s.ShipData.ShipType == ShipType.Cruiser);
            _ltCruisers = ships.Count(s => s.ShipData.ShipType == ShipType.LtCruiser);
            _hvyCruisers = ships.Count(s => s.ShipData.ShipType == ShipType.HvyCruiser);
            _transports = ships.Count(s => s.ShipData.ShipType == ShipType.Transport);
        }
        public void RemoveDiplomacyUIGO(GameObject diploGO)
        {// if a diplomacy UI is destroyed, remove it from our list
            if (ListOfDiplomacyUiGos.Contains(diploGO))
            {
                ListOfDiplomacyUiGos.Remove(diploGO);
            }
        }
        private void OnDisable()
        {
            // When the UI menu closes (e.g., switching menus or hiding canvas)
            CleanupDestroyedOrInactiveUIs();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Debug.Log("DiplomacyMenuUIController: Instance cleared");
            }
        }

        public void CleanupDestroyedOrInactiveUIs()
        {
            // Remove any destroyed or inactive GameObjects from the list
            ListOfDiplomacyUiGos.RemoveAll(go => go == null || !go.activeInHierarchy);
            Debug.Log("DiplomacyMenuUIController: Cleaned up destroyed or inactive diplomacy UIs.");
        }

    }
}
