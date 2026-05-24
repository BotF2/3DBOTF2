using BOTF3D.Core;
using BOTF3D.GamePlay;
using BOTF3D.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;

namespace BOTF3D.Combat
{
    public class ShipController : MonoBehaviour
    {
        private ShipData shipData;
        public ShipData ShipData { get { return shipData; } set { shipData = value; } }
        public string Name;
        public GameObject torpedoPrefab;
        public GameObject beamWeaponPrefab;
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
            if (ShipData == null || ShipData.Distroyed || ShipData.TargetThisShipController == null || ShipData.TargetThisShipController.ShipData.Distroyed) return;

            ShipController target = ShipData.TargetThisShipController;
            var combatController = CombatUIManager.Instance?.CurrentCombatController;
            if (combatController == null) return;

            // ✅ General Line of Sight Check
            List<ShipController> potentialBlockers = new List<ShipController>();
            potentialBlockers.AddRange(combatController.CombatData.SideOneShipCons);
            potentialBlockers.AddRange(combatController.CombatData.SideTwoShipCons);

            ShipController blocker = LineOfSightBlocker.GetBlockingShip(transform.position, target.transform.position, potentialBlockers);
            if (blocker != null && blocker != target && blocker != this)
            {
                // Shot is blocked! Hit the blocker instead.
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
            if (initialDelay > 0)
                yield return new WaitForSecondsRealtime(initialDelay);

            bool beam = true;
            while (true)
            {
                if (ShipData == null || ShipData.Distroyed) yield break;

                if (ShipData.TargetThisShipController == null || ShipData.TargetThisShipController.ShipData.Distroyed)
                {
                    // Wait until a new target is assigned
                    yield return new WaitForSecondsRealtime(0.5f);
                    continue;
                }

                FireWeapons(beam);
                beam = !beam;

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

            // Calculate fire position (slightly in front of ship)
            Vector3 firePosition = transform.position + transform.forward * 5f;
            Quaternion fireRotation = Quaternion.LookRotation(targetTransform.position - firePosition);

            // Instantiate torpedo
            GameObject torpedoGO = Instantiate(torpedoPrefab, firePosition, fireRotation);
            Torpedo torpedo = torpedoGO.GetComponent<Torpedo>();

            if (torpedo != null)
            {
                torpedo.Target = targetTransform;
                torpedo.OwnerCivEnum = ShipData.CivEnum;
                
                // ✅ Apply combat order multiplier to base damage
                int calculatedDamage = Mathf.RoundToInt(ShipData.TorpedoDamage * damageMultiplier);

                torpedo.Initialize(
                    calculatedDamage,
                    clipTorpedoFire,
                    null
                );

                Debug.Log($"🚀 {ShipData.ShipName} fired torpedo at {targetTransform.name} (Base Damage: {ShipData.TorpedoDamage}, Multiplier: {damageMultiplier:F2})");
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
                StartCoroutine(DestroyBeamAfterDelay(beamGO, 0.2f));
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
                if (ShipData.CurrentFleetController != null) ShipData.CurrentFleetController.RemoveShipFromFleet(this);
                if (CombatManager.Instance != null) CombatManager.Instance.RemoveThisShipController(this);
                if (ShipCombatCameraController.Instance != null) ShipCombatCameraController.Instance.OnShipDestroyed(this);

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

            for (int i = 0; i < torpedoPrefabs.Length; i++)
            {
                if (torpedoPrefabs[i].name.Contains(ShipData.CivEnum.ToString().ToUpper())) torpedoPrefab = torpedoPrefabs[i];
            }
            for (int i = 0; i < beamPrefabs.Length; i++)
            {
                if (beamPrefabs[i].name.Contains(ShipData.CivEnum.ToString().ToUpper())) beamWeaponPrefab = beamPrefabs[i];
            }
            if (torpedoPrefab == null) torpedoPrefab = torpedoPrefabs.FirstOrDefault();
            if (beamWeaponPrefab == null) beamWeaponPrefab = beamPrefabs.FirstOrDefault();
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
