using BOTF3D.Combat;
using BOTF3D.Core;

using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;


/// <summary>
/// The UI controller owns hierarchy and presentation.
/// </summary>
/// 
namespace BOTF3D.UI
{
    public class DiplomacyMenuUIController : MonoBehaviour
    {
        public void Initialize() { }
        public void UpdateState() { }
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
        private GameObject combatButtonGO;
        [SerializeField]
        private GameObject withdrawButtonGO;
        [SerializeField]
        private GameObject declareWarButtonGO;
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

        private bool _subscribedToTurnAdvance;

        private void OnEnable()
        {
            DiplomacyManager.OnDiplomacyProjectResolved += RefreshAllActiveProposalDisplays;
            TrySubscribeToTurnAdvance();
        }

        // TimeManager.Instance may not exist yet when OnEnable runs (same race
        // IntelligenceUIController.TrySubscribe guards against) - also called from
        // SetUpDiplomacyUIElements, which is guaranteed to run once real gameplay starts, as a
        // fallback retry point.
        private void TrySubscribeToTurnAdvance()
        {
            if (!_subscribedToTurnAdvance && TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTurnAdvanced += RefreshAllActiveProposalDisplays;
                _subscribedToTurnAdvance = true;
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
            TrySubscribeToTurnAdvance();

            if (diplomacyUIGO != null)
                diplomacyCon.DiplomacyUIGameObject = diplomacyUIGO;
            GalaxyMenuUIController.Instance.HideNoContactUI();

            // Only the single-encounter popup (ADiplomacyMenuView) should open here - the full
            // DiplomacyMenuView list (every previously met civ's card, via diplomacyListContainter)
            // must stay closed. That list is only supposed to be revealed by the ribbon's own
            // Diplomacy button (see GalaxyMenuUIController.OpenMenu's Menu.Diplomacy case). Every
            // civ's card was left parented (and active) inside diplomacyListContainter from its own
            // past encounter, so previously activating that container here made every earlier
            // contact's window reappear alongside a brand-new one - e.g. reaching a 3rd civ's
            // homeworld also popped the 1st and 2nd civs' windows back open.
            ADiplomacyMenuView.gameObject.SetActive(true);

            // Evict whatever card (if any) is currently sitting in the single-encounter view - e.g.
            // a prior encounter that got hidden without being fully closed - back to storage first,
            // so ADiplomacyMenuView never ends up holding more than one card at once.
            MoveBackAnyDiplomacyUIGO();

            CivController partyOne = CivManager.Instance.GetCivControllerByCivEnum(diplomacyCon.DiplomacyData.CivEnumSideOne);
            CivController partyTwo = CivManager.Instance.GetCivControllerByCivEnum(diplomacyCon.DiplomacyData.CivEnumSideTwo);
            CivController notLocalPlayerCiv;
            CivController localPlayerCiv;
            StarSysController homeSysController;
            diplomacyCon.DiplomacyUIGameObject.SetActive(true);

            // Parent into the single-encounter popup view (not the full list container) so only
            // this civ's card is visible.
            diplomacyCon.DiplomacyUIGameObject.transform.SetParent(ADiplomacyMenuView.transform, false);
            diplomacyCon.DiplomacyUIGameObject.transform.SetAsFirstSibling();

            if (!DiplomacyManager.Instance.DiplomacyControllers.Contains(diplomacyCon))
                DiplomacyManager.Instance.DiplomacyControllers.Add(diplomacyCon);
            if (!ListOfDiplomacyUiGos.Contains(diplomacyCon.DiplomacyUIGameObject))
                ListOfDiplomacyUiGos.Add(diplomacyCon.DiplomacyUIGameObject);
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
            IntelligenceUIController.Instance?.PinCiv(notLocalPlayerCiv.CivData.CivEnum);

            Image[] listOfImages = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<Image>();
            bool foundRaceImage = false;       // ✅ Declared OUTSIDE the loop
            bool foundInsigniaImage = false;   // ✅ Declared OUTSIDE the loop
            for (int q = 0; q < listOfImages.Length; q++)
            {
                listOfImages[q].enabled = true;
                var name = listOfImages[q].name;
                switch (name)
                {
                    case "RaceImage":
                        listOfImages[q].sprite = notLocalPlayerCiv.CivData.CivRaceSprite;
                        foundRaceImage = true;
                        Debug.Log($"  ✅ RaceImage set for {notLocalPlayerCiv.CivData.CivShortName}");
                        break;
                    case "InsigniaTheirCiv":   // ✅ FIX: removed erroneous " (TMP)" suffix
                        listOfImages[q].sprite = notLocalPlayerCiv.CivData.InsigniaSprite;
                        foundInsigniaImage = true;
                        Debug.Log($"  ✅ InsigniaTheirCiv set for {notLocalPlayerCiv.CivData.CivShortName}");
                        break;
                    default:
                        break;
                }
                if (foundRaceImage && foundInsigniaImage)
                {
                    break; // ✅ Now reachable: both found, stop searching
                }
            }
            if (!foundInsigniaImage)
            {
                Debug.LogWarning($"  ⚠️ InsigniaTheirCiv Image not found in DiplomacyUIGameObject for {notLocalPlayerCiv.CivData.CivShortName}. Check the GameObject name in the prefab.");
            }
            RectTransform[] rectTransforms = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<RectTransform>();
            for (int i = 0; i < rectTransforms.Length; i++)
            {
                switch (rectTransforms[i].name)
                {
                    case "MiniMap":
                        rectTransforms[i].gameObject.SetActive(true);
                        // FindTheirHomeSystem above can legitimately return null (it logs a warning
                        // rather than throwing - e.g. a minor race's home system not yet tracked at
                        // first contact) - dereferencing it unguarded used to throw a
                        // NullReferenceException right here, aborting this whole method before the
                        // button-wiring loop below ever ran, silently leaving every Diplomacy button
                        // (and the proposal display) permanently unwired for that civ's card.
                        if (homeSysController != null)
                        {
                            Vector3 homeSysPos = homeSysController.transform.position;
                            float x = homeSysPos.x * 0.12f;
                            float z = homeSysPos.z * 0.12f;

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
                        }
                        else
                        {
                            Debug.LogWarning($"SetUpDiplomacyUIElements: home system not found for {notLocalPlayerCiv.CivData.CivShortName} - MiniMap dot left unpositioned, continuing setup.");
                        }
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
                    case "CombatButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        combatButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "WithdrawButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        withdrawButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "DeclareWarButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        declareWarButtonGO = rectTransforms[i].gameObject;
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
                        ourTMPs[i].text = notLocalPlayerCiv.CivData.Xenophobia.ToString();
                        break;
                    case "TraitText (3)":
                        ourTMPs[i].text = notLocalPlayerCiv.CivData.Ruthless.ToString();
                        break;
                    case "TraitText (4)":
                        ourTMPs[i].text = notLocalPlayerCiv.CivData.Greedy.ToString();
                        break;
                    case "OurTraitText (1)":
                        ourTMPs[i].text = localPlayerCiv.CivData.Warlike.ToString();
                        break;
                    case "OurTraitText (2)":
                        ourTMPs[i].text = localPlayerCiv.CivData.Xenophobia.ToString();
                        break;
                    case "OurTraitText (3)":
                        ourTMPs[i].text = localPlayerCiv.CivData.Ruthless.ToString();
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
                    case "SystemReconButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.SystemRecon(diplomacyCon));
                        break;
                    case "CombatButton":
                        //fleetCon.FleetData.FleetButtonUIClose = listButton;
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.Combat(diplomacyCon));
                        break;
                    case "WithdrawButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() =>
                        {
                            // isSideOne must match DiplomacyData.CivOne/FleetControllerCivOne, not
                            // CivEnumSideOne (which OpenDiplomacyUI reassigns to "whichever civ is
                            // the local player" purely for display) - see DiplomacyController.SetResponse.
                            bool isSideOne = diplomacyCon.DiplomacyData.CivOne != null &&
                                GameController.Instance.AreWeLocalPlayer(diplomacyCon.DiplomacyData.CivOne.CivData.CivEnum);
                            diplomacyCon.SetResponse(isSideOne, DiplomacyData.EncounterResponse.Withdraw);
                        });
                        break;
                    case "DeclareWarButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.DeclareWar(diplomacyCon));
                        break;
                    default:
                        break;
                }
            }

            // AutoImproveToggle (see DiploUIprefab.prefab) - opts this civ pair into
            // DiplomacyManager.ProcessAutoImproveRelations. Reflects the card's current
            // DiplomacyData.AutoImproveRelations value without firing the listener (SetIsOn's second
            // argument), since this runs every time the card is (re)populated and shouldn't re-toggle
            // the backend state just from being displayed.
            Toggle autoImproveToggle = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<Toggle>(true)
                .FirstOrDefault(t => t.name == "AutoImproveToggle");
            if (autoImproveToggle != null)
            {
                autoImproveToggle.SetIsOnWithoutNotify(diplomacyCon.DiplomacyData.AutoImproveRelations);
                autoImproveToggle.onValueChanged.RemoveAllListeners();
                autoImproveToggle.onValueChanged.AddListener((isOn) => diplomacyCon.SetAutoImproveRelations(isOn));
            }

            RefreshActiveProposalDisplay(diplomacyCon);
        }

        /// <summary>
        /// Updates ActiveProposalText and ProposalProgressBar (see DiploUIprefab.prefab, under
        /// ProposalHeader/ProgressHeader) for one civ pair's card. Called from
        /// SetUpDiplomacyUIElements (card first shown/reopened), from the per-turn/on-resolve
        /// refresh below (a card left sitting open still catches proposals resolving or being
        /// started by AI/auto-improve), and from DiplomacyController.SendProposal so a button click
        /// shows "PENDING" immediately instead of waiting for the next turn tick or a reopen.
        /// Public so DiplomacyController (a different namespace) can call it directly.
        /// </summary>
        public void RefreshActiveProposalDisplay(DiplomacyController diplomacyCon)
        {
            if (diplomacyCon?.DiplomacyUIGameObject == null || diplomacyCon.DiplomacyData == null) return;

            TextMeshProUGUI activeProposalText = diplomacyCon.DiplomacyUIGameObject
                .GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "ActiveProposalText");
            if (activeProposalText != null)
                activeProposalText.text = FormatActiveProposal(diplomacyCon.DiplomacyData);

            Slider progressBar = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<Slider>(true)
                .FirstOrDefault(s => s.name == "ProposalProgressBar");
            if (progressBar != null)
            {
                DiplomacyProject activeProject = diplomacyCon.DiplomacyData.ActiveProjects.Find(p => !p.IsComplete);
                if (activeProject != null)
                {
                    progressBar.minValue = 0;
                    progressBar.maxValue = activeProject.TurnsTotal;
                    progressBar.value = activeProject.TurnsTotal - activeProject.TurnsRemaining;
                }
                else
                {
                    progressBar.minValue = 0;
                    progressBar.maxValue = 1;
                    progressBar.value = 0;
                }
            }
        }

        /// <summary>
        /// Refreshes every currently-tracked civ pair's proposal display - hooked to
        /// TimeManager.OnTurnAdvanced (so a card left open catches a proposal that just resolved,
        /// or one an AI/auto-improve civ started) and to DiplomacyManager.OnDiplomacyProjectResolved
        /// (so the human-initiated case updates as soon as it resolves, not just on the next
        /// button click or reopen).
        /// </summary>
        private void RefreshAllActiveProposalDisplays()
        {
            if (DiplomacyManager.Instance == null) return;
            foreach (var diploCon in DiplomacyManager.Instance.DiplomacyControllers)
                RefreshActiveProposalDisplay(diploCon);
        }

        private void RefreshAllActiveProposalDisplays(NegotiationPloysEnum _, CivEnum __, CivEnum ___, bool ____)
            => RefreshAllActiveProposalDisplays();

        /// <summary>
        /// Text for the "ACTIVE PROPOSAL" panel (see DiploUIprefab.prefab's ActiveProposalText,
        /// repurposing the formerly-empty EventHeader/ResponseHeader real estate - neither had any
        /// content wired to them). Only ever at most one entry, per
        /// DiplomacyManager.CreateDiplomacyProject's one-proposal-at-a-time cap.
        /// </summary>
        private static string FormatActiveProposal(DiplomacyData data)
        {
            DiplomacyProject project = data?.ActiveProjects?.Find(p => !p.IsComplete);
            if (project != null)
            {
                return $"PENDING: {project.ProposalType}\nTo: {project.TargetCiv}\n" +
                       $"Turn {project.TurnsTotal - project.TurnsRemaining} of {project.TurnsTotal}\n" +
                       $"Acceptance: {project.AcceptanceChance:P0}";
            }

            // No proposal in flight - report what happened to the last one (see
            // DiplomacyManager.ResolveDiplomacyProject) rather than silently going blank, until a
            // new proposal is sent (DiplomacyController.SendProposal clears this).
            if (!string.IsNullOrEmpty(data?.LastProposalOutcome))
                return data.LastProposalOutcome;

            return "No active proposal";
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

            DiplomacyManager.OnDiplomacyProjectResolved -= RefreshAllActiveProposalDisplays;
            if (_subscribedToTurnAdvance && TimeManager.Instance != null)
                TimeManager.Instance.OnTurnAdvanced -= RefreshAllActiveProposalDisplays;
            _subscribedToTurnAdvance = false;
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
