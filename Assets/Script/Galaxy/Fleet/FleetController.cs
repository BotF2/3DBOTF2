using BOTF3D.Civilization;
using BOTF3D.Combat;
using BOTF3D.Core;
using BOTF3D.UI;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;



namespace BOTF3D.Galaxy
{
    [RequireComponent(typeof(Rigidbody))]
    /// <summary>
    /// Controlling fleet movement and interactions while the matching FeetData class
    /// holds key info on status and for save game
    /// </summary>
    public class FleetController : NetworkBehaviour
    {
        public void Initialize() { }
        public void UpdateState() { }
        //Fields
        private FleetData fleetData;
        public FleetData FleetData { get { return fleetData; } set { fleetData = value; } }

        // Replicated alongside SyncedCivEnum below so OnCivEnumChanged can rebuild the same "Fleet N"
        // name FleetManager.InstantiateFleet assigns server-side (see GetNewFleetInt). Declared before
        // SyncedCivEnum so Mirror's weaver-generated deserializer assigns this field first within the
        // same sync payload - OnCivEnumChanged's hook can then rely on it already being current.
        [SyncVar]
        public int SyncedFleetInt;

        // FleetSO.MaxWarpFactor defaults to 0 and is only ever corrected by UpdateMaxWarp() computing
        // the slowest ship's speed - a server-only call (FleetManager.InstantiateFleet, ship
        // deploy/merge, etc.). A non-host client's reconstructed FleetData (see OnCivEnumChanged) has
        // no ships to run that computation on, so it was permanently stuck at 0, which made the warp
        // slider's maxValue 0 and every warp-up click get rejected. Declared before SyncedCivEnum for
        // the same reason as SyncedFleetInt above - OnCivEnumChanged reads this field directly rather
        // than waiting for its own hook, so it must already be current by then.
        [SyncVar(hook = nameof(OnMaxWarpFactorChanged))]
        public float SyncedMaxWarpFactor;

        // True for fleets created empty via the ship-deploy UI (split off an existing fleet, or
        // formed from a system's docked ships - see FleetManager.ServerCreateSplitFleet /
        // ServerCreateFleetFromSystem), false for a civ's very first starting fleet. OnCivEnumChanged
        // needs this to know whether its client-reconstructed FleetData should start with 0 ships or
        // rebuild that civ's starting roster via ShipManager.BuildShipsOfFirstFleet (see below) -
        // without it, every remote client's "new empty fleet" would incorrectly get repopulated with
        // that civ's starting ships. Declared before SyncedCivEnum for the same field-ordering reason
        // as SyncedFleetInt/SyncedMaxWarpFactor above.
        [SyncVar]
        public bool SyncedIsNewFleet;

        // Fleet creation/position is server-authoritative (see FleetManager.InstantiateFleet), so
        // remote clients never run InstantiateFleet themselves and their FleetData stays null. This
        // SyncVar is the minimal piece of FleetData replicated to every client so each one can locally
        // decide fog-of-war visibility/insignia and register the fleet in its own FleetManager lists -
        // NOT a substitute for full FleetData sync (ships, warp factor, destination remain server-only
        // for now).
        [SyncVar(hook = nameof(OnCivEnumChanged))]
        public CivEnum SyncedCivEnum;

        // Counter (not bool) because a fleet can be a party to more than one concurrent encounter
        // group (see GalaxyEncounterQueue's simultaneous-convergence batching) - a bool would risk one
        // resolution path clearing a flag another still-pending encounter needs. Server-detected state,
        // never set from client code directly - mutate only via the Server helpers below so non-host
        // clients still see it replicate (see standing project note on FleetData mutations needing an
        // explicit relay or silently no-op-ing on non-host clients).
        [SyncVar]
        private int syncedPendingEncounterCount = 0;
        public bool IsAwaitingEncounterResolution => syncedPendingEncounterCount > 0;

        [Server]
        public void ServerIncrementPendingEncounters()
        {
            syncedPendingEncounterCount++;
            Debug.Log($"[EncounterGate] {name}: pending++ -> {syncedPendingEncounterCount}");
        }

        [Server]
        public void ServerDecrementPendingEncounters()
        {
            syncedPendingEncounterCount = Mathf.Max(0, syncedPendingEncounterCount - 1);
            Debug.Log($"[EncounterGate] {name}: pending-- -> {syncedPendingEncounterCount}");
        }

        // Fires whenever the server recomputes MaxWarpFactor (ship added/removed/merged) after the
        // initial spawn sync. Keeps a non-host client's already-built FleetData/slider in sync with
        // fleet composition changes it has no other way of detecting.
        private void OnMaxWarpFactorChanged(float oldValue, float newValue)
        {
            Debug.LogWarning($"🛰️[MAXWARP] OnMaxWarpFactorChanged: fleet '{name}' oldValue={oldValue}, newValue={newValue}, NetworkServer.active={NetworkServer.active}, FleetData={(FleetData == null ? "NULL" : "OK")}.");
            if (NetworkServer.active) return;
            if (FleetData == null) return;

            FleetData.MaxWarpFactor = newValue;
            if (GalaxyUI != null)
                FleetUI.UpdateFleetMaxWarpUI(this, newValue);
        }

        // Fires on every client whenever SyncedCivEnum changes, including the initial sync on spawn
        // (same Mirror hook behavior relied on by LocalHumanPlayerController.OnPlayerCivChanged).
        // NetworkServer.active (not isServer) is the right guard here: the host machine already ran
        // the full setup directly inside FleetManager.InstantiateFleet, and NetworkServer.active is
        // true there regardless of whether this particular NetworkIdentity has finished spawning yet.
        // Mirror's weaver-generated SyncVar deserializer only invokes a hook when the incoming
        // value differs from whatever the backing field already holds (see
        // NetworkBehaviour.GeneratedSyncVarDeserialize) - including on the very first sync. Since
        // CivEnum.FED is ordinal 0 (GameEnums.cs), that's also C#'s zero-init default for an
        // unassigned CivEnum field, so this hook NEVER fires on a remote client for any fleet
        // whose civ is FED: the synced value (FED) equals the field's pre-deserialize default
        // (FED), so Mirror sees "no change" and skips the callback entirely. OnStartClient()
        // below is the fallback for that case - this flag makes sure whichever of the two runs
        // first (hook here, or the fallback there) is the only one that actually does the setup.
        private bool _civSetupInitiated;

        private void OnCivEnumChanged(CivEnum oldCiv, CivEnum newCiv)
        {
            if (NetworkServer.active) return;
            if (_civSetupInitiated) return;
            _civSetupInitiated = true;

            // Three things HandleCivEnumChanged depends on can all be unresolved during a freshly-
            // connected client's initial spawn-message burst for pre-existing fleets (e.g. the host's):
            //
            // 1. GameController.AreWeLocalPlayer(newCiv) (used by RegisterFleetControllerAndSetupVisuals)
            //    falls back to GameData.LocalPlayerCivEnum (defaulted to CivEnum.FED - see GameData.cs,
            //    "temp set to fed for now") whenever PlayerManager.LocalPlayerController hasn't been set
            //    yet (only assigned from this client's own player object's OnStartLocalPlayer). If the
            //    remote fleet's civ happens to equal that stale FED default (e.g. the host is actually
            //    playing FED), it gets misclassified as "ours": RegisterFleetControllerAndSetupVisuals
            //    takes the local-player branch, which never called SetActive on the insignia GameObjects
            //    (fixed defensively in FleetManager, but relying on that is still guessing).
            // 2. The 0.4-scale bake-in below reads FleetManager.Instance.GalaxyCenter.transform.lossyScale
            //    (10,10,10) to compensate for this client copy being unparented. If GalaxyCenter hasn't
            //    been resolved yet, it silently falls back to Vector3.one, baking in 0.4 instead of
            //    0.4*10=4 - a fleet that renders 10x too small, with no later retry since this SyncVar
            //    hook only fires once per value change.
            // 3. FleetData's constructor (called below when isFreshReconstruction) reads
            //    CivManager.Instance.GetCivDataByCivEnum(civ).CivLongName. CivManager.CivDataInGameList
            //    is populated by this client's own CivDataFromSO run, which - like PlayerManager's
            //    LocalPlayerController above - hasn't necessarily finished by the time pre-existing
            //    fleets' spawn messages arrive. GetCivDataByCivEnum returns null rather than throwing,
            //    so this surfaced as a NullReferenceException inside FleetData's constructor (crashing
            //    Mirror's OnDeserialize for this object, and failing the spawn entirely) rather than a
            //    silently-wrong value like the other two - so it can't be defended against downstream,
            //    it must be caught here before HandleCivEnumChanged ever runs.
            //
            // All three are the same class of bug as the InsigniaUnknownGO one above - defer until we
            // can answer all of them reliably instead of guessing.
            if (!IsReadyToHandleCivChange(newCiv))
            {
                StartCoroutine(WaitThenHandleCivChanged(newCiv));
                return;
            }

            HandleCivEnumChanged(newCiv);
        }

        // Fallback for the FED-hook-skip case described on _civSetupInitiated above. Mirror
        // guarantees SyncVar deserialization (and any hooks it does invoke) completes before
        // OnStartClient runs (NetworkClient.OnObjectSpawnFinished applies the spawn payload, then
        // BootstrapIdentity -> OnStartClient - see NetworkClient.cs), so SyncedCivEnum is always
        // current here, and this is guaranteed to run strictly after OnCivEnumChanged would have
        // fired had Mirror invoked it. _civSetupInitiated is the shared guard, so this only does
        // anything when the hook above never ran at all.
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (NetworkServer.active) return;
            if (_civSetupInitiated) return;
            _civSetupInitiated = true;

            if (!IsReadyToHandleCivChange(SyncedCivEnum))
            {
                StartCoroutine(WaitThenHandleCivChanged(SyncedCivEnum));
                return;
            }

            HandleCivEnumChanged(SyncedCivEnum);
        }

        private bool IsReadyToHandleCivChange(CivEnum civ)
        {
            if (PlayerManager.Instance == null || PlayerManager.Instance.LocalPlayerController == null)
                return false;

            // LocalPlayerController existing isn't enough on its own: its PlayerCiv SyncVar defaults
            // to CivEnum.FED (see LocalHumanPlayerController.playerCiv) and only gets its real,
            // player-confirmed value once CmdSetPlayerCiv actually runs - but LocalPlayerController
            // itself is assigned much earlier, as soon as OnStartLocalPlayer fires on connect. In
            // that window, GameController.AreWeLocalPlayer(FED) wrongly returns true for every
            // connecting player (their real civ hasn't synced yet), regardless of which civ they'll
            // actually end up as. If Federation's own fleet happens to reconstruct on a non-Federation
            // client during that exact window, RegisterFleetControllerAndSetupVisuals permanently
            // misclassifies it as "our own fleet" - unconditionally revealing its real insignia with
            // zero contact ever having happened (observed: Player 2/3 seeing Federation's real sprite
            // through fog of war before any first contact, intermittently, on a fresh game).
            if (!PlayerManager.Instance.LocalPlayerController.CivConfirmed)
                return false;

            if (FleetManager.Instance != null && FleetManager.Instance.GalaxyCenter == null)
                FleetManager.Instance.FindGalaxyReferences();

            if (FleetManager.Instance == null || FleetManager.Instance.GalaxyCenter == null)
                return false;

            // See point 3 in OnCivEnumChanged's comment above - HandleCivEnumChanged's FleetData(fleetSO)
            // construction needs this civ's CivData to already exist in this client's own CivManager.
            if (CivManager.Instance == null || CivManager.Instance.GetCivDataByCivEnum(civ) == null)
                return false;

            // CivData existing isn't enough - FleetData(FleetSO)'s constructor also resolves
            // CivController from CivManager.CivControllersInGame (a separate list, populated on its
            // own schedule) and silently leaves it null via FirstOrDefault() if that civ's controller
            // hasn't been added yet. A null CivController here doesn't surface until much later, when
            // DiplomacyManager.FleetControllerVsOtherCivFleet dereferences
            // otherFleet.FleetData.CivController.CivData - which throws inside a ClientRpc handler and
            // gets that client disconnected by Mirror (see conversation: "Disconnecting connection...
            // caused an Exception"). Gate reconstruction on this too so it retries via
            // WaitThenHandleCivChanged instead of ever producing a fleet with no CivController.
            if (CivManager.Instance.GetCivControllerByCivEnum(civ) == null)
                return false;

            return true;
        }

        private IEnumerator WaitThenHandleCivChanged(CivEnum newCiv)
        {
            const float timeout = 5f;
            float elapsed = 0f;
            while (!IsReadyToHandleCivChange(newCiv) && elapsed < timeout)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            if (!IsReadyToHandleCivChange(newCiv))
                Debug.LogWarning($"FleetController.OnCivEnumChanged: local player/GalaxyCenter/CivManager still unresolved after {timeout}s for fleet '{name}' - proceeding anyway, civ classification/scale/FleetData may be wrong or throw.");

            HandleCivEnumChanged(newCiv);
        }

        private void HandleCivEnumChanged(CivEnum newCiv)
        {
            // NetworkTransform's coordinateSpace is set to World (needed so the fleet's synced
            // position isn't misread as this unparented client copy's own local position - see
            // FleetPrefab.prefab), but Mirror's NetworkTransformBase forcibly disables scale sync
            // entirely whenever coordinateSpace is World (never implemented world/lossy-scale sync -
            // see NetworkTransformBase.SetScale). FleetManager.InstantiateFleet's runtime scale
            // (0.4, 0.4, 1) therefore never reaches any remote client. It's the same fixed constant
            // for every fleet regardless of civ (one shared fleetPrefab - see InstantiateFleet), so
            // just apply it locally here instead of relying on a sync that Mirror can't do.
            //
            // FleetManager.InstantiateFleet also parents the fleet under GalaxyCenter (scale 10,10,10
            // in GalaxyScene.unity) BEFORE applying that 0.4 local scale, so the server/host's actual
            // effective world scale is 0.4 * 10 = 4, not 0.4. This client copy is unparented (see
            // above), so its local scale IS its world scale - it must bake in that same GalaxyCenter
            // multiplier itself or it renders 10x too small, which is exactly the "hierarchy says 0.7
            // but looks way smaller" symptom this was meant to fix in the first place.
            // On a freshly-connected client, this hook can fire for pre-existing fleets (e.g. the
            // host's) as part of the initial spawn-message burst, before this client's own
            // FleetManager has resolved GalaxyCenter (that normally happens later during this
            // client's own galaxy scene setup). Falling back to Vector3.one in that case silently
            // bakes in the wrong scale forever (0.4 instead of 0.4*10=4) - retry the lookup first,
            // same pattern as FleetManager.SetUpDropLine's galaxyImage retry.
            if (FleetManager.Instance != null && FleetManager.Instance.GalaxyCenter == null)
                FleetManager.Instance.FindGalaxyReferences();

            // Mirror places a newly-spawned client copy into whatever scene happens to be active on
            // THIS client when its spawn message is processed - it isn't necessarily GalaxyScene. If
            // that message arrives while SceneController.LoadCombatSceneAdditive has already called
            // SceneManager.SetActiveScene(combatScene) (e.g. this fleet just came into network
            // relevance/fog-of-war contact for the first time, mid-load), the fleet becomes a root
            // object of CombatScene instead - invisible to SceneController's GalaxyScene
            // deactivate/reactivate sweep, so it stays permanently visible over every future combat
            // instead of being hidden with the rest of the galaxy. Pin every client-reconstructed
            // fleet to GalaxyScene explicitly so its scene membership never depends on load timing.
            Scene galaxyScene = SceneManager.GetSceneByName("GalaxyScene");
            if (galaxyScene.IsValid() && gameObject.scene != galaxyScene)
                SceneManager.MoveGameObjectToScene(gameObject, galaxyScene);

            // Re-homing into GalaxyScene above only fixes future combats' deactivate/reactivate
            // sweeps (SceneController.LoadCombatSceneAdditive/ReturnToGalaxyFromCombat) - it doesn't
            // retroactively hide this fleet if its setup is running mid-combat, after this combat's
            // own Step 2 sweep already happened (e.g. a fleet just coming into fog-of-war/network
            // relevance for the first time while Combat scene is loaded). Galaxy fleet icons should
            // never be visible while a combat is up, so match that state immediately here too.
            Scene combatScene = SceneManager.GetSceneByName("CombatScene");
            if (combatScene.IsValid() && combatScene.isLoaded)
                gameObject.SetActive(false);

            Vector3 galaxyScale = FleetManager.Instance != null && FleetManager.Instance.GalaxyCenter != null
                ? FleetManager.Instance.GalaxyCenter.transform.lossyScale
                : Vector3.one;
            // All three axes must stay equal (a fleet sprite billboard, not an oblong shape) -
            // derive all of them from galaxyScale.x alone rather than scaling each axis
            // independently. Billboard.cs fully matches the camera's rotation (not a Y-axis-only
            // cylindrical billboard), so a non-uniform local scale here still shows up as visible
            // stretching once rotated to face the shallow-angle top-down galaxy camera - leaving Z
            // at the raw galaxyScale.z previously stretched the sprite ~2.5x taller than wide and
            // (since the SphereCollider is a sibling component on this same transform, and bakes
            // the largest lossyScale axis into its effective radius) made the collider ~2.5x too
            // big as well.
            transform.localScale = new Vector3(0.4f * galaxyScale.x, 0.4f * galaxyScale.x, 0.4f * galaxyScale.x);

            bool isFreshReconstruction = FleetData == null;
            if (isFreshReconstruction)
            {
                FleetSO fleetSO = FleetManager.Instance.GetFleetSO_byInt((int)newCiv);
                if (fleetSO == null)
                {
                    Debug.LogWarning($"FleetController.OnCivEnumChanged: no FleetSO found for civ {newCiv}; cannot set up remote fleet visuals for '{name}' - RegisterFleetControllerAndSetupVisuals (insignia/fog/DropLine) will NOT run for this fleet.");
                    return;
                }
                FleetData = new FleetData(fleetSO);
                if (SyncedIsNewFleet)
                    FleetData.ShipsList = new List<ShipController>(); // start empty - see SyncedIsNewFleet comment
                FleetData.CivEnum = newCiv;
                // FleetData(FleetSO) doesn't set FleetName - only InstantiateFleet does that,
                // server-side, and this client-reconstructed FleetData never goes through it. Without
                // this, FleetNameText/the map label read FleetData.FleetName as null/empty.
                FleetData.FleetInt = SyncedFleetInt;
                FleetData.FleetName = "Fleet " + SyncedFleetInt.ToString();
                // SyncedMaxWarpFactor is already deserialized at this point (declared before
                // SyncedCivEnum - see its own comment), and reflects the server's real
                // UpdateMaxWarp() result rather than the FleetSO's unconfigured 0 default.
                Debug.LogWarning($"🛰️[MAXWARP] OnCivEnumChanged: fleet '{name}' raw SyncedMaxWarpFactor={SyncedMaxWarpFactor} at FleetData construction time (fleetSO default MaxWarpFactor={fleetSO.MaxWarpFactor}).");
                if (SyncedMaxWarpFactor > 0f)
                    FleetData.MaxWarpFactor = SyncedMaxWarpFactor;
                if (FleetManager.Instance.GalaxyCenter != null)
                    FleetData.Destination = FleetManager.Instance.GalaxyCenter;
            }

            // isFreshReconstruction=false here would mean this SyncVar hook fired AGAIN for a fleet
            // that already has FleetData - i.e. a stray re-fire, not the expected once-per-client
            // initial spawn sync. TEMP: also logs position while diagnosing the bystander-fleet-
            // displaced-after-combat bug, to see whether a re-fire (and its Vector3.one-fallback
            // scale/position math a few lines up) coincides with the displacement.
            //
            // netId + SyncedFleetInt added (2026-08-07c): this log previously printed identically for
            // a genuine duplicate of an already-existing fleet vs. a legitimately new fleet (e.g. one
            // created mid-session via ship split/deploy) reaching this client for the first time -
            // both look like "isFreshReconstruction=True" with no way to tell them apart. FleetInt=1
            // means "this claims to be the same fleet number as the original starting fleet" (a real
            // duplicate); FleetInt=2/3/etc. would mean a genuinely different, separate fleet.
            var diagIdentity = GetComponent<NetworkIdentity>();
            Debug.Log($"OnCivEnumChanged: civ={newCiv} synced for fleet '{name}' netId={(diagIdentity != null ? diagIdentity.netId.ToString() : "NULL")} SyncedFleetInt={SyncedFleetInt} (GalaxyCenter={(FleetManager.Instance.GalaxyCenter != null ? "OK" : "NULL")}, isFreshReconstruction={isFreshReconstruction}, pos={transform.position}) - calling RegisterFleetControllerAndSetupVisuals.");
            FleetManager.Instance.RegisterFleetControllerAndSetupVisuals(this, FleetData);

            // ShipController has no NetworkIdentity/SyncList - the server's own
            // ShipManager.BuildShipsOfFirstFleet call inside InstantiateFleet (for a civ's original
            // starting fleet, isNewFleet=false) never reaches this client. GetStartingFleetShips is a
            // pure function of civ + tech level, so rebuilding it locally here reconstructs the same
            // Scout/Destroyer/Transport (majors) or single ship (minors) the server already built,
            // without needing to network ShipController itself. Player-created fleets
            // (SyncedIsNewFleet) must stay empty here - they get their ships from the deploy UI drag
            // session instead.
            if (isFreshReconstruction && !SyncedIsNewFleet)
                ShipManager.Instance?.BuildShipsOfFirstFleet(this);

            // Replay a roster that arrived via RpcSyncShipRoster before FleetData existed (see
            // _pendingRosterShipIDs) - it always reflects the fleet's true current roster, so it's
            // safe to apply even over the just-rebuilt starting roster above.
            if (_pendingRosterShipIDs != null)
            {
                List<int> pending = _pendingRosterShipIDs;
                _pendingRosterShipIDs = null;
                Debug.Log($"OnCivEnumChanged: '{name}' applying deferred roster of {pending.Count} ship(s) now that FleetData exists.");
                ApplyShipRoster(pending);
            }
        }

        // Non-host clients have no Mirror authority over any fleet (spawned via plain
        // NetworkServer.Spawn with no owning connection - see FleetManager.InstantiateFleet), so every
        // order-relay Command below uses requiresAuthority = false and checks this instead: the calling
        // connection's own player civ (via its player-owned LocalHumanPlayerController) must match the
        // civ of the fleet being commanded, so Player 2 can order their own fleet but not the Fed one.
        private bool IsSenderAuthorizedForThisFleet(NetworkConnectionToClient sender)
        {
            if (sender?.identity == null) return false;
            LocalHumanPlayerController playerCon = sender.identity.GetComponent<LocalHumanPlayerController>();
            return playerCon != null && FleetData != null && playerCon.PlayerCiv == FleetData.CivEnum;
        }
        [SerializeField]
        private GameObject _fleetUIGameObject;

        public GameObject FleetUIGameObject
        {
            get => _fleetUIGameObject;
            set
            {
                if (value != null && (value == this.gameObject || value.transform.IsChildOf(this.transform)))
                {
                    Debug.LogError($"❌ FleetUIGameObject on '{name}' cannot be set to its own 3D GameObject or a child of it! This would deactivate the fleet from the galaxy map. Assignment blocked.");
                    return;
                }
                _fleetUIGameObject = value;
            }
        }
        public GameObject GalaxyCanvasGo;
        public string Name;
        public int intName = 1;
        // Units-per-second scaler. Tune this in the Inspector on the fleet prefab, against
        // a Small game (GalaxySizeExtensions.SpeedScale() == 1x there) - Medium/Large/Extreme
        // are then automatically slowed proportionally, see GalaxySizeSpeedScale below.
        // 4f ≈ 60% slower than the original 10f baseline.
        [SerializeField] private float warpFudgeFactor = 4f;
        private Rigidbody rb;
        private float updateInterval = 0.1f; // ~10 updates/sec (adjust for smoothness vs performance)
        private float lastUpdateTime;
        private float lastDropLineUpdateTime;
        public MapLineMovable DropLine;
        public MapLineMovable DestinationLine;
        public GameObject BackgroundGalaxyImage;

        // Which fleet we're currently pursuing (set via the Intercept button)
        public static FleetController PendingInterceptFleet; // fleet waiting for player to pick a target
        private Vector3 interceptPoint;
        private float interceptUpdateTimer;
        private const float INTERCEPT_UPDATE_INTERVAL = 0.5f;
        private float galaxyWidth = 1f;
        private float galaxyHeight = 1f;
        private float minimapWidth = 200f;
        private bool gotMapSizeFromGameManager = false;
        [SerializeField] private GameObject backgroundGalaxyImage;
        private Camera galaxyEventCamera;
        private readonly GameObject aNull = null; // used to pass a null object to the UI when needed in Diplomacy
        public Canvas FleetUICanvas { get; private set; }
        //public Canvas CanvasToolTip; // not used for now, see start method and in instantiation of fleetController in FleetManager.cs
        public PlayerDefinedTargetController TargetController;
        private Vector3 vectorOffset;
        private readonly float ourZCoordinate;
        [SerializeField]
        private GameObject warpUpButtonGO;
        [SerializeField]
        private GameObject warpDownButtonGO;
        [SerializeField]
        private float warpChange = 0.1f;
        [SerializeField]
        private Slider warpSlider;
        [SerializeField]
        private TextMeshProUGUI warpSliderText;
        private readonly TMP_Dropdown shipDropdown;

        public GameObject ShipDropDownGO;
        [SerializeField]
        private TMP_Text dropdownShipText;
        [SerializeField]
        private TMP_Text FleetName;
        [SerializeField]
        private TextMeshProUGUI destinationName;
        [SerializeField]
        private TextMeshProUGUI destinationCoordinates;
        [SerializeField]
        private TMP_Text selectDestinationBttonText;
        internal int ownerId;
        private GalaxyMenuUIController galaxyUI;
        private GalaxyMenuUIController GalaxyUI
        {
            get
            {
                if (galaxyUI == null)
                    galaxyUI = GalaxyMenuUIController.Instance;
                return galaxyUI;
            }
        }
        private FleetMenuUIController fleetUI;
        private FleetMenuUIController FleetUI
        {
            get
            {
                if (fleetUI == null)
                    fleetUI = FleetMenuUIController.Instance;
                return fleetUI;
            }
        }
        private GameController gameController;
        private float distanceToDestination;

        // Cached per-fleet since GalaxySize is fixed for the life of a game; see
        // GalaxySizeExtensions.SpeedScale for why this keeps larger maps' fleet speed
        // proportional to the Small-map tuning baseline instead of needing its own pass.
        private float? galaxySizeSpeedScale;
        private float GalaxySizeSpeedScale
        {
            get
            {
                if (galaxySizeSpeedScale == null)
                {
                    galaxySizeSpeedScale = GameController.Instance != null
                        ? GameController.Instance.GameData.GalaxySize.SpeedScale()
                        : 1f;
                }
                return galaxySizeSpeedScale.Value;
            }
        }

        private void Awake()
        {
            gameController = GameController.Instance;
        }

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            // A headless dedicated server has no Camera in the scene at all, so this lookup
            // legitimately returns null there - only wire up the camera-dependent UI (world-space
            // canvas, minimap) when one actually exists, instead of throwing and aborting the rest
            // of Start() (DestinationLine setup, UpdateMinimapPosition) on every fleet spawn.
            GameObject mainCameraGo = GameObject.FindGameObjectWithTag("MainCamera");
            galaxyEventCamera = mainCameraGo != null ? mainCameraGo.GetComponent<Camera>() : null;
            if (GalaxyCanvasGo != null)
                FleetUICanvas = GalaxyCanvasGo.GetComponent<Canvas>();
            if (FleetUICanvas != null)
                FleetUICanvas.worldCamera = galaxyEventCamera;
            DestinationLine = this.GetComponentInChildren<MapLineMovable>();
            DestinationLine.GetLineRenderer();
            DestinationLine.transform.SetParent(transform, false);
            if (FleetData != null && FleetData.Destination != null)
            {
                FleetData.Destination = FleetManager.Instance.GalaxyCenter;
            }
            // GalaxyView.Instance can legitimately be null here on a headless dedicated server
            // (no galaxy map view is ever created there) or if this fleet spawns before the galaxy
            // map finishes building on a client - fall back to the 1f defaults above and skip the
            // minimap update rather than throwing and aborting the rest of Start().
            if (GalaxyView.Instance != null)
            {
                galaxyWidth = GalaxyView.Instance.GalaxyWidth;
                galaxyHeight = GalaxyView.Instance.GalaxyHeight;

                // Otherwise a fleet that never moves (e.g. a freshly split fleet, which by definition
                // starts at CurrentWarpFactor=0 with no destination) never runs the movement-branch calls
                // to UpdateMinimapPosition() below, leaving its red dot at the prefab's default
                // anchoredPosition (map center) instead of its real spawn position.
                UpdateMinimapPosition();
            }
        }
        private void Update()
        {
            // DropLine is only otherwise refreshed from MoveToInterceptPoint/MoveToDesitinationGO,
            // both of which only run from FixedUpdate while TimeManager.TurnPhase ==
            // TurnProgression. A stationary fleet (e.g. a freshly-deployed one, never given a
            // destination) never reaches either, so its very first placement - drawn inside
            // FleetManager.SetUpDropLine at fleet-registration time - is the only one it ever gets.
            // On a non-host client that registration happens inside FleetController.OnCivEnumChanged,
            // which (same root cause as GetMapSise/UpdateMinimapPosition above) can fire before
            // NetworkTransform has delivered this fleet's first synced position, leaving the line
            // drawn from whatever stale/default position the local placeholder GameObject started at.
            // Re-drawing it here every frame off the current (by-then-synced) transform.position is
            // cheap and self-healing regardless of the exact timing that caused the mismatch.
            if (Time.time - lastDropLineUpdateTime < updateInterval) return;
            lastDropLineUpdateTime = Time.time;

            if (DropLine == null)
            {
                // DropLine can still be null here if FleetManager.SetUpDropLine's own one-shot
                // galaxyImage-readiness retry lost the race when this fleet was first reconstructed
                // (most likely for a client's OWN starting fleet - it's in the earliest spawn-message
                // burst on connect, before this client's galaxyImage/GalaxyCenter refs are necessarily
                // resolved). SetUpDropLine is idempotent and cheap to no-op once it succeeds, so keep
                // retrying at the same throttled rate as the position refresh below until it does.
                FleetManager.Instance?.SetUpDropLine(this);
                return;
            }

            Vector3 galaxyPlanePoint = new Vector3(transform.position.x, -60f, transform.position.z);
            DropLine.SetUpLine(new Vector3[] { transform.position, galaxyPlanePoint });
        }

        private void FixedUpdate()
        {
            if (FleetData == null) return;
            if (TimeManager.Instance == null || TimeManager.Instance.TurnPhase == TurnPhase.InterTurn) return;
            if (IsAwaitingEncounterResolution) return;

            // ── Intercept mode ────────────────────────────────────────────────
            // Checked via IsPursuingIntercept rather than "InterceptTarget != null": a destroyed
            // FleetController's reference compares equal to null (UnityEngine.Object's == override),
            // so once the target dies, "InterceptTarget != null" is already false and would silently
            // skip straight past the destroyed-target handling below without ever running it.
            if (FleetData.IsPursuingIntercept)
            {
                if (FleetData.InterceptTarget == null)
                {
                    OnInterceptTargetLost(); // target was destroyed mid-pursuit
                }
                else if (FleetData.CurrentWarpFactor > 0f)
                {
                    interceptUpdateTimer -= Time.fixedDeltaTime;
                    if (interceptUpdateTimer <= 0f)
                    {
                        interceptPoint = ComputeInterceptPoint();
                        interceptUpdateTimer = INTERCEPT_UPDATE_INTERVAL;
                    }

                    MoveToInterceptPoint();

                    if (!gotMapSizeFromGameManager) GetMapSise();
                    if (Time.time - lastUpdateTime >= updateInterval)
                    {
                        DrawDestinationLine(interceptPoint);
                        UpdateMinimapPosition();
                        lastUpdateTime = Time.time;
                    }
                }
                return;
            }

            // ── Normal destination mode ───────────────────────────────────────
            if (FleetData.Destination != null && FleetData.CurrentWarpFactor > 0f)
            {
                if (FleetData.Destination != FleetManager.Instance?.GalaxyCenter)
                {
                    distanceToDestination = Vector3.Distance(transform.position, FleetData.Destination.transform.position);
                    MoveToDesitinationGO(GetDirection());
                    if (!gotMapSizeFromGameManager) GetMapSise();
                    if (Time.time - lastUpdateTime >= updateInterval)
                    {
                        DrawDestinationLine(FleetData.Destination.transform.position);
                        UpdateMinimapPosition();
                        lastUpdateTime = Time.time;
                    }
                }
            }
        }

        // ── Intercept helpers ─────────────────────────────────────────────────

        public void SetInterceptTarget(FleetController target)
        {
            FleetData.InterceptTarget = target;
            FleetData.IsPursuingIntercept = true;
            interceptPoint = target.transform.position;
            interceptUpdateTimer = 0f; // force immediate recompute
            FleetData.CurrentWarpFactor = FleetData.MaxWarpFactor; // full speed ahead
            Debug.Log($"{name}: intercept target set to '{target.name}'");

            // Same reasoning as SetAsDestinationInUI/SliderOnValueChange: on a non-host client this
            // only mutated a disconnected local FleetData copy - the server-authoritative fleet never
            // learned about the intercept order, so it never actually moved. This was the root cause
            // of giving an enemy fleet as a destination working for the host (whose own FleetController
            // IS the server-authoritative instance) but silently doing nothing for a non-host client.
            if (!isServer)
            {
                NetworkIdentity targetIdentity = target.GetComponent<NetworkIdentity>();
                if (targetIdentity != null)
                    CmdSetInterceptTarget(targetIdentity);
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdSetInterceptTarget(NetworkIdentity targetIdentity, NetworkConnectionToClient sender = null)
        {
            if (!IsSenderAuthorizedForThisFleet(sender))
            {
                Debug.LogWarning($"CmdSetInterceptTarget: connection {sender?.connectionId} is not authorized to command fleet '{name}'.");
                return;
            }
            FleetController targetFleetCon = targetIdentity != null ? targetIdentity.GetComponent<FleetController>() : null;
            if (targetFleetCon == null) return;
            Debug.Log($"CmdSetInterceptTarget: connection {sender?.connectionId} authorized, setting intercept target='{targetFleetCon.name}' on fleet '{name}'.");
            SetInterceptTarget(targetFleetCon);
        }

        public void CancelIntercept()
        {
            FleetData.InterceptTarget = null;
            FleetData.IsPursuingIntercept = false;
            interceptPoint = Vector3.zero;
        }

        /// <summary>Called when a pursued intercept target is destroyed mid-transit. Stops the fleet
        /// in place rather than letting it silently coast toward a stale point.</summary>
        private void OnInterceptTargetLost()
        {
            CancelIntercept();
            FleetData.CurrentWarpFactor = 0f;
            FleetUI?.UpdateFleetWarpUI(this, 0f);

            var fields = FleetUIGameObject != null ? FleetUIGameObject.GetComponent<FleetUI_Fields>() : null;
            if (fields != null)
            {
                fields.InterceptTargetButton?.gameObject.SetActive(true);
                fields.CancelInterceptButton?.gameObject.SetActive(false);
            }

            // Clear any pending merge target regardless of IsConvoy — this also applies to a "real"
            // fleet sent to merge into another via HandleShipMergeSelection.
            OnConvoyTargetLost();

            Debug.Log($"{name}: intercept target was destroyed — movement stopped");
        }

        private Vector3 ComputeInterceptPoint()
        {
            var target = FleetData.InterceptTarget;
            if (target == null) return transform.position;

            Vector3 targetPos = target.transform.position;

            // If target is stopped, just go straight to it
            if (target.FleetData.CurrentWarpFactor <= 0f ||
                target.FleetData.Destination == null ||
                target.FleetData.Destination == FleetManager.Instance.GalaxyCenter)
                return targetPos;

            Vector3 targetDest = target.FleetData.Destination.transform.position;
            Vector3 targetDir = (targetDest - targetPos).normalized;
            float targetSpeed = target.FleetData.CurrentWarpFactor * warpFudgeFactor;
            float ourSpeed = FleetData.CurrentWarpFactor * warpFudgeFactor;
            if (ourSpeed <= 0f) return targetPos;

            // Estimate time to close the gap and predict where target will be
            float dist = Vector3.Distance(transform.position, targetPos);
            float timeEstimate = dist / ourSpeed;
            Vector3 predicted = targetPos + targetDir * targetSpeed * timeEstimate;

            // Don't predict past the target's own destination
            float distToDest = Vector3.Distance(targetPos, targetDest);
            if (Vector3.Distance(targetPos, predicted) > distToDest)
                predicted = targetDest;

            return predicted;
        }

        private void MoveToInterceptPoint()
        {
            // Movement is server-authoritative - NetworkTransform on this prefab replicates the
            // resulting position out to every client. Clients must not also call MovePosition here,
            // or their local Rigidbody would fight the incoming NetworkTransform replication.
            if (isServer)
            {
                FleetData.ReleaseDockSlotIfAny(); // no-op once already released, so safe every tick

                float howFast = Mathf.Min(FleetData.CurrentWarpFactor, FleetData.MaxWarpFactor);
                Vector3 nextPos = Vector3.MoveTowards(rb.position, interceptPoint,
                    howFast * warpFudgeFactor * GalaxySizeSpeedScale * Time.fixedDeltaTime);
                rb.MovePosition(nextPos);
                FleetData.Position = nextPos;
            }

            Vector3 galaxyPlanePoint = new Vector3(rb.position.x, -60f, rb.position.z);
            DropLine.SetUpLine(new Vector3[] { rb.position, galaxyPlanePoint });
        }

        // Server-only, tiny positional nudge used by FleetManager.UnstackOverlappingFleets to visually
        // separate stationary fleets that end up on top of each other (e.g. two fleets parked at the
        // same system), so each keeps its own clickable SphereCollider instead of fighting for the same
        // click. Goes through rb.MovePosition + FleetData.Position exactly like real movement above so
        // NetworkTransform replicates it normally instead of a client-local offset getting overwritten
        // by the next incoming sync (see MoveToInterceptPoint's comment on why clients must not do this).
        [Server]
        public void NudgePosition(Vector3 delta)
        {
            Vector3 newPos = rb.position + delta;
            rb.MovePosition(newPos);
            FleetData.Position = newPos;
        }

        private void GetMapSise()
        {
            if (FleetUIGameObject == null) return;
            var fleetUIFields = FleetUIGameObject.GetComponent<FleetUI_Fields>();
            if (fleetUIFields == null || fleetUIFields.MinimapRedDot == null) return;
            RectTransform minimapRect = fleetUIFields.MinimapRedDot.parent.GetComponent<RectTransform>();
            float minimapWidth = minimapRect.rect.width;
            float minimapHeight = minimapRect.rect.height;
            gotMapSizeFromGameManager = true;
        }

        public void UpdateMinimapPosition()
        {
            if (FleetUIGameObject == null) return;

            var fleetUIFields = FleetUIGameObject.GetComponent<FleetUI_Fields>();
            if (fleetUIFields == null || fleetUIFields.MinimapRedDot == null) return;

            // Convert world position to mini-map coordinates
            Vector2 minimapPos = WorldToMinimapPosition(transform.position);
            fleetUIFields.MinimapRedDot.anchoredPosition = minimapPos;
        }
        public GameObject ShipListUIParent
        {
            get => FleetData?.ShipListUIParent;
            set
            {
                if (FleetData != null)
                    FleetData.ShipListUIParent = value;
                // Process any pending ship UI items now that the parent is available.
                if (ShipManager.Instance != null)
                    ShipManager.Instance.ProcessPendingShipUIs();
            }
        }
        private Vector2 WorldToMinimapPosition(Vector3 worldPos)
        {
            // Assuming the mini-map represents a specific area of the galaxy
            //float galaxyWidth = 2f; // GameManager.Instance.GalaxyWidth;
            //float galaxyHeight = 4f; // GameManager.Instance.GalaxyHeight;
            // Get mini-map RectTransform
            var fleetUIFields = FleetUIGameObject.GetComponent<FleetUI_Fields>();
            if (fleetUIFields == null || fleetUIFields.MinimapRedDot == null) return Vector2.zero;
            RectTransform minimapRect = fleetUIFields.MinimapRedDot.parent.GetComponent<RectTransform>();
            float minimapWidth = minimapRect.rect.width;
            float minimapHeight = minimapRect.rect.height;
            // Convert world position to mini-map coordinates
            float x = (worldPos.x / galaxyWidth) * minimapWidth;
            float y = (worldPos.z / galaxyHeight) * minimapHeight; // Assuming z is forward in world space
            return new Vector2(x, y);
        }
        public Rigidbody GetRigidBody() { return rb; }

        private void OnMouseDrag()
        {
            if (this.TargetController != null)
            {
                this.TargetController.gameObject.transform.position = GetMouseWorldPosition() + vectorOffset;
            }
        }
        void OnTriggerEnter(Collider collider) // Not using OnCollisionEnter....
        {
            if (FleetData != null)
            {
                bool weAreLocalPlayer = gameController.AreWeLocalPlayer(this.FleetData.CivEnum);
                float initialWarpFactor = this.FleetData.CurrentWarpFactor;

                bool isOurDestination = false;
                if (this.FleetData.Destination == collider.gameObject) // it is our destination
                {
                    isOurDestination = true;
                    if (weAreLocalPlayer)
                    {
                        SliderOnValueChange(0f); // stop the fleet on arrival
                        FleetUI.UpdateFleetWarpUI(this, 0f);
                        CloseUnLoadFleetUI(this); // we are there and have other things to do
                    }
                }

                if (collider.gameObject.TryGetComponent(out FleetController hitFleetCon))
                {
                    if (hitFleetCon == this || hitFleetCon == null) return; // ignore self

                    // Stop both fleets on contact (whether queued or immediate)
                    bool contactIsIntercept = (FleetData.InterceptTarget == hitFleetCon);
                    if (contactIsIntercept) CancelIntercept();

                    bool isEnemyFleet = FleetData.CivEnum != hitFleetCon.FleetData.CivEnum;

                    // Client-local visibility reveal (per-civ "have we met" cache) - deliberately
                    // NOT gated behind isOurDestination/contactIsIntercept below. Those two only
                    // describe whether THIS fleet was the one actively approaching hitFleetCon -
                    // when it's the other way around (hitFleetCon set THIS fleet as its own
                    // destination and did the approaching), this fleet's own OnTriggerEnter still
                    // fires on physical overlap (Unity raises it on both colliders), but previously
                    // never reached this call, so the approached side's owner never saw the
                    // encountered civ's insignia update - even though the diplomacy encounter
                    // itself opened fine for both sides (GalaxyEncounterQueue resolves that
                    // separately, not gated the same way). Recognition on contact should be mutual
                    // regardless of who initiated the approach.
                    //
                    // MUST additionally be gated on "is the local player actually one of these two
                    // civs" - regression found 2026-08-08: OnTriggerEnter fires from LOCAL physics on
                    // EVERY client independently (colliders overlap identically everywhere once synced
                    // positions genuinely overlap, regardless of which machine's collider pair this
                    // is), not just the two participants'. Without this check, a bystander with zero
                    // relationship to either side (e.g. Romulan watching Federation and Klingon meet
                    // each other) got ExposeAllFleetInsigniaSprites called on their own client purely
                    // because their own local physics also detected the same collision - revealing
                    // BOTH sides' real insignia through fog-of-war with no contact of their own ever
                    // having happened. weAreLocalPlayer (this fleet's civ) was already computed above;
                    // also check hitFleetCon's civ so either participant's own client still reveals
                    // correctly regardless of which of the two fleets' OnTriggerEnter this is.
                    if (isEnemyFleet && (weAreLocalPlayer || gameController.AreWeLocalPlayer(hitFleetCon.FleetData.CivEnum)))
                        EncounterUnknownFleetGetNameAndSprite(collider.gameObject);

                    if (isOurDestination || contactIsIntercept)
                    {
                        ClickCancelDestinationButton(); // we stop

                        if (isEnemyFleet)
                        {
                            // Encounter resolution must be server-authoritative: GalaxyEncounterQueue
                            // is only ever processed on the server, and DiplomacyManager is a plain,
                            // unnetworked per-client singleton - resolving this locally on whichever
                            // client's physics happened to fire this trigger would only ever open the
                            // Diplomacy UI on that one machine. Always queue rather than resolving
                            // inline here: GalaxyEncounterQueue.ProcessPendingForThisTick (run once per
                            // physics step, after every fleet's own FixedUpdate) groups everything that
                            // arrived in the same tick before resolving any of it, so two fleets that
                            // converge on each other simultaneously are drawn into the same decision
                            // instead of whichever collider fired first getting an initiative advantage.
                            if (isServer)
                            {
                                hitFleetCon.FleetData.CurrentWarpFactor = 0f; // stop them too
                                OnADestinationThatIsOtherCivFleet(hitFleetCon);
                                GalaxyEncounterQueue.Instance?.EnqueueFleetVsFleet(this, hitFleetCon);
                            }

                            if (hitFleetCon.FleetData.Destination == this.gameObject)
                                CloseUnLoadFleetUI(this);
                        }
                        else // friendly fleet
                        {
                            OnADestinationThatIsOurOtherFleet(hitFleetCon);
                        }
                    }
                }
                else if (collider.gameObject.TryGetComponent(out StarSysController sysCon))
                {
                    if (isOurDestination)
                    {
                        ClickCancelDestinationButton();

                        int firstUninhabited = (int)CivEnum.ZZUNINHABITED1;

                        if ((int)sysCon.StarSysData.CurrentOwnerCivEnum >= firstUninhabited)
                        {
                            if (sysCon.StarSysData.IsHabitable)
                            {
                                Debug.Log($"Fleet arrived at uninhabited habitable system '{sysCon.StarSysData.SysName}'");
                                if (weAreLocalPlayer)
                                {
                                    FleetUI.MoveBackAnyaFleetUIGO();
                                    HabitableSysUIController.Instance?.LoadHabitableSysUI(sysCon, this.FleetData.CivController);
                                }
                            }
                            else
                            {
                                Debug.Log($"Fleet arrived at uninhabited non-habitable system '{sysCon.StarSysData.SysName}'");
                            }
                        }
                        else if (this.FleetData.CivEnum != sysCon.StarSysData.CurrentOwnerCivEnum)
                        {
                            if (weAreLocalPlayer)
                                EncounterUnknownSystemShowName(collider.gameObject);

                            // Same server-authoritative reasoning as the fleet-vs-fleet branch above -
                            // always queue and let GalaxyEncounterQueue.ProcessPendingForThisTick group
                            // and resolve it, instead of resolving inline here.
                            if (isServer)
                                GalaxyEncounterQueue.Instance?.EnqueueFleetVsSystem(this, sysCon);
                        }
                        else
                        {
                            Debug.Log($"Fleet arrived at our own system '{sysCon.StarSysData.SysName}'");
                            if (FleetData.IsConvoy && FleetData.ConvoyMergeSystem == sysCon)
                                DepositConvoyAt(sysCon);
                        }
                    }
                }
                else if (collider.gameObject.TryGetComponent(out PlayerDefinedTargetController targetCon))
                {
                    // Only destroy the target if it is our destination, the fleet was moving, AND it's not currently being dragged
                    if (isOurDestination && initialWarpFactor > 0f && !targetCon.IsDragging)
                    {
                        ClickCancelDestinationButton(); // we stop, cancel destination & remove the player defined target
                    }
                }
            }

        }
        // TEMP DEBUG (2026-08-07e): dev-only hover tooltip showing this fleet's TRUE civ/name
        // regardless of contact status, purely for testing/verifying which civ a sprite actually is
        // (see project_bystander_fleet_duplication - distinguishing minor civs like XINDI/MALON from
        // Federation/Klingon). Deliberately NOT wired into any real gameplay path - HandleNormalClick
        // has no branch at all for an unowned, uncontacted fleet (confirmed: clicking one currently
        // does nothing), and this must never change that. Uses OnGUI so it's fully self-contained,
        // touches no real UI/Canvas, and is trivial to delete once done. REMOVE BEFORE SHIPPING.
        private bool debugHovered;
        private void OnMouseEnter() => debugHovered = true;
        private void OnMouseExit() => debugHovered = false;
        private void OnGUI()
        {
            if (!debugHovered || FleetData == null) return;
            Vector2 mousePos = Event.current.mousePosition;
            GUI.Box(new Rect(mousePos.x + 12, mousePos.y, 240, 24),
                $"[DEBUG] {FleetData.CivEnum} - {FleetData.FleetName}");
        }

        private void OnMouseDown()
        {
            // OnMouseDown is a raw physics raycast (SendMouseEvents) that runs independently of
            // uGUI - it fires even when the click actually landed on a UI button (e.g. the "New
            // Fleet" button in AFleetMenu) that happens to sit over this fleet's screen position.
            // Without this guard, clicking that button also re-triggers HandleNormalClick below,
            // which calls OpenMenu(AFleetMenu) on the same fleet - closing/rebuilding the menu
            // (and its buttons) between the button's pointer-down and pointer-up, which cancels
            // the button's OnClick and makes the New Fleet action silently do nothing.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var clickedFleetCon = GetComponentInParent<FleetController>();

            Debug.Log($"FleetController.OnMouseDown: raw click on '{name}' (layer={gameObject.layer}), clickedFleetCon={(clickedFleetCon == null ? "NULL" : clickedFleetCon.name)}, CurrentClickMode={GalaxyUI?.CurrentClickMode}.");
            if (clickedFleetCon == null) return;

            switch (GalaxyUI.CurrentClickMode)
            {
                case GalaxyClickMode.Normal:
                    // ✅ Only close ship deploy if it's actually open!
                    if (ShipDeployMenuUIController.Instance != null &&
                        ShipDeployMenuUIController.Instance.ShipDeployPanel != null &&
                        ShipDeployMenuUIController.Instance.ShipDeployPanel.activeSelf)
                    {
                        GalaxyMenuUIController.Instance.CloseShipDeployMenu();
                        if (StarSysMenuUIController.Instance != null)
                        {
                            StarSysMenuUIController.Instance.MoveBackAnyStarSysUIGO();
                            Debug.Log("HandleNormalClick: Cleaned up star system UIs before opening new UI");
                        }
                        // ✅ NEW: Ensure all fleet UIs are moved back to storage after closing deploy menu
                        if (FleetMenuUIController.Instance != null)
                        {
                            FleetMenuUIController.Instance.MoveBackAnyaFleetUIGO();
                            Debug.Log("OnMouseDown: Cleaned up fleet UIs after closing ship deploy menu");
                        }
                    }
                    HandleNormalClick(clickedFleetCon);
                    break;
                case GalaxyClickMode.SetDestination:
                    HandleDestinationClick(clickedFleetCon);
                    break;
                // no case GalaxyClickMode.SelectForNewFleet. that is a new fleet button click not a fleet click
                case GalaxyClickMode.SelectForShipDeploy:
                    if (gameController.AreWeLocalPlayer(clickedFleetCon.FleetData.CivEnum))
                        HandleShipDeploySelection(clickedFleetCon);
                    break;
                case GalaxyClickMode.SelectForShipMerge:
                    if (gameController.AreWeLocalPlayer(clickedFleetCon.FleetData.CivEnum))
                        HandleShipMergeSelection(clickedFleetCon);
                    break;
                case GalaxyClickMode.SelectForIntercept:
                    HandleInterceptSelection(clickedFleetCon);
                    break;
            }
        }

        private void HandleNormalClick(FleetController clickedFleetCon)
        {
            // Menu system will handle cleanup when transitioning between menus
            if (gameController.AreWeLocalPlayer(clickedFleetCon.FleetData.CivEnum))
            {
                GalaxyUI.OpenMenu(Menu.AFleetMenu, this.gameObject);
            }
            else if (DiplomacyManager.Instance.FoundADiplomacyController(CivManager.Instance.LocalPlayerCivController, clickedFleetCon.FleetData.CivController))
            { // this is a fleet local player does not own but we know them
                DiplomacyManager.Instance.ResolveDiplomacyForClickFleetWeKnow(CivManager.Instance.LocalPlayerCivController, clickedFleetCon);
            }
        }
        private void HandleDestinationClick(FleetController clickedFleetCon)
        {
            var pursuing = GalaxyUI.FleetLookingForDestination;
            if (pursuing == null || clickedFleetCon == pursuing) return;

            if (pursuing.TargetController != null)
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(pursuing);

            // Clicked a moving fleet → use intercept logic
            pursuing.SetInterceptTarget(clickedFleetCon);

            var fields = pursuing.FleetUIGameObject != null ? pursuing.FleetUIGameObject.GetComponent<FleetUI_Fields>() : null;
            if (fields != null)
            {
                fields.InterceptTargetButton?.gameObject.SetActive(false);
                fields.CancelInterceptButton?.gameObject.SetActive(true);
                if (fields.DestinationName != null)
                    fields.DestinationName.text = clickedFleetCon.FleetData.FleetName;
                if (fields.DestinationCoordinates != null)
                    fields.DestinationCoordinates.text = "";
            }

            GalaxyUI.CompleteSetDestination();
            MousePointerChanger.Instance?.ResetCursor();
        }

        private void HandleInterceptSelection(FleetController clickedFleetCon)
        {
            if (PendingInterceptFleet == null) return;
            if (clickedFleetCon == PendingInterceptFleet) return; // can't intercept self
            if (clickedFleetCon.FleetData.CivEnum == PendingInterceptFleet.FleetData.CivEnum) return; // same civ

            PendingInterceptFleet.SetInterceptTarget(clickedFleetCon);
            PendingInterceptFleet = null;
            GalaxyUI.ResetClickMode();
            MousePointerChanger.Instance?.ResetCursor();
        }

        private void HandleShipDeploySelection(FleetController clickedFleetCon)
        {
            if (clickedFleetCon != this) { return; }
            MousePointerChanger.Instance.ResetCursor();
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.WhatFleetIsSelectedForShipDiploy(clickedFleetCon);
            var fleetLooking = galaxyUI.FleetLookingForShipDeploy;
            var starSysLooking = galaxyUI.StarSystLookingForShipDeploy;

            if (fleetLooking != null && fleetLooking != this) // We have a fleet looking for ship deploy
            {
                var aFleetView = FleetUI.AFleetMenuView.gameObject;
                aFleetView.gameObject.SetActive(true);

                // Parent both fleet UIs
                if (fleetLooking.FleetUIGameObject != null)
                {
                    fleetLooking.FleetUIGameObject.transform.SetParent(aFleetView.transform, false);
                    fleetLooking.FleetUIGameObject.SetActive(true);
                }

                clickedFleetCon.FleetUIGameObject.transform.SetParent(aFleetView.transform, false);
                clickedFleetCon.FleetUIGameObject.SetActive(true);
                FleetUIGameObject.transform.SetAsLastSibling();

                ShipDeployMenuUIController.Instance.SetUpTopShipLists(fleetLooking.FleetData.ShipsList);
                ShipDeployMenuUIController.Instance.SetUpBottomShipLists(clickedFleetCon, true);
            }
            else if (starSysLooking != null) // We have a star system looking for ship deploy
            {
                var aSysView = StarSysMenuUIController.Instance.ASystemMenuView.gameObject;
                aSysView.SetActive(true);

                // ✅ Update star system UI with current values (minimap, facilities, etc.)
                if (starSysLooking.StarSysUIGameObject != null)
                {
                    starSysLooking.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                    starSysLooking.StarSysUIGameObject.SetActive(true);

                    // Update facility UI to show current load values
                    var starSysUI = StarSysMenuUIController.Instance;
                    if (starSysUI != null)
                    {
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.Factory);
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.Shipyard);
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.ShieldGenerator);
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.OrbitalBattery);
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.ResearchCenter);

                        // Update minimap position
                        var sysUIFields = starSysLooking.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                        if (sysUIFields != null && sysUIFields.redDot != null)
                        {
                            Vector3 sysPos = starSysLooking.transform.localPosition;
                            sysUIFields.redDot.anchoredPosition = GalaxyPositionBounds.ToMiniMapPosition(sysPos);
                        }
                    }
                }

                clickedFleetCon.FleetUIGameObject.transform.SetParent(aSysView.transform, false);
                clickedFleetCon.FleetUIGameObject.SetActive(true);
                FleetUIGameObject.transform.SetAsLastSibling();

                ShipDeployMenuUIController.Instance.SetUpTopShipLists(starSysLooking.StarSysData.ShipsList);
                ShipDeployMenuUIController.Instance.SetUpBottomShipLists(clickedFleetCon, true);
            }

            ShipDeployMenuUIController.Instance.ShowShipDeployMenuView();
        }
        private void HandleShipMergeSelection(FleetController clickedFleetCon)
        {
            if (clickedFleetCon != this) { return; }
            MousePointerChanger.Instance.ResetCursor();
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.WhatFleetIsSelectedForShipMerge(clickedFleetCon);
            var fleetLooking = galaxyUI.FleetLookingForShipMerge;
            var starSysLooking = galaxyUI.StarSystLookingForShipMerge;

            var shipDeployUI = ShipDeployMenuUIController.Instance;

            if (fleetLooking != null && fleetLooking != this) // Fleet-to-Fleet merge
            {
                // Rather than opening the manual ship-deploy panel, send the entire source fleet to
                // physically travel to the target fleet and auto-merge into it on arrival. Reuses the
                // same arrival logic as a temporary convoy (see OnADestinationThatIsOurOtherFleet /
                // MergeConvoyInto) — the source fleet is consumed (destroyed) once its ships are
                // transferred into the target.
                fleetLooking.FleetData.ConvoyMergeTarget = clickedFleetCon;
                fleetLooking.SetInterceptTarget(clickedFleetCon);

                // Use the existing destination UI (SelectDestination/CancelDestination + name/coords
                // text), same fields SetAsDestinationInUI/SetAsDestination use for a normal move order,
                // rather than the legacy InterceptTargetButton/CancelInterceptButton aliases. The Cancel
                // Destination button is already wired (see FleetMenuUIController.SetupFleetUIElements)
                // to fleetCon.ClickCancelDestinationButton(), which now also aborts the pending merge.
                var fields = fleetLooking.FleetUIGameObject != null ? fleetLooking.FleetUIGameObject.GetComponent<FleetUI_Fields>() : null;
                if (fields != null)
                {
                    if (fields.DestinationDragTarget != null)
                        fields.DestinationDragTarget.gameObject.SetActive(false);
                    if (fields.CancelDestination != null)
                        fields.CancelDestination.gameObject.SetActive(true);
                    if (fields.DestinationName != null)
                        fields.DestinationName.text = clickedFleetCon.FleetData.FleetName;
                    if (fields.DestinationCoordinates != null)
                        fields.DestinationCoordinates.text = "";
                }

                Debug.Log($"🚚 HandleShipMergeSelection: '{fleetLooking.name}' set to travel to and auto-merge into '{clickedFleetCon.name}'");

                galaxyUI.ClickCancelShipDeployButton(); // clears merge-selection state and click mode
                galaxyUI.CloseMenu(Menu.AFleetMenu);
                galaxyUI.CloseMenu(Menu.FleetMenu);
                MousePointerChanger.Instance.ResetCursor();
                return;
            }
            else if (starSysLooking != null) // System-to-Fleet merge
            {
                var aFleetView = FleetMenuUIController.Instance.AFleetMenuView.gameObject;
                aFleetView.gameObject.SetActive(true);

                // ✅ Add VerticalLayoutGroup if not present
                var layoutGroup = aFleetView.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = aFleetView.AddComponent<VerticalLayoutGroup>();
                    layoutGroup.childAlignment = TextAnchor.UpperLeft;
                    layoutGroup.spacing = 20f;
                    layoutGroup.childForceExpandHeight = false;
                    layoutGroup.childForceExpandWidth = false;
                    layoutGroup.childControlHeight = false;
                    layoutGroup.childControlWidth = false;
                }

                // Parent system UI to container (TOP position)
                if (starSysLooking.StarSysUIGameObject != null)
                {
                    starSysLooking.StarSysUIGameObject.transform.SetParent(aFleetView.transform, false);
                    starSysLooking.StarSysUIGameObject.transform.SetAsLastSibling();
                    starSysLooking.StarSysUIGameObject.SetActive(true);
                    Debug.Log($"✅ System UI parented to AFleetMenuView (top)");

                    // Update system facility UI
                    var starSysUI = StarSysMenuUIController.Instance;
                    if (starSysUI != null)
                    {
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.Factory);
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.Shipyard);
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.ShieldGenerator);
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.OrbitalBattery);
                        starSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.ResearchCenter);
                    }
                }

                // Parent fleet UI to container (BOTTOM position)
                clickedFleetCon.FleetUIGameObject.transform.SetParent(aFleetView.transform, false);
                clickedFleetCon.FleetUIGameObject.transform.SetAsLastSibling();
                clickedFleetCon.FleetUIGameObject.SetActive(true);
                Debug.Log($"✅ Fleet UI parented to AFleetMenuView (bottom)");

                var combinedShipsList = new System.Collections.Generic.List<ShipController>();
                combinedShipsList.AddRange(starSysLooking.StarSysData.ShipsList);
                combinedShipsList.AddRange(clickedFleetCon.FleetData.ShipsList);

                Debug.Log($"Merge System-to-Fleet: {starSysLooking.StarSysData.ShipsList.Count} + {clickedFleetCon.FleetData.ShipsList.Count} = {combinedShipsList.Count} ships");

                shipDeployUI.SetUpTopShipLists(new System.Collections.Generic.List<ShipController>());
                shipDeployUI.SetUpBottomShipListsForMerge(combinedShipsList, clickedFleetCon, null, starSysLooking, null);
            }

            shipDeployUI.ShowShipDeployMenuView();
        }
        private Vector3 GetMouseWorldPosition()
        {
            // pixel coordinates (x,y)
            Vector3 mousePoint = Input.mousePosition;

            //z coordinate of game object on screen
            mousePoint.z = ourZCoordinate;

            return galaxyEventCamera.ScreenToWorldPoint(mousePoint);
        }
        private void OnRemoveDestination(GameObject destination, int destinationInt) // for the C# event system
        {
            if (destination == this.FleetData.Destination)
            {
                // not implemented, looking for a good use case
            }

            if (destinationInt < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(destinationInt), destinationInt, $"'{nameof(destinationInt)}' cannot be negative");
            }
        }
        private void NewDestination(GameObject hitObject) // here is a destination
        {

            DestinationLine.gameObject.SetActive(true);
            DestinationLine.lineRenderer.gameObject.SetActive(true);
            DestinationLine.lineRenderer.enabled = true;
            DestinationLine.lineRenderer.startColor = Color.blue;
            DestinationLine.lineRenderer.endColor = Color.red;
            // turn off cursor of destination

            MousePointerChanger.Instance.ResetCursor(); // reset to default cursor because we just got to destination
            //MousePointerChanger.Instance.HaveGalaxyMapCursor = false;
            SetAsDestinationInUI(hitObject);

        }
        public void PlayerTargetAsNewDestination(GameObject destinationGo)
        {
            SetAsDestinationInUI(destinationGo);
        }


        private void EncounterUnknownSystemShowName(GameObject hitGO)
        {
            var sysData = hitGO.GetComponent<StarSysController>().StarSysData;

            StarSysManager.Instance.ExposeAllSystemName(sysData.CurrentOwnerCivEnum);
            FleetManager.Instance.ExposeAllFleetInsigniaSprites(sysData.CurrentOwnerCivEnum);

        }
        private void EncounterUnknownFleetGetNameAndSprite(GameObject hitGO)
        {
            var fleetData = hitGO.GetComponent<FleetController>().FleetData;
            StarSysManager.Instance.ExposeAllSystemName(fleetData.CivEnum);
            FleetManager.Instance.ExposeAllFleetInsigniaSprites(fleetData.CivEnum);
        }

        private Vector3 GetDirection()
        {
            return (this.FleetData.Destination.transform.position - transform.position).normalized;
        }

        void MoveToDesitinationGO(Vector3 direction)
        {
            // Movement is server-authoritative - NetworkTransform on this prefab replicates the
            // resulting position out to every client. Clients must not also call MovePosition here,
            // or their local Rigidbody would fight the incoming NetworkTransform replication.
            if (isServer)
            {
                FleetData.ReleaseDockSlotIfAny(); // no-op once already released, so safe every tick

                float howFast = this.FleetData.CurrentWarpFactor;
                if (howFast > this.FleetData.MaxWarpFactor)
                    this.FleetData.CurrentWarpFactor = this.FleetData.MaxWarpFactor;

                Vector3 nextPosition = Vector3.MoveTowards(rb.position, FleetData.Destination.transform.position,
                    howFast * warpFudgeFactor * GalaxySizeSpeedScale * Time.fixedDeltaTime);
                rb.MovePosition(nextPosition);
                this.FleetData.Position = nextPosition;
            }

            Vector3 galaxyPlanePoint = new Vector3(rb.position.x, -60f, rb.position.z);
            DropLine.SetUpLine(new Vector3[] { rb.position, galaxyPlanePoint });
        }
        void DrawDestinationLine(Vector3 destinationPoint)
        {
            // Destination lines are only meaningful to the civ giving the order - the host/server
            // process runs FixedUpdate (and this draw call) for every fleet in the game, not just its
            // own, so without this gate the host would see every civ's destination lines, not just
            // its own local player's.
            if (FleetData != null && GameController.Instance != null && !GameController.Instance.AreWeLocalPlayer(FleetData.CivEnum))
            {
                if (DestinationLine != null)
                    DestinationLine.gameObject.SetActive(false);
                return;
            }

            if (DestinationLine != null) { }
            else
            {
                DestinationLine = this.GetComponentInChildren<MapLineMovable>();
                DestinationLine.GetLineRenderer();
                DestinationLine.transform.SetParent(transform, false);
                DestinationLine.enabled = true;
            }
            Vector3[] points = { transform.position, destinationPoint };
            DestinationLine.gameObject.SetActive(true);
            DestinationLine.lineRenderer.startColor = Color.blue;
            DestinationLine.lineRenderer.endColor = Color.red;
            DestinationLine.SetUpLine(points);
        }
        void OnADestinationThatIsOtherCivFleet(FleetController theirFleetCon)
        {
            // Fleet only Logic to handle what happens when our fleet arrives at their fleet destination
            //GalaxyUI.ClickCancelDestinationButton(); 
        }
        void OnADestinationThatIsOurOtherFleet(FleetController ourOtherFleet)
        {
            // Logic to handle what happens when the fleet arrives at our other fleet as destination
            // how do we manage both fleets trying to do something with the other fleet?
            // Not gated on FleetData.IsConvoy: this also fires for a "real" fleet sent via
            // HandleShipMergeSelection to travel to and merge into another of our fleets — IsConvoy is
            // reserved for temporary carrier fleets spawned by CreateConvoyFleet.
            if (FleetData.ConvoyMergeTarget == ourOtherFleet)
                MergeConvoyInto(ourOtherFleet);
        }

        /// <summary>Transfers this fleet's ships into the fleet it was sent to merge into, then removes
        /// the now-empty source fleet. Called once the source fleet physically reaches ConvoyMergeTarget,
        /// whether via intercept pursuit (moving target) or a static destination (target happened to be
        /// stationary). Used both for temporary convoy carrier fleets and for a whole real fleet sent to
        /// merge into another via HandleShipMergeSelection.</summary>
        private void MergeConvoyInto(FleetController targetFleet)
        {
            if (targetFleet == null) return;

            // Guard against re-entrant/duplicate invocation (e.g. OnTriggerEnter firing more than
            // once for the same arrival collision). Clearing the merge target immediately makes this
            // call idempotent — a second call for the same arrival will see ConvoyMergeTarget == null
            // and OnADestinationThatIsOurOtherFleet's guard will skip it before we get here again.
            FleetData.ConvoyMergeTarget = null;

            // Distinct() guards against a source ShipsList that already contains the same ship
            // reference twice (possible from state formed before FleetData.AddToShipList's own
            // duplicate guard was added) — without this, iterating a doubled entry would add the
            // ship once (fine) then hit AddToShipList's "already in" warning on the second pass.
            var ships = new List<ShipController>(FleetData.ShipsList.Distinct());
            Debug.Log($"🚚 MergeConvoyInto: convoy '{name}' merging {ships.Count} ship(s) into target '{targetFleet.name}' " +
                $"(target ShipsList.Count before={targetFleet.FleetData.ShipsList.Count}, target ShipListUIParent={(targetFleet.FleetData.ShipListUIParent != null ? targetFleet.FleetData.ShipListUIParent.name : "NULL")})");

            foreach (var ship in ships)
            {
                if (ship == null)
                {
                    Debug.LogWarning("🚚 MergeConvoyInto: null ship reference in convoy ShipsList, skipping");
                    continue;
                }

                var shipUIItem = ship.ShipListUIGameObject != null
                    ? ship.ShipListUIGameObject.GetComponent<ShipListUI_Item>()
                    : null;

                targetFleet.AddToShipList(ship);
                ship.ShipData.CurrentFleetController = targetFleet;
                ship.ShipData.CurrentStarSysController = null;

                if (shipUIItem != null)
                {
                    shipUIItem.CurrentFleet = targetFleet;
                    shipUIItem.CurrentStarSyst = null;
                }

                FleetData.RemoveFromShipList(ship);

                Debug.Log($"🚚 MergeConvoyInto:   transferred '{ship.ShipData?.ShipName}' — target ShipsList.Count now={targetFleet.FleetData.ShipsList.Count}, " +
                    $"ship.ShipListUIGameObject={(ship.ShipListUIGameObject != null ? "SET" : "NULL")}");
            }

            Debug.Log($"🚚 MergeConvoyInto: complete — target '{targetFleet.name}' ShipsList.Count final={targetFleet.FleetData.ShipsList.Count}, destroying convoy '{name}'");

            targetFleet.UpdateMaxWarp();
            FleetManager.Instance.DestroyFleetController(this);
        }

        /// <summary>Transfers this convoy's ships into the star system it was sent to deposit at, then
        /// removes the now-empty convoy. Called from OnTriggerEnter's "arrived at our own system" branch
        /// when this fleet is a convoy that has arrived at its ConvoyMergeSystem.</summary>
        private void DepositConvoyAt(StarSysController targetSystem)
        {
            if (targetSystem == null) return;

            // Same idempotency guard as MergeConvoyInto — clear the merge target immediately so a
            // duplicate OnTriggerEnter for the same arrival is skipped instead of double-depositing.
            FleetData.ConvoyMergeTarget = null;

            var ships = new List<ShipController>(FleetData.ShipsList);
            foreach (var ship in ships)
            {
                if (ship == null) continue;

                var shipUIItem = ship.ShipListUIGameObject != null
                    ? ship.ShipListUIGameObject.GetComponent<ShipListUI_Item>()
                    : null;

                targetSystem.AddToShipList(ship);
                ship.ShipData.CurrentStarSysController = targetSystem;
                ship.ShipData.CurrentFleetController = null;

                if (shipUIItem != null)
                {
                    shipUIItem.CurrentStarSyst = targetSystem;
                    shipUIItem.CurrentFleet = null;
                }

                FleetData.RemoveFromShipList(ship);
            }

            FleetManager.Instance.DestroyFleetController(this);
        }

        /// <summary>Called when this convoy's pursuit target is destroyed mid-transit (see
        /// OnInterceptTargetLost). The convoy has already been stopped in place; this just clears the
        /// stale merge target so a future SetInterceptTarget call isn't misread as still-pending.</summary>
        private void OnConvoyTargetLost()
        {
            FleetData.ConvoyMergeTarget = null;
        }
        void OnADestinationThatIsPlayerTarget()
        {
            // Logic to handle what happens when the fleet arrives at the system destination
            //GalaxyUI.ClickCancelDestinationButton(); 
        }
        //void OnEnterForeignStarSystem()
        //{ 
        //    // do something
        //}
        public void AddToShipList(ShipController shipController)
        {
            if (shipController == null) return;

            // Reparent gameplay ship under this fleet so scene hierarchy and transform stay correct.
            // Keep world position so the ship doesn't jump unexpectedly.
            shipController.transform.SetParent(transform, worldPositionStays: true);

            // Add to FleetData (model). FleetData.AddToShipList should guard duplicates but check anyway.
            if (!FleetData.ShipsList.Contains(shipController))
                FleetData.AddToShipList(shipController);
            else
                Debug.LogWarning($"AddToShipList: '{shipController.ShipData?.ShipName}' already in '{name}'.FleetData.ShipsList — skipped duplicate add");

            // Move the UI representation under the fleet's UI parent if available.
            if (shipController.ShipListUIGameObject != null && FleetData.ShipListUIParent != null)
            {
                shipController.ShipListUIGameObject.transform.SetParent(FleetData.ShipListUIParent.transform, false);
            }

            // Update controller state (max warp etc.)
            UpdateMaxWarp();
        }

        public void RemoveFromShipList(ShipController shipController)
        {
            if (shipController == null) return;

            // Remove from model list
            if (FleetData.ShipsList.Contains(shipController))
                FleetData.RemoveFromShipList(shipController);// ship controllers go are children of a fleet controller go, not to confuse with the ship UI go we see on drag drop

            // Update controller state
            UpdateMaxWarp();

            // If the ship controller was parented to this fleet controller go under GalaxyCenter in the scene hierarchy, unparent it to scene root.
            if (shipController.transform.IsChildOf(transform))
                shipController.transform.SetParent(null, worldPositionStays: true);

            // Optionally move ship UI GO item to a neutral parent if the fleet UI parent still exists.

            //if (shipController.ShipListUIGameObject != null && FleetData.ShipListUIParent != null)
            //{
            //    shipController.ShipListUIGameObject.transform.SetParent(FleetData.ShipListUIParent.transform, false);
            //}
        }

        // ---------------------------------------------------------------------------------------
        // Ship roster replication. ShipController has no NetworkIdentity/SyncList (see
        // OnCivEnumChanged's SyncedIsNewFleet branch above), so a fleet's starting roster only
        // stays consistent across peers because BuildShipsOfFirstFleet is a deterministic pure
        // function every peer runs independently. A fleet built/modified by a player action
        // instead (ship-deploy UI merge/split, convoy formation) has no such guarantee - the
        // AddToShipList/RemoveFromShipList calls those flows make only ever run on whichever
        // client's UI the player was dragging ships in, so FleetData.ShipsList silently stays
        // empty on every other peer forever. That's exactly what crashed CombatInstantiator.
        // CreateCombatData's sideOneShipCons[0]/sideTwoShipCons[0] the moment a freshly split
        // fleet reached combat on a peer that never saw the split. Call RequestSyncShipRoster
        // once, right after any such local roster mutation finishes, to fix that.
        // ---------------------------------------------------------------------------------------

        public void RequestSyncShipRoster()
        {
            if (FleetData == null)
            {
                Debug.LogWarning($"RequestSyncShipRoster: '{name}' has null FleetData - skipping.");
                return;
            }
            List<int> shipIDs = FleetData.ShipsList
                .Where(s => s != null && s.ShipData != null)
                .Select(s => s.ShipData.ShipID)
                .ToList();

            if (isServer)
            {
                ServerSyncShipRoster(shipIDs);
                return;
            }
            CmdSyncShipRoster(shipIDs);
        }

        [Command(requiresAuthority = false)]
        private void CmdSyncShipRoster(List<int> shipIDs, NetworkConnectionToClient sender = null)
        {
            if (!IsSenderAuthorizedForThisFleet(sender))
            {
                Debug.LogWarning($"CmdSyncShipRoster: connection {sender?.connectionId} is not authorized to set '{name}''s ship roster - request ignored.");
                return;
            }
            ServerSyncShipRoster(shipIDs);
        }

        [Server]
        private void ServerSyncShipRoster(List<int> shipIDs)
        {
            RpcSyncShipRoster(shipIDs);
        }

        // A freshly-created fleet (e.g. a ship-deploy split) can have its first RequestSyncShipRoster
        // RPC reach a given client before that same client's OnCivEnumChanged SyncVar hook has
        // finished constructing FleetData for the newly-spawned NetworkIdentity - Mirror doesn't
        // guarantee spawn-message and RPC-message handling are interleaved in dependency order per
        // object. Stash the roster here when that race is lost; OnCivEnumChanged replays it via
        // ApplyShipRoster() the moment FleetData actually exists.
        private List<int> _pendingRosterShipIDs = null;

        // Every peer applies this the same way, including the client that originated the roster -
        // resolving each ShipID back through the local ShipRegistry is idempotent and guarantees
        // this fleet's FleetData.ShipsList ends up built from the same resolution path everywhere,
        // rather than the originating client trusting its own live ShipController references while
        // every other peer trusts this Rpc.
        [ClientRpc]
        private void RpcSyncShipRoster(List<int> shipIDs)
        {
            if (FleetData == null)
            {
                Debug.LogWarning($"RpcSyncShipRoster: '{name}' has no FleetData yet (spawn/RPC ordering race) - deferring roster of {shipIDs.Count} ship(s) until OnCivEnumChanged initializes it.");
                _pendingRosterShipIDs = shipIDs;
                return;
            }
            ApplyShipRoster(shipIDs);
        }

        private void ApplyShipRoster(List<int> shipIDs)
        {
            List<ShipController> resolved = new List<ShipController>();
            foreach (int shipID in shipIDs)
            {
                ShipController shipCon = ShipManager.Instance?.GetShipControllerByShipID(shipID);
                if (shipCon != null)
                    resolved.Add(shipCon);
                else
                    Debug.LogWarning($"ApplyShipRoster: '{name}' could not resolve ShipID={shipID} to a local ShipController - this peer doesn't know about that ship yet, its roster will be short by one.");
            }

            FleetData.SetShipList(resolved);
            UpdateMaxWarp();
            Debug.Log($"ApplyShipRoster: '{name}' roster synced - {resolved.Count}/{shipIDs.Count} ship(s) resolved locally.");
        }

        public void UpdateMaxWarp()
        {
            if (fleetData == null)
            {
                Debug.LogWarning($"UpdateMaxWarp: '{name}' has null FleetData - skipping.");
                return;
            }
            float maxWarp = 10f;
            for (int i = 0; i < fleetData.ShipsList.Count; i++)
            { // find the slowest ship
                if (fleetData.ShipsList[i] != null && fleetData.ShipsList[i].ShipData.maxWarpFactor < maxWarp)
                {
                    maxWarp = fleetData.ShipsList[i].ShipData.maxWarpFactor;
                }
            }
            fleetData.MaxWarpFactor = maxWarp;
            Debug.LogWarning($"🛰️[MAXWARP] UpdateMaxWarp: fleet '{name}' computed maxWarp={maxWarp} from {fleetData.ShipsList.Count} ship(s), isServer={isServer}, NetworkServer.active={NetworkServer.active}, SyncedMaxWarpFactor(before)={SyncedMaxWarpFactor}.");
            // isServer requires the NetworkIdentity to have already been spawned (NetworkServer.Spawn),
            // but InstantiateFleet calls UpdateMaxWarp() twice (ShipManager.BuildShipsOfFirstFleet and
            // just before Spawn) while building the fleet, i.e. before Spawn has run - isServer reads
            // false there even though this is unambiguously server code, so SyncedMaxWarpFactor was
            // silently never assigned pre-spawn. NetworkServer.active is true in server code regardless
            // of spawn timing - same reasoning as OnCivEnumChanged's NetworkServer.active guard above.
            if (NetworkServer.active)
                SyncedMaxWarpFactor = maxWarp;
            if (GalaxyUI != null)
                FleetUI.UpdateFleetMaxWarpUI(this, maxWarp);
        }
        public void DestroyFleet(FleetData fleetData, GameObject fleetGO)
        {
            FleetManager.Instance.RemoveFleetNumInUse(fleetData.CivEnum, fleetData.FleetInt);
            if (FleetManager.Instance.FleetControllerList.Contains(this))
            {
                FleetManager.Instance.FleetControllerList.Remove(this);
                Destroy(fleetGO.gameObject);
                // TimeManager.Instance.ResumeTime();
            }
        }

        public void FleetOnWarpUpClick(FleetController fleetCon)
        {
            if (this == fleetCon)
            {
                warpChange = 0.1f;
            }
            Debug.Log($"FleetOnWarpUpClick: fleet '{fleetCon.name}' current={fleetCon.FleetData.CurrentWarpFactor}, max={fleetCon.FleetData.MaxWarpFactor}, warpChange={warpChange}.");
            if (fleetCon.FleetData.CurrentWarpFactor + warpChange > fleetCon.FleetData.MaxWarpFactor)
            {
                Debug.LogWarning($"🛰️[MAXWARP] FleetOnWarpUpClick: fleet '{fleetCon.name}' would exceed MaxWarpFactor ({fleetCon.FleetData.CurrentWarpFactor}+{warpChange} > {fleetCon.FleetData.MaxWarpFactor}) - click ignored.");
                warpChange = 0f;
                return;
            }
            SliderOnValueChange(fleetCon.FleetData.CurrentWarpFactor + warpChange); // this called method updates the UI too
        }
        public void FleetOnWarpDownClick(FleetController fleetCon)
        {
            if (this == fleetCon)
            {
                warpChange = -0.1f;
            }
            Debug.Log($"FleetOnWarpDownClick: fleet '{fleetCon.name}' current={fleetCon.FleetData.CurrentWarpFactor}, max={fleetCon.FleetData.MaxWarpFactor}, warpChange={warpChange}.");
            if (fleetCon.FleetData.CurrentWarpFactor - warpChange < 0f)
            {
                Debug.LogWarning($"🛰️[MAXWARP] FleetOnWarpDownClick: fleet '{fleetCon.name}' would go below 0 ({fleetCon.FleetData.CurrentWarpFactor}-{warpChange} < 0) - click ignored.");
                warpChange = 0f;
                return;
            }
            SliderOnValueChange(fleetCon.FleetData.CurrentWarpFactor + warpChange);
        }

        public void SliderOnValueChange(float newWarpValue)
        {
            Debug.Log($"SliderOnValueChange: fleet '{name}' requested newWarpValue={newWarpValue}, MaxWarpFactor={this.FleetData.MaxWarpFactor}.");
            float maxSliderValue = this.FleetData.MaxWarpFactor;

            if (newWarpValue < 0f)
            {
                newWarpValue = 0f;
            }
            if (newWarpValue > maxSliderValue)
            {
                newWarpValue = maxSliderValue;
            }

            FleetData.CurrentWarpFactor = newWarpValue;
            FleetUI.UpdateFleetWarpUI(this, newWarpValue);

            // Movement in FixedUpdate() reads FleetData off whichever machine's FleetController this
            // is - on a non-host client that's a disconnected copy (see OnCivEnumChanged), so the order
            // has to be relayed to the server-authoritative instance. isServer is true on the host, so
            // this is a no-op there and the block above is all that ever runs.
            if (!isServer)
            {
                Debug.Log($"SliderOnValueChange: non-host client relaying warp={newWarpValue} for fleet '{name}' via CmdSetWarpFactor.");
                CmdSetWarpFactor(newWarpValue);
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdSetWarpFactor(float newWarpValue, NetworkConnectionToClient sender = null)
        {
            if (!IsSenderAuthorizedForThisFleet(sender))
            {
                Debug.LogWarning($"CmdSetWarpFactor: connection {sender?.connectionId} is not authorized to command fleet '{name}'.");
                return;
            }
            Debug.Log($"CmdSetWarpFactor: connection {sender?.connectionId} authorized, setting warp={newWarpValue} on fleet '{name}'.");
            SliderOnValueChange(newWarpValue);
        }

        //internal void SelectedUsForShips(FleetController fleetCon)
        //{
        //    galaxyUI.WhoIsSelectedForShipDiploy(this);
        //}
        public void CloseUnLoadFleetUI(FleetController theFleetCon)
        {
            GalaxyUI.ResetClickMode();
            MousePointerChanger.Instance.ResetCursor();
            FleetUI.UpdateFleetWarpUI(theFleetCon, 0);
            GalaxyUI.CloseMenu(Menu.AFleetMenu); // The single fleet UI
            GalaxyUI.CloseMenu(Menu.FleetMenu);
        }

        internal void RemoveShipFromFleet(ShipController shipController)
        {
            this.FleetData.ShipsList.Remove(shipController);
            if (this.FleetData.ShipsList.Count == 0)
            {
                // Fleet is empty. EndCombat() calls DestroyFleetController() which also cleans up FleetUIGameObject and DropLine.
                FleetManager.Instance.RemoveFleetNumInUse(this.FleetData.CivEnum, this.FleetData.FleetInt);
            }
            else
            {
                UpdateMaxWarp();
            }
        }
        public void IsTheFleetDestroyed()
        {
            if (this.FleetData.ShipsList.Count == 0)
            {
                OnDestroy();
            }
        }
        private void OnDestroy()
        {
            // TEMP DIAGNOSTIC (2026-08-07): chasing the bystander-client fleet-duplication bug (see
            // project_bystander_fleet_duplication memory). Mirror's NetworkClient.FindOrSpawnObject
            // treats a Unity "fake-null" spawned[netId] entry (dictionary key still present, but
            // pointing at a destroyed UnityEngine.Object) as "never spawned", so if THIS GameObject
            // gets destroyed for ANY reason - explicit Destroy() call, or Unity's own scene-unload
            // GC silently killing it because it ended up parented under the wrong active scene -
            // without Mirror's own NetworkServer.Destroy/ObjectDestroyMessage path running, the next
            // update for this netId spawns a brand-new duplicate copy instead of updating this one.
            // OnDestroy() fires regardless of *how* the object died, so this is the one place that
            // can catch every case, including a bare scene-unload with no explicit Destroy() call
            // anywhere in project code. Remove once root-caused.
            // Stack trace removed (2026-08-07b) - see matching note in NetworkClient.FindOrSpawnObject.
            // This call site fires far less often (only ever seen 5x, all benign empty-placeholder
            // destroys) so it was lower-risk, but simplified for consistency while diagnosing.
            var diagNetId = GetComponent<NetworkIdentity>();
            Debug.LogWarning($"🪦[FleetDestroyDiag] OnDestroy: fleet '{name}' civ={FleetData?.CivEnum} netId={(diagNetId != null ? diagNetId.netId.ToString() : "NULL")} scene='{gameObject.scene.name}' NetworkServer.active={NetworkServer.active} NetworkClient.active={NetworkClient.active} pos={transform.position}");

            // Remove fog revealer when fleet is destroyed
            if (FischlWorks_FogWar.csFogWar.Instance != null && transform != null)
            {
                FischlWorks_FogWar.csFogWar.Instance.RemoveRevealer(transform);
            }

            ServerClearPlayerTargetMarker();

            // Existing cleanup code...
            //StopAllCoroutines();
            if (this.FleetData != null)
            {
                FleetManager.Instance.RemoveFleetNumInUse(this.FleetData.CivEnum, this.FleetData.FleetInt);
                if (FleetManager.Instance.FleetControllerList.Contains(this))
                {
                    FleetManager.Instance.FleetControllerList.Remove(this);
                    Destroy(this.gameObject);
                    //TimeManager.Instance.ResumeTime();
                }
            }
        }

        public void CleanupFleetUIs()
        {
            foreach (var diplomacyCon in FleetManager.Instance.FleetControllerList)
            {
                if (diplomacyCon.FleetUIGameObject == null)
                    continue;

                if (!diplomacyCon.FleetUIGameObject.activeInHierarchy)
                {
                    diplomacyCon.FleetUIGameObject = null;
                }
            }
        }
        public void CloseShipDeploy(FleetController fleetCon)
        {
            if (fleetCon == this)
            {
                if (fleetCon.TargetController != null)
                {
                    PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(fleetCon);
                }
                if (ShipDeployMenuUIController.Instance != null)
                {
                    ShipDeployMenuUIController.Instance.OnSaveCloseButtonClicked();
                }
                fleetCon.FleetUIGameObject.SetActive(false);

                //ShipDeployMenuUIController.Instance.CloseShipDeployMenuView();
            }
        }
        public void ClickCancelDestinationButton()
        {
            // Zero warp FIRST — unconditional stop before any failable lookups
            FleetData.LastDestination = FleetData.Destination;
            FleetData.CurrentWarpFactor = 0f;
            FleetData.Destination = FleetManager.Instance != null ? FleetManager.Instance.GalaxyCenter : null;

            // Clear any active intercept (fleet-chasing) state
            if (FleetData.IsPursuingIntercept)
                CancelIntercept();
            PendingInterceptFleet = null;

            // Sync warp slider to 0 so scroll/drag can't silently restore a non-zero value
            FleetUI?.UpdateFleetWarpUI(this, 0f);

            if (TargetController != null)
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(this);

            if (DestinationLine != null)
                DestinationLine.gameObject.SetActive(false);

            GalaxyUI?.CompleteSetDestination();
            FleetUI?.ClickCancelDestinationButton(this);
            GalaxyUI?.SetClickMode(GalaxyClickMode.Normal);
            MousePointerChanger.Instance?.ResetCursor();

            // Same reasoning as SetAsDestinationInUI/SliderOnValueChange: on a non-host client this
            // method only mutated a disconnected local FleetData copy. Without this relay, the
            // server-authoritative fleet never learned the destination/warp were canceled, so the
            // next TurnProgression tick resumed movement toward the "canceled" destination.
            if (!isServer)
                CmdCancelDestination();
            else
                ServerClearPlayerTargetMarker(); // host calling this directly never goes through the Cmd above
        }

        [Command(requiresAuthority = false)]
        private void CmdCancelDestination(NetworkConnectionToClient sender = null)
        {
            if (!IsSenderAuthorizedForThisFleet(sender))
            {
                Debug.LogWarning($"CmdCancelDestination: connection {sender?.connectionId} is not authorized to command fleet '{name}'.");
                return;
            }
            Debug.Log($"CmdCancelDestination: connection {sender?.connectionId} authorized, canceling destination/warp on fleet '{name}'.");
            FleetData.LastDestination = FleetData.Destination;
            FleetData.CurrentWarpFactor = 0f;
            FleetData.Destination = FleetManager.Instance != null ? FleetManager.Instance.GalaxyCenter : null;
            if (FleetData.IsPursuingIntercept)
                CancelIntercept();
            ServerClearPlayerTargetMarker();
        }

        /// <summary>User-initiated abort of a pending convoy-deploy or fleet-merge. Deliberately kept
        /// separate from ClickCancelDestinationButton(), which OnTriggerEnter also calls internally just
        /// to stop movement on arrival — if that shared method cleared ConvoyMergeTarget/ConvoyMergeSystem
        /// too, every normal arrival would wipe the merge target moments before
        /// OnADestinationThatIsOurOtherFleet / the ConvoyMergeSystem check in OnTriggerEnter reads it,
        /// silently breaking every convoy/fleet merge. Only wire this to the UI Cancel Destination button.</summary>
        public void AbortPendingConvoyMerge()
        {
            FleetData.ConvoyMergeTarget = null;
            FleetData.ConvoyMergeSystem = null;
        }

        public void SetAsDestinationInUI(GameObject hitObject)
        {
            fleetData.Destination = hitObject;
            GalaxyObjectType destinationType = GalaxyObjectType.None;
            string destinationNameText = "";

            string coordiatesText = "X " + (hitObject.transform.position.x).ToString()
                + " / Y " + (hitObject.transform.position.y).ToString()
                + " / Z " + (hitObject.transform.position.z).ToString();

            if (hitObject.GetComponent<StarSysController>() != null)
            {
                StarSysController starSysController = hitObject.GetComponent<StarSysController>();
                if (DiplomacyManager.Instance.FoundADiplomacyController(CivManager.Instance.LocalPlayerCivController, starSysController.StarSysData.CurrentCivController))
                {
                    destinationType = 0;
                    destinationNameText += starSysController.StarSysData.SysName;
                }
                else
                {
                    destinationType = starSysController.StarSysData.SystemType;
                }
            }
            else if (hitObject.GetComponent<FleetController>() != null)
            {
                FleetController fleetCon = hitObject.GetComponent<FleetController>();
                if (DiplomacyManager.Instance.FoundADiplomacyController(CivManager.Instance.LocalPlayerCivController, fleetCon.FleetData.CivController))
                {
                    destinationType = GalaxyObjectType.Fleet;
                    destinationNameText = fleetCon.FleetData.FleetName;
                }
                else
                {
                    destinationType = GalaxyObjectType.UnknownFleet;
                }
            }

            switch (destinationType)
            {
                case GalaxyObjectType.None:
                    break;
                case GalaxyObjectType.BlueStar:
                    destinationNameText = "Blue Star at";
                    break;
                case GalaxyObjectType.WhiteStar:
                    destinationNameText = "White Star at";
                    break;
                case GalaxyObjectType.YellowStar:
                    destinationNameText = "Yellow Star at";
                    break;
                case GalaxyObjectType.OrangeStar:
                    destinationNameText = "Orange Star at";
                    break;
                case GalaxyObjectType.RedStar:
                    destinationNameText = "Red Star at";
                    break;
                case GalaxyObjectType.Nebula:
                case GalaxyObjectType.OmarianNebula:
                case GalaxyObjectType.ORIONNEBULA:
                    destinationNameText = "Nebula at";
                    break;
                case GalaxyObjectType.Station:
                    destinationNameText = "Station at";
                    break;
                case GalaxyObjectType.BlackHole:
                    destinationNameText = "Black Hole at";
                    break;
                case GalaxyObjectType.WormHole:
                    destinationNameText = "WormHole at";
                    break;
                case GalaxyObjectType.TargetDestination:
                    destinationNameText = "Target at";
                    break;
                case GalaxyObjectType.UnknownFleet:
                case GalaxyObjectType.Fleet:
                    destinationNameText = "Fleet at";
                    break;
                default:
                    destinationNameText = "";
                    break;
            }

            FleetMenuUIController.Instance?.SetAsDestination(destinationNameText, coordiatesText);

            // Same reasoning as SliderOnValueChange: on a non-host client this method only mutated a
            // disconnected local FleetData copy, so relay to the server-authoritative instance too.
            if (!isServer)
                RelayDestinationToServer(hitObject);
        }

        // hitObject can be a star system, a fleet, GalaxyCenter, or a manually-placed player-defined
        // target point - each needs its own Command since Mirror can only serialize
        // GameObject/NetworkIdentity parameters for objects that have a spawned NetworkIdentity, and
        // StarSysController/GalaxyCenter/player-defined targets don't have one (only FleetController
        // does - see Stage 1). Player-defined targets are relayed by raw world position instead (see
        // CmdSetDestinationToPlayerTarget) since the dragged marker itself only exists client-side.
        private void RelayDestinationToServer(GameObject hitObject)
        {
            if (hitObject == FleetManager.Instance?.GalaxyCenter)
            {
                Debug.Log($"RelayDestinationToServer: fleet '{name}' relaying destination=GalaxyCenter via CmdSetDestinationToGalaxyCenter.");
                CmdSetDestinationToGalaxyCenter();
                return;
            }

            StarSysController starSysCon = hitObject.GetComponent<StarSysController>();
            if (starSysCon != null)
            {
                Debug.Log($"RelayDestinationToServer: fleet '{name}' relaying destination='{starSysCon.StarSysData.GetSysName()}' via CmdSetDestinationToStarSystem.");
                CmdSetDestinationToStarSystem(starSysCon.StarSysData.GetSysName());
                return;
            }

            FleetController targetFleetCon = hitObject.GetComponent<FleetController>();
            if (targetFleetCon != null)
            {
                NetworkIdentity targetIdentity = targetFleetCon.GetComponent<NetworkIdentity>();
                if (targetIdentity != null)
                {
                    Debug.Log($"RelayDestinationToServer: fleet '{name}' relaying destination=fleet '{targetFleetCon.name}' via CmdSetDestinationToFleet.");
                    CmdSetDestinationToFleet(targetIdentity);
                    return;
                }
            }

            if (hitObject.GetComponent<PlayerDefinedTargetController>() != null)
            {
                Debug.Log($"RelayDestinationToServer: fleet '{name}' relaying destination=player-defined target at {hitObject.transform.position} via CmdSetDestinationToPlayerTarget.");
                CmdSetDestinationToPlayerTarget(hitObject.transform.position);
                return;
            }

            Debug.LogWarning($"RelayDestinationToServer: '{hitObject.name}' has no networked relay path - this order will not reach the server on a non-host client.");
        }

        [Command(requiresAuthority = false)]
        private void CmdSetDestinationToGalaxyCenter(NetworkConnectionToClient sender = null)
        {
            if (!IsSenderAuthorizedForThisFleet(sender))
            {
                Debug.LogWarning($"CmdSetDestinationToGalaxyCenter: connection {sender?.connectionId} is not authorized to command fleet '{name}'.");
                return;
            }
            Debug.Log($"CmdSetDestinationToGalaxyCenter: connection {sender?.connectionId} authorized, setting destination on fleet '{name}'.");
            FleetData.Destination = FleetManager.Instance?.GalaxyCenter;
        }

        [Command(requiresAuthority = false)]
        private void CmdSetDestinationToStarSystem(string starSysName, NetworkConnectionToClient sender = null)
        {
            if (!IsSenderAuthorizedForThisFleet(sender))
            {
                Debug.LogWarning($"CmdSetDestinationToStarSystem: connection {sender?.connectionId} is not authorized to command fleet '{name}'.");
                return;
            }
            StarSysController targetSys = StarSysManager.Instance.GetStarSysControllerByName(starSysName);
            if (targetSys == null)
            {
                Debug.LogWarning($"CmdSetDestinationToStarSystem: no star system named '{starSysName}' found server-side (possible galaxy-generation divergence between client and server).");
                return;
            }
            Debug.Log($"CmdSetDestinationToStarSystem: connection {sender?.connectionId} authorized, setting destination='{starSysName}' on fleet '{name}'.");
            FleetData.Destination = targetSys.gameObject;
        }

        [Command(requiresAuthority = false)]
        private void CmdSetDestinationToFleet(NetworkIdentity targetFleetIdentity, NetworkConnectionToClient sender = null)
        {
            if (!IsSenderAuthorizedForThisFleet(sender))
            {
                Debug.LogWarning($"CmdSetDestinationToFleet: connection {sender?.connectionId} is not authorized to command fleet '{name}'.");
                return;
            }
            if (targetFleetIdentity == null) return;
            Debug.Log($"CmdSetDestinationToFleet: connection {sender?.connectionId} authorized, setting destination=fleet '{targetFleetIdentity.name}' on fleet '{name}'.");
            FleetData.Destination = targetFleetIdentity.gameObject;
        }

        // A player-defined target point is a client-only drag marker with no NetworkIdentity, so it
        // can't be sent as a GameObject/NetworkIdentity Command parameter like the other destination
        // types. Instead we relay its raw world position and hold a lightweight, unspawned marker
        // GameObject server-side purely so FleetData.Destination.transform.position has somewhere to
        // read from - the server never needs to render this marker, only use its position.
        private GameObject serverPlayerTargetMarker;

        // Destroys the server-only marker (if any) so it doesn't outlive the destination it stood
        // in for. Previously only OnDestroy() did this - i.e. only when the fleet itself died - so
        // every other way of clearing a player-defined-target destination (arriving normally, or
        // CmdCancelDestination/ClickCancelDestinationButton canceling one) left an orphaned
        // "ServerPlayerTargetMarker_<fleet>" GameObject sitting under GalaxyCenter forever.
        // Deliberately NOT [Server]-attributed: OnDestroy() below calls this unconditionally on
        // every machine's copy of a destroyed fleet, server or client - on a client,
        // serverPlayerTargetMarker is always null (only CmdSetDestinationToPlayerTarget, a
        // server-only Command body, ever assigns it), so this is already a harmless no-op there;
        // a [Server] guard would just add a spurious warning log to every client's OnDestroy.
        private void ServerClearPlayerTargetMarker()
        {
            if (serverPlayerTargetMarker != null)
            {
                Destroy(serverPlayerTargetMarker);
                serverPlayerTargetMarker = null;
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdSetDestinationToPlayerTarget(Vector3 targetPosition, NetworkConnectionToClient sender = null)
        {
            if (!IsSenderAuthorizedForThisFleet(sender))
            {
                Debug.LogWarning($"CmdSetDestinationToPlayerTarget: connection {sender?.connectionId} is not authorized to command fleet '{name}'.");
                return;
            }
            if (serverPlayerTargetMarker == null)
            {
                serverPlayerTargetMarker = new GameObject($"ServerPlayerTargetMarker_{name}");
                if (FleetManager.Instance?.GalaxyCenter != null)
                    serverPlayerTargetMarker.transform.SetParent(FleetManager.Instance.GalaxyCenter.transform, true);
            }
            serverPlayerTargetMarker.transform.position = targetPosition;
            Debug.Log($"CmdSetDestinationToPlayerTarget: connection {sender?.connectionId} authorized, setting destination=player-defined target at {targetPosition} on fleet '{name}'.");
            FleetData.Destination = serverPlayerTargetMarker;
        }

        // ── Encounter/diplomacy/combat networking ─────────────────────────────
        // DiplomacyManager and DiplomacyController are plain, unnetworked per-client singletons/
        // MonoBehaviours - each client builds its own local DiplomacyController/UI from its own
        // local-player perspective (see DiplomacyManager.OpenDiplomacyUI). GalaxyEncounterQueue is
        // only ever drained on the server (TimeManager's TimeProgression coroutine returns early on
        // non-host clients), so resolving an encounter there previously only ever updated the host's
        // own local DiplomacyManager - Player 2 never got told. These Rpc/Command pairs broadcast the
        // server's encounter/combat decisions to every client so each one runs its own local
        // resolution/UI-open logic.

        [Server]
        public void ServerNotifyFleetVsFleetEncounter(FleetController otherFleet)
        {
            NetworkIdentity otherIdentity = otherFleet != null ? otherFleet.GetComponent<NetworkIdentity>() : null;
            if (otherIdentity == null) return;
            RpcFleetVsFleetEncounter(otherIdentity);
        }

        [ClientRpc]
        private void RpcFleetVsFleetEncounter(NetworkIdentity otherFleetIdentity)
        {
            FleetController otherFleetCon = otherFleetIdentity != null ? otherFleetIdentity.GetComponent<FleetController>() : null;
            if (otherFleetCon == null) return;

            // ClientRpc reaches every connected client, not just the two combatants - a bystander
            // civ (no ships in this encounter) has no business having its Diplomacy UI opened. The
            // host is the one exception: DiplomacyController.DoAIDiplomacy's AI Fight/Withdraw
            // decision (and the unresponsive-human timeout fallback) only ever runs where
            // NetworkServer.active is true, but DiplomacyManager is an unnetworked per-client
            // singleton - if the host bailed out here as a bystander, no DiplomacyController for
            // this pair would ever exist anywhere, and an AI-vs-AI (or AI-owned-system) encounter
            // with neither party played by the host would freeze forever with nothing to resolve
            // it. The host still processes headlessly here; UI only opens for whichever side is
            // actually the local player (see InstantiateDiplomacyUIGameObject/DoAIDiplomacy).
            if (!IsLocalPlayerACombatant(FleetData.CivEnum, otherFleetCon.FleetData.CivEnum) && !NetworkServer.active) return;

            FleetUI?.MoveBackAnyaFleetUIGO();
            DiplomacyManager.Instance.FleetControllerVsOtherCivFleet(this, otherFleetCon);
        }

        [Server]
        public void ServerNotifyFleetVsSystemEncounter(StarSysController sysCon)
        {
            if (sysCon == null || sysCon.StarSysData == null) return;
            RpcFleetVsSystemEncounter(sysCon.StarSysData.GetSysName());
        }

        [ClientRpc]
        private void RpcFleetVsSystemEncounter(string starSysName)
        {
            StarSysController sysCon = StarSysManager.Instance.GetStarSysControllerByName(starSysName);
            if (sysCon == null) return;

            // Same bystander/host guard as RpcFleetVsFleetEncounter above.
            if (!IsLocalPlayerACombatant(FleetData.CivEnum, sysCon.StarSysData.CurrentOwnerCivEnum) && !NetworkServer.active) return;

            FleetUI?.MoveBackAnyaFleetUIGO();
            DiplomacyManager.Instance.ResolveEncounterOtherCivSystem(this, sysCon);
        }

        // Entry point called from DiplomacyController.Combat() - either combatant's client can click
        // Combat from their own Diplomacy window, so this must reach the server regardless of which
        // client (host or non-host) called it.
        public void RequestStartCombat(FleetController otherFleetCon, StarSysController sysCon)
        {
            if (isServer)
            {
                ServerStartCombat(otherFleetCon, sysCon);
                return;
            }

            NetworkIdentity otherIdentity = otherFleetCon != null ? otherFleetCon.GetComponent<NetworkIdentity>() : null;
            string sysName = sysCon != null && sysCon.StarSysData != null ? sysCon.StarSysData.GetSysName() : null;
            CmdRequestStartCombat(otherIdentity, sysName);
        }

        [Command(requiresAuthority = false)]
        private void CmdRequestStartCombat(NetworkIdentity otherFleetIdentity, string starSysName, NetworkConnectionToClient sender = null)
        {
            LocalHumanPlayerController playerCon = sender?.identity != null ? sender.identity.GetComponent<LocalHumanPlayerController>() : null;
            if (playerCon == null)
            {
                Debug.LogWarning($"CmdRequestStartCombat: connection {sender?.connectionId} has no LocalHumanPlayerController.");
                return;
            }

            FleetController otherFleetCon = otherFleetIdentity != null ? otherFleetIdentity.GetComponent<FleetController>() : null;
            StarSysController sysCon = !string.IsNullOrEmpty(starSysName) ? StarSysManager.Instance.GetStarSysControllerByName(starSysName) : null;

            // Either combatant's own client may trigger combat: the attacking fleet (this), the
            // defending fleet (otherFleetCon), or the owner of the defending star system (sysCon).
            bool controlsThis = FleetData != null && playerCon.PlayerCiv == FleetData.CivEnum;
            bool controlsOther = otherFleetCon != null && otherFleetCon.FleetData != null && playerCon.PlayerCiv == otherFleetCon.FleetData.CivEnum;
            bool controlsSystem = sysCon != null && sysCon.StarSysData != null && playerCon.PlayerCiv == sysCon.StarSysData.CurrentOwnerCivEnum;

            if (!controlsThis && !controlsOther && !controlsSystem)
            {
                Debug.LogWarning($"CmdRequestStartCombat: connection {sender?.connectionId} (civ {playerCon.PlayerCiv}) is not a combatant in this encounter - request ignored.");
                return;
            }

            ServerStartCombat(otherFleetCon, sysCon);
        }

        [Server]
        private void ServerStartCombat(FleetController otherFleetCon, StarSysController sysCon)
        {
            NetworkIdentity otherIdentity = otherFleetCon != null ? otherFleetCon.GetComponent<NetworkIdentity>() : null;
            string sysName = sysCon != null && sysCon.StarSysData != null ? sysCon.StarSysData.GetSysName() : null;
            RpcStartCombat(otherIdentity, sysName);

            // RpcStartCombat above is a ClientRpc - Mirror only runs it on machines with an active
            // local client. A true dedicated server (NetworkServer.active, no NetworkClient.active -
            // see PersistentSceneBootstrap.StartDedicatedServer) has none, so it never loads
            // CombatScene and never gets its own CombatController/TurnBasedCombatResolver. Without
            // that, ServerSubmitCombatOrder below can never find an active combat to resolve into,
            // so every order submission - human or AI - silently no-ops and combat freezes forever.
            // A dedicated server must simulate every combat in the world regardless of which civs
            // are involved (it has no "local player" to filter by, unlike RpcStartCombat's
            // IsLocalPlayerACombatant bystander check), so trigger it directly here instead.
            if (NetworkServer.active && !NetworkClient.active)
            {
                SceneController.Instance.LoadCombatScene(this, otherFleetCon, sysCon);
            }
        }

        [ClientRpc]
        private void RpcStartCombat(NetworkIdentity otherFleetIdentity, string starSysName)
        {
            FleetController otherFleetCon = otherFleetIdentity != null ? otherFleetIdentity.GetComponent<FleetController>() : null;
            StarSysController sysCon = !string.IsNullOrEmpty(starSysName) ? StarSysManager.Instance.GetStarSysControllerByName(starSysName) : null;

            CivEnum otherCiv = otherFleetCon != null ? otherFleetCon.FleetData.CivEnum : sysCon.StarSysData.CurrentOwnerCivEnum;

            // Bystander civs (no ships in this fight) stay on the Galaxy map instead of being
            // dragged into the Combat scene - see CmdRequestStartCombat's combatant check, which
            // this mirrors client-side. They just get a lightweight notice instead.
            if (!IsLocalPlayerACombatant(FleetData.CivEnum, otherCiv))
            {
                CombatPausedNoticeUI.Instance?.Show(FleetData.CivEnum, otherCiv);
                return;
            }

            SceneController.Instance.LoadCombatScene(this, otherFleetCon, sysCon);
        }

        // Shared by the encounter/combat Rpcs above: is our local human player one of the two
        // civs actually involved, or just a bystander who happens to also be a connected client?
        private static bool IsLocalPlayerACombatant(CivEnum civA, CivEnum civB)
        {
            return GameController.Instance != null &&
                   (GameController.Instance.AreWeLocalPlayer(civA) || GameController.Instance.AreWeLocalPlayer(civB));
        }

        // Entry point called from DiplomacyController.SetResponse for a human player's Fight/
        // Withdraw click - mirrors RequestStartCombat's relay pattern. isSideOne means "this
        // response belongs to the DiplomacyData.CivOne/FleetControllerCivOne party" and is passed
        // through as plain data, not inferred from "this" fleet, which is only the transport.
        public void RequestSetEncounterResponse(bool isSideOne, DiplomacyData.EncounterResponse response, FleetController otherFleetCon, StarSysController sysCon)
        {
            if (isServer)
            {
                ServerSetEncounterResponse(isSideOne, response, otherFleetCon, sysCon);
                return;
            }

            NetworkIdentity otherIdentity = otherFleetCon != null ? otherFleetCon.GetComponent<NetworkIdentity>() : null;
            string sysName = sysCon != null && sysCon.StarSysData != null ? sysCon.StarSysData.GetSysName() : null;
            CmdSetEncounterResponse(isSideOne, response, otherIdentity, sysName);
        }

        [Command(requiresAuthority = false)]
        private void CmdSetEncounterResponse(bool isSideOne, DiplomacyData.EncounterResponse response, NetworkIdentity otherFleetIdentity, string starSysName, NetworkConnectionToClient sender = null)
        {
            LocalHumanPlayerController playerCon = sender?.identity != null ? sender.identity.GetComponent<LocalHumanPlayerController>() : null;
            if (playerCon == null)
            {
                Debug.LogWarning($"CmdSetEncounterResponse: connection {sender?.connectionId} has no LocalHumanPlayerController.");
                return;
            }

            if (FleetData == null || playerCon.PlayerCiv != FleetData.CivEnum)
            {
                Debug.LogWarning($"CmdSetEncounterResponse: connection {sender?.connectionId} (civ {playerCon.PlayerCiv}) does not control this fleet - request ignored.");
                return;
            }

            FleetController otherFleetCon = otherFleetIdentity != null ? otherFleetIdentity.GetComponent<FleetController>() : null;
            StarSysController sysCon = !string.IsNullOrEmpty(starSysName) ? StarSysManager.Instance.GetStarSysControllerByName(starSysName) : null;
            ServerSetEncounterResponse(isSideOne, response, otherFleetCon, sysCon);
        }

        // Also called directly (no Cmd/human sender) from DiplomacyController.DoAIDiplomacy for an
        // AI-controlled side, since there's no LocalHumanPlayerController to authorize a Cmd for -
        // the server already has full authority over every spawned object, AI-owned or not.
        [Server]
        public void ServerSetEncounterResponse(bool isSideOne, DiplomacyData.EncounterResponse response, FleetController otherFleetCon, StarSysController sysCon)
        {
            NetworkIdentity otherIdentity = otherFleetCon != null ? otherFleetCon.GetComponent<NetworkIdentity>() : null;
            string sysName = sysCon != null && sysCon.StarSysData != null ? sysCon.StarSysData.GetSysName() : null;
            RpcSetEncounterResponse(isSideOne, response, otherIdentity, sysName);
        }

        [ClientRpc]
        private void RpcSetEncounterResponse(bool isSideOne, DiplomacyData.EncounterResponse response, NetworkIdentity otherFleetIdentity, string starSysName)
        {
            FleetController otherFleetCon = otherFleetIdentity != null ? otherFleetIdentity.GetComponent<FleetController>() : null;
            StarSysController sysCon = !string.IsNullOrEmpty(starSysName) ? StarSysManager.Instance.GetStarSysControllerByName(starSysName) : null;
            CivEnum otherCiv = otherFleetCon != null ? otherFleetCon.FleetData.CivEnum
                : (sysCon != null ? sysCon.StarSysData.CurrentOwnerCivEnum : FleetData.CivEnum);

            // Same bystander/host guard as RpcFleetVsFleetEncounter above - the host must still
            // apply the authoritative response to its own headless DiplomacyController even when
            // it isn't a combatant, so TryResolveEncounter's Fight/Withdraw side effects actually
            // fire server-side.
            if (!IsLocalPlayerACombatant(FleetData.CivEnum, otherCiv) && !NetworkServer.active) return;

            DiplomacyController diploCon = DiplomacyManager.Instance?.ReturnADiplomacyController(FleetData.CivEnum, otherCiv);
            diploCon?.SetResponse(isSideOne, response, false);
        }

        [Server]
        public void ServerNotifyCombatEnded(CivEnum civA, CivEnum civB)
        {
            RpcCombatEnded(civA, civB);
        }

        [ClientRpc]
        private void RpcCombatEnded(CivEnum civA, CivEnum civB)
        {
            // Unconditional, first line, before anything that could throw or short-circuit below -
            // if this is missing from a peer's log entirely, the Rpc never arrived/executed on that
            // peer at all (rules out every branch below as the cause).
            Debug.Log($"📩[RpcCombatEndedDiag] RpcCombatEnded RECEIVED on '{name}' for {civA} vs {civB} (isServer={isServer}, isClient={isClient}, isOwned={isOwned}).");

            // Bystanders who were shown the paused notice above hide it again.
            CombatPausedNoticeUI.Instance?.Hide();

            // Combatants tear down their own Combat scene view here. The server already ran
            // CombatController.EndCombat() (that's what triggered this Rpc, via
            // ServerNotifyCombatEnded) - EndCombat's own idempotency guard makes it safe to call
            // again from the host's own client connection. Non-host combatants haven't run it yet,
            // so this is their only trigger to leave the Combat scene.
            //
            // Look up by "this" fleet (the networked FleetController this Rpc is running on -
            // ServerNotifyCombatEnded is always called on combatEndedAnchor, one of the combat's
            // own involved fleets), NOT by civ pair. Civ-pair matching broke under back-to-back
            // same-civ-pair test battles: if a second FED-vs-KLING combat's scene finished loading
            // on a client before the first FED-vs-KLING combat's delayed end-of-combat broadcast
            // (ShowCombatEndSequence has ~2.5s of WaitForSecondsRealtime before EndCombat() even
            // runs) arrived, GetActiveCombatControllerForCivs(FED, KLING) would match whichever
            // combat happened to be first from that ambiguous, non-unique civ-pair search - which
            // could be the brand new combat instead of the one that actually ended, tearing down a
            // fight that had just started. A fleet can only be a combatant in one active combat at
            // a time, so matching on "this" fleet's identity is unambiguous.
            CombatController combatCon = CombatManager.Instance?.GetActiveCombatControllerForFleet(this);
            if (combatCon == null)
            {
                Debug.LogWarning($"⚠️[RpcCombatEndedDiag] RpcCombatEnded received for {civA} vs {civB} but no matching active CombatController found for anchor fleet '{name}' (CombatManager.Instance null={CombatManager.Instance == null}) - this client's Combat scene will NOT be torn down.");
            }
            else
            {
                Debug.Log($"✅[RpcCombatEndedDiag] RpcCombatEnded received for {civA} vs {civB} - found combat controller, calling EndCombat().");
            }
            combatCon?.EndCombat();

            // DiplomacyData.CombatIntiated is a one-shot latch set by DiplomacyController.Combat()
            // to stop the Combat button from double-firing while combat is loading/running - it's
            // never cleared elsewhere, so without this reset a civ pair could only ever fight once
            // per game, and the Combat button would silently do nothing on any later encounter.
            if (DiplomacyManager.Instance != null)
            {
                DiplomacyController diploCon = DiplomacyManager.Instance.ReturnADiplomacyController(civA, civB);
                if (diploCon != null && diploCon.DiplomacyData != null)
                    diploCon.DiplomacyData.CombatIntiated = false;
            }
        }

        // ---------------------------------------------------------------------------------------
        // Turn-based combat order submission - CombatUIManager/TurnBasedCombatResolver aren't
        // NetworkBehaviours, so they route through whichever FleetController belongs to the local
        // player's own fleet in the active combat (see CombatController.GetInvolvedFleetAnchor).
        // ---------------------------------------------------------------------------------------

        public void RequestSubmitCombatOrder(CombatOrders order)
        {
            if (isServer)
            {
                ServerSubmitCombatOrder(GameController.Instance.GetOurCiv(), order);
                return;
            }
            CmdSubmitCombatOrder(order);
        }

        [Command(requiresAuthority = false)]
        private void CmdSubmitCombatOrder(CombatOrders order, NetworkConnectionToClient sender = null)
        {
            LocalHumanPlayerController playerCon = sender?.identity != null ? sender.identity.GetComponent<LocalHumanPlayerController>() : null;
            if (playerCon == null)
            {
                Debug.LogWarning($"CmdSubmitCombatOrder: connection {sender?.connectionId} has no LocalHumanPlayerController.");
                return;
            }
            ServerSubmitCombatOrder(playerCon.PlayerCiv, order);
        }

        [Server]
        private void ServerSubmitCombatOrder(CivEnum civ, CombatOrders order)
        {
            CombatController combatCon = CombatManager.Instance?.GetActiveCombatControllerForCiv(civ);
            if (combatCon == null || combatCon.TurnResolver == null)
            {
                Debug.LogWarning($"ServerSubmitCombatOrder: no active combat found for civ {civ} - order ignored.");
                return;
            }
            combatCon.TurnResolver.ServerSubmitOrder(civ, order);
        }

        [Server]
        public void ServerBroadcastOrderLocked(int side)
        {
            RpcOrderLocked(side);
        }

        [ClientRpc]
        private void RpcOrderLocked(int side)
        {
            CombatUIManager.Instance?.OnOrderLocked(side);
        }

        [Server]
        public void ServerBroadcastOrdersResolved(CombatOrders sideOne, CombatOrders sideTwo)
        {
            RpcOrdersResolved(sideOne, sideTwo);

            // A pure dedicated server (StartServer() only, no local NetworkClient - see
            // PersistentSceneBootstrap.StartDedicatedServer) never receives its own ClientRpc:
            // Rpc bodies only run on machines with an actual NetworkClient connection, which a
            // headless server doesn't have. Without this, the server's own TurnResolver never
            // advances past this turn (ApplyResolvedOrdersAndResolve is otherwise only reachable
            // via RpcOrdersResolved above), so it can never detect combat-over or call
            // CombatController.EndCombat() - leaving every real client stuck forever on its own
            // locally-simulated victory panel, waiting for a resolution that never comes.
            // CombatUIManager.Instance.CurrentCombatController is NOT usable here - it's only ever
            // set by CombatManager.SetUpLocalPlayer's SetupForCombat() path, which deliberately
            // skips itself entirely on a dedicated server (see CombatManager.SetUpLocalPlayer).
            // Use the same UI-independent lookup ServerSubmitCombatOrder already uses instead.
            // Skipped when NetworkClient.active (host mode) since the loopback client already
            // delivers the Rpc above - calling this a second time here would double-resolve the turn.
            if (!NetworkClient.active)
            {
                var resolver = CombatManager.Instance?.GetActiveCombatControllerForFleet(this)?.TurnResolver;
                resolver?.ApplyResolvedOrdersAndResolve(sideOne, sideTwo);
            }
        }

        [ClientRpc]
        private void RpcOrdersResolved(CombatOrders sideOne, CombatOrders sideTwo)
        {
            CombatUIManager.Instance?.CurrentCombatController?.TurnResolver?.ApplyResolvedOrdersAndResolve(sideOne, sideTwo);
        }

        [Server]
        public void ServerBroadcastCombatPhaseChanged(CombatPhase phase)
        {
            RpcCombatPhaseChanged(phase);
        }

        [ClientRpc]
        private void RpcCombatPhaseChanged(CombatPhase phase)
        {
            CombatUIManager.Instance?.OnCombatPhaseChanged(phase);
        }

        // ---------------------------------------------------------------------------------------
        // Ship-outcome reconciliation. Each client redundantly simulates weapon fire/damage locally
        // (see TurnBasedCombatResolver/ShipController) so mid-combat HP can drift slightly between
        // the two combatants' clients, but the decisions below (a ship dies / is captured / is
        // scuttled) are re-broadcast from whichever client made the call while NetworkServer.active,
        // and applied idempotently everywhere via ShipController.ApplyXFromServer so both clients'
        // final outcome (who survived) converges even if the exact HP trace didn't.
        // ---------------------------------------------------------------------------------------

        [Server]
        public void ServerBroadcastShipDestroyed(int shipID)
        {
            RpcShipDestroyed(shipID);
            CombatManager.Instance?.GetActiveCombatControllerForCiv(FleetData.CivEnum)?.TurnResolver?.ServerCheckCombatOverAfterReconciliation();
        }

        [ClientRpc]
        private void RpcShipDestroyed(int shipID)
        {
            CombatManager.Instance?.GetActiveCombatControllerForCiv(FleetData.CivEnum)?.GetShipByID(shipID)?.ApplyDestroyedFromServer();
        }

        [Server]
        public void ServerBroadcastShipCaptured(int shipID)
        {
            RpcShipCaptured(shipID);
            CombatManager.Instance?.GetActiveCombatControllerForCiv(FleetData.CivEnum)?.TurnResolver?.ServerCheckCombatOverAfterReconciliation();
        }

        [ClientRpc]
        private void RpcShipCaptured(int shipID)
        {
            CombatManager.Instance?.GetActiveCombatControllerForCiv(FleetData.CivEnum)?.GetShipByID(shipID)?.ApplyCapturedFromServer();
        }

        [Server]
        public void ServerBroadcastShipScuttled(int shipID)
        {
            RpcShipScuttled(shipID);
        }

        [ClientRpc]
        private void RpcShipScuttled(int shipID)
        {
            CombatManager.Instance?.GetActiveCombatControllerForCiv(FleetData.CivEnum)?.GetShipByID(shipID)?.ApplyScuttledFromServer();
        }

        // ---------------------------------------------------------------------------------------
        // Non-host report path for the reconciliation above. Each client's damage simulation can
        // reach a kill/capture decision before the host's does (or the host's simulation may never
        // reach it at all - see ResolveTurn/IsCombatOver), which previously left the host's combat
        // stuck waiting on its own local outcome forever. These let a non-host client that detects
        // a kill/capture ask the server to adopt it immediately, so the server's ship-alive state
        // (and thus IsCombatOver) converges regardless of which client noticed first.
        // ---------------------------------------------------------------------------------------

        public void RequestReportShipDestroyed(int shipID)
        {
            if (isServer)
            {
                ServerBroadcastShipDestroyed(shipID);
                return;
            }
            CmdReportShipDestroyed(shipID);
        }

        [Command(requiresAuthority = false)]
        private void CmdReportShipDestroyed(int shipID)
        {
            ServerBroadcastShipDestroyed(shipID);
        }

        public void RequestReportShipCaptured(int shipID)
        {
            if (isServer)
            {
                ServerBroadcastShipCaptured(shipID);
                return;
            }
            CmdReportShipCaptured(shipID);
        }

        [Command(requiresAuthority = false)]
        private void CmdReportShipCaptured(int shipID)
        {
            ServerBroadcastShipCaptured(shipID);
        }

        public void GetPlayerDefinedTargetDestination(FleetController fleetCon)
        {
            if (fleetCon == null || fleetCon.FleetUIGameObject == null) return;

            // ✅ CRITICAL: Tell GalaxyMenuUIController which fleet is looking for destination
            var galaxyUI = GalaxyMenuUIController.Instance;
            if (galaxyUI != null)
            {
                galaxyUI.BeginSetDestination(fleetCon);
            }

            // Get buttons from the specific fleet's UI
            var fields = fleetCon.FleetUIGameObject.GetComponent<FleetUI_Fields>();
            if (fields != null)
            {
                if (fields.DestinationDragTarget != null)
                    fields.DestinationDragTarget.gameObject.SetActive(false);
                if (fields.CancelDestination != null)
                    fields.CancelDestination.gameObject.SetActive(true);
                if (fields.SelectDestination != null)
                    fields.SelectDestination.gameObject.SetActive(true);
            }

            MousePointerChanger.Instance.SetDestinationCursor();

            // ✅ Create the PlayerDefinedTarget
            PlayerDefinedTargetManager.Instance.PlayerTargetFromData(fleetCon.gameObject);
        }
    }
}

