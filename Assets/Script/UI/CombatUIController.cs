//namespace BOTF3D.Core
//{
/// <summary>
/// UI: When a local player selects an order, call GiveOrder on their controller.
/// Networking: When a remote order is received, call GiveOrder on the remote controller.
/// AI: On AI’s turn, call GiveOrder with its chosen order.
/// </summary>
//[RequireComponent(typeof(Toggle))]
//public class CombatUIController : MonoBehaviour
//{
//    // public static CombatUIController Instance;
//    public CombatController CombatController; // this is the combat controller that will handle the combat UI and orders
//    public CivEnum sideOneEnum;
//    public CivEnum sideTwoEnum;
//    public CivEnum CivEnumLocalPlayer; // the local player enum, used to determine which side the local player is on
//    public GameObject PanelCombat_Menu;
//    public GameObject PanelShipCombat;
//    public GameObject PanelCombatOver;
//    public int negIsSideOnePosIsSideTwo = 1; // used to determine which side the local player is on, -1 for side one, 1 for side two
//    public List<ShipController> SideOneShipControllers; // mainly local player vs not local player
//    public List<ShipController> SideTwoShipControllers;
//    [SerializeField]
//    TextMeshProUGUI timerText;
//    [SerializeField]
//    float remainingTime = 10f; // used to keep track of the timer for the combat UI
//    bool isTimerRunning = false; // used to determine if the timer is running or not
//    [SerializeField]
//    private List<GameObject> listOfShipsUiGos;
//    public static Toggle Engage, Rush, Retreat, Formation, TargetTransports;
//    public List<Toggle> toggleOrderList = new List<Toggle>() { Engage, Rush, Retreat, Formation, TargetTransports };
//    private Toggle activeLocalPlayerToggle;
//    private Toggle previousToggle;
//    public static CombatOrders order = CombatOrders.Engage;
//    public LocalHumanPlayerController LocalPlayer; // reference to the local player controller, used to execute combat orders
//    public List<AiPlayerController> AiPlayerControllers; // list of AI player controllers, used to handle AI combat orders
//    public List<RemoteHumanPlayerController> RemoteHumanPlayerControllers; // list of remote human player controllers, used to handle remote player combat orders
//    private void Start()
//    {
//        previousToggle = toggleOrderList[0];
//        CivEnumLocalPlayer = GameController.Instance.GameData.LocalPlayerCivEnum; // get the local player civ enum from the game controller
//        LocalPlayer = LocalHumanPlayerController.localInstance;//FindFirstObjectByType<LocalHumanPlayerController>(); // Or assign via inspector?
//        PanelCombatOver.SetActive(false);
//    }
//    private void Awake()
//    {
//        //if (Instance != null && Instance != this)
//        //{
//        //    Debug.LogWarning("?? Duplicate CombatUIController found - destroying old instance");
//        //    Destroy(Instance.gameObject); // ? Destroy the OLD one, keep the new
//        //}

//        //Instance = this;
//        //// ? NO DontDestroyOnLoad - this controller belongs to CombatScene
//    }

//    private void OnDestroy()
//    {
//        //// ? Clear singleton reference when destroyed with scene
//        //if (Instance == this)
//        //{
//        //    Instance = null;
//        //    Debug.Log("? CombatUIController destroyed with CombatScene");
//        //}
//    }
//    private void Update()
//    {
//        if (isTimerRunning)
//        {
//            remainingTime -= Time.deltaTime;
//            if (remainingTime > 0f)
//            {
//                timerText.text = Mathf.FloorToInt(remainingTime).ToString("10");
//                // Here you can add logic to handle the end of the timer, start the combat phase
//            }
//            else
//            {
//                isTimerRunning = false;
//                remainingTime = 0f;
//                timerText.text = "00";
//                EnterShipCombatPhase();
//            }
//        }
//    }
//    public void ActivePlayerToggle(Toggle activeToggle)
//    {
//        switch (activeToggle.name.ToUpper())
//        {
//            case "TOGGLE_ENGAGE":
//                activeToggle.enabled = !activeToggle.isOn; // toggle the engage button
//                activeLocalPlayerToggle = Engage;
//                order = CombatOrders.Engage;
//                Debug.Log("Active Engage.");
//                break;
//            case "TOGGLE_RUSH":
//                Debug.Log("Active Rush.");
//                activeToggle.enabled = !activeToggle.isOn; // toggle the engage button
//                activeLocalPlayerToggle = Rush;
//                order = CombatOrders.Rush;
//                break;
//            case "TOGGLE_RETREAT":
//                activeToggle.enabled = !activeToggle.isOn; // toggle the engage button
//                activeLocalPlayerToggle = Retreat;
//                order = CombatOrders.Retreat;
//                Debug.Log("Active Retreat.");
//                // CombatController.SetThisUILocalPlayerCombatOrder(CombatOrders.Retreat, CivEnumLocalPlayer);
//                break;
//            case "TOGGLE_FORMATION":
//                Debug.Log("Active Formation.");
//                activeToggle.enabled = !activeToggle.isOn; // toggle the engage button
//                activeLocalPlayerToggle = Formation;
//                order = CombatOrders.Formation;
//                //CombatController.SetThisUILocalPlayerCombatOrder(CombatOrders.Formation, CivEnumLocalPlayer);
//                break;
//            case "TOGGLE_TARGET_TRANSPORTS":
//                activeToggle.enabled = !activeToggle.isOn; // toggle the engage button
//                activeLocalPlayerToggle = TargetTransports;
//                order = CombatOrders.TargetTransports;
//                Debug.Log("Active Target Transports.");
//                //CombatController.SetThisUILocalPlayerCombatOrder(CombatOrders.TargetTransports, CivEnumLocalPlayer);
//                break;
//            default:
//                break;
//        }
//    }
//    private void OnToggleENGAGE(bool isOn)
//    {
//        order = CombatOrders.Engage;
//        if (Engage.isOn)
//        {
//            if (previousToggle != Engage)
//            {
//                previousToggle.isOn = false;
//            }
//            previousToggle = Engage;
//            ActivePlayerToggle(Engage);
//        }
//    }
//    private void OnToggleRUSH(bool isOn)
//    {
//        order = CombatOrders.Rush;
//        if (Rush.isOn)
//        {
//            if (previousToggle != Rush)
//            {
//                previousToggle.isOn = false;
//            }
//            previousToggle = Rush;
//            ActivePlayerToggle(Rush);
//        }
//    }
//    private void OnToggleRETREAT(bool isOn)
//    {
//        order = CombatOrders.Retreat;
//        if (Retreat.isOn)
//        {
//            if (previousToggle != Retreat)
//            {
//                previousToggle.isOn = false;
//            }
//            previousToggle = Retreat;
//            ActivePlayerToggle(Retreat);
//        }
//    }
//    private void OnToggleFORMATION(bool isOn)
//    {
//        order = CombatOrders.Formation;
//        if (Formation.isOn)
//        {
//            if (previousToggle != Formation)
//            {
//                previousToggle.isOn = false;
//            }
//            previousToggle = Formation;
//            ActivePlayerToggle(Formation);
//        }
//    }
//    private void OnToggleTARGET_TRANSPORTS(bool isOn)
//    {
//        order = CombatOrders.TargetTransports;
//        if (TargetTransports.isOn)
//        {
//            if (previousToggle != TargetTransports)
//            {
//                previousToggle.isOn = false;
//            }
//            previousToggle = TargetTransports;
//            ActivePlayerToggle(TargetTransports);
//        }
//    }

//    public void OpenCombatUI(GameObject thisCombatUIGameObject)
//    {
//        RectTransform[] rectTransforms = thisCombatUIGameObject.GetComponentsInChildren<RectTransform>();
//        for (int i = 0; i < rectTransforms.Length; i++)
//        {
//            switch (rectTransforms[i].name)
//            {
//                case "PanelCombat_Menu":
//                    PanelCombat_Menu = rectTransforms[i].gameObject;
//                    PanelCombat_Menu.SetActive(true);
//                    break;
//                case "PanelShipCombat":
//                    PanelShipCombat = rectTransforms[i].gameObject;
//                    PanelShipCombat.SetActive(false);
//                    break;
//                case "Toggle_ENGAGE":
//                    rectTransforms[i].gameObject.SetActive(true);
//                    break;
//                case "Toggle_RUSH":
//                    rectTransforms[i].gameObject.SetActive(true);
//                    break;
//                case "Toggle_RETREAT":
//                    rectTransforms[i].gameObject.SetActive(true);
//                    break;
//                case "Toggle_FORMATION":
//                    rectTransforms[i].gameObject.SetActive(true);
//                    break;
//                case "Toggle_TARGET_TRANSPORTS":
//                    rectTransforms[i].gameObject.SetActive(true);
//                    break;
//                case "Load MainMenu":
//                    rectTransforms[i].gameObject.SetActive(true);
//                    break;
//                case "ButtonEnterCombat":
//                    rectTransforms[i].gameObject.SetActive(true);
//                    break;
//                default:
//                    break;
//            }
//        }
//        TextMeshProUGUI[] ourTMPs = thisCombatUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
//        for (int i = 0; i < ourTMPs.Length; i++)
//        {
//            if (ourTMPs[i].name == "Timer Text")
//            {
//                timerText = ourTMPs[i];
//            }
//            // ToDO: we can put some more data in the UI here,
//            //int techLevelInt = (int)CivManager.Instance.LocalPlayerCivContoller.CivData.TechLevel / 100; // Early Tech level = 100, Supreme = 900;
//            //ourTMPs[i].enabled = true;
//            //var name = ourTMPs[i].name;

//        }
//        Toggle[] ArrayToggles = thisCombatUIGameObject.GetComponentsInChildren<Toggle>();
//        foreach (var aToggle in ArrayToggles)
//        {
//            //CombatController = thisCombatUIGameObject.GetComponent<CombatController>();

//            switch (aToggle.name)
//            {
//                case "Toggle_ENGAGE":
//                    Engage = aToggle;
//                    Engage.onValueChanged.RemoveAllListeners();
//                    Engage.onValueChanged.AddListener(OnToggleENGAGE);
//                    break;
//                case "Toggle_RUSH":
//                    Rush = aToggle;
//                    Rush.onValueChanged.RemoveAllListeners();
//                    Rush.onValueChanged.AddListener(OnToggleRUSH);
//                    break;
//                case "Toggle_RETREAT":
//                    Retreat = aToggle;
//                    Retreat.onValueChanged.RemoveAllListeners();
//                    Retreat.onValueChanged.AddListener(OnToggleRETREAT);
//                    break;
//                case "Toggle_FORMATION":
//                    Formation = aToggle;
//                    Formation.onValueChanged.RemoveAllListeners();
//                    Formation.onValueChanged.AddListener(OnToggleFORMATION);
//                    break;
//                case "Toggle_TARGET_TRANSPORTS":
//                    TargetTransports = aToggle;
//                    TargetTransports.onValueChanged.RemoveAllListeners();
//                    TargetTransports.onValueChanged.AddListener(OnToggleTARGET_TRANSPORTS);
//                    break;
//                default:
//                    break;
//            }
//        }
//        Button[] ArrayButtons = thisCombatUIGameObject.GetComponentsInChildren<Button>();
//        foreach (var button in ArrayButtons)
//        {
//            switch (button.name)
//            {
//                case "ButtonEnterCombat":
//                    button.onClick.RemoveAllListeners();
//                    button.onClick.AddListener(EnterShipCombatPhase);
//                    break;
//                default:
//                    break;
//            }
//        }
//        isTimerRunning = true;
//    }

//    // In CombatUIController.cs - EnterShipCombatPhase()

//    public void EnterShipCombatPhase()
//    {
//        // ? Always use singleton instead of cached reference
//        if (CombatController != null)
//        {
//            CombatController.RunAnimation();
//        }
//        else
//        {
//            Debug.LogError("CombatController.Instance is null! Cannot start combat animation.");
//        }
//    }
//    private void SetUpCombat(CombatOrders order, CivEnum sideOneCiv, CivEnum sideTwoCiv)
//    {
//        //SetSideOneOrderOrSideTwo();
//        List<ShipController> shipControllers;
//        if (GameController.Instance.AreWeLocalPlayer(sideOneCiv))
//        {
//            shipControllers = SideOneShipControllers;
//            negIsSideOnePosIsSideTwo = -1;
//        }
//        else
//        {
//            shipControllers = SideTwoShipControllers;
//            negIsSideOnePosIsSideTwo = 1;
//        }
//        switch (order)
//        {
//            case CombatOrders.Engage:
//                CombatController.SetShipOrders(CombatOrders.Engage, CivEnumLocalPlayer);
//                Debug.Log("Engaging in combat.");
//                break;
//            case CombatOrders.Rush:
//                CombatController.SetShipOrders(CombatOrders.Rush, CivEnumLocalPlayer);
//                Debug.Log("Rushing towards the enemy.");
//                break;
//            case CombatOrders.Retreat:
//                CombatController.SetShipOrders(CombatOrders.Retreat, CivEnumLocalPlayer);
//                Debug.Log("Retreating from combat.");
//                break;
//            case CombatOrders.Formation:
//                CombatController.SetShipOrders(CombatOrders.Formation, CivEnumLocalPlayer);
//                Debug.Log("Forming a combat formation.");
//                break;
//            case CombatOrders.TargetTransports:
//                CombatController.SetShipOrders(CombatOrders.TargetTransports, CivEnumLocalPlayer);
//                Debug.Log("Targeting enemy transports.");
//                break;
//            default:
//                CombatController.SetShipOrders(CombatOrders.Engage, CivEnumLocalPlayer);
//                //CombatController.ActOnCombatOrders(shipControllers, negIsSideOnePosIsSideTwo);
//                Debug.Log("Unknown order.");
//                break;
//        }
//    }

//    internal void RunCombatOverPanel()
//    {
//        PanelCombatOver.SetActive(true);
//    }

//    internal void CloseCombatOverPanel()
//    {
//        PanelCombatOver.SetActive(false);
//    }
//}
//}


