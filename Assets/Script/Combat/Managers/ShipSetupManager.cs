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
        /// Setup ships for one side (combat ships + transports)
        /// </summary>
        private void SetupShips(List<ShipController> shipList, int side)
        {
            Debug.Log($"=== SetupShips Side {side}: {shipList.Count} total ships ===");

            // Separate combat ships from transports
            List<ShipController> combatShips = shipList
                .Where(s => s != null && s.ShipData != null && s.ShipData.ShipType != ShipType.Transport)
                .ToList();
            List<ShipController> transportShips = shipList
                .Where(s => s != null && s.ShipData != null && s.ShipData.ShipType == ShipType.Transport)
                .ToList();

            Debug.Log($"  Side {side}: {combatShips.Count} combat, {transportShips.Count} transports");

            // Generate spiral positions
            List<Vector2Int> combatSpiralPositions = formationManager.GenerateSpiralPositions(combatShips.Count);

            // Offset transport spiral so they don't overlap combat ships
            int transportSpiralOffset = Mathf.CeilToInt(Mathf.Sqrt(combatShips.Count)) + 1;
            List<Vector2Int> transportSpiralPositions = formationManager.GenerateSpiralPositions(transportShips.Count + transportSpiralOffset)
                .Skip(transportSpiralOffset)
                .ToList();

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

            Debug.Log($"Side {side}: Setup {combatShips.Count} combat ships + {transportShips.Count} transports");
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
            if (ship.GetComponent<CombatOrderStateMachine>() == null)
            {
                ship.gameObject.AddComponent<CombatOrderStateMachine>();
                Debug.Log($"  ➕ Added CombatOrderStateMachine to {ship.ShipData.ShipName}");
            }

            // Store warp data for animation
            WarpData warpData = ship.gameObject.AddComponent<WarpData>();
            warpData.Initialize(startPosition, endPosition, shipModel, side);

            // Setup weapons
            SetupShipWeapons(ship, side);

            Debug.Log($"  ✅ Setup {ship.ShipData.ShipName} in CombatScene at {startPosition}");
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
            GameObject fbx = GetShipSOForShip(ship).ShipFBX_ModelAsGOPrefab;
            if (fbx == null)
            {
                Debug.LogError($"❌ Ship FBX prefab is null for {ship.ShipData.ShipName}!");
                return null;
            }

            GameObject shipModel = Object.Instantiate(fbx);
            shipModel.transform.SetParent(ship.transform, false);
            shipModel.transform.localPosition = Vector3.zero;

            // Flip child model 180° because FBX models face backwards
            shipModel.transform.localRotation = Quaternion.Euler(0, 180, 0);
            shipModel.transform.localScale = Vector3.one;

            DisableStencilOnShipRenderers(shipModel);
            SetLayerRecursively(ship.gameObject, LayerMask.NameToLayer("Default"));

            return shipModel;
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
                    GameObject fbx = GetShipSOForShip(ship).ShipFBX_ModelAsGOPrefab;
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
            List<ShipSO> daList = ShipManager.Instance.FedShipSOList;
            CivEnum daCiv = shipCon.ShipData.CivEnum;

            switch (daCiv)
            {
                case CivEnum.FED: daList = ShipManager.Instance.FedShipSOList; break;
                case CivEnum.KLING: daList = ShipManager.Instance.KlingShipSOList; break;
                case CivEnum.ROM: daList = ShipManager.Instance.RomShipSOList; break;
                case CivEnum.CARD: daList = ShipManager.Instance.CardShipSOList; break;
                case CivEnum.DOM: daList = ShipManager.Instance.DomShipSOList; break;
                case CivEnum.BORG: daList = ShipManager.Instance.BorgShipSOList; break;
                case CivEnum.TERRAN: daList = ShipManager.Instance.TerranShipSOList; break;
                default: daList = ShipManager.Instance.FedShipSOList; break;
            }

            for (int j = 0; j < daList.Count; j++)
            {
                if (daList[j].ShipName == shipCon.ShipData.ShipName)
                {
                    return daList[j];
                }
            }

            return ShipManager.Instance.FedShipSOList.FirstOrDefault();
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
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.SetInt("_StencilComp", 0);
                    renderer.material.SetInt("_Stencil", 0);
                    renderer.material.SetInt("_StencilOp", 0);
                    renderer.material.SetInt("_StencilWriteMask", 0);
                    renderer.material.SetInt("_StencilReadMask", 0);
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
