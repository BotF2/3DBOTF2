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
        private AudioSource theSource;
        public Transform TargetGroup;
        private int flipShipForward = 1;
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
            theSource = GetComponent<AudioSource>();
            if (transform.position.x < 0) flipShipForward = -1;

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
            ShipManager.Instance = shipManager;
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

            float distance = Vector3.Distance(transform.position, target.transform.position);

            CombatOrders myOrder = combatController.CombatData.SideOneShipCons.Contains(this)
                ? combatController.CombatData.SideOneOrder
                : combatController.CombatData.SideTwoOrder;
            CombatOrders targetOrder = combatController.CombatData.SideOneShipCons.Contains(target)
                ? combatController.CombatData.SideOneOrder
                : combatController.CombatData.SideTwoOrder;

            float rpsMultiplier = CombatOrderHelper.GetOrderMultiplier(myOrder, targetOrder);

            if (beam && ShipData.BeamDamage > 0)
            {
                float maxBeamRange = 600f;
                float distanceFactor = Mathf.Clamp01(1f - (distance / maxBeamRange));
                float finalDamage = ShipData.BeamDamage * distanceFactor * rpsMultiplier;

                if (finalDamage > 0) ExecuteBeamFire(target, (int)finalDamage);
            }
            else if (!beam && ShipData.TorpedoDamage > 0)
            {
                Vector3 relVel = GetVelocity() - target.GetVelocity();
                float relVelMag = relVel.magnitude;
                float maxRelVel = 100f;

                float velocityFactor = Mathf.Clamp01(1f - (relVelMag / maxRelVel));
                float finalDamage = ShipData.TorpedoDamage * velocityFactor * rpsMultiplier;

                if (finalDamage > 0) FireTorpedo(target.transform);
            }
        }

        private void ExecuteBeamFire(ShipController target, int damage)
        {
            if (clipBeamFire != null)
                BOTF3D.Audio.AudioManager.Instance?.PlaySFX3DClip(clipBeamFire, transform.position);

            if (clipTorpedoFire != null)
            {
                Debug.Log($"✅ Set torpedo fire clip for '{ShipData.ShipName}' (Civ: {ShipData.CivEnum}): {clipTorpedoFire.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Torpedo fire clip is NULL for '{ShipData.ShipName}' (Civ: {ShipData.CivEnum})");
            }
        }
        public void SetShipOrder(CombatOrders order)
        {
            Order = order;
            switch (order)
            {
                case CombatOrders.Engage:
                    Order = CombatOrders.Engage;
                    break;
                case CombatOrders.Rush:
                    Order = CombatOrders.Rush;
                    break;
                case CombatOrders.Retreat:
                    Order = CombatOrders.Retreat;
                    break;
                case CombatOrders.Formation:
                    Order = CombatOrders.Formation;
                    break;
                case CombatOrders.AttackTransports:
                    Order = CombatOrders.AttackTransports;
                    break;
                case CombatOrders.None:
                    Order = CombatOrders.None;
                    break;
                default:
                    break;
            }
        }
        //public void OnShipEncounteredShip(ShipController shipController)
        //{
        //    //1) player get the ShipController of the ship GO we hit
        //    //2) player ask your factionOwner (CivManager) 
        //}
        //public void OnShipEncounteredOther(StarSysController StarSysController)
        //{
        //    //1) player get the OtherController of the GO
        //}
        public IEnumerator ShipFireLoop(float initialDelay)
        {
            Debug.Log($"🔫 ShipFireLoop started for '{ShipData.ShipName}' with {initialDelay}s delay");

            // ✅ Use WaitForSecondsRealtime instead of WaitForSeconds
            yield return new WaitForSecondsRealtime(initialDelay);

            Debug.Log($"🔫 '{ShipData.ShipName}' starting weapon fire loop");

            bool beam = true;
            int shotCount = 0;
            while (true) // ToDo: not true when ship weapons are offline?
            {
                // ✅ CRITICAL: Check if target still exists before firing
                if (ShipData.TargetThisShipController == null || ShipData.TargetThisShipController.ShipData.Distroyed)
                {
                    Debug.Log($"🔫 '{ShipData.ShipName}' target destroyed or null - stopping fire loop");
                    yield break; // Exit the coroutine
                }
                shotCount++;
                Debug.Log($"🔫 '{ShipData.ShipName}' firing shot #{shotCount} (beam={beam})");
                // Weapon firing logic (separate from ship rotation)
                Vector3 fireDirection = (ShipData.TargetThisShipController.transform.position - transform.position).normalized;
                // Fire weapon in calculated direction, ignore ship's transform.forward
                // Fire the ship's beam weapons
                FireWeapons(beam);

                if (beam)
                    beam = false;
                else
                    beam = true;

                // Wait for a random refire delay before next shot
                float refireDelay = UnityEngine.Random.Range(minRefireDelay, maxRefireDelay);
                Debug.Log($"   Waiting {refireDelay}s before next shot...");

                // ✅ Use WaitForSecondsRealtime instead of WaitForSeconds
                yield return new WaitForSecondsRealtime(refireDelay);
            }
        }
        /// <summary>
        /// Fire torpedo at target with proper initialization
        /// </summary>
        private void FireTorpedo(Transform targetTransform)
        {
            if (torpedoPrefab == null || targetTransform == null)
            {
                Debug.LogWarning($"{ShipData.ShipName}: Cannot fire torpedo - missing prefab or target");
                return;
            }

            // Calculate fire position (slightly in front of ship)
            Vector3 firePosition = transform.position + transform.forward * 5f;
            Quaternion fireRotation = Quaternion.LookRotation(targetTransform.position - firePosition);

            // Instantiate torpedo
            GameObject torpedoGO = Instantiate(torpedoPrefab, firePosition, fireRotation);
            Torpedo torpedo = torpedoGO.GetComponent<Torpedo>();

            if (torpedo != null)
            {
                // Set target
                torpedo.Target = targetTransform;
                torpedo.OwnerCivEnum = ShipData.CivEnum;

                // ✅ Initialize with ship's damage and sounds
                torpedo.Initialize(
                    ShipData.TorpedoDamage,    // From ShipSO
                    clipTorpedoFire,            // From ShipController
                    null                        // Optional impact sound (can add later)
                );

                Debug.Log($"🚀 {ShipData.ShipName} fired torpedo at {targetTransform.name}");
            }
            else
            {
                Debug.LogError($"Torpedo prefab missing Torpedo component!");
                Destroy(torpedoGO);
            }
        }

        /// <summary>
        /// Fire beam at target with proper initialization
        /// </summary>
        private void FireBeam(Transform targetTransform)
        {
            if (beamWeaponPrefab == null || targetTransform == null)
            {
                Debug.LogWarning($"{ShipData.ShipName}: Cannot fire beam - missing prefab or target");
                return;
            }

            // Instantiate beam weapon
            GameObject beamGO = Instantiate(beamWeaponPrefab, transform.position, Quaternion.identity);
            BeamWeapon beam = beamGO.GetComponent<BeamWeapon>();

            if (beam != null)
            {
                // Set weapon and target transforms for rendering
                beam.SetWeaponAndTarget(transform, targetTransform);

                // ✅ Initialize with ship's damage and sounds
                beam.Initialize(
                    this,                       // Owner ship
                    ShipData.BeamDamage,       // From ShipSO
                    clipBeamFire,              // From ShipController
                    null                        // Optional impact sound
                );

                // Fire immediately
                ShipController targetShip = targetTransform.GetComponentInParent<ShipController>();
                if (targetShip != null)
                {
                    beam.Fire(targetShip);
                }

                Debug.Log($"🔫 {ShipData.ShipName} fired beam at {targetTransform.name}");

                // Destroy beam after brief display
                Destroy(beamGO, 0.2f);
            }
            else
            {
                Debug.LogError($"Beam prefab missing BeamWeapon component!");
                Destroy(beamGO);
            }
        }
        //internal void FireWeapons(bool beam)
        //{
        //    Debug.Log($"🎯 FireWeapons called for '{ShipData.ShipName}', beam={beam}, target={ShipData.TargetThisShipController?.ShipData.ShipName ?? "NULL"}");

        //    if (ShipData.TargetThisShipController != null)
        //    {
        //        if (this != null && transform != null)
        //        {
        //            // ✅ Apply accuracy multiplier from combat order
        //            float accuracyMult = 1f;
        //            bool hitSuccess = UnityEngine.Random.value < accuracyMult;

        //            if (!hitSuccess)
        //            {
        //                // Miss! Don't deal damage
        //                Debug.Log($"  ❌ Ship '{ShipData.ShipName}' missed! (Accuracy={accuracyMult:F2})");
        //                return;
        //            }

        //            // Modify FireWeapons to track beams (around line 406-421)
        //            if (beam && ShipData.BeamDamage > 0)
        //            {
        //                Debug.Log($"  💥 Firing BEAM from '{ShipData.ShipName}' → '{ShipData.TargetThisShipController.ShipData.ShipName}' (Damage={ShipData.BeamDamage})");

        //                // ✅ FIX: Add CivEnum check to prevent friendly fire
        //                if (ShipData.TargetThisShipController != null &&
        //                    ShipData.CivEnum != ShipData.TargetThisShipController.ShipData.CivEnum)
        //                {
        //                    // ✅ Play beam fire sound through AudioManager (respects master volume)
        //                    if (clipBeamFire != null)
        //                    {
        //                        if (BOTF3D.Audio.AudioManager.Instance != null)
        //                        {
        //                            BOTF3D.Audio.AudioManager.Instance.PlaySFX3DClip(clipBeamFire, transform.position);
        //                            Debug.Log($"  🔊 Playing beam fire sound through AudioManager");
        //                        }
        //                        else
        //                        {
        //                            Debug.LogError($"  ❌ AudioManager.Instance is NULL!");
        //                        }
        //                    }
        //                    else
        //                    {
        //                        Debug.LogWarning($"  ⚠️ clipBeamFire is null for '{ShipData.ShipName}'");
        //                    }

        //                    GameObject beamWeaponGO = Instantiate(beamWeaponPrefab, transform.position, Quaternion.identity);

        //                    // ✅ CRITICAL: Disable any AudioSource on the beam prefab to prevent duplicate sounds
        //                    var beamAudioSources = beamWeaponGO.GetComponentsInChildren<AudioSource>(true);
        //                    foreach (var audioSrc in beamAudioSources)
        //                    {
        //                        audioSrc.enabled = false;
        //                        Debug.Log($"    🔇 Disabled AudioSource on beam weapon");
        //                    }

        //                    // ✅ NEW: Track this beam weapon
        //                    activeBeamWeapons.Add(beamWeaponGO);

        //                    Debug.Log($"  ⚡ Beam GameObject created: {beamWeaponGO.name}");

        //                    var lineRenderer = beamWeaponGO.GetComponent<LineRenderer>();
        //                    var beamWeaponScript = beamWeaponGO.GetComponent<BeamWeapon>();
        //                    beamWeaponScript.TargetTransform = ShipData.TargetThisShipController.ShipData.TargetOnThisShip.transform;
        //                    beamWeaponScript.WeaponTransform = this.transform;
        //                    beamWeaponScript.LineRenderer = lineRenderer;
        //                    beamWeaponScript.SetWeaponAndTarget(this.transform, ShipData.TargetThisShipController.ShipData.TargetOnThisShip.transform);


        //                    ShipData.TargetThisShipController.TakeDamage(ShipData.BeamDamage);

        //                    // ✅ Use coroutine with WaitForSecondsRealtime instead of Destroy(obj, time)
        //                    Debug.Log($"  ⏱️ Starting DestroyBeamAfterDelay coroutine for {beamWeaponGO.name}...");
        //                    StartCoroutine(DestroyBeamAfterDelay(beamWeaponGO, 0.5f));
        //                }
        //                else
        //                {
        //                    Debug.LogWarning($"  ⚠️ PREVENTED FRIENDLY FIRE: {ShipData.ShipName} tried to target friendly {ShipData.TargetThisShipController?.ShipData.ShipName}");
        //                }
        //            }
        //            else if (ShipData.TorpedoDamage > 0)
        //            {
        //                Debug.Log($"  🚀 Firing TORPEDO from '{ShipData.ShipName}' → '{ShipData.TargetThisShipController.ShipData.ShipName}' (Damage={ShipData.TorpedoDamage})");

        //                // ✅ Play torpedo fire sound through AudioManager (respects master volume)
        //                if (clipTorpedoFire != null)
        //                {
        //                    if (BOTF3D.Audio.AudioManager.Instance != null)
        //                    {
        //                        BOTF3D.Audio.AudioManager.Instance.PlaySFX3DClip(clipTorpedoFire, transform.position);
        //                        Debug.Log($"  🔊 Playing torpedo fire sound through AudioManager");
        //                    }
        //                    else
        //                    {
        //                        Debug.LogError($"  ❌ AudioManager.Instance is NULL!");
        //                    }
        //                }
        //                else
        //                {
        //                    Debug.LogWarning($"  ⚠️ clipTorpedoFire is null for '{ShipData.ShipName}'");
        //                }

        //    Vector3 firePosition = transform.position;
        //    Vector3 dir = (target.ShipData.TargetOnThisShip.transform.position - firePosition).normalized;
        //    Quaternion rot = Quaternion.LookRotation(dir);

        //    GameObject torpedoGO = Object.Instantiate(torpedoPrefab, firePosition, rot);
        //    Torpedo torpedo = torpedoGO.GetComponent<Torpedo>();
        //    if (torpedo != null)
        //    {
        //        torpedo.Target = target.ShipData.TargetOnThisShip.transform;
        //        torpedo.OwnerCivEnum = ShipData.CivEnum;
        //        torpedo.TorpedoDamage = damage;
        //    }
        //}

        private void DestroyAllActiveBeams()
        {
            foreach (var beam in activeBeamWeapons)
            {
                if (beam != null) Object.Destroy(beam);
            }
            activeBeamWeapons.Clear();
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
                Object.Destroy(gameObject);
            }
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

        //public void SetShipOrder(CombatOrders order) => Order = order;

        //public IEnumerator ShipFireLoop(float initialDelay)
        //{
        //    yield return new WaitForSecondsRealtime(initialDelay);
        //    bool beam = true;
        //    while (true)
        //    {
        //        if (ShipData == null || ShipData.Distroyed) yield break;
        //        if (ShipData.TargetThisShipController == null || ShipData.TargetThisShipController.ShipData.Distroyed)
        //        {
        //            yield return new WaitForSecondsRealtime(0.5f);
        //            continue;
        //        }
        //        FireWeapons(beam);
        //        beam = !beam;
        //        yield return new WaitForSecondsRealtime(Random.Range(minRefireDelay, maxRefireDelay));
        //    }
        //}

        public void SetWarpInOver() => WarpingInOver = true;

        public void OnShipEncounteredShip(ShipController shipController) { }
        public void OnShipEncounteredOther(StarSysController StarSysController) { }
        public void ValidateOwnership() { }
    }
}
