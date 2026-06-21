using BOTF3D.Core;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;


namespace BOTF3D.Combat
{
    public class ShipController : MonoBehaviour, IController
    {
        public void Initialize() { }
        public void UpdateState() { }

        private void OnEnable()
        {
            GameEvents.OnShipDestroyed += HandleShipDestroyed;
        }

        private void OnDisable()
        {
            GameEvents.OnShipDestroyed -= HandleShipDestroyed;
        }

        private void HandleShipDestroyed(int shipID)
        {
            if (ShipData != null && ShipData.ShipID == shipID)
            {
                // Logic already handled in TakeDamage, but this shows the pattern
                GameLogger.Log(GameLogger.LogCategory.Combat, $"Ship {ShipData.ShipName} event received: Destroyed.", this);
            }
        }
        private ShipData shipData;
        public ShipData ShipData { get { return shipData; } set { shipData = value; } }
        public string Name;
        public GameObject torpedoPrefab;
        public GameObject beamWeaponPrefab;
        public GameObject explosionPrefab;
        public GameObject ShipListUIGameObject;
        public AudioClip clipTorpedoFire;
        public AudioClip clipBeamFire;
        public Transform TargetGroup;
        public bool WarpingInOver = false;
        private List<GameObject> activeBeamWeapons = new List<GameObject>();
        public CombatOrders Order;
        [SerializeField] private float minRefireDelay;
        [SerializeField] private float maxRefireDelay;

        // Ships in Formation stay at x=±200, giving a ~400-unit gap between sides.
        // Torpedoes require a closer launch window so Formation-vs-Retreat never gets a
        // torpedo salvo at the start of the weapon-fire phase.
        private const float TorpedoMaxRange = 350f;
        public Image HealthFillImage;
        public Image HealthBackgroundImage;
        public float HealthSpeed;
        public float TargetHealthFillAmount { get; set; } = 1.0f;

        private Vector3 lastPosition;
        private Vector3 currentVelocity;
        public Vector3 GetVelocity() => currentVelocity;

        private void Start()
        {
            minRefireDelay = 1.5f;
            maxRefireDelay = 2.5f;
            HealthSpeed = 10.0f;
            lastPosition = transform.position;

            if (ShipData != null && ShipData.ShieldHealth == 0 && ShipData.HullHealth == 0
                && ShipData.ShieldMaxHealth > 0)
            {
                ShipData.ShieldHealth = ShipData.ShieldMaxHealth;
                ShipData.HullHealth   = ShipData.HullMaxHealth;
            }
        }

        void Update()
        {
            if (HealthFillImage != null && ShipData != null)
            {
                int maxHealth = GetMaxHealth();
                int currentHealth = GetCurrentTotalHealth();
                TargetHealthFillAmount = (float)currentHealth / maxHealth;

                HealthFillImage.fillAmount = Mathf.Lerp(
                    HealthFillImage.fillAmount,
                    TargetHealthFillAmount,
                    HealthSpeed * Time.unscaledDeltaTime
                );

                if (HealthBackgroundImage != null)
                {
                    HealthBackgroundImage.color = Color.red;
                    HealthBackgroundImage.fillAmount = 1.0f;
                }

                float healthPercent = TargetHealthFillAmount;
                if (healthPercent > 0.66f) HealthFillImage.color = Color.green;
                else if (healthPercent > 0.33f) HealthFillImage.color = Color.cyan;
                else if (healthPercent > 0) HealthFillImage.color = Color.yellow;
                else HealthFillImage.color = Color.red;
            }
        }

        private int GetMaxHealth()
        {
            if (ShipData != null) return ShipData.ShieldMaxHealth + ShipData.HullMaxHealth;
            return 100;
        }

        private int GetCurrentTotalHealth()
        {
            if (ShipData != null) return ShipData.ShieldHealth + ShipData.HullHealth;
            return 0;
        }

        private void FixedUpdate()
        {
            currentVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
            lastPosition = transform.position;
        }

        public void Init(ShipManager shipManager)
        {
            // ShipManager.Instance is static and typically assigned in its Awake
        }

        void OnTriggerEnter(Collider collider)
        {
            ShipController shipController = collider.gameObject.GetComponent<ShipController>();
            if (shipController != null)
            {
                OnShipEncounteredShip(shipController);
            }
        }

        internal void FireWeapons(bool beam)
        {
            // ✅ Safety checks: Don't fire if destroyed, captured, no target, or a transport
            if (ShipData == null || ShipData.Distroyed || ShipData.IsCaptured || ShipData.ShipType == ShipType.Transport) return;
            if (ShipData.TargetThisShipController == null ||
                ShipData.TargetThisShipController.ShipData.Distroyed ||
                !ShipData.TargetThisShipController.gameObject.activeInHierarchy) return;

            ShipController target = ShipData.TargetThisShipController;

            // ✅ Friendly fire check (primary target)
            if (target.ShipData.CivEnum == this.ShipData.CivEnum)
            {
                Debug.LogWarning($"⚠️ {ShipData.ShipName} attempted to fire on friendly {target.ShipData.ShipName} - Aborting.");
                return;
            }

            var combatController = CombatUIManager.Instance?.CurrentCombatController;
            if (combatController == null) return;

            // ✅ General Line of Sight Check
            List<ShipController> potentialBlockers = new List<ShipController>();
            potentialBlockers.AddRange(combatController.CombatData.SideOneShipCons);
            potentialBlockers.AddRange(combatController.CombatData.SideTwoShipCons);

            ShipController blocker = LineOfSightBlocker.GetBlockingShip(transform.position, target.transform.position, potentialBlockers);
            if (blocker != null && blocker != target && blocker != this)
            {
                // ✅ Friendly fire check (blocker)
                // If a friendly ship is in the way, stop the shot to avoid hitting them
                if (blocker.ShipData.CivEnum == this.ShipData.CivEnum)
                {
                    Debug.Log($"🚫 {ShipData.ShipName} holding fire - {blocker.ShipData.ShipName} is in the way!");
                    return;
                }

                // Don't redirect to a transport unless this ship has AttackTransports order
                bool myOrderIsAT = combatController.CombatData.SideOneShipCons.Contains(this)
                    ? combatController.CombatData.SideOneOrder == CombatOrders.AttackTransports
                    : combatController.CombatData.SideTwoOrder == CombatOrders.AttackTransports;
                if (blocker.ShipData.ShipType == ShipType.Transport && !myOrderIsAT)
                    return;

                // Shot is blocked by an enemy! Hit the blocker instead.
                target = blocker;
            }

            if (beam && ShipData.BeamDamage > 0)
            {
                FireBeam(target.transform);
            }
            else if (!beam && ShipData.TorpedoDamage > 0)
            {
                FireTorpedo(target.transform);
            }
        }

        public void SetShipOrder(CombatOrders order)
        {
            Order = order;
        }

        public IEnumerator ShipFireLoop(float initialDelay)
        {
            // ✅ Transports don't fire weapons
            if (ShipData != null && ShipData.ShipType == ShipType.Transport)
            {
                yield break;
            }

            if (initialDelay > 0)
                yield return new WaitForSecondsRealtime(initialDelay);

            // Fire ONE torpedo salvo at start, then beams only
            bool hasFiredTorpedo = false;

            while (true)
            {
                if (ShipData == null || ShipData.Distroyed || ShipData.IsCaptured) yield break;

                if (ShipData.TargetThisShipController == null ||
                    ShipData.TargetThisShipController.ShipData.Distroyed ||
                    ShipData.TargetThisShipController.ShipData.IsCaptured ||
                    !ShipData.TargetThisShipController.gameObject.activeInHierarchy)
                {
                    // Stop permanently if no active enemies remain on the other side
                    var cc = CombatUIManager.Instance?.CurrentCombatController;
                    if (cc != null)
                    {
                        bool isSide1 = cc.CombatData.SideOneShipCons.Contains(this);
                        var enemies = isSide1 ? cc.CombatData.SideTwoShipCons : cc.CombatData.SideOneShipCons;
                        if (!enemies.Any(s => s != null && !s.ShipData.Distroyed && s.gameObject.activeInHierarchy))
                            yield break;
                    }
                    yield return new WaitForSecondsRealtime(0.5f);
                    continue;
                }

                // First shot: torpedo if available and within launch range.
                // Torpedo range is limited so Formation-vs-Retreat ships (400 u apart at start)
                // never receive a torpedo salvo; beams fire instead until ships close in.
                if (!hasFiredTorpedo && ShipData.TorpedoDamage > 0)
                {
                    float distToTarget = Vector3.Distance(
                        transform.position,
                        ShipData.TargetThisShipController.transform.position);

                    if (distToTarget <= TorpedoMaxRange)
                    {
                        FireWeapons(false); // torpedo
                        hasFiredTorpedo = true;
                    }
                    else
                    {
                        FireWeapons(true); // beam while out of torpedo range
                    }
                }
                else
                {
                    // All subsequent shots: beams only
                    FireWeapons(true); // Fire beam
                }

                float refireDelay = UnityEngine.Random.Range(minRefireDelay, maxRefireDelay);

                // Apply delay multiplier for retreating ships
                var osm = GetComponent<CombatOrderStateMachine>();
                if (osm != null && osm.CurrentOrder == CombatOrders.Retreat)
                {
                    refireDelay *= 1.2f;
                }

                // While waiting for the next beam, poll torpedo range every 0.25 s so
                // the torpedo fires as soon as ships close into range, regardless of orders.
                if (!hasFiredTorpedo && ShipData.TorpedoDamage > 0)
                {
                    float elapsed = 0f;
                    while (elapsed < refireDelay && !hasFiredTorpedo)
                    {
                        float step = Mathf.Min(0.25f, refireDelay - elapsed);
                        yield return new WaitForSecondsRealtime(step);
                        elapsed += step;

                        // Ship may have been destroyed during the yield
                        if (this == null || ShipData == null || ShipData.Distroyed) yield break;

                        var tgt = ShipData.TargetThisShipController;
                        if (tgt != null && !tgt.ShipData.Distroyed && tgt.gameObject.activeInHierarchy &&
                            Vector3.Distance(transform.position, tgt.transform.position) <= TorpedoMaxRange)
                        {
                            FireWeapons(false); // torpedo — triggered by range crossing
                            hasFiredTorpedo = true;
                            float remaining = refireDelay - elapsed;
                            if (remaining > 0f)
                                yield return new WaitForSecondsRealtime(remaining);
                        }
                    }
                }
                else
                {
                    yield return new WaitForSecondsRealtime(refireDelay);
                }
            }
        }

        /// <summary>
        /// Fire torpedo at target with proper initialization and combat order multiplier
        /// </summary>
        private void FireTorpedo(Transform targetTransform)
        {
            if (torpedoPrefab == null || targetTransform == null) return;

            // Spawn torpedo in the direction of the target so it always appears in front,
            // regardless of which way transform.forward points vs the visual model nose.
            Vector3 toTarget = (targetTransform.position - transform.position).normalized;
            Vector3 firePosition = transform.position + toTarget * 15f;
            Quaternion fireRotation = Quaternion.LookRotation(toTarget);

            GameObject torpedoGO = Instantiate(torpedoPrefab, firePosition, fireRotation);
            Torpedo torpedo = torpedoGO.GetComponent<Torpedo>();

            if (torpedo != null)
            {
                torpedo.Target = targetTransform;
                torpedo.OwnerCivEnum = ShipData.CivEnum;

                // Torpedo must outrun the firing ship; Rush ships move at maxWarpFactor * 28 units/sec
                float torpedoVelocity = Mathf.Max(400f, ShipData.maxWarpFactor * 60f);
                torpedo.Initialize(ShipData.TorpedoDamage, clipTorpedoFire, null, torpedoVelocity);

                Debug.Log($"🚀 {ShipData.ShipName} fired torpedo at {targetTransform.name} (Damage: {ShipData.TorpedoDamage}, Velocity: {torpedoVelocity})");
            }
            else
            {
                Debug.LogError($"Torpedo prefab missing Torpedo component!");
                Destroy(torpedoGO);
            }
        }

        /// <summary>
        /// Fire beam at target with proper initialization and combat order multiplier
        /// </summary>
        private void FireBeam(Transform targetTransform)
        {
            if (beamWeaponPrefab == null || targetTransform == null) return;

            GameObject beamGO = Instantiate(beamWeaponPrefab, transform.position, Quaternion.identity);
            BeamWeapon beam = beamGO.GetComponent<BeamWeapon>();

            if (beam != null)
            {
                beam.SetWeaponAndTarget(transform, targetTransform);
                activeBeamWeapons.Add(beamGO);

                beam.Initialize(this, ShipData.BeamDamage, clipBeamFire, null);

                ShipController targetShip = targetTransform.GetComponentInParent<ShipController>();
                if (targetShip != null)
                    beam.Fire(targetShip);

                Debug.Log($"   {ShipData.ShipName} fired beam at {targetTransform.name} (Damage: {ShipData.BeamDamage})");

                if (gameObject.activeInHierarchy)
                    StartCoroutine(DestroyBeamAfterDelay(beamGO, 0.2f));
                else
                    Destroy(beamGO);
            }
            else
            {
                Debug.LogError($"Beam prefab missing BeamWeapon component!");
                Destroy(beamGO);
            }
        }

        private IEnumerator DestroyBeamAfterDelay(GameObject beamGO, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (beamGO != null)
            {
                activeBeamWeapons.Remove(beamGO);
                Destroy(beamGO);
            }
        }

        public void TakeDamage(int weaponDamageInt)
        {
            if (ShipData == null || ShipData.Distroyed || ShipData.IsCaptured) return;

            // No damage after combat has ended
            if (CombatUIManager.Instance?.CurrentCombatController?.CombatEnded == true) return;

            // ✅ Ships are invulnerable during warp-out
            var orderStateMachine = GetComponent<CombatOrderStateMachine>();
            if (orderStateMachine != null && orderStateMachine.IsWarpingOut())
            {
                GameLogger.Log(GameLogger.LogCategory.Combat, $"🛡️ {ShipData.ShipName} is warping out - invulnerable to damage!", this);
                return;
            }

            // Shield overlap: nearby friendly ships provide overlapping shield coverage
            var cc = CombatUIManager.Instance?.CurrentCombatController;
            if (cc != null)
            {
                const float SHIELD_OVERLAP_RANGE = 40f;
                bool isSideOne = cc.CombatData.SideOneShipCons.Contains(this);
                var friendlies = isSideOne ? cc.CombatData.SideOneShipCons : cc.CombatData.SideTwoShipCons;
                int nearby = friendlies.Count(s => s != null && s != this && !s.ShipData.Distroyed
                                                && s.gameObject.activeInHierarchy
                                                && Vector3.Distance(transform.position, s.transform.position) <= SHIELD_OVERLAP_RANGE);
                // Each nearby friendly reduces damage by 10% (was 20%), capped at 25% max reduction (was 50%)
                float overlapMultiplier = Mathf.Max(0.75f, 1f - (nearby * 0.1f));
                weaponDamageInt = Mathf.RoundToInt(weaponDamageInt * overlapMultiplier);
            }

            if (ShipData.ShieldHealth > 0)
            {
                ShipData.ShieldHealth -= weaponDamageInt;
                if (ShipData.ShieldHealth < 0)
                {
                    int overflow = -ShipData.ShieldHealth;
                    ShipData.ShieldHealth = 0;
                    ShipData.HullHealth -= overflow;
                }
            }
            else ShipData.HullHealth -= weaponDamageInt;

            ShipData.HullHealth = Mathf.Max(ShipData.HullHealth, 0);

            if (ShipData.HullHealth <= 0)
            {
                // Check capture condition: enemy has Capture, this ship has Retreat or failed Scuttle
                var ccc = CombatUIManager.Instance?.CurrentCombatController;
                if (ccc != null)
                {
                    bool isSideOne = ccc.CombatData.SideOneShipCons.Contains(this);
                    CombatOrders myOrder = isSideOne ? ccc.CombatData.SideOneOrder : ccc.CombatData.SideTwoOrder;
                    CombatOrders enemyOrder = isSideOne ? ccc.CombatData.SideTwoOrder : ccc.CombatData.SideOneOrder;

                    bool vulnerableToCapture = myOrder == CombatOrders.Retreat || myOrder == CombatOrders.Scuttle;
                    if (enemyOrder == CombatOrders.Capture && vulnerableToCapture)
                    {
                        ShipData.IsCaptured = true;
                        ShipData.HullHealth = 0;
                        cc.CombatData.CapturedShips.Add(this);
                        DestroyAllActiveBeams();
                        Debug.Log($"🎯 {ShipData.ShipName} CAPTURED by {cc.CombatData.CivEnumSideOne}/{cc.CombatData.CivEnumSideTwo}!");
                        return;
                    }
                }

                ShipData.Distroyed = true;
                cc?.CombatData?.DestroyedShips.Add(ShipData);
                if (ShipListUIGameObject != null) ShipListUIGameObject.SetActive(false);
                if (ShipData.CurrentFleetController != null) ShipData.CurrentFleetController.RemoveShipFromFleet(this);
                if (CombatManager.Instance != null) CombatManager.Instance.RemoveThisShipController(this);
                if (ShipCombatCameraController.Instance != null) ShipCombatCameraController.Instance.OnShipDestroyed(this);

                if (explosionPrefab != null)
                {
                    Instantiate(explosionPrefab, transform.position, transform.rotation);
                    GameLogger.Log(GameLogger.LogCategory.Combat, $"💥..💥Ship Explosion {ShipData.ShipName}", this);
                }
                CombatUIManager.Instance?.CurrentCombatController?.PlayExplosionSound(transform.position);

                DestroyAllActiveBeams();
                Destroy(gameObject);
            }
        }

        private void DestroyAllActiveBeams()
        {
            foreach (var beam in activeBeamWeapons)
            {
                if (beam != null) Destroy(beam);
            }
            activeBeamWeapons.Clear();
        }

        public void SetWeaponPrefabs()
        {
            if (ShipManager.Instance == null) return;
            GameObject[] torpedoPrefabs = ShipManager.Instance.torpedoPrefabs;
            GameObject[] beamPrefabs = ShipManager.Instance.beamWeaponPrefabs;
            explosionPrefab = ShipManager.Instance.explosionPrefab;

            string civKey = ShipData.CivEnum.ToString().ToUpper();

            for (int i = 0; i < torpedoPrefabs.Length; i++)
                if (torpedoPrefabs[i] != null && torpedoPrefabs[i].name.Contains(civKey)) torpedoPrefab = torpedoPrefabs[i];
            for (int i = 0; i < beamPrefabs.Length; i++)
                if (beamPrefabs[i] != null && beamPrefabs[i].name.Contains(civKey)) beamWeaponPrefab = beamPrefabs[i];

            if (torpedoPrefab == null)
            {
                torpedoPrefab = torpedoPrefabs.FirstOrDefault();
                Debug.LogWarning($"⚠️ {ShipData.ShipName}: no torpedo prefab matched '{civKey}', using fallback '{torpedoPrefab?.name}'");
            }
            if (beamWeaponPrefab == null)
            {
                beamWeaponPrefab = beamPrefabs.FirstOrDefault();
                Debug.LogWarning($"⚠️ {ShipData.ShipName}: no beam prefab matched '{civKey}', using fallback '{beamWeaponPrefab?.name}'");
            }

            Debug.Log($"🔧 {ShipData.ShipName} ({civKey}): torpedo={torpedoPrefab?.name ?? "NULL"}, beam={beamWeaponPrefab?.name ?? "NULL"}");
        }

        public void SetWeaponAudioClips(AudioClip beamClip, AudioClip torpedoClip)
        {
            clipBeamFire = beamClip;
            clipTorpedoFire = torpedoClip;
        }

        /// <summary>
        /// Force-destroy this ship (Scuttle order: called before weapon fire begins).
        /// Identical to normal destruction path minus the capture check.
        /// </summary>
        public void SelfDestruct()
        {
            if (ShipData == null || ShipData.Distroyed) return;

            ShipData.ShieldHealth = 0;
            ShipData.HullHealth = 0;
            ShipData.Distroyed = true;
            CombatUIManager.Instance?.CurrentCombatController?.CombatData?.DestroyedShips.Add(ShipData);

            if (ShipListUIGameObject != null) ShipListUIGameObject.SetActive(false);
            if (ShipData.CurrentFleetController != null) ShipData.CurrentFleetController.RemoveShipFromFleet(this);
            if (CombatManager.Instance != null) CombatManager.Instance.RemoveThisShipController(this);
            if (ShipCombatCameraController.Instance != null) ShipCombatCameraController.Instance.OnShipDestroyed(this);

            if (explosionPrefab != null)
                Instantiate(explosionPrefab, transform.position, transform.rotation);
            CombatUIManager.Instance?.CurrentCombatController?.PlayExplosionSound(transform.position);

            DestroyAllActiveBeams();
            Destroy(gameObject);
        }

        public void SetWarpInOver() => WarpingInOver = true;
        public void OnShipEncounteredShip(ShipController shipController) { }
        public void OnShipEncounteredOther(StarSysController StarSysController) { }
        public void ValidateOwnership() { }
    }
}
