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

            if (ShipData != null && ShipData.ShipSO != null)
            {
                if (ShipData.ShieldHealth == 0 && ShipData.HullHealth == 0)
                {
                    ShipData.ShieldHealth = ShipData.ShipSO.ShieldMaxHealth;
                    ShipData.HullHealth = ShipData.ShipSO.HullMaxHealth;
                }
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
            if (ShipData?.ShipSO != null) return ShipData.ShipSO.ShieldMaxHealth + ShipData.ShipSO.HullMaxHealth;
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
            // ✅ Safety checks: Don't fire if destroyed, no target, or a transport
            if (ShipData == null || ShipData.Distroyed || ShipData.ShipType == ShipType.Transport) return;
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

            // Calculate Combat Order Multiplier (RPS logic)
            CombatOrders myOrder = combatController.CombatData.SideOneShipCons.Contains(this)
                ? combatController.CombatData.SideOneOrder
                : combatController.CombatData.SideTwoOrder;
            CombatOrders targetOrder = combatController.CombatData.SideOneShipCons.Contains(target)
                ? combatController.CombatData.SideOneOrder
                : combatController.CombatData.SideTwoOrder;

            float rpsMultiplier = CombatOrderHelper.GetOrderMultiplier(myOrder, targetOrder);

            if (beam && ShipData.BeamDamage > 0)
            {
                FireBeam(target.transform, rpsMultiplier);
            }
            else if (!beam && ShipData.TorpedoDamage > 0)
            {
                FireTorpedo(target.transform, rpsMultiplier);
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
                if (ShipData == null || ShipData.Distroyed) yield break;

                if (ShipData.TargetThisShipController == null ||
                    ShipData.TargetThisShipController.ShipData.Distroyed ||
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

                // First shot: torpedo if available
                if (!hasFiredTorpedo && ShipData.TorpedoDamage > 0)
                {
                    FireWeapons(false); // Fire torpedo
                    hasFiredTorpedo = true;
                }
                else
                {
                    // All subsequent shots: beams only
                    FireWeapons(true); // Fire beam
                }

                float refireDelay = UnityEngine.Random.Range(minRefireDelay, maxRefireDelay);
                yield return new WaitForSecondsRealtime(refireDelay);
            }
        }

        /// <summary>
        /// Fire torpedo at target with proper initialization and combat order multiplier
        /// </summary>
        private void FireTorpedo(Transform targetTransform, float damageMultiplier)
        {
            if (torpedoPrefab == null || targetTransform == null) return;

            // Spawn torpedo in the direction of the target so it always appears in front,
            // regardless of which way transform.forward points vs the visual model nose.
            Vector3 toTarget = (targetTransform.position - transform.position).normalized;
            Vector3 firePosition = transform.position + toTarget * 15f;
            Quaternion fireRotation = Quaternion.LookRotation(toTarget);

            // Instantiate torpedo
            GameObject torpedoGO = Instantiate(torpedoPrefab, firePosition, fireRotation);
            Torpedo torpedo = torpedoGO.GetComponent<Torpedo>();

            if (torpedo != null)
            {
                torpedo.Target = targetTransform;
                torpedo.OwnerCivEnum = ShipData.CivEnum;

                // ✅ Apply combat order multiplier to base damage
                int calculatedDamage = Mathf.RoundToInt(ShipData.TorpedoDamage * damageMultiplier);

                // Torpedo must outrun the firing ship; Rush ships move at maxWarpFactor * 28 units/sec
                // We ensure torpedo is significantly faster (min 400 or maxWarpFactor * 60)
                float torpedoVelocity = Mathf.Max(400f, ShipData.maxWarpFactor * 60f);
                torpedo.Initialize(calculatedDamage, clipTorpedoFire, null, torpedoVelocity);

                Debug.Log($"🚀 {ShipData.ShipName} fired torpedo at {targetTransform.name} (Base Damage: {ShipData.TorpedoDamage}, Multiplier: {damageMultiplier:F2}, Velocity: {torpedoVelocity})");
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
        private void FireBeam(Transform targetTransform, float damageMultiplier)
        {
            if (beamWeaponPrefab == null || targetTransform == null) return;

            // Instantiate beam weapon
            GameObject beamGO = Instantiate(beamWeaponPrefab, transform.position, Quaternion.identity);
            BeamWeapon beam = beamGO.GetComponent<BeamWeapon>();

            if (beam != null)
            {
                // Set weapon and target transforms for rendering
                beam.SetWeaponAndTarget(transform, targetTransform);

                // Track active beam
                activeBeamWeapons.Add(beamGO);

                // ✅ Apply combat order multiplier to base damage
                int calculatedBaseDamage = Mathf.RoundToInt(ShipData.BeamDamage * damageMultiplier);

                beam.Initialize(
                    this,
                    calculatedBaseDamage,
                    clipBeamFire,
                    null
                );

                // Fire immediately (applies distance falloff inside beam.Fire)
                ShipController targetShip = targetTransform.GetComponentInParent<ShipController>();
                if (targetShip != null)
                {
                    beam.Fire(targetShip);
                }

                Debug.Log($"   {ShipData.ShipName} fired beam at {targetTransform.name} (Base Damage: {ShipData.BeamDamage}, Multiplier: {damageMultiplier:F2})");

                // Destroy beam after brief display
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
            if (ShipData == null || ShipData.Distroyed) return;

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
                const float SHIELD_OVERLAP_RANGE = 55f;
                bool isSideOne = cc.CombatData.SideOneShipCons.Contains(this);
                var friendlies = isSideOne ? cc.CombatData.SideOneShipCons : cc.CombatData.SideTwoShipCons;
                int nearby = friendlies.Count(s => s != null && s != this && !s.ShipData.Distroyed
                                                && s.gameObject.activeInHierarchy
                                                && Vector3.Distance(transform.position, s.transform.position) <= SHIELD_OVERLAP_RANGE);
                float overlapMultiplier = Mathf.Max(0.5f, 1f - (nearby * 0.1f));
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
                ShipData.Distroyed = true;
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

        public void SetWarpInOver() => WarpingInOver = true;
        public void OnShipEncounteredShip(ShipController shipController) { }
        public void OnShipEncounteredOther(StarSysController StarSysController) { }
        public void ValidateOwnership() { }
    }
}
