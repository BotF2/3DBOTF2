using BOTF3D.Combat;
using BOTF3D.Core;
using BOTF3D.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;

namespace BOTF3D.GamePlay
{
    public class ShipController : MonoBehaviour
    {
        private ShipData shipData;
        public ShipData ShipData { get { return shipData; } set { shipData = value; } }
        public string Name;
        public GameObject torpedoPrefab;
        public GameObject beamWeaponPrefab;
        public GameObject ShipListUIGameObject; //The instantiated ship UI for this fleet and system ship lists, a prefab clone, not a class but a game object
        public AudioClip clipTorpedoFire;
        public AudioClip clipBeamFire;
        private AudioSource theSource;
        public Transform TargetGroup; // for movement, The center or leader of the other group
        private int flipShipForward = 1;
        public bool WarpingInOver = false;
        private GameObject beamWeaponGO;
        private List<GameObject> activeBeamWeapons = new List<GameObject>();
        public CombatOrders Order; // orders for the ship, e.g. attack, defend, patrol
        [SerializeField] private float minRefireDelay; // see Start()
        [SerializeField] private float maxRefireDelay;
        public Image HealthFillImage;
        public Image HealthBackgroundImage;
        public float HealthSpeed;
        public float TargetHealthFillAmount { get; set; } = 1.0f;

        private void Start()
        {
            theSource = GetComponent<AudioSource>();
            if (transform.position.x < 0) flipShipForward = -1;

            minRefireDelay = 1.5f;
            maxRefireDelay = 2.5f;
            HealthSpeed = 10.0f;

            // ✅ ADD THIS DEBUG CHECK
            Debug.Log($"Ship '{ShipData?.ShipName}' audio clips: Beam={clipBeamFire != null}, Torpedo={clipTorpedoFire != null}");

            // ✅ Initialize ShipData health values from ShipSO if not already set
            if (ShipData != null && ShipData.ShipSO != null)
            {
                // Only initialize if values are 0 (new ship) or if we're resetting for combat
                if (ShipData.ShieldHealth == 0 && ShipData.HullHealth == 0)
                {
                    ShipData.ShieldHealth = ShipData.ShipSO.ShieldMaxHealth;
                    ShipData.HullHealth = ShipData.ShipSO.HullMaxHealth;
                    Debug.Log($"✅ Ship '{ShipData.ShipName}' initialized: Shields={ShipData.ShieldHealth}, Hull={ShipData.HullHealth}");
                }
                else
                {
                    Debug.Log($"📊 Ship '{ShipData.ShipName}' entering combat: Shields={ShipData.ShieldHealth}, Hull={ShipData.HullHealth}");
                }
            }
            // TEMPORARY TEST - Remove after testing
            if (BOTF3D.Audio.AudioManager.Instance != null && clipBeamFire != null)
            {
                Debug.Log("🧪 TESTING: Playing beam sound immediately");
                BOTF3D.Audio.AudioManager.Instance.PlaySFX3DClip(clipBeamFire, transform.position);
            }
        }
        void Update()
        {
            //Health: 100% → [████████████████████] Green fill, no red background showing
            //Health:  80% → [█████████████░░░░░░░] Green fill, red showing on right
            //Health:  50% → [██████████░░░░░░░░░░] Cyan fill, red showing on right
            //Health:  20% → [████░░░░░░░░░░░░░░░░] Yellow fill, red showing on right
            //Health:   0% → [░░░░░░░░░░░░░░░░░░░░] All red

            if (HealthFillImage != null && ShipData != null)
            {
                // ✅ Calculate total health percentage (shields + hull)
                int maxHealth = GetMaxHealth(); // shield and hull max combined, starting values
                int currentHealth = GetCurrentTotalHealth();
                TargetHealthFillAmount = (float)currentHealth / maxHealth;

                // ✅ Smooth lerp to target
                HealthFillImage.fillAmount = Mathf.Lerp(
                    HealthFillImage.fillAmount,      // current fill amount
                    TargetHealthFillAmount,           // target fill amount based on current health
                    HealthSpeed * Time.unscaledDeltaTime // speed of lerp
                );

                // ✅ Set background to RED (damage color) - always full
                if (HealthBackgroundImage != null)
                {
                    HealthBackgroundImage.color = Color.red;
                    HealthBackgroundImage.fillAmount = 1.0f; // Always full
                }

                // ✅ Color the FILLED portion (remaining health)
                float healthPercent = TargetHealthFillAmount;

                if (healthPercent > 0.66f)
                {
                    // Healthy: Green
                    HealthFillImage.color = Color.green;
                }
                else if (healthPercent > 0.33f)
                {
                    // Damaged: Cyan
                    HealthFillImage.color = Color.cyan;
                }
                else if (healthPercent > 0)
                {
                    // Critical: Yellow
                    HealthFillImage.color = Color.yellow;
                }
                else
                {
                    // Destroyed: Red (entire bar red)
                    HealthFillImage.color = Color.red;
                }
            }
        }

        // Add helper method to get total max health
        private int GetMaxHealth()
        {
            if (ShipData?.ShipSO != null)
            {
                return ShipData.ShipSO.ShieldMaxHealth + ShipData.ShipSO.HullMaxHealth;
            }

            Debug.LogWarning($"ShipSO not found for {ShipData?.ShipName}, using default max health");
            return 100;
        }

        // Add helper method to get current total health
        private int GetCurrentTotalHealth()
        {
            if (ShipData != null)
            {
                return ShipData.ShieldHealth + ShipData.HullHealth;
            }
            return 0;
        }

        private void FixedUpdate()
        {
            if (WarpingInOver)
            {
                switch (Order)
                {
                    case CombatOrders.None:
                        // No orders, do nothing
                        break;
                    case CombatOrders.Engage:
                        //EngageLooksLikeNewtonianPhysics();
                        // simple forward movement with deceleration to stop point
                        break;
                    case CombatOrders.Formation:
                        // move into a defensive formation
                        break;
                    case CombatOrders.Retreat:
                        // try for warp out
                        break;
                    case CombatOrders.AttackTransports:
                        break;
                    case CombatOrders.Rush:
                        break;
                    default:
                        break;
                }
            }
        }
        public void Init(ShipManager shipManager)
        {
            ShipManager.Instance = shipManager;
        }
        void OnTriggerEnter(Collider collider)
        {
            // !!! this is for ships / SpaceCombatScene, not galaxy map fleets
            ShipController shipController = collider.gameObject.GetComponent<ShipController>();
            if (shipController != null) // it is a shipController 
            {
                OnShipEncounteredShip(shipController); // does nothing yet
                Debug.Log("Controller collided with " + shipController.gameObject.name);
            }
        }
        private void EngageLooksLikeNewtonianPhysics()
        {
            #region Simplistic but mostly realistic Newtonian movement along a path in space

            // *** using Unity physics, One time push simulating warp in residual velocity
            //Vector3 move = currentVelocity * transform.forward * flipShipForward;
            //if (setSpeed)
            //{
            //    rb.linearVelocity = Vector3.zero; //  0 linear momentum
            //    rb.angularVelocity = Vector3.zero; // 0 angular momentum
            //    rb.AddForce(move * Acceleration, ForceMode.Acceleration);
            //    setSpeed = false;
            //}
            //else
            //{
            //    // Gradually slow down when approaching the stop point
            //    float distanceToCenter = Mathf.Abs(transform.position.x - 0f);

            //    if (distanceToCenter > StopDistance && rb.linearVelocity.magnitude > 0.1f)
            //    {
            //        Vector3 brakingForce = -rb.linearVelocity.normalized * Deceleration;
            //        rb.angularVelocity = Vector3.zero; // 0 angular momentum
            //        rb.AddForce(brakingForce, ForceMode.Acceleration);
            //        if (this.ShipData.ShipType == ShipType.Transport) // extra braking for transports
            //        {
            //            rb.AddForce(brakingForce * 0.5f, ForceMode.Acceleration);
            //        }
            //    }
            //    else
            //    {
            //        rb.linearVelocity = Vector3.zero; // Full stop
            //    }
            //}

            #endregion

        }
        private void MoveLikeAirplane()
        {
            #region How to make ships circle each other, move like airplanes
            //Instead of always moving towards the enemy group’s center, compute a circle vector around that center:
            // Ships move like banking airplanes and not like spaceships in a vacuum.
            //if (TargetGroup != null)
            //{
            //    // Direction to the enemy group
            //    Vector3 toTarget = (TargetGroup.position - rb.position).normalized;

            //    // Choose an "orbit axis" (here: world up for flat 2D circling)
            //    Vector3 orbitAxis = Vector3.up;

            //    // Rotate the direction vector 90° around the axis to get tangent direction
            //    Vector3 orbitDirection = Quaternion.AngleAxis(90, orbitAxis) * toTarget;

            //    // Blend between circling and moving toward the orbit distance
            //    Vector3 desiredPosition = TargetGroup.position - toTarget * OrbitDistance;
            //    Vector3 moveDir = (desiredPosition - rb.position).normalized;

            //    // Add orbiting movement
            //    Vector3 finalDir = (moveDir + orbitDirection * 0.5f).normalized;

            //    // Move
            //    Vector3 nextPosition = rb.position + finalDir * velocity * Time.fixedUnscaledDeltaTime;
            //    rb.MovePosition(nextPosition);

            //    // Rotate to face movement
            //    if (finalDir != Vector3.zero)
            //    {
            //        Quaternion targetRot = Quaternion.LookRotation(finalDir);
            //        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, 200f * Time.fixedUnscaledDeltaTime));
            //    }
            //    //        What this does

            //    //TargetGroup = the other fleet’s center or leader GameObject.

            //    //Ships try to maintain a distance(OrbitDistance) from that point.

            //    //They add a tangential offset(orbitDirection) so they don’t collide head - on but instead circle.

            //    //Both groups, if given each other as TargetGroup, will end up orbiting each other like two swarms circling.

            //    //Options to tweak

            //    //Change orbitAxis: Vector3.up for flat 2D plane battles, or Vector3.Cross(toTarget, Vector3.up) for more dynamic 3D orbits.

            //    //Adjust OrbitDistance to avoid collisions between fleets.

            //    //Randomize OrbitSpeed slightly per ship for more natural motion.
            //}
            #endregion
        }
        // ✅ Modify FireAtTarget to check for blocking
        private void FireAtTarget(ShipController target)
        {
            if (target == null) return;

            // ✅ Check if friendly ships block the shot
            List<ShipController> friendlyShips = GetFriendlyShipsInCombat();

            ShipController blockingShip = LineOfSightBlocker.GetBlockingShip(
                transform.position,
                target.transform.position,
                friendlyShips,
                blockingRadius: 15f
            );

            if (blockingShip != null)
            {
                // ✅ Shot blocked - hit the blocking ship instead!
                Debug.Log($"🛡️ Shot blocked by {blockingShip.ShipData.ShipName}");
                target = blockingShip; // Redirect damage
            }

            // ... fire beam/torpedo at target (possibly the blocker)
        }

        private List<ShipController> GetFriendlyShipsInCombat()
        {
            // Get all ships on same side in combat
            // (from CombatData.SideOneShipCons or SideTwoShipCons)
            return new List<ShipController>(); // TODO: implement
        }
        public void SetWeaponPrefabs()
        {
            if (ShipManager.Instance == null)
            {
                Debug.LogError($"❌ ShipManager.Instance is null - cannot set weapon prefabs for {ShipData.ShipName}");
                return;
            }

            GameObject[] torpedoPrefabs = ShipManager.Instance.torpedoPrefabs;
            GameObject[] beamPrefabs = ShipManager.Instance.beamWeaponPrefabs;

            if (torpedoPrefabs == null || torpedoPrefabs.Length == 0)
            {
                Debug.LogError($"❌ ShipManager.Instance.torpedoPrefabs is null or empty!");
                return;
            }

            if (beamPrefabs == null || beamPrefabs.Length == 0)
            {
                Debug.LogError($"❌ ShipManager.Instance.beamWeaponPrefabs is null or empty!");
                return;
            }

            // Set torpedo prefab
            for (int i = 0; i < torpedoPrefabs.Length; i++)
            {
                if ((int)this.ShipData.CivEnum > 7)
                {
                    torpedoPrefab = torpedoPrefabs.LastOrDefault();
                }
                else if (torpedoPrefabs[i].name.Contains(ShipData.CivEnum.ToString().ToUpper()))
                {
                    torpedoPrefab = torpedoPrefabs[i];
                }
            }

            // Set beam prefab
            for (int i = 0; i < beamPrefabs.Length; i++)
            {
                if ((int)ShipData.CivEnum > 7)
                {
                    beamWeaponPrefab = beamPrefabs.LastOrDefault();
                }
                else if (beamPrefabs[i].name.Contains(ShipData.CivEnum.ToString().ToUpper()))
                {
                    beamWeaponPrefab = beamPrefabs[i];
                }
            }

            // ✅ Verify prefabs were set
            if (torpedoPrefab == null)
            {
                Debug.LogWarning($"⚠️ No torpedo prefab found for {ShipData.CivEnum} - using fallback");
                torpedoPrefab = torpedoPrefabs.FirstOrDefault();
            }

            if (beamWeaponPrefab == null)
            {
                Debug.LogWarning($"⚠️ No beam prefab found for {ShipData.CivEnum} - using fallback");
                beamWeaponPrefab = beamPrefabs.FirstOrDefault();
            }

            Debug.Log($"✅ {ShipData.ShipName} ({ShipData.CivEnum}): Torpedo={torpedoPrefab?.name}, Beam={beamWeaponPrefab?.name}");
        }
        /// <summary>
        /// Set civilization-specific weapon audio clips
        /// </summary>
        public void SetWeaponAudioClips(AudioClip beamClip, AudioClip torpedoClip)
        {
            clipBeamFire = beamClip;
            clipTorpedoFire = torpedoClip;

            if (clipBeamFire != null)
            {
                Debug.Log($"✅ Set beam fire clip for '{ShipData.ShipName}' (Civ: {ShipData.CivEnum}): {clipBeamFire.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Beam fire clip is NULL for '{ShipData.ShipName}' (Civ: {ShipData.CivEnum})");
            }

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
        public void OnShipEncounteredShip(ShipController shipController)
        {
            //1) player get the ShipController of the ship GO we hit
            //2) player ask your factionOwner (CivManager) 
        }
        public void OnShipEncounteredOther(StarSysController StarSysController)
        {
            //1) player get the OtherController of the GO
        }
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
        internal void FireWeapons(bool beam)
        {
            Debug.Log($"🎯 FireWeapons called for '{ShipData.ShipName}', beam={beam}, target={ShipData.TargetThisShipController?.ShipData.ShipName ?? "NULL"}");

            if (ShipData.TargetThisShipController != null)
            {
                if (this != null && transform != null)
                {
                    // ✅ Apply accuracy multiplier from combat order
                    float accuracyMult = 1f;
                    bool hitSuccess = UnityEngine.Random.value < accuracyMult;

                    if (!hitSuccess)
                    {
                        // Miss! Don't deal damage
                        Debug.Log($"  ❌ Ship '{ShipData.ShipName}' missed! (Accuracy={accuracyMult:F2})");
                        return;
                    }

                    // Modify FireWeapons to track beams (around line 406-421)
                    if (beam && ShipData.BeamDamage > 0)
                    {
                        Debug.Log($"  💥 Firing BEAM from '{ShipData.ShipName}' → '{ShipData.TargetThisShipController.ShipData.ShipName}' (Damage={ShipData.BeamDamage})");

                        // ✅ FIX: Add CivEnum check to prevent friendly fire
                        if (ShipData.TargetThisShipController != null &&
                            ShipData.CivEnum != ShipData.TargetThisShipController.ShipData.CivEnum)
                        {
                            // ✅ Play beam fire sound through AudioManager (respects master volume)
                            if (clipBeamFire != null)
                            {
                                if (BOTF3D.Audio.AudioManager.Instance != null)
                                {
                                    BOTF3D.Audio.AudioManager.Instance.PlaySFX3DClip(clipBeamFire, transform.position);
                                    Debug.Log($"  🔊 Playing beam fire sound through AudioManager");
                                }
                                else
                                {
                                    Debug.LogError($"  ❌ AudioManager.Instance is NULL!");
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"  ⚠️ clipBeamFire is null for '{ShipData.ShipName}'");
                            }

                            GameObject beamWeaponGO = Instantiate(beamWeaponPrefab, transform.position, Quaternion.identity);

                            // ✅ CRITICAL: Disable any AudioSource on the beam prefab to prevent duplicate sounds
                            var beamAudioSources = beamWeaponGO.GetComponentsInChildren<AudioSource>(true);
                            foreach (var audioSrc in beamAudioSources)
                            {
                                audioSrc.enabled = false;
                                Debug.Log($"    🔇 Disabled AudioSource on beam weapon");
                            }

                            // ✅ NEW: Track this beam weapon
                            activeBeamWeapons.Add(beamWeaponGO);

                            Debug.Log($"  ⚡ Beam GameObject created: {beamWeaponGO.name}");

                            var lineRenderer = beamWeaponGO.GetComponent<LineRenderer>();
                            var beamWeaponScript = beamWeaponGO.GetComponent<BeamWeapon>();
                            beamWeaponScript.TargetTransform = ShipData.TargetThisShipController.ShipData.TargetOnThisShip.transform;
                            beamWeaponScript.WeaponTransform = this.transform;
                            beamWeaponScript.LineRenderer = lineRenderer;
                            beamWeaponScript.SetWeaponAndTarget(this.transform, ShipData.TargetThisShipController.ShipData.TargetOnThisShip.transform);


                            ShipData.TargetThisShipController.TakeDamage(ShipData.BeamDamage);

                            // ✅ Use coroutine with WaitForSecondsRealtime instead of Destroy(obj, time)
                            Debug.Log($"  ⏱️ Starting DestroyBeamAfterDelay coroutine for {beamWeaponGO.name}...");
                            StartCoroutine(DestroyBeamAfterDelay(beamWeaponGO, 0.5f));
                        }
                        else
                        {
                            Debug.LogWarning($"  ⚠️ PREVENTED FRIENDLY FIRE: {ShipData.ShipName} tried to target friendly {ShipData.TargetThisShipController?.ShipData.ShipName}");
                        }
                    }
                    else if (ShipData.TorpedoDamage > 0)
                    {
                        Debug.Log($"  🚀 Firing TORPEDO from '{ShipData.ShipName}' → '{ShipData.TargetThisShipController.ShipData.ShipName}' (Damage={ShipData.TorpedoDamage})");

                        // ✅ Play torpedo fire sound through AudioManager (respects master volume)
                        if (clipTorpedoFire != null)
                        {
                            if (BOTF3D.Audio.AudioManager.Instance != null)
                            {
                                BOTF3D.Audio.AudioManager.Instance.PlaySFX3DClip(clipTorpedoFire, transform.position);
                                Debug.Log($"  🔊 Playing torpedo fire sound through AudioManager");
                            }
                            else
                            {
                                Debug.LogError($"  ❌ AudioManager.Instance is NULL!");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"  ⚠️ clipTorpedoFire is null for '{ShipData.ShipName}'");
                        }

                        // ✅ Calculate fire position and rotation
                        Vector3 firePosition = transform.position;
                        Transform targetTransform = ShipData.TargetThisShipController.ShipData.TargetOnThisShip.transform;
                        Vector3 directionToTarget = (targetTransform.position - firePosition).normalized;
                        Quaternion fireRotation = Quaternion.LookRotation(directionToTarget);

                        GameObject torpedoGO = Instantiate(torpedoPrefab, firePosition, fireRotation);
                        Torpedo torpedo = torpedoGO.GetComponent<Torpedo>();
                        if (torpedo != null)
                        {
                            torpedo.Target = targetTransform;
                            torpedo.OwnerCivEnum = ShipData.CivEnum;

                            // ✅ Initialize with ship's damage and sounds
                            torpedo.Initialize(
                                ShipData.TorpedoDamage,
                                clipTorpedoFire,
                                null // optional impact sound
                            );
                        }
                        Debug.Log($"  🎯 Torpedo GameObject created: {torpedoGO.name}, active={torpedoGO.activeSelf}");

                        // ✅ CRITICAL: Disable any AudioSource on the torpedo prefab to prevent duplicate sounds
                        var torpedoAudioSources = torpedoGO.GetComponentsInChildren<AudioSource>(true);
                        foreach (var audioSrc in torpedoAudioSources)
                        {
                            audioSrc.enabled = false;
                            Debug.Log($"    🔇 Disabled AudioSource on torpedo");
                        }

                        var torpedoScript = torpedoGO.GetComponent<Torpedo>();
                        if (torpedoScript == null)
                        {
                            Debug.LogError($"  ❌ Torpedo prefab has NO Torpedo component!");
                        }
                        else
                        {
                            Debug.Log($"  ✅ Torpedo script found, setting Damage and target...");
                            torpedoScript.TorpedoDamage = ShipData.TorpedoDamage;
                            torpedoScript.OwnerCivEnum = ShipData.CivEnum;
                            if (ShipData.TargetThisShipController != null)
                            {
                                torpedoScript.Target = ShipData.TargetThisShipController.ShipData.TargetOnThisShip.transform;
                                torpedoScript.TargetCivEnum = ShipData.TargetThisShipController.ShipData.CivEnum;
                                Debug.Log($"  🎯 Torpedo target set to: {torpedoScript.Target.name}");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"  ⚠️ Ship '{ShipData.ShipName}' has no weapon Damage! BeamDamage={ShipData.BeamDamage}, TorpedoDamage={ShipData.TorpedoDamage}");
                    }
                }
                else
                {
                    Debug.LogError($"  ❌ Ship or transform is NULL for '{ShipData.ShipName}'");
                }
            }
            else
            {
                Debug.LogWarning($"  ⚠️ No target assigned for '{ShipData.ShipName}'");
            }
        }

        // ✅ NEW: Coroutine to destroy beam weapon using realtime
        private IEnumerator DestroyBeamAfterDelay(GameObject beamObj, float delay)
        {
            Debug.Log($"  ⏱️ DestroyBeamAfterDelay STARTED for {beamObj?.name ?? "NULL"}, waiting {delay}s...");

            yield return new WaitForSecondsRealtime(delay);

            Debug.Log($"  ⏱️ DestroyBeamAfterDelay WAIT COMPLETE after {delay}s");

            if (beamObj != null)
            {
                Debug.Log($"  🔴 Destroying beam weapon: {beamObj.name}");

                // ✅ NEW: Remove from tracking list
                activeBeamWeapons.Remove(beamObj);

                Destroy(beamObj);
            }
            else
            {
                Debug.LogWarning($"  ⚠️ Beam object is NULL, can't destroy");
            }
        }
        /// <summary>
        /// Destroys all active beam weapons created by or targeting this ship
        /// </summary>
        private void DestroyAllActiveBeams()
        {
            Debug.Log($"  🧹 Cleaning up {activeBeamWeapons.Count} active beam weapons for '{ShipData.ShipName}'");

            // Create a copy of the list to avoid modification during iteration
            var beamsToDestroy = new List<GameObject>(activeBeamWeapons);

            foreach (var beam in beamsToDestroy)
            {
                if (beam != null)
                {
                    Debug.Log($"    🗑️ Destroying beam: {beam.name}");
                    Destroy(beam);
                }
            }
            activeBeamWeapons.Clear();

            // ✅ ALSO: Find and destroy any beam weapons targeting THIS ship
            var allBeams = FindObjectsByType<BeamWeapon>(FindObjectsSortMode.None);
            foreach (var beamWeapon in allBeams)
            {
                if (beamWeapon.TargetTransform != null &&
                    beamWeapon.TargetTransform.root == this.transform.root)
                {
                    Debug.Log($"    🎯 Destroying beam targeting this ship: {beamWeapon.gameObject.name}");
                    Destroy(beamWeapon.gameObject);
                }
            }
        }
        // Update TakeDamage to modify ShipData.HullHealth directly (lines 426-480)
        public void TakeDamage(int weaponDamageInt)
        {
            if (ShipData == null)
            {
                Debug.LogError("TakeDamage called but ShipData is null!");
                return;
            }

            // ✅ Ship must have health to take damage
            if (ShipData.ShieldHealth <= 0 && ShipData.HullHealth <= 0)
            {
                Debug.LogWarning($"Ship '{ShipData.ShipName}' already destroyed, ignoring Damage");
                return;
            }

            // ✅ Apply defensive multiplier based on combat order
            float defenseMult = 1;
            float adjustedDamage = weaponDamageInt * defenseMult;

            int oldShields = ShipData.ShieldHealth;
            int oldHull = ShipData.HullHealth;
            int totalOldHealth = oldShields + oldHull;

            // ✅ SHIELDS-FIRST DAMAGE SYSTEM
            if (ShipData.ShieldHealth > 0)
            {
                // Damage shields first
                int shieldDamage = Mathf.RoundToInt(adjustedDamage);
                ShipData.ShieldHealth -= shieldDamage;

                if (ShipData.ShieldHealth < 0)
                {
                    // Shields depleted - overflow damage goes to hull
                    int overflowDamage = -ShipData.ShieldHealth;
                    ShipData.ShieldHealth = 0;
                    ShipData.HullHealth -= overflowDamage;
                    ShipData.HullHealth = Mathf.Max(ShipData.HullHealth, 0);

                    Debug.Log($"  🛡️💥 '{ShipData.ShipName}' shields COLLAPSED! {shieldDamage} Damage: {oldShields} shields → 0, overflow {overflowDamage} to hull");
                }
                else
                {
                    Debug.Log($"  🛡️ '{ShipData.ShipName}' shields absorbed {shieldDamage} Damage: {oldShields} → {ShipData.ShieldHealth}");
                }
            }
            else
            {
                // Shields already down - damage hull directly
                int hullDamage = Mathf.RoundToInt(adjustedDamage);
                ShipData.HullHealth -= hullDamage;
                ShipData.HullHealth = Mathf.Max(ShipData.HullHealth, 0);

                Debug.Log($"  💔 '{ShipData.ShipName}' hull hit for {hullDamage} Damage: {oldHull} → {ShipData.HullHealth}");
            }

            // ✅ Calculate health percentage
            int maxHealth = GetMaxHealth();
            int currentHealth = GetCurrentTotalHealth();
            float healthPercent = (float)currentHealth / maxHealth;

            Debug.Log($"  📊 '{ShipData.ShipName}' status: Shields={ShipData.ShieldHealth}, Hull={ShipData.HullHealth}, Total={currentHealth}/{maxHealth} ({healthPercent:P0})");
            Debug.Log($"     Order={Order}, DefenseMult={defenseMult:F2}, AdjustedDamage={adjustedDamage:F1}");

            // ✅ Check if ship is destroyed (hull depleted)
            if (ShipData.HullHealth <= 0)
            {
                Debug.Log($"  ☠️☠️☠️ Ship '{ShipData.ShipName}' DESTROYED! ☠️☠️☠️");
                Debug.Log($"  Final Damage breakdown: Shields {oldShields}→{ShipData.ShieldHealth}, Hull {oldHull}→{ShipData.HullHealth}");

                // ✅ Mark ship as destroyed and clean up
                if (!ShipData.Distroyed)
                {
                    ShipData.Distroyed = true;

                    // Remove from fleet
                    if (ShipData.CurrentFleetController != null)
                    {
                        ShipData.CurrentFleetController.RemoveShipFromFleet(this);
                        Debug.Log($"    ✅ Removed from fleet '{ShipData.CurrentFleetController.name}'");
                    }

                    // Remove from combat tracking
                    if (CombatManager.Instance != null)
                    {
                        CombatManager.Instance.RemoveThisShipController(this);
                    }

                    // Remove from camera targets
                    if (ShipCombatCameraController.Instance != null)
                    {
                        ShipCombatCameraController.Instance.OnShipDestroyed(this);
                    }

                    // Clear target reference
                    ShipData.TargetThisShipController = null;

                    // ✅ CRITICAL: Destroy the ship GameObject IMMEDIATELY
                    Debug.Log($"    🗑️ DESTROYING ship GameObject immediately!");

                    // ✅ NEW: Clean up beam weapons BEFORE stopping coroutines
                    DestroyAllActiveBeams();

                    // Stop any coroutines first (weapon fire, etc.)
                    StopAllCoroutines();

                    // Destroy immediately so ship vanishes from combat scene
                    Destroy(gameObject);
                }
            }
        }

        internal void SetWarpInOver()
        {
            WarpingInOver = true;

        }
        /// <summary>
        /// Ensures UI and data ownership are synchronized
        /// </summary>
        public void ValidateOwnership()
        {
            if (ShipListUIGameObject != null)
            {
                var uiItem = ShipListUIGameObject.GetComponent<ShipListUI_Item>();
                if (uiItem != null)
                {
                    uiItem.CurrentFleet = ShipData?.CurrentFleetController;
                    uiItem.CurrentStarSyst = ShipData?.CurrentStarSysController;
                }
            }
        }
    }
}
