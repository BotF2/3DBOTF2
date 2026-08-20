// Ignore Spelling: Sys Habitalbe Unregister
using BOTF3D.Combat;
using BOTF3D.Core;
using BOTF3D.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BOTF3D.Civilization;
using BOTF3D.Audio;



namespace BOTF3D.Galaxy
{
    /// <summary>
    /// Controlling Star System interactions while the matching StarSystemData class
    /// holds key info on status and for save game
    /// </summary>
    public class StarSysController : MonoBehaviour
    {
        public void Initialize() { }
        public void UpdateState() { }
        //Fields
        public StarSysBuildManager StarSysBuildManager { get; set; }
        private StarSysData starSysData;
        public int PlayerID; // network player ID, not used in single player
        public StarSysData StarSysData { get { return starSysData; } set { starSysData = value; } }

        [SerializeField] // Make backing field serializable for Inspector
        private GameObject _starSysUIGameObject;

        public GameObject StarSysUIGameObject
        {
            get => _starSysUIGameObject;
            set
            {
                // ✅ Guard: never allow the 3D world object to be used as the UI object
                if (value != null && (value == this.gameObject || value.transform.IsChildOf(this.transform)))
                {
                    Debug.LogError($"❌ StarSysUIGameObject on '{name}' cannot be set to its own 3D GameObject or a child! Assignment blocked.");
                    return;
                }

                if (value != _starSysUIGameObject)
                {
                    // ... existing null-logging code stays here ...
                    _starSysUIGameObject = value;
                }
            }
        }


        private GameObject goForPowerOverload;
        public Camera GalaxyEventCamera { get; set; }
        [SerializeField]
        private Canvas canvasToolTip;
        public static event Action<TrekRandomEventSO> TrekEventDisasters;
        public GridLayoutGroup BuildListGridLayoutGroup;
        private BuildQueueWatcher buildQueueWatcher;
        public GridLayoutGroup ShipListGridLayoutGroup;
        private ShipQueueWatcher shipQueueWatcher;
        [SerializeField]
        internal List<Transform> sysBuildQueueList;
        private Transform buildingItem;
        [SerializeField]
        internal List<Transform> sysShipBuildQueueList;
        private Transform shipBuildingItem;
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
        private StarSysMenuUIController starSysUI;
        private StarSysMenuUIController StarSysUI
        {
            get
            {
                if (starSysUI == null)
                    starSysUI = StarSysMenuUIController.Instance;
                return starSysUI;
            }
        }

        private GameController gameController;

        [Header("Star Visual (Phase 1)")]
        // Sprite Brightness values (3-6, see StarVisualLibrary) push the glow sprite into HDR
        // range so Bloom picks up its bright core - but applied at full strength they clip the
        // sprite's authored surface detail (granulation, corona wisps) to solid white. Scale
        // it down so the texture's detail survives while its brightest pixels still cross the
        // Bloom threshold.
        [SerializeField] private float spriteBrightnessScale = 0.35f;

        [Header("Star Visual - Surface Noise (Phase 3)")]
        // Replaces an earlier whole-sprite brightness pulse (read as the sprite fading in/out,
        // not as a boiling surface). Instead Shaders/StarSurfaceNoise.shader warps the glow
        // sprite's own UVs with scrolling value noise, so its existing baked-in
        // granulation/corona detail (see the star textures) visibly churns in place. Shared
        // across every star (see GetOrCreateStarSurfaceMaterial) since it's a global look
        // knob, not a per-star-type one - StarVisualLibrary still owns per-type color/size.
        // Tuned at GalaxyCameraDragMoveZoom's referenceFieldOfViewForStarNoise (default 50deg,
        // roughly the Home-button distance). SetFieldOfViewForSurfaceNoise rescales
        // NoiseScale/DistortStrength around these as the camera zooms so the boiling reads at
        // a consistent apparent size rather than being tuned for one specific zoom level.
        [SerializeField] private float surfaceNoiseScale = 4f;
        [SerializeField] private float surfaceNoiseSpeed = 0.7f;
        [SerializeField, Range(0f, 0.3f)] private float surfaceDistortStrength = 0.025f;
        [SerializeField, Range(0f, 1f)] private float surfaceBrightnessNoise = 0.2f;
        private static Material starSurfaceNoiseMaterial;
        private static Material nebulaAdditiveMaterial;
        private static float cachedNoiseScale = 4f;
        private static float cachedNoiseSpeed = 0.7f;
        private static float cachedDistortStrength = 0.025f;
        private static float cachedBrightnessNoise = 0.2f;
        private static float zoomNoiseScaleMultiplier = 1f;

        [Header("Star Visual - Selected (Phase 2)")]
        // World-space radius, scaled by the star type's own SizeMultiplier so bigger/brighter
        // stars get a proportionally bigger ring, and sized so the ring sits outside the star
        // sprite/name label rather than covering them. Absolute value, calibrated by eye -
        // sprite.bounds-derived sizing was tried and proved unreliable (~5x off).
        [SerializeField] private float ringRadius = 1.2f;
        [SerializeField] private float ringWidthFraction = 0.08f; // line width, relative to ringRadius
        [SerializeField] private float ringAlpha = 0.6f;
        [SerializeField] private float ringBrightnessMultiplier = 2f;
        private const int ringSegments = 64;
        private bool isSelected;
        private GameObject selectionRingGO;
        private StarVisualProfile cachedVisualProfile;
        private bool hasVisualProfile;
        private SpriteRenderer starGlowSpriteRenderer;

        // Boosts the star's glow sprite into HDR range so Bloom picks up its bright core, and
        // swaps in the shared boiling-surface-noise shader. No-ops for non-star, non-nebula
        // GalaxyObjectTypes (BlackHole, WormHole, UniComplex, Station, ...), which keep their
        // existing sprite-only rendering untouched.
        public void ApplyStarVisual(GalaxyObjectType starType, SpriteRenderer glowSpriteRenderer)
        {
            if (IsNebulaType(starType))
            {
                // Nebula art bakes a fully-opaque near-black halo around the cloud instead of a
                // clean alpha cutout (visible as a dark patch against the map's non-black
                // background). Additive blending makes black contribute nothing regardless of
                // what's behind it, fixing this without touching the source art. See
                // NebulaAdditive.shader.
                if (glowSpriteRenderer != null)
                {
                    Material nebulaMaterial = GetOrCreateNebulaAdditiveMaterial();
                    if (nebulaMaterial != null)
                        glowSpriteRenderer.sharedMaterial = nebulaMaterial;
                }
                return;
            }

            if (!StarVisualLibrary.TryGetProfile(starType, out StarVisualProfile profile))
                return;

            cachedVisualProfile = profile;
            hasVisualProfile = true;
            starGlowSpriteRenderer = glowSpriteRenderer;

            if (glowSpriteRenderer != null)
            {
                glowSpriteRenderer.color = Color.white * profile.Brightness * spriteBrightnessScale;

                Material surfaceMaterial = GetOrCreateStarSurfaceMaterial();
                if (surfaceMaterial != null)
                    glowSpriteRenderer.sharedMaterial = surfaceMaterial;

                // One-time random roll around the view axis so identically-shaped star sprites
                // (same texture, same corona/rim shape) don't all look identical when several of
                // the same star type are visible at once. The sprite inherits its parent
                // LookAtCameraHolder's camera-facing rotation every frame (BillboardCameraGalactica),
                // so this local Z rotation just adds a per-star twist on top of that for free -
                // no per-frame cost.
                glowSpriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            }

            ApplySurfaceNoiseTunables();
        }

        // Lazily builds the single Material shared by every star's glow sprite. Shared (not
        // per-instance) so tuning the noise sliders affects the whole galaxy map at once and
        // stars don't pay for one material each; per-star variation instead comes from the
        // shader hashing each sprite's world position into its own noise phase.
        private static Material GetOrCreateStarSurfaceMaterial()
        {
            if (starSurfaceNoiseMaterial != null)
                return starSurfaceNoiseMaterial;

            Shader shader = Shader.Find("Custom/StarSurfaceNoise");
            if (shader == null)
            {
                Debug.LogError("StarSysController: Shader 'Custom/StarSurfaceNoise' not found - star surface noise disabled.");
                return null;
            }

            starSurfaceNoiseMaterial = new Material(shader) { name = "Mat_StarSurfaceNoise" };
            return starSurfaceNoiseMaterial;
        }

        private static bool IsNebulaType(GalaxyObjectType type)
        {
            return type == GalaxyObjectType.Nebula
                || type == GalaxyObjectType.OmarianNebula
                || type == GalaxyObjectType.ORIONNEBULA;
        }

        // Lazily builds the single Material shared by every nebula sprite, mirroring
        // GetOrCreateStarSurfaceMaterial above.
        private static Material GetOrCreateNebulaAdditiveMaterial()
        {
            if (nebulaAdditiveMaterial != null)
                return nebulaAdditiveMaterial;

            Shader shader = Shader.Find("Custom/NebulaAdditive");
            if (shader == null)
            {
                Debug.LogError("StarSysController: Shader 'Custom/NebulaAdditive' not found - nebula additive blending disabled.");
                return null;
            }

            nebulaAdditiveMaterial = new Material(shader) { name = "Mat_NebulaAdditive" };
            return nebulaAdditiveMaterial;
        }

        // Caches this controller's Inspector tunables as the zoom-neutral "base" values, then
        // pushes them (scaled by the current zoom multiplier) onto the shared surface-noise
        // material. Called from ApplyStarVisual (initial setup) and OnValidate (live Play Mode
        // tuning). Cached statically (rather than read live off this instance) so
        // SetFieldOfViewForSurfaceNoise can reapply them on zoom changes without needing to
        // hold a reference to whichever StarSysController happened to set them last.
        private void ApplySurfaceNoiseTunables()
        {
            cachedNoiseScale = surfaceNoiseScale;
            cachedNoiseSpeed = surfaceNoiseSpeed;
            cachedDistortStrength = surfaceDistortStrength;
            cachedBrightnessNoise = surfaceBrightnessNoise;
            PushSurfaceNoiseMaterialProperties();
        }

        private static void PushSurfaceNoiseMaterialProperties()
        {
            Material surfaceMaterial = GetOrCreateStarSurfaceMaterial();
            if (surfaceMaterial == null)
                return;

            surfaceMaterial.SetFloat("_NoiseScale", cachedNoiseScale * zoomNoiseScaleMultiplier);
            surfaceMaterial.SetFloat("_NoiseSpeed", cachedNoiseSpeed);
            surfaceMaterial.SetFloat("_DistortStrength", cachedDistortStrength / Mathf.Max(zoomNoiseScaleMultiplier, 0.01f));
            surfaceMaterial.SetFloat("_BrightnessNoiseStrength", cachedBrightnessNoise);
        }

        // Called by GalaxyCameraDragMoveZoom whenever the galaxy camera's field of view
        // changes. A fixed UV-space noise scale/distortion covers more or fewer screen pixels
        // as the star sprite's on-screen size changes with FOV, so values tuned at one zoom
        // level look too fine when zoomed out or too strong/rubbery when zoomed in. Apparent
        // size scales with tan(FOV/2), so re-deriving the multiplier from that ratio keeps the
        // boiling reading at roughly the same screen-space cell size/wobble at any zoom.
        public static void SetFieldOfViewForSurfaceNoise(float fieldOfView, float referenceFieldOfView)
        {
            float currentTan = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            float referenceTan = Mathf.Tan(referenceFieldOfView * 0.5f * Mathf.Deg2Rad);
            if (currentTan <= 0.0001f || referenceTan <= 0.0001f)
                return;

            zoomNoiseScaleMultiplier = referenceTan / currentTan;
            PushSurfaceNoiseMaterialProperties();
        }

        // Lets the Inspector tunables above actually be tuned live in Play Mode - without
        // this, ApplyStarVisual/CreateSelectionRing only ever ran once at star creation, so
        // editing a slider had no visible effect until the star was recreated.
        private void OnValidate()
        {
            if (!hasVisualProfile)
                return;

            ApplySurfaceNoiseTunables();

            if (selectionRingGO != null)
            {
                bool wasActive = selectionRingGO.activeSelf;
                Destroy(selectionRingGO);
                selectionRingGO = null;
                CreateSelectionRing();
                selectionRingGO.SetActive(wasActive);
            }
        }

        // Push notification from StarSysMenuUIController when this system's detail UI is
        // opened/closed (SetActiveSetParentUIGO / MoveBackAnyStarSysUIGO). Shows/hides a
        // static ring around the star; no-ops for non-star objects, which never get a cached
        // visual profile from ApplyStarVisual. Deliberately does not touch the star sprite
        // itself (no dimming/pulsing) - the sprite's own look (including its boiling surface
        // noise, see ApplyStarVisual) is Phase 1/3's job, not the selection indicator's.
        public void SetSelected(bool selected)
        {
            if (selected == isSelected || !hasVisualProfile)
                return;

            isSelected = selected;

            if (isSelected)
            {
                if (selectionRingGO == null)
                    CreateSelectionRing();
                selectionRingGO.SetActive(true);
            }
            else if (selectionRingGO != null)
            {
                selectionRingGO.SetActive(false);
            }
        }

        // Simple static ring around the currently selected star, sized to sit outside the
        // star sprite and its name label rather than covering them. No rotation/animation -
        // just a steady attention marker. Parented directly to the glow sprite's own
        // transform (a child of LookAtCameraHolder - see Billboard.cs) so it inherits the
        // sprite's billboard rotation for free: its local XY plane always faces the camera,
        // exactly like the sprite, with no per-frame position sync needed.
        private void CreateSelectionRing()
        {
            selectionRingGO = new GameObject("SelectionRing");
            Transform parentTransform = starGlowSpriteRenderer != null ? starGlowSpriteRenderer.transform : this.transform;
            selectionRingGO.transform.SetParent(parentTransform, false);
            selectionRingGO.transform.localPosition = Vector3.zero;
            selectionRingGO.transform.localRotation = Quaternion.identity;
            selectionRingGO.transform.localScale = Vector3.one;
            selectionRingGO.layer = this.gameObject.layer;

            float radius = ringRadius * cachedVisualProfile.SizeMultiplier;
            Vector3[] points = new Vector3[ringSegments];
            for (int i = 0; i < ringSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / ringSegments;
                points[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            }
            float width = radius * ringWidthFraction;

            Color color = cachedVisualProfile.Color * ringBrightnessMultiplier;
            color.a = ringAlpha;

            LineRenderer lr = selectionRingGO.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.alignment = LineAlignment.TransformZ;
            lr.positionCount = points.Length;
            lr.SetPositions(points);
            lr.startWidth = width;
            lr.endWidth = width;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            lr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                Material material = new Material(shader) { name = "Mat_SelectionRing" };
                material.SetColor("_BaseColor", color);
                // Default URP Unlit material is opaque, which would ignore ringAlpha entirely.
                // Explicitly configure standard alpha blending.
                material.SetFloat("_Surface", 1f); // Transparent
                material.SetFloat("_Blend", 0f); // Alpha
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                // The star's glow sprite uses the built-in Sprites-Default material (queue
                // 3000) and, via its billboard holder's baked -0.5 local Z offset, sits
                // physically closer to the camera than most other transparent geometry at this
                // position. Push this material's queue past the sprite's so it draws after and
                // wins ties in the transparent (back-to-front) sort.
                material.renderQueue = 3100;
                lr.material = material;
            }
        }

        public GameObject ShipListUIParent
        {
            get => StarSysData?.ShipListUIParent;
            set
            {
                if (StarSysData != null)
                    StarSysData.ShipListUIParent = value;
                ShipManager.Instance?.ProcessPendingShipUIs();
            }
        }
        private bool deployNotMerge = true; // true=deploy, false=merge
        private void Awake()
        {
            gameController = GameController.Instance;
            if (sysBuildQueueList == null)
                sysBuildQueueList = new List<Transform>();

            if (sysShipBuildQueueList == null)
                sysShipBuildQueueList = new List<Transform>();
        }


        //************ToDo, next steps:***********
        //Pause / resume building
        //Speed modifiers(tech, civ traits)
        //Save/load coroutine state
        //Replace Update() with OnTransformChildrenChanged()
        //private void Awake()

        private void Start()
        {
            // ✅ Use assigned camera, fallback to find if not set
            if (GalaxyEventCamera == null)
            {
                GalaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
                Debug.LogWarning($"StarSysController {name}: Had to find camera in Start() - should be set by StarSysManager");
            }

            if (canvasToolTip != null && GalaxyEventCamera != null)
            {
                canvasToolTip.worldCamera = GalaxyEventCamera;
            }
            else if (canvasToolTip != null)
            {
                Debug.LogError($"StarSysController {name}: Cannot set tooltip camera - GalaxyEventCamera is NULL!");
            }

            if (StarSysUI != null)
                goForPowerOverload = StarSysUI.PowerOverloadImage;
        }
        private void OnTransformChildrenChanged() //Unity automatically invokes when the transform hierarchy of the Controller GameObject changes.
        {  // This UI Queue is in the SysBuildUIListPanel prefab and not a child of StarSysController, BuildQueueWatcher helps call OnTransformChildrenChanged()
            if (BuildListGridLayoutGroup != null)
                GridFactoryQueueUpdate();

            if (ShipListGridLayoutGroup != null)
                GridShipQueueUpdate();
        }
        public void RegisterBuildQueueWatcher(BuildQueueWatcher watcher)
        {
            buildQueueWatcher = watcher;
        }

        public void UnregisterBuildQueueWatcher(BuildQueueWatcher watcher)
        {
            if (buildQueueWatcher == watcher)
                buildQueueWatcher = null;
        }

        public void RegisterShipQueueWatcher(ShipQueueWatcher watcher)
        {
            shipQueueWatcher = watcher;
        }

        public void UnregisterShipQueueWatcher(ShipQueueWatcher watcher)
        {
            if (shipQueueWatcher == watcher)
                shipQueueWatcher = null;
        }

        public void GridFactoryQueueUpdate()
        {
            if (BuildListGridLayoutGroup == null)
                return;
            // 1️⃣ Sync queue list FIRST
            foreach (Transform child in BuildListGridLayoutGroup.transform)
            {
                if (!sysBuildQueueList.Contains(child))
                    sysBuildQueueList.Add(child);
            }

            sysBuildQueueList.RemoveAll(t => t == null || t.parent != BuildListGridLayoutGroup.transform);

            sysBuildQueueList = sysBuildQueueList
                .OrderByDescending(t => t.localPosition.y)
                .ThenBy(t => t.localPosition.x)
                .ToList();

            // 2️⃣ Start build only when a turn is actively progressing
            if (!StarSysBuildManager.IsBuildingFacility && sysBuildQueueList.Count > 0 &&
                TimeManager.Instance?.TurnPhase == TurnPhase.TurnProgression)
            {
                StarSysBuildManager.StartNextFacilityBuildIfAny();
            }
        }


        public void GridShipQueueUpdate()
        {
            if (ShipListGridLayoutGroup == null)
                return;
            foreach (Transform child in ShipListGridLayoutGroup.transform)
            {
                if (!sysShipBuildQueueList.Contains(child))
                {
                    child.gameObject.SetActive(true);
                    sysShipBuildQueueList.Add(child);
                }
            }

            sysShipBuildQueueList.RemoveAll(t => t == null || t.parent != ShipListGridLayoutGroup.transform);

            sysShipBuildQueueList = sysShipBuildQueueList
                .OrderByDescending(t => t.localPosition.y)
                .ThenBy(t => t.localPosition.x)
                .ToList();

            if (!StarSysBuildManager.IsBuildingShip && sysShipBuildQueueList.Count > 0 &&
                TimeManager.Instance?.TurnPhase == TurnPhase.TurnProgression)
            {
                StarSysBuildManager.StartNextShipBuildIfAny();
            }
        }
        public void UpgradeShipToCurrentTech(ShipController ship)
        {
            var currentTech = this.StarSysData.CurrentCivController.CivData.CurrentTechLevel;

            if (ship.ShipData.TechLevel < currentTech)
            {
                // Add to upgrade queue
                // Cost: BuildDuration / 2, uses shipyard
                Debug.Log($"Upgrading {ship.ShipData.ShipName} from {ship.ShipData.TechLevel} to {currentTech}");
            }
        }
        public void DoHabitalbeSystemUI(CivController discoveringCiv)
        {
            if (discoveringCiv != null)
            {
                HabitableSysUIController.Instance.LoadHabitableSysUI(this, discoveringCiv);
            }
        }

        public void UpdateOwner(CivEnum newOwner) // system captured or colonized
        {
            starSysData.CurrentOwnerCivEnum = newOwner;
        }

        /// <summary>
        /// Founds a new colony on this uninhabited, habitable system by consuming the given
        /// Transport ship. Claims ownership instantly (same steps as ClaimSystem) and consumes the
        /// Transport instantly, but the starting Power Plant + Factory aren't granted until
        /// ColonizeTurns turns later (see ColonizeTimerCoroutine) - IsColonizing is true for the
        /// duration. Driven by the Fleet menu's Colonize order button (see
        /// FleetMenuUIController.ClickColonizeButton), not the arrival popup.
        /// </summary>
        public const int ColonizeTurns = 2;
        public const int TerraformTurns = 3;

        public bool ColonizeWithTransport(ShipController transportShip)
        {
            if (transportShip == null || transportShip.ShipData == null
                || transportShip.ShipData.ShipType != ShipType.Transport || transportShip.ShipData.Distroyed)
            {
                Debug.LogWarning($"ColonizeWithTransport: '{name}' - no valid Transport ship supplied.");
                return false;
            }

            // Ownership is allowed to already belong to the transport's own civ here - that's the
            // post-Terraform case (TerraformSystem claims ownership instantly, IsHabitable only
            // flips true TerraformTurns later, and it's this same call that actually drops the
            // starting facilities). Still uninhabited-sentinel-owned covers the direct
            // habitable-on-first-contact case. Any OTHER real civ's ownership is rejected.
            int firstUninhabited = (int)CivEnum.ZZUNINHABITED1;
            bool systemIsUninhabited = (int)starSysData.CurrentOwnerCivEnum >= firstUninhabited;
            bool systemAlreadyOwnedByUs = !systemIsUninhabited && starSysData.CurrentOwnerCivEnum == transportShip.ShipData.CivEnum;
            if ((!systemIsUninhabited && !systemAlreadyOwnedByUs) || !starSysData.IsHabitable || starSysData.IsColonizing)
            {
                Debug.LogWarning($"ColonizeWithTransport: '{name}' is no longer a qualifying uninhabited/habitable system.");
                return false;
            }

            var fleetCon = transportShip.ShipData.CurrentFleetController;
            CivController colonizingCiv = fleetCon != null ? fleetCon.FleetData.CivController
                : CivManager.Instance.GetCivControllerByCivEnum(transportShip.ShipData.CivEnum);
            if (colonizingCiv == null)
            {
                Debug.LogWarning($"ColonizeWithTransport: '{name}' - could not resolve the colonizing civilization.");
                return false;
            }

            // Claim ownership.
            CivEnum previousOwnerCivEnum = starSysData.CurrentOwnerCivEnum;
            starSysData.CurrentOwnerCivEnum = colonizingCiv.CivData.CivEnum;
            starSysData.CurrentCivController = colonizingCiv;
            if (!colonizingCiv.CivData.StarSysWeOwn.Contains(this))
                colonizingCiv.CivData.StarSysWeOwn.Add(this);

            // Consume the transport instantly - its dilithium fuels the new power plant once
            // colonization completes.
            if (fleetCon != null)
                fleetCon.RemoveShipFromFleet(transportShip);
            var occupiedSysCon = transportShip.ShipData.CurrentStarSysController;
            if (occupiedSysCon != null)
                occupiedSysCon.RemoveFromShipList(transportShip);
            GameEvents.ShipDestroyed(transportShip.ShipData.ShipID);
            ShipManager.Instance.RemoveShipControllerFromList(transportShip);
            // Clear both contact fields - covers both the direct habitable-on-arrival path
            // (ColonizableSystem) and the post-Terraform path (TerraformableSystem, still set per
            // the comment in TerraformSystem above, since it was needed to get this far).
            if (fleetCon != null)
            {
                fleetCon.FleetData.ColonizableSystem = null;
                fleetCon.FleetData.TerraformableSystem = null;
            }

            if (GameController.Instance.AreWeLocalPlayer(colonizingCiv.CivData.CivEnum) && StarSysManager.Instance != null)
                StarSysManager.Instance.InstantiateStarSysUI(this);

            GameEvents.SystemOwnershipChanged(starSysData.SysName, previousOwnerCivEnum, colonizingCiv.CivData.CivEnum);

            // Start the colonize timer - the starting Power Plant + Factory land when it completes.
            starSysData.IsColonizing = true;
            starSysData.ColonizeCompleteStardate = TimeManager.Instance.CurrentStarDate() + ColonizeTurns * TimeManager.Instance.StarDatesPerTurn;
            if (gameObject.activeInHierarchy)
                StartCoroutine(ColonizeTimerCoroutine(colonizingCiv));

            Debug.Log($"'{starSysData.SysName}' colonizing for {colonizingCiv.CivData.CivShortName} via Transport - completes turn {starSysData.ColonizeCompleteStardate / TimeManager.Instance.StarDatesPerTurn}.");
            return true;
        }

        private IEnumerator ColonizeTimerCoroutine(CivController colonizingCiv)
        {
            while (TimeManager.Instance.CurrentStarDate() < starSysData.ColonizeCompleteStardate)
                yield return null;

            // Starting facilities: 1 Power Plant + 1 Factory, no Shipyard, no other facilities.
            int civInt = (int)colonizingCiv.CivData.CivEnum;
            starSysData.PowerPlants = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.PowerPlantPrefab, civInt, 1, this);
            starSysData.CurrentPowerPlantCount = starSysData.PowerPlants.Count;
            starSysData.Factories = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.FactoryPrefab, civInt, 1, this);
            if (StarSysMenuUIController.Instance != null)
                StarSysMenuUIController.Instance.UpdateSystemPowerBalance(this);

            starSysData.IsColonizing = false;
            Debug.Log($"Colonized '{starSysData.SysName}' for {colonizingCiv.CivData.CivShortName} - facilities online at stardate {TimeManager.Instance.CurrentStarDate()}.");
        }

        /// <summary>
        /// Begins terraforming this uninhabited, IsTerraformable-but-not-yet-habitable system by
        /// consuming the given Transport ship. Claims ownership and consumes the Transport
        /// instantly, but IsHabitable doesn't flip true until TerraformTurns turns later (see
        /// TerraformTimerCoroutine) - IsTerraforming is true for the duration. No facilities are
        /// granted; a later Colonize (bringing a second Transport) is what actually founds the
        /// colony once the system is habitable. Driven by the Fleet menu's Terraform order button
        /// (see FleetMenuUIController.ClickTerraformButton).
        /// </summary>
        public bool TerraformSystem(ShipController transportShip)
        {
            int firstUninhabited = (int)CivEnum.ZZUNINHABITED1;
            if ((int)starSysData.CurrentOwnerCivEnum < firstUninhabited || starSysData.IsHabitable
                || starSysData.IsTerraformable != true || starSysData.IsTerraforming)
            {
                Debug.LogWarning($"TerraformSystem: '{name}' is no longer a qualifying uninhabited/terraformable system.");
                return false;
            }

            if (transportShip == null || transportShip.ShipData == null
                || transportShip.ShipData.ShipType != ShipType.Transport || transportShip.ShipData.Distroyed)
            {
                Debug.LogWarning($"TerraformSystem: '{name}' - no valid Transport ship supplied.");
                return false;
            }

            var fleetCon = transportShip.ShipData.CurrentFleetController;
            CivController terraformingCiv = fleetCon != null ? fleetCon.FleetData.CivController
                : CivManager.Instance.GetCivControllerByCivEnum(transportShip.ShipData.CivEnum);
            if (terraformingCiv == null)
            {
                Debug.LogWarning($"TerraformSystem: '{name}' - could not resolve the terraforming civilization.");
                return false;
            }

            // Claim ownership.
            CivEnum previousOwnerCivEnum = starSysData.CurrentOwnerCivEnum;
            starSysData.CurrentOwnerCivEnum = terraformingCiv.CivData.CivEnum;
            starSysData.CurrentCivController = terraformingCiv;
            if (!terraformingCiv.CivData.StarSysWeOwn.Contains(this))
                terraformingCiv.CivData.StarSysWeOwn.Add(this);

            // Consume the transport instantly - its personnel begin terraforming immediately.
            if (fleetCon != null)
                fleetCon.RemoveShipFromFleet(transportShip);
            var occupiedSysCon = transportShip.ShipData.CurrentStarSysController;
            if (occupiedSysCon != null)
                occupiedSysCon.RemoveFromShipList(transportShip);
            GameEvents.ShipDestroyed(transportShip.ShipData.ShipID);
            ShipManager.Instance.RemoveShipControllerFromList(transportShip);
            // Deliberately NOT nulling fleetCon.FleetData.TerraformableSystem here (unlike the
            // equivalent line in ColonizeWithTransport) - this reference needs to survive so that
            // once IsHabitable flips true (TerraformTimerCoroutine below), FleetMenuUIController can
            // still find this system and offer Colonize without requiring a fresh OnTriggerEnter.

            if (GameController.Instance.AreWeLocalPlayer(terraformingCiv.CivData.CivEnum) && StarSysManager.Instance != null)
                StarSysManager.Instance.InstantiateStarSysUI(this);

            GameEvents.SystemOwnershipChanged(starSysData.SysName, previousOwnerCivEnum, terraformingCiv.CivData.CivEnum);

            starSysData.IsTerraforming = true;
            starSysData.TerraformCompleteStardate = TimeManager.Instance.CurrentStarDate() + TerraformTurns * TimeManager.Instance.StarDatesPerTurn;
            if (gameObject.activeInHierarchy)
                StartCoroutine(TerraformTimerCoroutine());

            Debug.Log($"'{starSysData.SysName}' terraforming for {terraformingCiv.CivData.CivShortName} - completes turn {starSysData.TerraformCompleteStardate / TimeManager.Instance.StarDatesPerTurn}.");
            return true;
        }

        private IEnumerator TerraformTimerCoroutine()
        {
            while (TimeManager.Instance.CurrentStarDate() < starSysData.TerraformCompleteStardate)
                yield return null;

            starSysData.IsTerraforming = false;
            starSysData.IsHabitable = true;
            GameEvents.SystemHabitabilityChanged(starSysData.SysName, true);
            Debug.Log($"'{starSysData.SysName}' finished terraforming - now habitable at stardate {TimeManager.Instance.CurrentStarDate()}.");
        }

        /// <summary>
        /// Claims this uninhabited system (habitable, or terraformable-but-not-yet-habitable) for
        /// the given civ - ownership only, no Transport required and no facilities granted. Just
        /// plants the claiming civ's OwnerInsignia so the system reads as theirs on the map; a
        /// Transport can colonize/terraform it properly later via ColonizeWithTransport/
        /// TerraformSystem. Driven by the Fleet menu's Claim System button (see
        /// FleetMenuUIController.ClickClaimSystemButton).
        /// </summary>
        public bool ClaimSystem(CivController claimingCiv)
        {
            int firstUninhabited = (int)CivEnum.ZZUNINHABITED1;
            if ((int)starSysData.CurrentOwnerCivEnum < firstUninhabited
                || !(starSysData.IsHabitable || starSysData.IsTerraformable == true))
            {
                Debug.LogWarning($"ClaimSystem: '{name}' is no longer a qualifying uninhabited/habitable/terraformable system.");
                return false;
            }

            if (claimingCiv == null || claimingCiv.CivData == null)
            {
                Debug.LogWarning($"ClaimSystem: '{name}' - no valid claiming civilization supplied.");
                return false;
            }

            // Claim ownership.
            CivEnum previousOwnerCivEnum = starSysData.CurrentOwnerCivEnum;
            starSysData.CurrentOwnerCivEnum = claimingCiv.CivData.CivEnum;
            starSysData.CurrentCivController = claimingCiv;
            if (!claimingCiv.CivData.StarSysWeOwn.Contains(this))
                claimingCiv.CivData.StarSysWeOwn.Add(this);

            // Plant the claiming civ's insignia.
            StarSysChildFields fields = GetComponent<StarSysChildFields>();
            if (fields != null && fields.OwnerInsigniaGO != null)
            {
                fields.OwnerInsigniaGO.SetActive(true);
                SpriteRenderer srInsignia = fields.OwnerInsigniaGO.GetComponent<SpriteRenderer>();
                if (srInsignia != null)
                {
                    srInsignia.sprite = claimingCiv.CivData.InsigniaSprite;
                    srInsignia.enabled = GameController.Instance.AreWeLocalPlayer(claimingCiv.CivData.CivEnum);
                }
            }

            if (GameController.Instance.AreWeLocalPlayer(claimingCiv.CivData.CivEnum) && StarSysManager.Instance != null)
                StarSysManager.Instance.InstantiateStarSysUI(this);

            GameEvents.SystemOwnershipChanged(starSysData.SysName, previousOwnerCivEnum, claimingCiv.CivData.CivEnum);
            Debug.Log($"Claimed '{starSysData.SysName}' for {claimingCiv.CivData.CivShortName}.");
            return true;
        }
        private void OnMouseDown()
        {
            // See matching comment in FleetController.OnMouseDown: this raw physics click fires
            // even when a UI button over this system's screen position was the actual click target.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var clickedSystemCon = GetComponentInParent<StarSysController>();
            if (clickedSystemCon == null) return;

            var galaxyUI = GalaxyMenuUIController.Instance;

            Debug.Log($"OnMouseDown: system '{clickedSystemCon.name}' clicked, CurrentClickMode={galaxyUI.CurrentClickMode}.");

            switch (galaxyUI.CurrentClickMode)
            {
                case GalaxyClickMode.Normal:
                    // ✅ Only close ship deploy if it's actually open!
                    if (ShipDeployMenuUIController.Instance != null &&
                        ShipDeployMenuUIController.Instance.ShipDeployPanel != null &&
                        ShipDeployMenuUIController.Instance.ShipDeployPanel.activeSelf)
                    {
                        galaxyUI.CloseShipDeployMenu();
                    }

                    // ✅ CRITICAL: Clean up BOTH star system UIs AND fleet UIs
                    if (StarSysMenuUIController.Instance != null)
                    {
                        StarSysMenuUIController.Instance.MoveBackAnyStarSysUIGO();
                    }
                    if (FleetMenuUIController.Instance != null)
                    {
                        FleetMenuUIController.Instance.MoveBackAnyaFleetUIGO();
                        Debug.Log("OnMouseDown: Cleaned up fleet UIs before opening star system");
                    }

                    HandleNormalClick(clickedSystemCon);
                    break;

                case GalaxyClickMode.SetDestination:
                    Debug.Log($"OnMouseDown: SetDestination click on system '{clickedSystemCon.name}'.");
                    HandleDestinationClick(clickedSystemCon);
                    break;

                case GalaxyClickMode.SelectForShipDeploy:
                    if (GameController.Instance.AreWeLocalPlayer(clickedSystemCon.StarSysData.CurrentOwnerCivEnum))
                        HandleShipDeploySelection(clickedSystemCon);
                    break;

                case GalaxyClickMode.SelectForShipMerge:
                    if (GameController.Instance.AreWeLocalPlayer(clickedSystemCon.StarSysData.CurrentOwnerCivEnum))
                        HandleMergeSelection(clickedSystemCon);
                    break;
            }
        }

        private void HandleMergeSelection(StarSysController clickedSystemCon)
        {
            if (clickedSystemCon != this) { return; }
            MousePointerChanger.Instance.ResetCursor();
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.WhatSystemIsSelectedForShipMerge(clickedSystemCon);

            var fleetLooking = galaxyUI.FleetLookingForShipMerge;
            var starSysLooking = galaxyUI.StarSystLookingForShipMerge;
            var shipDeployUI = ShipDeployMenuUIController.Instance;

            if (fleetLooking != null) // Fleet-to-System merge
            {
                var aSysView = StarSysMenuUIController.Instance.ASystemMenuView.gameObject;
                aSysView.gameObject.SetActive(true);

                // ✅ Add VerticalLayoutGroup if not present
                var layoutGroup = aSysView.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = aSysView.AddComponent<VerticalLayoutGroup>();
                    layoutGroup.childAlignment = TextAnchor.UpperLeft;
                    layoutGroup.spacing = 20f; // Space between fleet and system UI
                    layoutGroup.childForceExpandHeight = false;
                    layoutGroup.childForceExpandWidth = false;
                    layoutGroup.childControlHeight = false;
                    layoutGroup.childControlWidth = false;
                }

                // Parent fleet UI to container (TOP position)
                if (fleetLooking.FleetUIGameObject != null)
                {
                    fleetLooking.FleetUIGameObject.transform.SetParent(aSysView.transform, false);
                    fleetLooking.FleetUIGameObject.transform.SetAsFirstSibling();
                    fleetLooking.FleetUIGameObject.SetActive(true);
                    Debug.Log($"✅ Fleet UI parented to ASystemMenuView (top)");
                }

                // Parent system UI to container (BOTTOM position)
                clickedSystemCon.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                clickedSystemCon.StarSysUIGameObject.transform.SetAsLastSibling();
                clickedSystemCon.StarSysUIGameObject.SetActive(true);
                Debug.Log($"✅ System UI parented to ASystemMenuView (bottom)");

                // Update facility UI
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Factory);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Shipyard);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ShieldGenerator);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.OrbitalBattery);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ResearchCenter);

                // Create combined ship list
                var combinedShipsList = new List<BOTF3D.Combat.ShipController>();
                combinedShipsList.AddRange(fleetLooking.FleetData.ShipsList);
                combinedShipsList.AddRange(clickedSystemCon.StarSysData.ShipsList);

                Debug.Log($"Merge Fleet-to-System: {fleetLooking.FleetData.ShipsList.Count} + {clickedSystemCon.StarSysData.ShipsList.Count} = {combinedShipsList.Count} ships");

                shipDeployUI.SetUpTopShipLists(new List<BOTF3D.Combat.ShipController>());
                shipDeployUI.SetUpBottomShipListsForMerge(combinedShipsList, null, fleetLooking, null, clickedSystemCon);
            }
            else if (starSysLooking != null && starSysLooking != this)
            {
                // System-to-System merge - same approach
                var aSysView = StarSysMenuUIController.Instance.ASystemMenuView.gameObject;
                aSysView.gameObject.SetActive(true);

                var layoutGroup = aSysView.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = aSysView.AddComponent<VerticalLayoutGroup>();
                    layoutGroup.childAlignment = TextAnchor.UpperLeft;
                    layoutGroup.spacing = 20f;
                    layoutGroup.childForceExpandHeight = false;
                    layoutGroup.childForceExpandWidth = false;
                }

                // Source system at TOP
                starSysLooking.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                starSysLooking.StarSysUIGameObject.transform.SetAsFirstSibling();
                starSysLooking.StarSysUIGameObject.SetActive(true);

                // Target system at BOTTOM
                clickedSystemCon.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                clickedSystemCon.StarSysUIGameObject.transform.SetAsLastSibling();
                clickedSystemCon.StarSysUIGameObject.SetActive(true);

                // Update both
                StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.Factory);
                StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.Shipyard);
                StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.ShieldGenerator);
                StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.OrbitalBattery);
                StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.ResearchCenter);

                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Factory);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Shipyard);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ShieldGenerator);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.OrbitalBattery);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ResearchCenter);

                var combinedShipsList = new List<BOTF3D.Combat.ShipController>();
                combinedShipsList.AddRange(starSysLooking.StarSysData.ShipsList);
                combinedShipsList.AddRange(clickedSystemCon.StarSysData.ShipsList);

                Debug.Log($"Merge System-to-System: {starSysLooking.StarSysData.ShipsList.Count} + {clickedSystemCon.StarSysData.ShipsList.Count} = {combinedShipsList.Count} ships");

                shipDeployUI.SetUpTopShipLists(new List<BOTF3D.Combat.ShipController>());
                shipDeployUI.SetUpBottomShipListsForMerge(combinedShipsList, null, null, starSysLooking, clickedSystemCon);
            }

            shipDeployUI.ShowShipDeployMenuView();
        }
        private void HandleShipDeploySelection(StarSysController clickedSystemCon)
        {
            if (clickedSystemCon != this) return;
            deployNotMerge = true;
            MousePointerChanger.Instance.ResetCursor();
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.WhatSystemIsSelectedForShipDeploy(clickedSystemCon);
            var fleetLooking = galaxyUI.FleetLookingForShipDeploy;
            var starSysLooking = galaxyUI.StarSystLookingForShipDeploy;

            if (fleetLooking == null && starSysLooking != null)
            {
                // Star system to star system deploy
                var aSysView = StarSysUI.ASystemMenuView.gameObject;
                aSysView.SetActive(true);

                // Parent the LOOKING star system UI (top)
                if (starSysLooking.StarSysUIGameObject != null)
                {
                    starSysLooking.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                    starSysLooking.StarSysUIGameObject.transform.SetAsFirstSibling();
                    starSysLooking.StarSysUIGameObject.SetActive(true);

                    // ✅ Update the LOOKING system's UI values
                    StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.Factory);
                    StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.Shipyard);
                    StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.ShieldGenerator);
                    StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.OrbitalBattery);
                    StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.ResearchCenter);

                    // Update mini map position for LOOKING system
                    var lookingSysUIFields = starSysLooking.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                    if (lookingSysUIFields != null && lookingSysUIFields.redDot != null)
                    {
                        Vector3 lookingPos = starSysLooking.transform.localPosition;
                        lookingSysUIFields.redDot.anchoredPosition = GalaxyPositionBounds.ToMiniMapPosition(lookingPos);
                        Debug.Log($"Updated mini map for LOOKING system '{starSysLooking.name}'");
                    }
                }

                // Parent the CLICKED star system UI (bottom)
                clickedSystemCon.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                clickedSystemCon.StarSysUIGameObject.transform.SetAsLastSibling();
                clickedSystemCon.StarSysUIGameObject.SetActive(true);

                // ✅ Update THIS (clicked) system's UI values
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Factory);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Shipyard);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ShieldGenerator);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.OrbitalBattery);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ResearchCenter);

                // Update mini map position for THIS system
                var thisSysUIFields = this.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                if (thisSysUIFields != null && thisSysUIFields.redDot != null)
                {
                    Vector3 thisPos = this.transform.localPosition;
                    thisSysUIFields.redDot.anchoredPosition = GalaxyPositionBounds.ToMiniMapPosition(thisPos);
                    Debug.Log($"Updated mini map for clicked system '{this.name}'");
                }
            }
            else if (fleetLooking != null && starSysLooking == null)
            {
                // Fleet to star system deploy
                // ✅ Use ASystemMenuView (not AFleetMenuView) to match the proven-working
                // "star system to star system deploy" branch above and HandleMergeSelection's
                // Fleet-to-System case — this is the container with the VerticalLayoutGroup
                // set up for stacking two prefab UIs on the left side of the deploy panel.
                var aSysView = StarSysUI.ASystemMenuView.gameObject;
                aSysView.SetActive(true);

                // Parent fleet UI (top)
                if (fleetLooking.FleetUIGameObject != null)
                {
                    fleetLooking.FleetUIGameObject.transform.SetParent(aSysView.transform, false);
                    fleetLooking.FleetUIGameObject.transform.SetAsFirstSibling();
                    fleetLooking.FleetUIGameObject.SetActive(true);
                }

                // Parent THIS system UI (bottom)
                clickedSystemCon.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                clickedSystemCon.StarSysUIGameObject.transform.SetAsLastSibling();
                clickedSystemCon.StarSysUIGameObject.SetActive(true);

                // ✅ Update THIS system's UI values
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Factory);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Shipyard);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ShieldGenerator);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.OrbitalBattery);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ResearchCenter);

                // Update mini map position
                var sysUIFields = this.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                if (sysUIFields != null && sysUIFields.redDot != null)
                {
                    Vector3 sysPos = this.transform.localPosition;
                    sysUIFields.redDot.anchoredPosition = GalaxyPositionBounds.ToMiniMapPosition(sysPos);
                    Debug.Log($"Updated mini map for system '{this.name}' in fleet-to-system deploy");
                }
            }

            if (fleetLooking != null)
            {
                ShipDeployMenuUIController.Instance.SetUpTopShipLists(fleetLooking.FleetData.ShipsList);
            }
            else if (starSysLooking != null)
            {
                ShipDeployMenuUIController.Instance.SetUpTopShipLists(starSysLooking.StarSysData.ShipsList);
            }
            else
            {
                ShipDeployMenuUIController.Instance.SetUpTopShipLists(clickedSystemCon.StarSysData.ShipsList);
            }

            ShipDeployMenuUIController.Instance.SetUpBottomShipLists(clickedSystemCon, deployNotMerge);
            ShipDeployMenuUIController.Instance.ShowShipDeployMenuView();

            Debug.Log($"HandleShipDeploySelection: ShipDeploy opened for system '{this.name}'");
        }
        private void HandleDestinationClick(StarSysController clickedSystemCon)
        {
            var galaxyUI = GalaxyMenuUIController.Instance;
            if (galaxyUI == null)
            {
                Debug.LogWarning("HandleDestinationClick: GalaxyMenuUIController.Instance is NULL - click ignored.");
                return;
            }

            FleetController theFleetConLookingForDestination = galaxyUI.FleetLookingForDestination;
            if (theFleetConLookingForDestination == null)
            {
                Debug.LogWarning($"HandleDestinationClick: galaxyUI.FleetLookingForDestination is NULL when clicking system '{clickedSystemCon.name}' - SetDestination mode was not armed for a fleet, click ignored.");
                return;
            }

            Debug.Log($"HandleDestinationClick: setting destination='{clickedSystemCon.name}' for fleet '{theFleetConLookingForDestination.name}'.");

            // ✅ Destroy any existing PlayerDefinedTarget before setting new destination
            if (theFleetConLookingForDestination.TargetController != null)
            {
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(theFleetConLookingForDestination);
            }

            // Set the destination
            theFleetConLookingForDestination.FleetData.Destination = this.gameObject;
            theFleetConLookingForDestination.SetAsDestinationInUI(clickedSystemCon.gameObject);

            // Reset mode and cursor
            galaxyUI.CompleteSetDestination();
            MousePointerChanger.Instance?.ResetCursor();
        }


        public void LoadAStarSystem()
        {
            HandleNormalClick(this);
        }
        private void HandleNormalClick(StarSysController clickedSystemCon)
        {
            GalaxyUI.CloseShipDeployMenu();
            if (clickedSystemCon == null) return;
            if (clickedSystemCon == this)
            {
                if (gameController.AreWeLocalPlayer(clickedSystemCon.StarSysData.CurrentOwnerCivEnum))
                {
                    // Our own system - open system UI
                    StarSysUI.SetActiveSetParentUIGO(this);
                    StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Factory);
                    StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Shipyard);
                    StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ShieldGenerator);
                    StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.OrbitalBattery);
                    StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ResearchCenter);
                    GalaxyUI.OpenMenu(Menu.ASystemMenu, clickedSystemCon.gameObject);
                }
                else
                {
                    // ✅ Check if this is an uninhabited system (CivEnum >= 158)
                    int firstUninhabited = (int)CivEnum.ZZUNINHABITED1; // 158

                    if ((int)this.StarSysData.CurrentOwnerCivEnum >= firstUninhabited)
                    {
                        // ✅ Uninhabited system - show colonization UI (if habitable)
                        if (this.StarSysData.IsHabitable)
                        {
                            Debug.Log($"Clicked uninhabited habitable system '{this.StarSysData.SysName}' - showing colonization UI");

                            // Show habitable system UI for colonization
                            HabitableSysUIController.Instance?.LoadHabitableSysUI(this, CivManager.Instance.LocalPlayerCivController);
                        }
                        else
                        {
                            Debug.Log($"Clicked uninhabited non-habitable system '{this.StarSysData.SysName}' - no action");
                            // Could show a "System scanned - not habitable" message
                        }
                    }
                    else if (DiplomacyManager.Instance.FoundADiplomacyController(CivManager.Instance.LocalPlayerCivController, this.StarSysData.CurrentCivController))
                    {
                        // ✅ Foreign owned system (real civilization) - open diplomacy
                        Debug.Log($"Clicked known foreign system '{this.StarSysData.SysName}' owned by {this.StarSysData.CurrentOwnerCivEnum}");
                        DiplomacyManager.Instance.ResolveDiplomacyForClickSystemWeKnow(CivManager.Instance.LocalPlayerCivController, this);
                    }
                    else
                    {
                        Debug.Log($"Clicked unknown foreign system '{this.StarSysData.SysName}' owned by {this.StarSysData.CurrentOwnerCivEnum} - no diplomacy controller found");
                        // First contact should happen via fleet OnTriggerEnter, not clicking
                    }
                }
            }
        }
        void OnTriggerEnter(Collider collider) // Not using OnCollisionEnter....
        {

        }

        public void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnRandomSpecialEvent += DoDisaster;
                TimeManager.Instance.OnTurnPhaseChanged += OnTurnPhaseChanged;
            }
        }
        public void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnRandomSpecialEvent -= DoDisaster;
                TimeManager.Instance.OnTurnPhaseChanged -= OnTurnPhaseChanged;
            }
        }

        private void OnTurnPhaseChanged(TurnPhase phase)
        {
            if (phase != TurnPhase.TurnProgression || StarSysBuildManager == null) return;
            if (!StarSysBuildManager.IsBuildingFacility && sysBuildQueueList.Count > 0)
                StarSysBuildManager.StartNextFacilityBuildIfAny();
            if (!StarSysBuildManager.IsBuildingShip && sysShipBuildQueueList.Count > 0)
                StarSysBuildManager.StartNextShipBuildIfAny();
        }
        private void DoDisaster(TrekRandomEventSO randomSpecialEvent)
        {
            if (randomSpecialEvent != null)
            {
                Debug.Log("Special event reached StarSystemController: " + randomSpecialEvent.eventName + " on oneInXChance " +
                    randomSpecialEvent.oneInXChance + " TrekRandomEvents: " + randomSpecialEvent.trekEventType +
                    " parameter: " + randomSpecialEvent.eventParameter);
                // Add your logic to handle the special event here
                switch (randomSpecialEvent.trekEventType)
                {
                    case TrekRandomEvents.AsteroidHit:
                        {
                            // ToDo: Do Disaster code for each disaster 
                            Debug.Log("******** Asteroid ***********"); ;
                            break;
                        }
                    case TrekRandomEvents.Pandemic:
                        {
                            Debug.Log("********** PANDEMIC **********");
                            break;
                        }
                    case TrekRandomEvents.SuperVolcano:
                        {
                            Debug.Log("********** SUPER VOLCANO **********");
                            break;
                        }
                    case TrekRandomEvents.GamaRayBurst:
                        {
                            Debug.Log("********** GAMERAY BURST **********");
                            break;
                        }

                    case TrekRandomEvents.SeismicEvent:
                        {
                            Debug.Log("********** SEISMEIC EVENT **********");
                            break;
                        }
                    case TrekRandomEvents.Teribals:
                        {
                            Debug.Log("********** TERIBAL TROUBLE **********");
                            break;
                        }
                    default:
                        break;
                }
            }
        }
        public void BuildClick(StarSysController sysCon) // open build and ship build list UI
        {
            StarSysManager.Instance.InstantiateSysBuildUI(this);
            // Do NOT call GalaxyUI.OpenMenu(BuildMenu) — that triggers CloseCurrentMenu(),
            // which hides ASystemMenuView before the build queue has even opened.
        }
        public void ShipClick(StarSysController sysCon) // open build and ship build list UI
        {
            StarSysManager.Instance.InstantiateSysBuildUI(this);
        }
        public void FactoryButtonOnClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                // Get the power overload image for THIS specific system
                GameObject powerOverloadImg = GetPowerOverloadImage();

                // Do we have enough power to turn a factory on?
                if (this.StarSysData.TotalSysPowerLoad + StarSysData.FactoryData.PowerLoad >
                        this.StarSysData.TotalSysPowerOutput)
                {
                    if (powerOverloadImg != null)
                        CoroutineRunner.FlashPowerOverload(powerOverloadImg);
                }
                for (int i = 0; i < this.StarSysData.Factories.Count; i++)
                {
                    if (StarSysData.Factories[i].GetComponent<TextMeshProUGUI>().text == "0")
                    {
                        if (this.StarSysData.TotalSysPowerLoad + StarSysData.FactoryData.PowerLoad <=
                            this.StarSysData.TotalSysPowerOutput)
                        {
                            this.StarSysData.TotalSysPowerLoad += StarSysData.FactoryData.PowerLoad;
                            StarSysData.Factories[i].GetComponent<TextMeshProUGUI>().text = "1";
                            StarSysUI.UpdateFacilityUI(this, 1, StarSysFacilityType.Factory);
                            break;
                        }
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }

        // Add this helper method to StarSysController
        private GameObject GetPowerOverloadImage()
        {
            if (StarSysUIGameObject != null)
            {
                var sysUIFields = StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                if (sysUIFields != null && sysUIFields.PowerOverload != null)
                {
                    return sysUIFields.PowerOverload;
                }
            }
            return null;
        }
        public void FactoryButtonOffClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                for (int i = 0; i < this.StarSysData.Factories.Count; i++)
                {
                    if (StarSysData.Factories[i].GetComponent<TextMeshProUGUI>().text == "1")
                    {
                        StarSysData.Factories[i].GetComponent<TextMeshProUGUI>().text = "0";
                        this.StarSysData.TotalSysPowerLoad -= StarSysData.FactoryData.PowerLoad;
                        StarSysUI.UpdateFacilityUI(this, -1, StarSysFacilityType.Factory);
                        break;
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }
        public void YardButtonOnClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                if (this.StarSysData.TotalSysPowerLoad + StarSysData.ShipyardData.PowerLoad >
                        this.StarSysData.TotalSysPowerOutput)
                {
                    CoroutineRunner.FlashPowerOverload(goForPowerOverload);
                }
                for (int i = 0; i < this.StarSysData.Shipyards.Count; i++)
                {
                    if (StarSysData.Shipyards[i].GetComponent<TextMeshProUGUI>().text == "0")
                    {
                        if (this.StarSysData.TotalSysPowerLoad + StarSysData.ShipyardData.PowerLoad <=
                            this.StarSysData.TotalSysPowerOutput)
                        {
                            StarSysData.Shipyards[i].GetComponent<TextMeshProUGUI>().text = "1";
                            this.StarSysData.TotalSysPowerLoad += StarSysData.ShipyardData.PowerLoad;
                            StarSysUI.UpdateFacilityUI(this, 1, StarSysFacilityType.Shipyard);
                            break;
                        }
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }
        public void YardButtonOffClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                for (int i = 0; i < this.StarSysData.Shipyards.Count; i++)
                {
                    if (StarSysData.Shipyards[i].GetComponent<TextMeshProUGUI>().text == "1")
                    {
                        StarSysData.Shipyards[i].GetComponent<TextMeshProUGUI>().text = "0";
                        this.StarSysData.TotalSysPowerLoad -= StarSysData.ShipyardData.PowerLoad;
                        StarSysUI.UpdateFacilityUI(this, -1, StarSysFacilityType.Shipyard);
                        break;
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }
        public void ShieldButtonOnClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                if (this.StarSysData.TotalSysPowerLoad + StarSysData.ShieldGeneratorData.PowerLoad >
                        this.StarSysData.TotalSysPowerOutput)
                {
                    CoroutineRunner.FlashPowerOverload(goForPowerOverload);

                }
                for (int i = 0; i < this.StarSysData.ShieldGenerators.Count; i++)
                {
                    if (StarSysData.ShieldGenerators[i].GetComponent<TextMeshProUGUI>().text == "0")
                    {
                        if (this.StarSysData.TotalSysPowerLoad + StarSysData.ShieldGeneratorData.PowerLoad <=
                            this.StarSysData.TotalSysPowerOutput)
                        {
                            StarSysData.ShieldGenerators[i].GetComponent<TextMeshProUGUI>().text = "1";
                            this.StarSysData.TotalSysPowerLoad += StarSysData.ShieldGeneratorData.PowerLoad;
                            StarSysUI.UpdateFacilityUI(this, 1, StarSysFacilityType.ShieldGenerator);
                            break;
                        }
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }
        public void ShieldButtonOffClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                for (int i = 0; i < this.StarSysData.ShieldGenerators.Count; i++)
                {
                    if (StarSysData.ShieldGenerators[i].GetComponent<TextMeshProUGUI>().text == "1")
                    {
                        StarSysData.ShieldGenerators[i].GetComponent<TextMeshProUGUI>().text = "0";
                        this.StarSysData.TotalSysPowerLoad -= StarSysData.ShieldGeneratorData.PowerLoad;
                        StarSysUI.UpdateFacilityUI(this, -1, StarSysFacilityType.ShieldGenerator);
                        break;
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }
        public void OBButtonOnClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
                if (this.StarSysData.TotalSysPowerLoad + StarSysData.OrbitalBatteryData.PowerLoad >
                            this.StarSysData.TotalSysPowerOutput)
                {
                    CoroutineRunner.FlashPowerOverload(goForPowerOverload);
                }
            for (int i = 0; i < this.StarSysData.OrbitalBatteries.Count; i++)
            {
                if (StarSysData.OrbitalBatteries[i].GetComponent<TextMeshProUGUI>().text == "0")
                {
                    if (this.StarSysData.TotalSysPowerLoad + StarSysData.OrbitalBatteryData.PowerLoad <=
                        this.StarSysData.TotalSysPowerOutput)
                    {
                        StarSysData.OrbitalBatteries[i].GetComponent<TextMeshProUGUI>().text = "1";
                        this.StarSysData.TotalSysPowerLoad += StarSysData.OrbitalBatteryData.PowerLoad;
                        StarSysUI.UpdateFacilityUI(this, 1, StarSysFacilityType.OrbitalBattery);
                        break;
                    }
                }
            }
            StarSysUI.UpdateSystemPowerBalance(this);
        }
        public void OBButtonOffClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                for (int i = 0; i < this.StarSysData.OrbitalBatteries.Count; i++)
                {
                    if (StarSysData.OrbitalBatteries[i].GetComponent<TextMeshProUGUI>().text == "1")
                    {
                        StarSysData.OrbitalBatteries[i].GetComponent<TextMeshProUGUI>().text = "0";
                        this.StarSysData.TotalSysPowerLoad -= StarSysData.OrbitalBatteryData.PowerLoad;
                        StarSysUI.UpdateFacilityUI(this, -1, StarSysFacilityType.OrbitalBattery);
                        break;
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }
        public void ResearchButtonOnClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                if (this.StarSysData.TotalSysPowerLoad + StarSysData.ResearchCenterData.PowerLoad >
                         this.StarSysData.TotalSysPowerOutput)
                {
                    CoroutineRunner.FlashPowerOverload(goForPowerOverload);
                }
                for (int i = 0; i < this.StarSysData.ResearchCenters.Count; i++)
                {
                    if (StarSysData.ResearchCenters[i].GetComponent<TextMeshProUGUI>().text == "0")
                    {
                        if (this.StarSysData.TotalSysPowerLoad + StarSysData.ResearchCenterData.PowerLoad <=
                            this.StarSysData.TotalSysPowerOutput)
                        {
                            StarSysData.ResearchCenters[i].GetComponent<TextMeshProUGUI>().text = "1";
                            this.StarSysData.TotalSysPowerLoad += StarSysData.ResearchCenterData.PowerLoad;
                            StarSysUI.UpdateFacilityUI(this, 1, StarSysFacilityType.ResearchCenter);
                            break;
                        }
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }
        public void ResearchButtonOffClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                for (int i = 0; i < this.StarSysData.ResearchCenters.Count; i++)
                {
                    if (StarSysData.ResearchCenters[i].GetComponent<TextMeshProUGUI>().text == "1")
                    {
                        StarSysData.ResearchCenters[i].GetComponent<TextMeshProUGUI>().text = "0";
                        this.StarSysData.TotalSysPowerLoad -= StarSysData.ResearchCenterData.PowerLoad;
                        StarSysUI.UpdateFacilityUI(this, -1, StarSysFacilityType.ResearchCenter);
                        break;
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }

        private void OnDestroy()
        {
            if (FischlWorks_FogWar.csFogWar.Instance != null && transform != null)
                FischlWorks_FogWar.csFogWar.Instance.RemoveRevealer(transform);

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnRandomSpecialEvent -= DoDisaster;
                TimeManager.Instance.OnTurnPhaseChanged -= OnTurnPhaseChanged;
            }
        }
        public void CleanupStarSysUIs()
        {
            foreach (var starSysCon in StarSysManager.Instance.StarSysControllerList)
            {
                if (starSysCon.StarSysUIGameObject == null)
                    continue;

                if (!starSysCon.StarSysUIGameObject.activeInHierarchy)
                {
                    starSysCon.StarSysUIGameObject = null;
                }
            }
        }


        internal void RemoveFromShipList(ShipController shipController)
        {
            // Remove from model list
            if (shipController == null) return;
            StarSysData.RemoveFromShipList(shipController);

            // If the ship controller GO is parented to this system (under the GalaxyCenter go), unparent it to scene root.
            if (shipController.transform.IsChildOf(transform))
                shipController.transform.SetParent(null, worldPositionStays: true);
        }

        /// <summary>
        /// Removes one built orbital-battery facility (the icon/UI widget in StarSysData.OrbitalBatteries,
        /// not a combat ShipController) — called when a battery's ShipController is destroyed in combat,
        /// so the built count stops overstating what's actually left defending the system. Picks the last
        /// entry, releases its power load if it was toggled on, and destroys its widget GameObject.
        /// </summary>
        internal void RemoveOrbitalBatteryFacility()
        {
            var batteries = StarSysData?.OrbitalBatteries;
            if (batteries == null || batteries.Count == 0) return;

            int lastIndex = batteries.Count - 1;
            GameObject facilityGO = batteries[lastIndex];
            batteries.RemoveAt(lastIndex);

            if (facilityGO != null)
            {
                var text = facilityGO.GetComponent<TextMeshProUGUI>();
                if (text != null && text.text == "1")
                    StarSysData.TotalSysPowerLoad -= StarSysData.OrbitalBatteryData.PowerLoad;

                Destroy(facilityGO);
            }

            if (StarSysUI != null)
            {
                StarSysUI.UpdateFacilityUI(this, -1, StarSysFacilityType.OrbitalBattery);
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }

        public void AddToShipList(ShipController shipController)
        {
            if (shipController == null) return;

            // Reparent gameplay ship under this star system in the scene
            shipController.transform.SetParent(transform, worldPositionStays: true);

            // Add to model list
            if (!StarSysData.ShipsList.Contains(shipController))
                StarSysData.AddToShipList(shipController);

            // Move UI element under system UI parent if available
            if (shipController.ShipListUIGameObject != null && StarSysData.ShipListUIParent != null)
                shipController.ShipListUIGameObject.transform.SetParent(StarSysData.ShipListUIParent.transform, false);
        }

        // ---------------------------------------------------------------------------------------
        // Ship roster replication backstop. StarSysController isn't a NetworkBehaviour (see
        // StarSysManager.GetStarSysControllerByInt's comment) so it can't host its own [Command]/
        // [ClientRpc] pair the way FleetController.RequestSyncShipRoster does - this relays through
        // LocalHumanPlayerController (a per-connection NetworkBehaviour every client already has)
        // to TimeManager.ServerSyncStarSysRoster, which broadcasts back out to every peer. The real,
        // authoritative fix for a single ship's move is the per-transfer Cmd path
        // (LocalHumanPlayerController.CmdTransferShipXXX -> FleetManager.ServerTransferShipXXX ->
        // FleetController.RequestSyncShipRoster / TimeManager.ServerSyncStarSysRoster); call this
        // instead wherever a whole StarSysData.ShipsList gets rebuilt client-locally from UI slot
        // state (see ShipDeployMenuUIController's DeployShipUIgoX methods) as a defensive resync so
        // any drift there still reaches every peer.
        // ---------------------------------------------------------------------------------------
        public void RequestSyncShipRoster()
        {
            List<int> shipIDs = StarSysData.ShipsList
                .Where(s => s != null && s.ShipData != null)
                .Select(s => s.ShipData.ShipID)
                .ToList();

            PlayerManager.Instance?.LocalPlayerController?.SubmitSyncStarSysRoster(StarSysData.GetStarSysInt(), shipIDs);
        }
    }
}
