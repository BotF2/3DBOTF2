using BOTF3D.Combat;
using BOTF3D.Core;
using BOTF3D.UI;
using System.Collections;
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
        public CombatOrders Order; // orders for the ship, e.g. attack, defend, patrol
        [SerializeField] private float minRefireDelay; // see Start()
        [SerializeField] private float maxRefireDelay;
        public Image HealthFillImage;
        public float HealthSpeed;
        public float TargetHealthFillAmount { get; set; } = 1.0f;
        public float Health;
        public float MaxHealth;


        void Awake()
        {
            // if we use Unity physics
            //rb = GetComponent<Rigidbody>();
            //rb.useGravity = false; // space, microgravity set to zero
            //rb.linearDamping = 0f; // no air drag
            //rb.angularDamping = 0.5f; // small resistance to rotation

        }
        private void Start()
        {
            theSource = GetComponent<AudioSource>();
            if (transform.position.x < 0) flipShipForward = -1; // if on left side of map, flip direction ship faces
                                                                //moveDirection = transform.forward.normalized * flipShipForward; // initial move direction
            minRefireDelay = 1.5f;
            maxRefireDelay = 2.5f;
            MaxHealth = ShipData.HullHealth + ShipData.HullHealth;
            Health = MaxHealth;
            HealthSpeed = 10.0f;

        }
        void Update()
        {
            if (HealthFillImage != null)
            {
                // ✅ Use unscaledDeltaTime so health bar updates even when Time.timeScale = 0
                HealthFillImage.fillAmount = Mathf.Lerp(HealthFillImage.fillAmount, TargetHealthFillAmount, HealthSpeed * Time.unscaledDeltaTime);
            }

            TargetHealthFillAmount = Health / MaxHealth;

            // ✅ Update health bar color based on health percentage
            if (HealthFillImage != null)
            {
                float healthPercent = TargetHealthFillAmount;

                if (healthPercent > 0.66f)
                {
                    HealthFillImage.color = Color.green; // Healthy
                }
                else if (healthPercent > 0.33f)
                {
                    HealthFillImage.color = Color.yellow; // Damaged
                }
                else
                {
                    HealthFillImage.color = Color.red; // Critical
                }
            }
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
                    case CombatOrders.TargetTransports:
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
        public void SetWeaponPrefabs()
        {
            GameObject[] torpedoPrefabs = ShipManager.Instance.torpedoPrefabs;
            GameObject[] beamPrefabs = ShipManager.Instance.beamWeaponPrefabs;
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
                case CombatOrders.TargetTransports:
                    Order = CombatOrders.TargetTransports;
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

        internal void FireWeapons(bool beam)
        {
            Debug.Log($"🎯 FireWeapons called for '{ShipData.ShipName}', beam={beam}, target={ShipData.TargetThisShipController?.ShipData.ShipName ?? "NULL"}");

            if (ShipData.TargetThisShipController != null)
            {
                if (this != null && transform != null)
                {
                    // ✅ Apply accuracy multiplier from combat order
                    float accuracyMult = CombatOrderMatrix.GetAccuracyMultiplier(Order);
                    bool hitSuccess = UnityEngine.Random.value < accuracyMult;

                    if (!hitSuccess)
                    {
                        // Miss! Don't deal damage
                        Debug.Log($"  ❌ Ship '{ShipData.ShipName}' missed! (Accuracy={accuracyMult:F2})");
                        return;
                    }

                    if (beam && ShipData.BeamDamage > 0)
                    {
                        Debug.Log($"  💥 Firing BEAM from '{ShipData.ShipName}' → '{ShipData.TargetThisShipController.ShipData.ShipName}' (damage={ShipData.BeamDamage})");

                        var beamWeaponGo = Instantiate(beamWeaponPrefab, this.transform.position, Quaternion.identity);
                        beamWeaponGO = beamWeaponGo;

                        Debug.Log($"  ⚡ Beam GameObject created: {beamWeaponGo.name}");

                        var lineRenderer = beamWeaponGo.GetComponent<LineRenderer>();
                        var beamWeaponScript = beamWeaponGo.GetComponent<BeamWeapon>();
                        beamWeaponScript.TargetTransform = ShipData.TargetThisShipController.ShipData.TargetOnThisShip.transform;
                        beamWeaponScript.WeaponTransform = this.transform;
                        beamWeaponScript.LineRenderer = lineRenderer;
                        beamWeaponScript.SetWeaponAndTarget(this.transform, ShipData.TargetThisShipController.ShipData.TargetOnThisShip.transform);
                        ShipData.TargetThisShipController.TakeDamage(ShipData.BeamDamage);

                        // ✅ Use coroutine with WaitForSecondsRealtime instead of Destroy(obj, time)
                        Debug.Log($"  ⏱️ Starting DestroyBeamAfterDelay coroutine for {beamWeaponGo.name}...");
                        StartCoroutine(DestroyBeamAfterDelay(beamWeaponGo, 0.5f));
                    }
                    else if (ShipData.TorpedoDamage > 0)
                    {
                        Debug.Log($"  🚀 Firing TORPEDO from '{ShipData.ShipName}' → '{ShipData.TargetThisShipController.ShipData.ShipName}' (damage={ShipData.TorpedoDamage})");

                        var torpedoGo = Instantiate(torpedoPrefab, this.transform.position, Quaternion.identity);
                        Debug.Log($"  🎯 Torpedo GameObject created: {torpedoGo.name}, active={torpedoGo.activeSelf}");

                        var torpedoScript = torpedoGo.GetComponent<Torpedo>();
                        if (torpedoScript == null)
                        {
                            Debug.LogError($"  ❌ Torpedo prefab has NO Torpedo component!");
                        }
                        else
                        {
                            Debug.Log($"  ✅ Torpedo script found, setting damage and target...");
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
                        Debug.LogWarning($"  ⚠️ Ship '{ShipData.ShipName}' has no weapon damage! BeamDamage={ShipData.BeamDamage}, TorpedoDamage={ShipData.TorpedoDamage}");
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
                Destroy(beamObj);
            }
            else
            {
                Debug.LogWarning($"  ⚠️ Beam object is NULL, can't destroy");
            }
        }

        public void TakeDamage(int weaponDamageInt)
        {
            if (Health > 0)
            {
                // ✅ Apply defensive multiplier based on combat order
                float defenseMult = CombatOrderMatrix.GetDefenseMultiplier(Order);
                float adjustedDamage = (weaponDamageInt / 3f) * defenseMult;

                float oldHealth = Health;
                Health -= adjustedDamage;
                Health = Mathf.Max(Health, 0.0f);

                float newHealthPercent = Health / MaxHealth;
                TargetHealthFillAmount = newHealthPercent;

                Debug.Log($"  💔 Ship '{ShipData.ShipName}' took {adjustedDamage:F1} damage (order={Order}, mult={defenseMult:F2})");
                Debug.Log($"     Health: {oldHealth:F1} → {Health:F1} ({newHealthPercent:P0}), TargetFillAmount={TargetHealthFillAmount:F2}");

                // ✅ Force immediate health bar update if needed
                if (HealthFillImage != null)
                {
                    Debug.Log($"     HealthBar fillAmount={HealthFillImage.fillAmount:F2} → target {TargetHealthFillAmount:F2}");
                }

                if (Health <= 0)
                {
                    Debug.LogWarning($"  ☠️ Ship '{ShipData.ShipName}' DESTROYED!");
                }
            }
            #region for tracking shields and hull individually
            //if (ShipData.ShieldHealth > 0)
            //{
            //    //If the ship has shields, damage the shields first
            //    ShipData.ShieldHealth -= (weaponDamageInt / 2);

            //    return;
            //}
            //else if (ShipData.HullHealth > 0)
            //{
            //    ShipData.HullHealth -= (weaponDamageInt  / 3);
            //    return;
            //}
            #endregion
            else
            {
                // If health is zero, destroy the ship
                var fleetController = this.ShipData.CurrentFleetController;
                var starSysController = this.ShipData.CurrentStarSysController;
                if (fleetController != null && !ShipData.Distroyed)
                {
                    ShipData.Distroyed = true;
                    fleetController.RemoveShipFromFleet(this);
                    CombatManager.Instance.RemoveThisShipController(this);
                    ShipCombatCameraController.Instance.OnShipDestroyed(this);
                    ShipData.TargetThisShipController = null; // Clear the target ship controller
                    this.ShipData.CurrentFleetController.FleetData.ShipsList.Remove(this); // Remove this ship from the fleet's ship list
                                                                                           // this can be problematic, FleetController can be null when its script is still running giving null reference exception
                                                                                           // FleetManager.Instance.RemoveFleetConIfShipListIsEmpty(this); // Remove this ship from all ship lists in FleetManager
                    Destroy(beamWeaponGO);
                    Destroy(gameObject);
                    this.ShipData.CurrentFleetController.IsTheFleetDestroyed();
                    ShipManager.Instance.RemoveShipControllerFromList(this);
                    // FindAnyObjectByType<AudioManager>().Play("ShipDestroyed");
                }

                else if (starSysController != null && !ShipData.Distroyed)
                {
                    ShipData.Distroyed = true;
                    starSysController.StarSysData.ShipsList.Remove(this);
                    CombatManager.Instance.RemoveThisShipController(this);
                    ShipCombatCameraController.Instance.OnShipDestroyed(this);
                    ShipData.TargetThisShipController = null; // Clear the target ship controller
                    Destroy(beamWeaponGO);
                    Destroy(gameObject);
                    ShipManager.Instance.RemoveShipControllerFromList(this);
                    // FindAnyObjectByType<AudioManager>().Play("ShipDestroyed");
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
