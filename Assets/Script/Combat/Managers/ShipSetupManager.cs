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

            Debug.Log($"  Side {side}: {combatShips.Count} combat, {transportShips.Count} transports, {systemShips.Count} system-owned (stationary)");

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
                formationManager.GenerateSpiralPositions(systemShips.Count + systemSpiralOffset)
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

            // Setup system-owned ships (stationed combat ships, orbital batteries, future
            // shields) — already in the system, spawn directly at the combat line with no
            // warp-in animation
            for (int i = 0; i < systemShips.Count; i++)
            {
                SetupSingleShipNoWarp(systemShips[i], side, systemSpiralPositions[i]);
            }

            Debug.Log($"Side {side}: Setup {combatShips.Count} combat ships + {transportShips.Count} transports + {systemShips.Count} system-owned (stationary)");
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
        private void SetupSingleShipNoWarp(ShipController ship, int side, Vector2Int spiralPos)
        {
            // System ships hold the same combat-line X as regular combat ships (±200) — well
            // inside both TorpedoMaxRange (350) and BeamWeapon's full/near-full damage band
            // (100-400) of where the fight actually happens, unlike the transport line further back.
            float endX = WarpAnimationController.GetWarpEndX(side, false);
            Vector3 position = new Vector3(endX, spiralPos.y * SPACING, spiralPos.x * SPACING);

            ship.transform.SetParent(null, true);
            MoveShipToCombatScene(ship);

            ship.transform.position = position;
            SetShipRotation(ship, side);
            ship.transform.localScale = Vector3.one;
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
