// Ignore Spelling: Unregister

using BOTF3D.Combat;
using BOTF3D.Core;

using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Core
{
    [System.Serializable]
    public struct RosterEntry
    {
        public int PlayerId;
        public string PlayerName;
        public CivEnum PlayerCiv;
        public PlayerType PlayerType;
    }

    public class PlayerManager : NetworkBehaviour, IManager
    {
        public void Initialize() { }
        public void Cleanup() { }
        public static PlayerManager Instance;

    public LocalHumanPlayerController LocalPlayerController { get; private set; }
    public IPlayerController LocalPlayer { get; private set; }
    public readonly List<IPlayerController> AllPlayerControllers = new List<IPlayerController>();

    // Server-authoritative lobby roster, replicated to every client for the multiplayer lobby/status panel.
    public readonly SyncList<RosterEntry> Roster = new SyncList<RosterEntry>();

    // Who currently has host-equivalent authority (sees/uses the host-only lobby controls and
    // may trigger ServerBroadcastStartGame) - distinct from NetworkServer.active, which is only
    // true on the literal machine running the server process. On a true dedicated server
    // (StartServer() only, no local player - see PersistentSceneBootstrap.StartDedicatedServer)
    // nobody has NetworkServer.active locally, so one connected client needs to stand in as game
    // master instead. -1 = unclaimed.
    [SyncVar] public int HostAuthorityPlayerId = -1;

    // RENAMED: PlayerData → PlayerInfo
    public List<GamePlayerInfo> PlayerInfoList { get; private set; } = new List<GamePlayerInfo>();

    private List<CivEnum> civEnumsForPlayerCons = new List<CivEnum>();

    private void Awake()
    {
            ServiceLocator.Register<PlayerManager>(this);
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void NetworkStatus()
    {
        if (NetworkServer.active && NetworkClient.active)
            Debug.Log("Host");
        else if (NetworkServer.active)
            Debug.Log("Dedicated server");
        else if (NetworkClient.active)
            Debug.Log("Client");
        else
            Debug.Log("Single Player (no network)");
    }

    // Same 7 majors as ClientRosterPanelUIController.SelectableCivs - kept in this order so the
    // first free slot a new player is assigned matches the order civs appear in the roster dropdown.
    private static readonly CivEnum[] AssignableCivs =
    {
        CivEnum.FED, CivEnum.ROM, CivEnum.KLING, CivEnum.CARD, CivEnum.DOM, CivEnum.BORG, CivEnum.TERRAN
    };

    // Returns the assigned civ so callers (LocalHumanPlayerController/AiPlayerController) can set
    // their own authoritative civ field to match - otherwise a new player's [SyncVar] playerCiv
    // stays at its own default (FED) even though the roster entry says otherwise, and the two
    // desync the moment anything reads the controller's civ directly instead of the roster.
    public CivEnum RegisterPlayer(IPlayerController controller, bool isLocalPlayerArg, string playerName, int playerId, PlayerType playerType)
    {
        CivEnum assignedCiv = GetFirstAvailableCiv(playerId);

        GamePlayerInfo playerData = new GamePlayerInfo(playerName)
        {
            PlayerId = playerId,
            PlayerType = playerType,
            PlayerCiv = assignedCiv
        };

        PlayerInfoList.Add(playerData);
        Roster.Add(new RosterEntry { PlayerId = playerId, PlayerName = playerName, PlayerCiv = assignedCiv, PlayerType = playerType });
        Debug.Log($"PlayerManager: Registered {playerType} player '{playerName}' (ID: {playerId}) with civ {assignedCiv}");
        return assignedCiv;
    }

    // Picks the first civ (in AssignableCivs order) not already held by another connected player,
    // so a newly-joined player doesn't collide with an existing player's civ pick (e.g. host on FED).
    private CivEnum GetFirstAvailableCiv(int excludingPlayerId)
    {
        for (int i = 0; i < AssignableCivs.Length; i++)
        {
            if (!IsCivTakenByAnotherPlayer(AssignableCivs[i], excludingPlayerId))
                return AssignableCivs[i];
        }
        return AssignableCivs[0]; // all 7 taken (more players than majors) - fall back, server already rejects the duplicate elsewhere
    }

    public void UnregisterPlayer(int playerID)
    {
        if (AllPlayerControllers != null)
        {
            for (int i = 0; i < AllPlayerControllers.Count; i++)
            {
                if (AllPlayerControllers[i].PlayerInfo.PlayerId == playerID)
                {
                    AllPlayerControllers.RemoveAt(i);
                    break;
                }
            }
        }

        // SyncList writes require an active server/client - during shutdown (e.g. this being
        // called from LocalHumanPlayerController.OnDestroy() after NetworkServer/NetworkClient
        // have already stopped), the whole Roster is being torn down anyway, so skip the mutation
        // rather than let SyncList throw.
        if (NetworkServer.active || NetworkClient.active)
        {
            for (int i = 0; i < Roster.Count; i++)
            {
                if (Roster[i].PlayerId == playerID)
                {
                    Roster.RemoveAt(i);
                    break;
                }
            }
        }

        Debug.Log($"UnregisterPlayer: Processed unregister for player ID {playerID}");
    }

    // Server-authoritative check used by CmdSetPlayerCiv to reject a civ pick already held by
    // a different connected player, so two clients can never end up on the same civilization.
    public bool IsCivTakenByAnotherPlayer(CivEnum civ, int requestingPlayerId)
    {
        for (int i = 0; i < Roster.Count; i++)
        {
            if (Roster[i].PlayerCiv == civ && Roster[i].PlayerId != requestingPlayerId)
                return true;
        }
        return false;
    }

    public void UpdateRosterEntry(int playerId, string playerName, CivEnum civ)
    {
        for (int i = 0; i < Roster.Count; i++)
        {
            if (Roster[i].PlayerId == playerId)
            {
                RosterEntry entry = Roster[i];
                entry.PlayerName = playerName;
                entry.PlayerCiv = civ;
                Roster[i] = entry;
                return;
            }
        }
    }

    public void SetLocalPlayerController(LocalHumanPlayerController controller)
    {
        LocalPlayerController = controller;
    }

    public void AddLocalPlayer(GamePlayerInfo data)
    {
        PlayerInfoList.Add(data);
    }

    public void RemoveLocalPlayer(GamePlayerInfo data)
    {
        PlayerInfoList.Remove(data);
    }

    public void AddPlayer(GamePlayerInfo player)
    {
        if (!PlayerInfoList.Contains(player))
        {
            PlayerInfoList.Add(player);
        }
    }

    public void RemovePlayer(GamePlayerInfo player)
    {
        if (PlayerInfoList.Contains(player))
        {
            PlayerInfoList.Remove(player);
        }
    }

    public GamePlayerInfo GetPlayerById(int playerId)
    {
        return PlayerInfoList.Find(player => player.PlayerId == playerId);
    }

    internal void GetCivsInGameAsGalaxyIsBuilt(List<CivSO> civSOsInGame)
    {
        for (int i = 0; i < civSOsInGame.Count; i++)
        {
            civEnumsForPlayerCons.Add(civSOsInGame[i].CivEnum);
        }
    }

    public void ResetPlayerList()
    {
        if (PlayerInfoList != null)
            PlayerInfoList.Clear();
    }

    public void AssignGoToPlayer(GameObject unitGO, int playerId)
    {
        GamePlayerInfo player = GetPlayerById(playerId);
        if (player != null)
            player.OwnedGameObjects.Add(unitGO);

        StarSysController starSysCon = unitGO.GetComponent<StarSysController>();
        if (starSysCon != null)
        {
            starSysCon.StarSysData.PlayerId = playerId;
        }

        FleetController fleetCon = unitGO.GetComponent<FleetController>();
        if (fleetCon != null)
        {
            fleetCon.FleetData.PlayerId = playerId;
        }

        ShipController shipCon = unitGO.GetComponent<ShipController>();
        if (shipCon != null)
        {
            shipCon.ShipData.PlayerId = playerId;
        }
    }

    private int GetAssignedConnectionIdForCiv(CivEnum civEnum)
    {
        return 0;
    }

    internal void SetMajorCivsInGameForMultiPlayer(List<CivEnum> majorCivsInGameList, CivEnum localPlayerCiv)
    {
        Debug.Log("SetMajorCivsInGameForMultiPlayer: Multiplayer setup pending implementation");
    }

    // Set once the galaxy has actually been generated for this server's lifetime - guards against
    // CmdRequestStartGame/LoadGalaxyScene being invoked a second time (e.g. a reconnecting player's
    // client clicking "Start Game" again, since there's no "rejoin in-progress game" flow yet).
    // Without this, ServerLoadGalaxyAndGenerate reseeds and reruns CivManager/StarSysManager
    // generation from scratch, producing a second differently-randomized civ list layered on top
    // of the first (duplicate systems, fleet numbering never resetting) and crashing
    // FleetManager.GetNewFleetInt with a KeyNotFoundException for any civ that only exists in the
    // second pass (e.g. TALAXIANS) - and RpcStartGame below would also make every ALREADY-connected
    // client redundantly rerun its own local CivManager.UpdatePlayableCivGameList/OnNewGameButtonClicked.
    private bool galaxyGenerationStarted = false;

    // Host-only entry point (called from MainMenuUIController.LoadGalaxyScene, which only the host
    // can reach). Broadcasts the host's chosen game parameters - including a shared RNG seed - to
    // every connected client so galaxy generation (CivManager/StarSysManager, both driven by the
    // global UnityEngine.Random) produces an identical result on every machine.
    [Server]
    public void ServerBroadcastStartGame(int galaxySize, int techLevel, int galaxyType, int seed, bool isSinglePlayer)
    {
        if (galaxyGenerationStarted)
        {
            Debug.LogWarning("ServerBroadcastStartGame: galaxy already generated for this server - ignoring duplicate start-game request (likely a reconnecting client).");
            return;
        }
        galaxyGenerationStarted = true;

        // RpcStartGame below only runs on machines genuinely acting as a client (including a
        // host's own loopback client). A true dedicated server (StartServer() only, no local
        // player) never receives its own ClientRpc, so CivManager/StarSysManager never build
        // their civ/system data there - leaving StarSysManager.Instance's system list empty and
        // NRE'ing the first Command that touches it (e.g. CmdCreateFleetFromSystem). Run the same
        // generation directly here when there's no local client to receive the Rpc.
        if (!NetworkClient.active)
        {
            Debug.Log("[DEDICATED-SERVER-GEN-v2] ServerBroadcastStartGame: no local client detected - starting server-side GalaxyScene load + generation.");
            StartCoroutine(ServerLoadGalaxyAndGenerate(galaxySize, techLevel, galaxyType, seed, isSinglePlayer));
        }

        RpcStartGame(galaxySize, techLevel, galaxyType, seed, isSinglePlayer);
    }

    // PersistentSceneBootstrap.StartDedicatedServer only calls NetworkManager.StartServer() - it
    // never loads GalaxyScene. StarSysManager/FleetManager/etc. all live inside GalaxyScene
    // ("No DontDestroyOnLoad - dies with GalaxyScene" - see StarSysManager.Awake) and stay null
    // until it's loaded, so a true dedicated server must load it here before generating, whereas
    // a host's own client already loads it via MainMenuUIController.LoadGalaxySceneCoroutine.
    private IEnumerator ServerLoadGalaxyAndGenerate(int galaxySize, int techLevel, int galaxyType, int seed, bool isSinglePlayer)
    {
        Debug.Log("[DEDICATED-SERVER-GEN-v2] ServerLoadGalaxyAndGenerate: coroutine started.");
        Scene galaxyScene = SceneManager.GetSceneByName("GalaxyScene");
        if (!galaxyScene.IsValid() || !galaxyScene.isLoaded)
        {
            Debug.Log("[DEDICATED-SERVER-GEN-v2] GalaxyScene not yet loaded - loading additively now.");
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("GalaxyScene", LoadSceneMode.Additive);
            while (!asyncLoad.isDone)
                yield return null;
        }
        Debug.Log($"[DEDICATED-SERVER-GEN-v2] GalaxyScene loaded. StarSysManager.Instance null? {StarSysManager.Instance == null}");

        // Same 2-frame settle MainMenuUIController.LoadGalaxySceneCoroutine/GalaxySceneInitializer
        // use after a fresh scene load, so scene-object Awake/Start calls have run before use.
        yield return null;
        yield return null;

        // Seed immediately before generation, same as MainMenuUIController.LoadGalaxySceneCoroutine,
        // so the server's UnityEngine.Random-driven generation matches every client's exactly.
        UnityEngine.Random.InitState(seed);

        // Mirrors MainMenuUIController's hardcoded MainMenuData.InGamePlayableCivList - every
        // client always initializes it to all 7 majors regardless of user choice (the per-civ
        // on/off toggles aren't actually wired to this list), so there's no per-game selection
        // to replicate from the host here.
        List<CivEnum> playableCivs = new List<CivEnum>
        {
            CivEnum.FED, CivEnum.ROM, CivEnum.KLING, CivEnum.CARD,
            CivEnum.DOM, CivEnum.BORG, CivEnum.TERRAN
        };
        CivManager.Instance.UpdatePlayableCivGameList(playableCivs, galaxySize, (GalaxyMapType)galaxyType);

        yield return CivManager.Instance.OnNewGameButtonClicked(
            galaxySize, techLevel, galaxyType, (int)CivEnum.FED, isSinglePlayer);
    }

    [ClientRpc]
    private void RpcStartGame(int galaxySize, int techLevel, int galaxyType, int seed, bool isSinglePlayer)
    {
        MainMenuUIController.Instance?.OnGameStartReceived(galaxySize, techLevel, galaxyType, seed, isSinglePlayer);
    }

    // Called from LocalHumanPlayerController.OnStartServer for every newly-spawned player object -
    // the first connector to find no one holding authority claims it automatically (chosen over an
    // explicit "claim" action so a client just needs to Connect to a dedicated server to become its
    // game master). Harmless when this machine is also the literal host (StartHost()): NetworkServer.active
    // already grants that client full control via MainMenuUIController.ApplyHostOnlyGating, so this
    // SyncVar just tracks it for consistency.
    [Server]
    public void ClaimHostAuthorityIfUnclaimed(int playerId)
    {
        if (HostAuthorityPlayerId == -1)
        {
            HostAuthorityPlayerId = playerId;
            Debug.Log($"PlayerManager: player {playerId} claimed host-equivalent authority (first connector).");
        }
    }

    // Called from LocalHumanPlayerController.OnStopServer. Releases authority rather than
    // reassigning it here - the next player to connect claims it via ClaimHostAuthorityIfUnclaimed above.
    [Server]
    public void ReleaseHostAuthorityIfHeldBy(int playerId)
    {
        if (HostAuthorityPlayerId == playerId)
        {
            HostAuthorityPlayerId = -1;
            Debug.Log($"PlayerManager: player {playerId} disconnected - host-equivalent authority released.");
        }
    }


        private void OnDestroy()
        {
            ServiceLocator.Unregister<PlayerManager>(); }
    }
}