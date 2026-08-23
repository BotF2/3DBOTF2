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
        [Header("Active Diplomacy Projects")]
        [SerializeField] private Transform activeProjectsContainer;
        [SerializeField] private GameObject diplomacyProjectEntryPrefab;
        private readonly List<DiplomacyProjectEntryUI> _activeProjectRows = new List<DiplomacyProjectEntryUI>();
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

        // Pending combats queued when Combat is clicked while another combat is already in
        // progress. Processed one at a time after each GameEvents.OnCombatEnded fires.
        private static readonly Queue<DiplomacyController> _pendingCombats = new Queue<DiplomacyController>();
        private static bool _combatInProgress = false;

        private void OnEnable()
        {
            DiplomacyManager.OnDiplomacyProjectResolved += RefreshAllActiveProposalDisplays;
            GameEvents.OnCombatEnded += OnCombatEndedProcessQueue;
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
            RefreshActiveProjectsList();

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

                // A card recycled back into the ribbon list is no longer a live encounter popup -
                // it's just a browsable relationship card (see PopulateDiplomacyList, which simply
                // re-activates whatever's sitting in diplomacyListContainter). Combat/Withdraw were
                // last set by SetUpDiplomacyUIElements for whatever genuine contact opened this
                // card, and nothing else ever turned them back off, so without this they stayed
                // active (and wired to real Combat()/SetResponse() calls) on every past, already-
                // resolved encounter forever after, just from browsing the list.
                DiplomacyController ownerCon = DiplomacyManager.Instance?.DiplomacyControllers
                    ?.FirstOrDefault(dc => dc != null && dc.DiplomacyUIGameObject == child);
                if (ownerCon != null)
                    RefreshEncounterButtonsState(ownerCon);

                // Reset to compact strip before returning to list storage — the ribbon Diplomacy
                // view only shows CompactHeader, so a card returned while ExpandContent was open
                // must collapse back to compact mode, otherwise it takes up full expanded height
                // in the ribbon list without the player having asked for it.
                SetDiploCardState(child, false);
                child.transform.SetParent(diplomacyListContainter.transform, false);
            }
        }

        /// <summary>
        /// Combat/Withdraw are only meaningful while diplomacyCon represents a genuine, unresolved
        /// encounter the local player still needs to answer - never while merely browsing a past
        /// (possibly long-resolved) contact's card from the ribbon list. Shows/hides both buttons
        /// to match. Safe to call at any time; no-ops gracefully if the card has no UI yet.
        /// </summary>
        public void RefreshEncounterButtonsState(DiplomacyController diplomacyCon)
        {
            if (diplomacyCon?.DiplomacyUIGameObject == null) return;

            bool activePending = IsEncounterGenuinelyPendingForLocalPlayer(diplomacyCon);
            RectTransform[] rectTransforms = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<RectTransform>(true);
            foreach (var rt in rectTransforms)
            {
                if (rt.name == "CombatButton" || rt.name == "WithdrawButton")
                    rt.gameObject.SetActive(activePending);
            }
        }

        private bool IsEncounterGenuinelyPendingForLocalPlayer(DiplomacyController diplomacyCon)
        {
            if (diplomacyCon?.DiplomacyData == null || diplomacyCon.DiplomacyData.EncounterResolved)
            {
                Debug.LogWarning($"[DiploPanelDiag] IsEncounterGenuinelyPendingForLocalPlayer: bailing early. " +
                    $"DiplomacyData null={diplomacyCon?.DiplomacyData == null} " +
                    $"EncounterResolved={diplomacyCon?.DiplomacyData?.EncounterResolved}");
                return false;
            }

            bool isSideOne = diplomacyCon.DiplomacyData.CivOne != null &&
                GameController.Instance.AreWeLocalPlayer(diplomacyCon.DiplomacyData.CivOne.CivData.CivEnum);
            DiplomacyData.EncounterResponse localResponse = isSideOne
                ? diplomacyCon.DiplomacyData.ResponseSideOne
                : diplomacyCon.DiplomacyData.ResponseSideTwo;

            // [DiploPanelDiag] Temporary - pins down why fleet-vs-star-system first contact was
            // reported showing Combat/Withdraw inactive when it should be a genuine pending
            // encounter. Prints every input this decision depends on so a mismatch (e.g. CivOne
            // pointing at the wrong side, or a response already resolved before this ran) is
            // visible from a single log line instead of requiring re-derivation from state.
            Debug.LogWarning($"[DiploPanelDiag] IsEncounterGenuinelyPendingForLocalPlayer: " +
                $"CivOne={diplomacyCon.DiplomacyData.CivOne?.CivData.CivShortName ?? "NULL"} " +
                $"CivTwo={diplomacyCon.DiplomacyData.CivTwo?.CivData.CivShortName ?? "NULL"} " +
                $"CivEnumSideOne={diplomacyCon.DiplomacyData.CivEnumSideOne} CivEnumSideTwo={diplomacyCon.DiplomacyData.CivEnumSideTwo} " +
                $"isSideOne={isSideOne} ResponseSideOne={diplomacyCon.DiplomacyData.ResponseSideOne} " +
                $"ResponseSideTwo={diplomacyCon.DiplomacyData.ResponseSideTwo} localResponse={localResponse} " +
                $"FleetControllerCivOne={(diplomacyCon.DiplomacyData.FleetControllerCivOne != null ? "set" : "null")} " +
                $"FleetContollerCivTwo={(diplomacyCon.DiplomacyData.FleetContollerCivTwo != null ? "set" : "null")} " +
                $"StarSysController={(diplomacyCon.DiplomacyData.StarSysController != null ? diplomacyCon.DiplomacyData.StarSysController.StarSysData.SysName : "null")} " +
                $"EncounterType={diplomacyCon.DiplomacyData.EncounterType}");

            return localResponse == DiplomacyData.EncounterResponse.Undecided;
        }
        /// <summary>
        /// Sets CompactHeader always active and ExpandContent per <paramref name="expand"/>.
        /// False = compact strip (ribbon list, first-contact notification).
        /// True  = fully open (player clicked a known fleet or system).
        /// </summary>
        public void SetDiploCardState(GameObject diploUIGO, bool expand)
        {
            if (diploUIGO == null) return;
            Transform t = diploUIGO.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform child = t.GetChild(i);
                if (child.name == "CompactHeader")
                    child.gameObject.SetActive(true);
                else if (child.name == "ExpandContent")
                    child.gameObject.SetActive(expand);
                else if (child.name == "Description")
                    child.gameObject.SetActive(false); // always collapse on any state change
            }
        }

        public void SetUpDiplomacyUIElements(GameObject diplomacyUIGO, GameObject diplomacyControllerGO, List<ShipController> shipList, bool expandedOnOpen = false)
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

            // Card layout setup. The prefab was designed with VLG + ContentSizeFitter (Preferred
            // Size, vertical) already on the card root. Children use sizeDelta=(0,0) and report
            // their heights via LayoutElement.preferredHeight so the card VLG sums them and the
            // card CSF sizes the card accordingly.
            //
            // The prefab VLG already has these settings; we re-apply them defensively in case a
            // future prefab edit accidentally changes them.
            var cardVLG = diplomacyUIGO.GetComponent<VerticalLayoutGroup>()
                          ?? diplomacyUIGO.AddComponent<VerticalLayoutGroup>();
            cardVLG.childForceExpandHeight = false;
            cardVLG.childForceExpandWidth  = true;
            cardVLG.childControlHeight     = true;
            cardVLG.childControlWidth      = true;
            cardVLG.spacing                = 0f;

            // The prefab ContentSizeFitter (Preferred Size, vertical) is what auto-sizes the card
            // to its children. Do NOT destroy it — it is required for the card to grow/shrink as
            // ExpandContent is toggled. (Earlier code erroneously called Destroy on it; that left
            // the card with no height driver and broke the close button positioning.)
            var cardCSF = diplomacyUIGO.GetComponent<ContentSizeFitter>()
                          ?? diplomacyUIGO.AddComponent<ContentSizeFitter>();
            cardCSF.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            cardCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // ExpandContent has no LayoutElement in the prefab — without one, childControlHeight=true
            // collapses it to 0 px (LayoutUtility.GetPreferredHeight returns 0 when no ILayoutElement
            // is present). Adding LayoutElement.preferredHeight=200 matches the designed height:
            // the deepest child inside ExpandContent sits at anchoredPos.y=-200 from its top edge.
            Transform expandContentT = diplomacyUIGO.transform.Find("ExpandContent");
            if (expandContentT != null)
            {
                var expandLE = expandContentT.GetComponent<LayoutElement>()
                               ?? expandContentT.gameObject.AddComponent<LayoutElement>();
                expandLE.preferredHeight = 200f;
            }

            // ButtonCloseDiplomacytUI is a direct child of the card root, so the card VLG would
            // normally stack it below CompactHeader and stretch it to 1920px wide. Marking it as
            // ignoreLayout=true excludes it from the VLG entirely; we then position it manually
            // as a 30x30 overlay at the top-right corner of the card (right end of CompactHeader).
            Transform closeButtonT = diplomacyUIGO.transform.Find("ButtonCloseDiplomacytUI");
            if (closeButtonT != null)
            {
                var closeLE = closeButtonT.GetComponent<LayoutElement>()
                              ?? closeButtonT.gameObject.AddComponent<LayoutElement>();
                closeLE.ignoreLayout = true;

                RectTransform closeRT = closeButtonT.GetComponent<RectTransform>();
                closeRT.anchorMin        = new Vector2(1f, 1f);
                closeRT.anchorMax        = new Vector2(1f, 1f);
                closeRT.pivot            = new Vector2(1f, 1f);
                closeRT.sizeDelta        = new Vector2(30f, 30f);
                closeRT.anchoredPosition = Vector2.zero;
            }

            // The list container positions cards vertically. childControlHeight=false lets each
            // card's own ContentSizeFitter drive its height; the container reads each card's
            // current sizeDelta.y (set by the card CSF) to place the next card below it.
            if (diplomacyListContainter != null)
            {
                var containerVLG = diplomacyListContainter.GetComponent<VerticalLayoutGroup>()
                                   ?? diplomacyListContainter.AddComponent<VerticalLayoutGroup>();
                containerVLG.childControlHeight     = false;
                containerVLG.childForceExpandHeight = false;
            }

            // Activate ExpandContent before populating so every GetComponentsInChildren call
            // finds images and buttons inside it regardless of what state the card was left in
            // (e.g. compact after sitting in the ribbon list). SetDiploCardState below closes it
            // again after all data is written.
            diplomacyUIGO.transform.Find("ExpandContent")?.gameObject.SetActive(true);

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

            // HomeSystemName and StarSysName (DiploUIprefab.prefab) sit at the same anchored
            // position and are mutually exclusive: DiplomacyData.StarSysController is only set to
            // something other than their home system when this card was (re)opened by clicking a
            // specific known foreign system (see DiplomacyManager.ResolveDiplomacyForClickSystemWeKnow) -
            // otherwise (first contact, or reopened via the ribbon) it defaults to showing their home system.
            bool showingSpecificOtherSystem = diplomacyCon.DiplomacyData.StarSysController != null &&
                diplomacyCon.DiplomacyData.StarSysController != homeSysController;

            Image[] listOfImages = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<Image>(true);
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
            // Combat/Withdraw are only relevant while this card represents a genuine, unresolved
            // encounter awaiting the local player's decision - not when this same DiplomacyController
            // is just being re-set-up cosmetically, and definitely not on the card's later trips
            // through the ribbon list (see RefreshEncounterButtonsState, which re-applies this same
            // check whenever a card is recycled back into diplomacyListContainter).
            bool encounterButtonsActive = IsEncounterGenuinelyPendingForLocalPlayer(diplomacyCon);

            // includeInactive: true - on a repeat encounter this reuses the same UI GameObject from
            // a prior, already-resolved contact, where RefreshEncounterButtonsState previously
            // SetActive(false)'d CombatButton/WithdrawButton. Without it, the active-only default
            // scan skips those two (now-inactive) buttons entirely, so the SetActive(true) below
            // never runs on them even when encounterButtonsActive is true - leaving Combat/Withdraw
            // permanently hidden for every encounter after the first with the same civ pair.
            RectTransform[] rectTransforms = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<RectTransform>(true);
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
                        rectTransforms[i].gameObject.SetActive(encounterButtonsActive);
                        combatButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "WithdrawButton":
                        rectTransforms[i].gameObject.SetActive(encounterButtonsActive);
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
            // includeInactive: true - HomeSystemName/StarSysName get SetActive(false) below to stay
            // mutually exclusive, so a later re-population (e.g. clicking back and forth between
            // systems) needs to find whichever one is currently hidden, not just the active one.
            TextMeshProUGUI[] ourTMPs = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<TextMeshProUGUI>(true);
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
                        SetRelationTextColdWarEmphasis(ourTMPs[i], diplomacyCon.DiplomacyData.DiplomacyStatusEnumOfCivs == DiplomacyStatusEnum.ColdWar);
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
                    case "HomeSystemName":
                        ourTMPs[i].text = homeSysController != null ? homeSysController.StarSysData.SysName : string.Empty;
                        ourTMPs[i].gameObject.SetActive(!showingSpecificOtherSystem);
                        break;
                    case "StarSysName":
                        // Only populated/shown when DiplomacyData.StarSysController points at a
                        // system other than their home system (see showingSpecificOtherSystem above);
                        // otherwise HomeSystemName covers the default "their home system" view.
                        ourTMPs[i].text = showingSpecificOtherSystem
                            ? diplomacyCon.DiplomacyData.StarSysController.StarSysData.SysName
                            : string.Empty;
                        ourTMPs[i].gameObject.SetActive(showingSpecificOtherSystem);
                        break;
                    default:
                        break;
                }
            }
            Button[] listButtons = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<Button>(true);
            foreach (var listButton in listButtons)
            {
                switch (listButton.name)
                {
                    case "OpenDescriptionButton":
                    {
                        listButton.onClick.RemoveAllListeners();
                        // Find once at wire-time; the closure holds the reference so the toggle
                        // works whether the card is in ADiplomacyMenuView or the ribbon list.
                        Transform descT = diplomacyUIGO.transform.Find("Description");
                        CivController descCiv = notLocalPlayerCiv;
                        listButton.onClick.AddListener(() =>
                        {
                            if (descT == null) return;
                            GameObject descGO = descT.gameObject;
                            bool show = !descGO.activeSelf;
                            descGO.SetActive(show);
                            if (!show) return;

                            // Populate text then resize Description, TMP, and Image to fit.
                            // ForceMeshUpdate computes preferredHeight at the current TMP width
                            // (1290 as set in the prefab) before we change any RectTransforms.
                            TextMeshProUGUI descTMP = descGO.GetComponentInChildren<TextMeshProUGUI>(true);
                            if (descTMP == null) return;
                            descTMP.text = descCiv.CivData.Decription;
                            descTMP.enableWordWrapping = true;
                            descTMP.ForceMeshUpdate();
                            float h = descTMP.preferredHeight;
                            descTMP.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
                            descT.GetComponent<RectTransform>()
                                 ?.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
                            Image descImg = descGO.GetComponentInChildren<Image>(true);
                            if (descImg != null)
                                descImg.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
                        });
                        break;
                    }
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
                    case "ButtonCloseDiplomacytUI":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() =>
                        {
                            GalaxyMenuUIController.Instance?.CloseCurrentMenu();
                            TurnEventQueue.Instance?.NotifyDismissed();
                        });
                        break;
                    case "CombatButton":
                    {
                        listButton.onClick.RemoveAllListeners();
                        var capturedDiplomacyCon = diplomacyCon;
                        listButton.onClick.AddListener(() =>
                        {
                            void LaunchOrQueueCombat()
                            {
                                if (_combatInProgress)
                                {
                                    _pendingCombats.Enqueue(capturedDiplomacyCon);
                                }
                                else
                                {
                                    _combatInProgress = true;
                                    capturedDiplomacyCon.Combat(capturedDiplomacyCon);
                                    TurnEventQueue.Instance?.NotifyDismissed();
                                }
                            }

                            if (TimeManager.Instance != null && TimeManager.Instance.TurnPhase == TurnPhase.TurnProgression)
                            {
                                // Defer until the turn finishes advancing
                                void OnPhaseChanged(TurnPhase phase)
                                {
                                    if (phase == TurnPhase.InterTurn)
                                    {
                                        TimeManager.Instance.OnTurnPhaseChanged -= OnPhaseChanged;
                                        LaunchOrQueueCombat();
                                    }
                                }
                                TimeManager.Instance.OnTurnPhaseChanged += OnPhaseChanged;
                            }
                            else
                            {
                                LaunchOrQueueCombat();
                            }
                        });
                        break;
                    }
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

                            // Hide immediately once the local side has committed, even if the panel
                            // stays open awaiting the other side (TryResolveEncounter only closes it
                            // once both sides have answered) - otherwise Combat/Withdraw kept showing
                            // as if still actionable after they'd already been used.
                            RefreshEncounterButtonsState(diplomacyCon);
                            // Player has committed to withdraw — unblock the queue.
                            TurnEventQueue.Instance?.NotifyDismissed();
                        });
                        break;
                    case "DeclareWarButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.DeclareWar(diplomacyCon));
                        break;
                    case "ExpandButton":
                    {
                        listButton.onClick.RemoveAllListeners();
                        GameObject expandContent = diplomacyUIGO.transform.Find("ExpandContent")?.gameObject;
                        RectTransform cardRT = diplomacyUIGO.GetComponent<RectTransform>();
                        RectTransform containerRT = diplomacyListContainter != null
                            ? diplomacyListContainter.GetComponent<RectTransform>() : null;
                        if (expandContent != null)
                            listButton.onClick.AddListener(() =>
                            {
                                expandContent.SetActive(!expandContent.activeSelf);
                                if (cardRT != null)
                                    LayoutRebuilder.ForceRebuildLayoutImmediate(cardRT);
                                if (containerRT != null)
                                    LayoutRebuilder.ForceRebuildLayoutImmediate(containerRT);
                            });
                        break;
                    }
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

            // All data written — now apply compact/expanded state. ExpandContent closes if
            // expandedOnOpen is false (first-contact and repeat-encounter notifications);
            // it stays open when the player clicked a known fleet or system (expandedOnOpen=true).
            // Description always starts hidden; the player opens it via OpenDescriptionButton.
            SetDiploCardState(diplomacyUIGO, expandedOnOpen);
            diplomacyUIGO.transform.Find("Description")?.gameObject.SetActive(false);

            RefreshActiveProposalDisplay(diplomacyCon);
        }

        private static readonly Color ColdWarFlashColor = new Color(1f, 0.5f, 0f); // orange

        /// <summary>
        /// Reinforces ColdWar relations (one step short of War - see DiplomacyStatusEnum) by
        /// flashing RelationText (DiploUIprefab.prefab, under "Text Status Header (TMP)") between
        /// its normal color and orange. Any other status restores the text's original prefab
        /// color and stops the flasher, so it can't keep running (or leave the text stuck orange)
        /// after relations move away from ColdWar.
        /// </summary>
        private void SetRelationTextColdWarEmphasis(TextMeshProUGUI relationText, bool isColdWar)
        {
            TextColorFlasher flasher = relationText.GetComponent<TextColorFlasher>();
            if (isColdWar)
            {
                if (flasher == null)
                    flasher = relationText.gameObject.AddComponent<TextColorFlasher>();
                flasher.StartFlashing(relationText, ColdWarFlashColor);
            }
            else if (flasher != null)
            {
                flasher.StopFlashing();
            }
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
            RefreshActiveProjectsList();
        }

        private void RefreshAllActiveProposalDisplays(NegotiationPloysEnum _, CivEnum __, CivEnum ___, bool ____)
            => RefreshAllActiveProposalDisplays();

        /// <summary>
        /// Aggregates every pending, non-complete DiplomacyProject involving the local player -
        /// both proposals they sent and incoming AI proposals targeting them - across every civ
        /// pair, into the "Active Diplomacy Projects" panel. Pooled list pattern matching
        /// IntelligenceUIController.RefreshCivTable / ReportEntryUI's row pooling.
        /// </summary>
        public void RefreshActiveProjectsList()
        {
            if (activeProjectsContainer == null || diplomacyProjectEntryPrefab == null) return;
            if (DiplomacyManager.Instance == null || GameController.Instance == null) return;

            CivEnum localCiv = GameController.Instance.GameData.LocalPlayerCivEnum;
            var projects = new List<DiplomacyProject>();
            foreach (var diploCon in DiplomacyManager.Instance.DiplomacyControllers)
            {
                var active = diploCon?.DiplomacyData?.ActiveProjects;
                if (active == null) continue;
                foreach (var p in active)
                    if (!p.IsComplete && (p.ProposerCiv == localCiv || p.TargetCiv == localCiv))
                        projects.Add(p);
            }

            while (_activeProjectRows.Count < projects.Count)
                _activeProjectRows.Add(Instantiate(diplomacyProjectEntryPrefab, activeProjectsContainer)
                    .GetComponent<DiplomacyProjectEntryUI>());

            for (int i = 0; i < projects.Count; i++)
            {
                _activeProjectRows[i].gameObject.SetActive(true);
                _activeProjectRows[i].Populate(projects[i], localCiv);
            }
            for (int i = projects.Count; i < _activeProjectRows.Count; i++)
                _activeProjectRows[i].gameObject.SetActive(false);
        }

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
            GameEvents.OnCombatEnded -= OnCombatEndedProcessQueue;
            if (_subscribedToTurnAdvance && TimeManager.Instance != null)
                TimeManager.Instance.OnTurnAdvanced -= RefreshAllActiveProposalDisplays;
            _subscribedToTurnAdvance = false;
        }

        private void OnCombatEndedProcessQueue(CivEnum winner, CivEnum loser)
        {
            _combatInProgress = false;
            ProcessNextPendingCombat();
        }

        private void ProcessNextPendingCombat()
        {
            while (_pendingCombats.Count > 0)
            {
                DiplomacyController next = _pendingCombats.Dequeue();
                if (next == null) continue;

                FleetController fleetOne = next.DiplomacyData.FleetControllerCivOne;
                FleetController fleetTwo = next.DiplomacyData.FleetContollerCivTwo;
                bool oneHasShips = fleetOne != null && fleetOne.FleetData != null && fleetOne.FleetData.ShipsList.Count > 0;
                bool twoHasShips = fleetTwo != null && fleetTwo.FleetData != null && fleetTwo.FleetData.ShipsList.Count > 0;

                if (!oneHasShips && !twoHasShips)
                {
                    // Both sides destroyed — cancel this combat and release any frozen fleets
                    if (fleetOne != null) fleetOne.ServerDecrementPendingEncounters();
                    if (fleetTwo != null) fleetTwo.ServerDecrementPendingEncounters();
                    continue;
                }

                _combatInProgress = true;
                next.Combat(next);
                return;
            }
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
