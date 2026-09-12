using BOTF3D.Core;

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Handles ship instantiation, model setup, and initial positioning for combat.
    /// Separates ship setup logic from CombatController.
    /// </summary>
    public class ShipSetupManager : IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        private readonly CombatController combatController;
        private readonly ShipFormationManager formationManager;

        // Weapon and audio prefabs (assigned by CombatController)
        public GameObject SideOneTorpedoPrefab;
        public GameObject SideTwoTorpedoPrefab;
        public GameObject SideOneBeamPrefab;
        public GameObject SideTwoBeamPrefab;
        public AudioClip SideOneBeamFireClip;
        public AudioClip SideTwoBeamFireClip;
        public AudioClip SideOneTorpedoFireClip;
        public AudioClip SideTwoTorpedoFireClip;

        private const int SPACING = 50;

        // Reused across ships to avoid per-instantiation allocation.
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private readonly MaterialPropertyBlock glowPropertyBlock = new MaterialPropertyBlock();

        private static readonly int StencilCompId = Shader.PropertyToID("_StencilComp");
        private static readonly int StencilId = Shader.PropertyToID("_Stencil");
        private static readonly int StencilOpId = Shader.PropertyToID("_StencilOp");
        private static readonly int StencilWriteMaskId = Shader.PropertyToID("_StencilWriteMask");
        private static readonly int StencilReadMaskId = Shader.PropertyToID("_StencilReadMask");
        private readonly MaterialPropertyBlock stencilPropertyBlock = new MaterialPropertyBlock();

        public ShipSetupManager(CombatController controller)
        {
            combatController = controller;
            formationManager = new ShipFormationManager();
        }

        /// <summary>
        /// Main entry point: Setup ships for both sides
        /// </summary>
        public void SetupAllShips()
        {
            Debug.Log("=== Starting Ship Setup ===");

            SetupShips(combatController.CombatData.SideOneShipCons, 1);
            SetupShips(combatController.CombatData.SideTwoShipCons, 2);

            Debug.Log("=== Ship Setup Complete ===");
        }

        /// <summary>
        /// Shifts spiral positions so their integer centroid is (0,0).
        /// Ensures the formation is always centered regardless of ship count.
        /// </summary>
        private static List<Vector2Int> CenterSpiralPositions(List<Vector2Int> positions)
        {
            if (positions == null || positions.Count == 0) return positions;
            float cx = 0f, cy = 0f;
            foreach (var p in positions) { cx += p.x; cy += p.y; }
            cx /= positions.Count;
            cy /= positions.Count;
            int ox = Mathf.RoundToInt(cx);
            int oy = Mathf.RoundToInt(cy);
            if (ox == 0 && oy == 0) return positions;
            var centered = new List<Vector2Int>(positions.Count);
            foreach (var p in positions)
                centered.Add(new Vector2Int(p.x - ox, p.y - oy));
            return centered;
        }

        /// <summary>
        /// Setup ships for one side (combat ships + transports)
        /// </summary>
        private void SetupShips(List<ShipController> shipList, int side)
        {
            Debug.Log($"=== SetupShips Side {side}: {shipList.Count} total ships ===");

            // Separate combat ships, transports, and system-owned assets (stationed ships,
            // orbital batteries, future shields — anything already at the star system being
            // fought over spawns already in place, never warp in; see SetupSingleShipNoWarp)
            List<ShipController> combatShips = shipList
                .Where(s => s != null && s.ShipData != null &&
                            s.ShipData.CurrentStarSysController == null &&
                            s.ShipData.ShipType != ShipType.Transport)
                .ToList();
            List<ShipController> transportShips = shipList
                .Where(s => s != null && s.ShipData != null &&
                            s.ShipData.CurrentStarSysController == null &&
                            s.ShipData.ShipType == ShipType.Transport)
                .ToList();
            List<ShipController> systemShips = shipList
                .Where(s => s != null && s.ShipData != null && s.ShipData.CurrentStarSysController != null)
                .ToList();

            // System Invasion Phase 1 (Docs/Design/SystemInvasion_Phase1_Design.md §3.2,
            // 2026-09-11 revision): Orbital Batteries form a wall between the enemy and the
            // Shipyard, not just another entry in the shared system-ships spiral. Split out so each
            // gets its own dedicated layout below - see SetupOrbitalBatteryWall/SetupShipyards.
            List<ShipController> orbitalBatteryShips = systemShips
                .Where(s => s.ShipData.ShipType == ShipType.OrbitalBattery).ToList();
            List<ShipController> shipyardShips = systemShips
                .Where(s => s.ShipData.ShipType == ShipType.Shipyard).ToList();
            List<ShipController> otherSystemShips = systemShips
                .Where(s => s.ShipData.ShipType != ShipType.OrbitalBattery && s.ShipData.ShipType != ShipType.Shipyard)
                .ToList();

            Debug.Log($"  Side {side}: {combatShips.Count} combat, {transportShips.Count} transports, " +
                      $"{otherSystemShips.Count} other system-owned, {orbitalBatteryShips.Count} OB (wall), {shipyardShips.Count} Shipyard");

            // Generate spiral positions and zero-center them so the formation
            // centroid is always at (0,0) regardless of ship count.
            List<Vector2Int> combatSpiralPositions = CenterSpiralPositions(
                formationManager.GenerateSpiralPositions(combatShips.Count));

            // Offset transport spiral so they don't overlap combat ships
            int transportSpiralOffset = Mathf.CeilToInt(Mathf.Sqrt(combatShips.Count)) + 1;
            List<Vector2Int> transportSpiralPositions = CenterSpiralPositions(
                formationManager.GenerateSpiralPositions(transportShips.Count + transportSpiralOffset)
                    .Skip(transportSpiralOffset)
                    .ToList());

            // Offset system-ship spiral further out again so it doesn't overlap combat ships or transports
            int systemSpiralOffset = transportSpiralOffset + Mathf.CeilToInt(Mathf.Sqrt(transportShips.Count)) + 1;
            List<Vector2Int> systemSpiralPositions = CenterSpiralPositions(
                formationManager.GenerateSpiralPositions(otherSystemShips.Count + systemSpiralOffset)
                    .Skip(systemSpiralOffset)
                    .ToList());

            // Setup combat ships
            for (int i = 0; i < combatShips.Count; i++)
            {
                SetupSingleShip(combatShips[i], side, false, combatSpiralPositions[i]);
            }

            // Setup transport ships
            for (int i = 0; i < transportShips.Count; i++)
            {
                SetupSingleShip(transportShips[i], side, true, transportSpiralPositions[i]);
            }

            // Setup other system-owned ships (stationed combat ships, etc.) — already in the
            // system, spawn directly at the combat line with no warp-in animation
            for (int i = 0; i < otherSystemShips.Count; i++)
            {
                SetupSingleShipNoWarp(otherSystemShips[i], side, systemSpiralPositions[i]);
            }

            // Orbital Batteries: a wall at the combat line (closest to the enemy of anything
            // defending this system - Warp=0 so this spawn position is also its permanent combat
            // position, it never physically moves into Formation like a mobile ship would).
            SetupOrbitalBatteryWall(orbitalBatteryShips, side);

            // Shipyard: behind the OB wall (further from the enemy along X, same Y/Z depth as the
            // system's own transport line) - the asset the wall exists to protect. Also Warp=0, so
            // this spawn position is likewise permanent.
            SetupShipyards(shipyardShips, side);

            Debug.Log($"Side {side}: Setup {combatShips.Count} combat ships + {transportShips.Count} transports + " +
                      $"{otherSystemShips.Count} other system-owned + {orbitalBatteryShips.Count} OB + {shipyardShips.Count} Shipyard");
        }

        /// <summary>
        /// Raw (uncentered) 5-wide row-major grid slots for `count` items - col wraps every 5, row
        /// increments after each full row. Always fills from (0,0) outward, so on its own a partial
        /// row/grid (e.g. a lone Shipyard, or an Orbital Battery wall thinned by combat losses) sits
        /// pinned to the top-left corner rather than centered - callers should run the result through
        /// CenterSpiralPositions (same helper the combat-ship spiral formation already uses) before
        /// use, the way SetupOrbitalBatteryWall/SetupShipyards below do.
        /// </summary>
        private static List<Vector2Int> GenerateWallPositions(int count)
        {
            var positions = new List<Vector2Int>(count);
            for (int i = 0; i < count; i++)
                positions.Add(new Vector2Int(i % 5, i / 5));
            return positions;
        }

        /// <summary>
        /// Arranges Orbital Batteries into an evenly-spaced line (wall) at the system's normal
        /// combat-line X, centered on (0,0) the same way the combat-ship spiral formation is (see
        /// CenterSpiralPositions) rather than hard-coded to a "col-2, row-2" offset that only lands
        /// on true center when the count exactly fills a 5-wide row - any other count (a wall
        /// thinned by combat losses, in particular) used to sit skewed toward one corner instead of
        /// facing the enemy's formation dead-on. System Invasion Phase 1, Docs/Design/
        /// SystemInvasion_Phase1_Design.md §3.2, 2026-09-11 revision.
        /// </summary>
        private void SetupOrbitalBatteryWall(List<ShipController> orbitalBatteries, int side)
        {
            List<Vector2Int> wallSlots = CenterSpiralPositions(GenerateWallPositions(orbitalBatteries.Count));
            for (int i = 0; i < orbitalBatteries.Count; i++)
            {
                SetupSingleShipNoWarp(orbitalBatteries[i], side, wallSlots[i]);
            }
        }

        /// <summary>
        /// Places the Shipyard behind the Orbital Battery wall - same centered wall-grid shape as
        /// SetupOrbitalBatteryWall (see that method's comment - a lone Shipyard, the common case,
        /// used to always land pinned to the grid's corner instead of centered), offset further from
        /// the enemy along X (xOverride) so OB actually stands between the enemy and the Shipyard
        /// rather than sharing its X. System Invasion Phase 1, Docs/Design/
        /// SystemInvasion_Phase1_Design.md §3.1/§3.2, 2026-09-11 revision.
        /// </summary>
        private void SetupShipyards(List<ShipController> shipyards, int side)
        {
            float sideSign = side == 1 ? -1f : 1f;
            // Pulled back from the OB wall's combat-line X (±200) toward the transport line
            // (±400) but not all the way there - stays well inside BeamWeapon's full/near-full
            // damage band (100-400) so it's still a meaningful combat target, just behind the wall.
            float shipyardX = sideSign * 300f;

            List<Vector2Int> wallSlots = CenterSpiralPositions(GenerateWallPositions(shipyards.Count));
            for (int i = 0; i < shipyards.Count; i++)
            {
                SetupSingleShipNoWarp(shipyards[i], side, wallSlots[i], shipyardX);
            }
        }

        /// <summary>
        /// Setup a single ship with model, position, and rotation
        /// </summary>
        private void SetupSingleShip(ShipController ship, int side, bool isTransport, Vector2Int spiralPos)
        {
            // Calculate start and end X positions
            float startX = WarpAnimationController.GetWarpStartX(side, isTransport);
            float endX = WarpAnimationController.GetWarpEndX(side, isTransport);

            // Use spiral position to spread ships in Y (vertical) and Z (depth)
            Vector3 startPosition = new Vector3(startX, spiralPos.y * SPACING, spiralPos.x * SPACING);
            Vector3 endPosition = new Vector3(endX, spiralPos.y * SPACING, spiralPos.x * SPACING);

            // Remove parent and move to CombatScene
            ship.transform.SetParent(null, true);
            MoveShipToCombatScene(ship);

            // Set ship transform
            ship.transform.position = startPosition;
            SetShipRotation(ship, side);
            ship.transform.localScale = Vector3.one;
            ship.name = ship.ShipData.ShipName;
            ship.gameObject.SetActive(true);

            // Instantiate ship model
            GameObject shipModel = InstantiateShipModel(ship);

            // Setup collider
            AddShipCollider(ship, shipModel);

            // Ensure CombatOrderStateMachine is present
            CombatOrderStateMachine stateMachine = ship.GetComponent<CombatOrderStateMachine>();
            if (stateMachine == null)
            {
                stateMachine = ship.gameObject.AddComponent<CombatOrderStateMachine>();
                Debug.Log($"  ➕ Added CombatOrderStateMachine to {ship.ShipData.ShipName}");
            }
            stateMachine.Side = side;
            stateMachine.ShipController = ship;

            // Store warp data for animation
            WarpData warpData = ship.gameObject.AddComponent<WarpData>();
            warpData.Initialize(startPosition, endPosition, shipModel, side);

            // Setup weapons
            SetupShipWeapons(ship, side);

            Debug.Log($"  ✅ Setup {ship.ShipData.ShipName} in CombatScene at {startPosition}");
        }

        /// <summary>
        /// Setup a system-owned ship (stationed combat ship, orbital battery, or future shield):
        /// it's already in the star system being attacked, so it spawns directly at its final
        /// combat-line position with no warp-in animation. Skips adding a WarpData component
        /// entirely — WarpAnimationController.CollectWarpData only gathers ships that have one,
        /// so a ship without WarpData is automatically left out of the warp coroutine and simply
        /// sits there, already "arrived", for the rest of setup.
        /// </summary>
        private void SetupSingleShipNoWarp(ShipController ship, int side, Vector2Int spiralPos, float? xOverride = null)
        {
            // System ships hold the same combat-line X as regular combat ships (±200) — well
            // inside both TorpedoMaxRange (350) and BeamWeapon's full/near-full damage band
            // (100-400) of where the fight actually happens, unlike the transport line further back.
            // xOverride lets a caller place a stationary system unit further back along this same
            // line (e.g. SetupShipyards, behind the Orbital Battery wall - System Invasion Phase 1,
            // Docs/Design/SystemInvasion_Phase1_Design.md §3.1/§3.2).
            float endX = xOverride ?? WarpAnimationController.GetWarpEndX(side, false);
            Vector3 position = new Vector3(endX, spiralPos.y * SPACING, spiralPos.x * SPACING);

            ship.transform.SetParent(null, true);
            MoveShipToCombatScene(ship);

            ship.transform.position = position;
            SetShipRotation(ship, side);
            // Orbital Battery reuses a small placeholder-scaled FBX - doubled here (root transform,
            // not the model prefab itself) so it reads as a distinct, substantial platform in the
            // wall rather than blending in at 1x scale. Every other system-owned type (Shipyard,
            // regular defending ships) stays at its authored 1x.
            ship.transform.localScale = ship.ShipData.ShipType == ShipType.OrbitalBattery ? Vector3.one * 2f : Vector3.one;
            ship.name = ship.ShipData.ShipName;
            ship.gameObject.SetActive(true);

            GameObject shipModel = InstantiateShipModel(ship);
            AddShipCollider(ship, shipModel);

            CombatOrderStateMachine stateMachine = ship.GetComponent<CombatOrderStateMachine>();
            if (stateMachine == null)
            {
                stateMachine = ship.gameObject.AddComponent<CombatOrderStateMachine>();
                Debug.Log($"  ➕ Added CombatOrderStateMachine to {ship.ShipData.ShipName}");
            }
            stateMachine.Side = side;
            stateMachine.ShipController = ship;

            // No WarpData component — this ship never warps in, it's already here.
            SetupShipWeapons(ship, side);

            Debug.Log($"  ✅ Setup system-owned ship {ship.ShipData.ShipName} in CombatScene at {position} (no warp-in)");
        }

        /// <summary>
        /// Injects a ship the system's Shipyard just completed while Phase A combat is already
        /// underway into the ongoing fight - launched from near the Shipyard rather than warping
        /// in from outside (it isn't arriving from elsewhere, it's leaving a facility already in
        /// the scene). Unlike SetupSingleShipNoWarp's system-owned ships (OB/Shipyard, which never
        /// move - CombatOrderStateMachine.isSystemOwned gates on ShipData.CurrentStarSysController
        /// != null), this ship has CurrentStarSysController cleared first so it's treated as a
        /// normal mobile combatant instead of a stationary defense. System Invasion Phase 1,
        /// Docs/Design/SystemInvasion_Phase1_Design.md §3 follow-up (2026-09-11) - see
        /// CombatController.AddReinforcementShip for the caller.
        /// </summary>
        public void SetupReinforcementShip(ShipController ship, int side, Vector3 nearPosition)
        {
            // Detach from the system's docked-ship bookkeeping so CombatOrderStateMachine treats
            // this as a mobile combatant, not a stationary system defense.
            ship.ShipData.CurrentStarSysController = null;

            ship.transform.SetParent(null, true);
            MoveShipToCombatScene(ship);

            // Small random jitter so multiple reinforcements launched close together don't spawn
            // stacked exactly on top of each other or the Shipyard.
            Vector3 jitter = new Vector3(0f, Random.Range(-SPACING, SPACING), Random.Range(-SPACING, SPACING));
            ship.transform.position = nearPosition + jitter;
            SetShipRotation(ship, side);
            ship.transform.localScale = Vector3.one;
            ship.name = ship.ShipData.ShipName;
            ship.gameObject.SetActive(true);

            GameObject shipModel = InstantiateShipModel(ship);
            AddShipCollider(ship, shipModel);

            CombatOrderStateMachine stateMachine = ship.GetComponent<CombatOrderStateMachine>();
            if (stateMachine == null)
                stateMachine = ship.gameObject.AddComponent<CombatOrderStateMachine>();
            stateMachine.Side = side;
            stateMachine.ShipController = ship;

            // Move toward the enemy and fire as targets are located, per the design request -
            // Engage is the order that does exactly this (ExecuteEngage's approach-and-fire
            // behavior), rather than inheriting the defending side's current collective order
            // (often Formation by default - see TurnBasedCombatResolver.PickAIOrder). This only
            // guarantees its FIRST turn joining the fight - CombatController.SetShipOrders
            // overwrites every ship's .Order (this one included) to the side's chosen order on
            // the very next full order-resolution turn, same as it already does for everyone else.
            ship.Order = CombatOrders.Engage;
            stateMachine.CurrentOrder = CombatOrders.Engage;

            // No WarpData component — this ship never warps in, it launches already in-scene.
            SetupShipWeapons(ship, side);

            Debug.Log($"  🆕 Reinforcement '{ship.ShipData.ShipName}' launched from Shipyard into combat at {ship.transform.position}, ordered to Engage");
        }

        /// <summary>
        /// Move ship GameObject to CombatScene
        /// </summary>
        private void MoveShipToCombatScene(ShipController ship)
        {
            Scene combatScene = SceneManager.GetSceneByName("CombatScene");
            if (combatScene.isLoaded)
            {
                if (ship.gameObject.scene != combatScene)
                {
                    Debug.Log($"  Moving ship '{ship.ShipData.ShipName}' from {ship.gameObject.scene.name} to CombatScene");
                    SceneManager.MoveGameObjectToScene(ship.gameObject, combatScene);
                }
            }
            else
            {
                Debug.LogError("❌ CombatScene is not loaded! Cannot move ships.");
            }
        }

        /// <summary>
        /// Set ship rotation based on side
        /// </summary>
        private void SetShipRotation(ShipController ship, int side)
        {
            if (side == 1)
            {
                ship.transform.rotation = Quaternion.Euler(0, 90, 0); // Side 1 faces +X (right)
            }
            else
            {
                ship.transform.rotation = Quaternion.Euler(0, -90, 0); // Side 2 faces -X (left)
            }
        }

        /// <summary>
        /// Instantiate ship model and attach to ship controller
        /// </summary>
        private GameObject InstantiateShipModel(ShipController ship)
        {
            ShipSO shipSO = GetShipSOForShip(ship);
            if (shipSO == null)
            {
                Debug.LogError($"❌ Cannot instantiate model for {ship.ShipData.ShipName} — no ShipSO found");
                return null;
            }
            GameObject fbx = shipSO.ShipFBX_ModelAsGOPrefab;
            if (fbx == null)
            {
                fbx = ShipManager.Instance.GetFallbackFbx(shipSO.ShipType, shipSO.CivEnum);
                if (fbx == null)
                {
                    Debug.LogError($"❌ Ship FBX prefab is null for {ship.ShipData.ShipName} (ShipSO: {shipSO.ShipName})! No fallback FBX found either.");
                    return null;
                }
                Debug.LogWarning($"⚠️ Ship FBX prefab is null for {ship.ShipData.ShipName} (ShipSO: {shipSO.ShipName}) — using fallback model '{fbx.name}'");
            }

            GameObject shipModel = Object.Instantiate(fbx);
            shipModel.name = fbx.name + "_Model"; // Ensure CleanupShips can find it
            shipModel.transform.SetParent(ship.transform, false);
            shipModel.transform.localPosition = Vector3.zero;

            shipModel.transform.localRotation = Quaternion.identity;
            shipModel.transform.localScale = Vector3.one;

            DisableStencilOnShipRenderers(shipModel);
            SetLayerRecursively(ship.gameObject, LayerMask.NameToLayer("Default"));
            ApplyCivGlowColor(shipModel, shipSO.CivEnum);

            return shipModel;
        }

        /// <summary>
        /// Tints every renderer slot using the shared Ship_Glow material to this civ's GlowColor
        /// (see CivSO.GlowColor) via MaterialPropertyBlock — keeps every ship on the one shared glow
        /// material asset (SRP Batcher friendly) instead of needing a per-civ material duplicate.
        /// </summary>
        private void ApplyCivGlowColor(GameObject shipModel, CivEnum civEnum)
        {
            if (shipModel == null) return;

            CivSO civSO = CivManager.Instance?.GetCivSOByCivEnum(civEnum);
            if (civSO == null)
            {
                Debug.LogWarning($"⚠️ ApplyCivGlowColor: no CivSO found for {civEnum} — Ship_Glow left at its authored default color");
                return;
            }

            foreach (Renderer renderer in shipModel.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || !mats[i].name.Contains("Ship_Glow")) continue;

                    renderer.GetPropertyBlock(glowPropertyBlock, i);
                    glowPropertyBlock.SetColor(EmissionColorId, civSO.GlowColor);
                    renderer.SetPropertyBlock(glowPropertyBlock, i);
                }
            }
        }

        /// <summary>
        /// Add box collider for targeting
        /// </summary>
        private void AddShipCollider(ShipController ship, GameObject shipModel)
        {
            BoxCollider boxCollider = ship.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = ship.gameObject.AddComponent<BoxCollider>();
            }
            boxCollider.isTrigger = true;

            // Set collider bounds from renderer
            if (shipModel != null)
            {
                Renderer renderer = shipModel.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    ShipSO so = GetShipSOForShip(ship);
                    if (so?.ShipFBX_ModelAsGOPrefab == null) return;
                    GameObject fbx = so.ShipFBX_ModelAsGOPrefab;
                    Vector3 localCenter = fbx.transform.InverseTransformPoint(renderer.bounds.center);
                    Vector3 localSize = fbx.transform.InverseTransformVector(renderer.bounds.size);
                    boxCollider.center = new Vector3(localCenter.x, localCenter.z, localCenter.y);
                    float width = Mathf.Abs(localSize.x);
                    float height = Mathf.Abs(localSize.z);
                    float length = Mathf.Abs(localSize.y);
                    boxCollider.size = new Vector3(width, height, length);
                }
            }
        }

        /// <summary>
        /// Setup weapon prefabs and audio for a ship
        /// </summary>
        private void SetupShipWeapons(ShipController ship, int side)
        {
            ship.SetWeaponPrefabs();
            ship.SetWeaponAudioClips(
                side == 1 ? SideOneBeamFireClip : SideTwoBeamFireClip,
                side == 1 ? SideOneTorpedoFireClip : SideTwoTorpedoFireClip
            );
        }

        /// <summary>
        /// Get ShipSO for a ship controller
        /// </summary>
        private ShipSO GetShipSOForShip(ShipController shipCon)
        {
            // Prefer the direct SO reference stored during initialization — avoids name-mismatch issues
            if (shipCon.ShipData?.ShipSO != null)
                return shipCon.ShipData.ShipSO;

            // Fallback: name-based lookup (for ships that pre-date the ShipSO reference)
            CivEnum daCiv = shipCon.ShipData.CivEnum;
            List<ShipSO> daList;
            switch (daCiv)
            {
                case CivEnum.FED:    daList = ShipManager.Instance.FedShipSOList;    break;
                case CivEnum.KLING:  daList = ShipManager.Instance.KlingShipSOList;  break;
                case CivEnum.ROM:    daList = ShipManager.Instance.RomShipSOList;    break;
                case CivEnum.CARD:   daList = ShipManager.Instance.CardShipSOList;   break;
                case CivEnum.DOM:    daList = ShipManager.Instance.DomShipSOList;    break;
                case CivEnum.BORG:   daList = ShipManager.Instance.BorgShipSOList;   break;
                case CivEnum.TERRAN: daList = ShipManager.Instance.TerranShipSOList; break;
                default:             daList = ShipManager.Instance.FedShipSOList;    break;
            }

            for (int j = 0; j < daList.Count; j++)
            {
                if (daList[j] != null && daList[j].ShipName == shipCon.ShipData.ShipName)
                    return daList[j];
            }

            Debug.LogError($"❌ GetShipSOForShip: No ShipSO match for '{shipCon.ShipData.ShipName}' ({daCiv}) — check ShipManager lists and ShipSO ShipName fields");
            return null;
        }

        /// <summary>
        /// Disable stencil buffer operations on ship renderers
        /// </summary>
        private void DisableStencilOnShipRenderers(GameObject shipModel)
        {
            if (shipModel == null) return;

            Renderer[] renderers = shipModel.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    // Use a MaterialPropertyBlock instead of renderer.material so we don't
                    // clone a new Material instance per ship (leaks memory, breaks SRP batching).
                    renderer.GetPropertyBlock(stencilPropertyBlock);
                    stencilPropertyBlock.SetInt(StencilCompId, 0);
                    stencilPropertyBlock.SetInt(StencilId, 0);
                    stencilPropertyBlock.SetInt(StencilOpId, 0);
                    stencilPropertyBlock.SetInt(StencilWriteMaskId, 0);
                    stencilPropertyBlock.SetInt(StencilReadMaskId, 0);
                    renderer.SetPropertyBlock(stencilPropertyBlock);
                }
            }
        }

        /// <summary>
        /// Set layer of GameObject and all children recursively
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;

            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
