using Assets.Core;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;


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
    public float Velocity = 10f;
    public float OrbitDistance = 20f;  // radius of the orbit
    public float Acceleration = 100f; 
    public float Deceleration = 3f; 
    public float StopDistance; // See Start()
    private int flipCombatSide =1;
    private float currentVelocity = 1;
    private Rigidbody rb;
    public bool WarpingInOver = false;
    private bool setSpeed = true;
    private GameObject beamWeaponGO;
    public CombatOrders Order; // orders for the ship, e.g. attack, defend, patrol
    [SerializeField] private float minRefireDelay; // see Start()
    [SerializeField] private float maxRefireDelay;
    public Image HealthFillImage;
    public float HealthSpeed;
    public float TargetFillAmount { get; set; } = 1.0f;
    public float Health;
    public float MaxHealth;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // space, microgravity set to zero
        rb.linearDamping = 0f; // no air drag
        rb.angularDamping = 0.5f; // small resistance to rotation

    }
    private void Start()
    {
        theSource = GetComponent<AudioSource>();
        currentVelocity = 300f; // initial speed
        if (transform.position.x < 0) flipCombatSide = -1; // if on left side of map, flip direction
        StopDistance = 3f;
        Deceleration = 3f;
        minRefireDelay = 1.5f;
        maxRefireDelay = 2.5f;
        MaxHealth = ShipData.HullHealth + ShipData.HullHealth;
        Health = MaxHealth;
        HealthSpeed = 10.0f;

    }
    void Update()
    {
        // see TakeDamaga()
        if (HealthFillImage != null)
            HealthFillImage.fillAmount = Mathf.Lerp(HealthFillImage.fillAmount, TargetFillAmount, HealthSpeed * Time.deltaTime);
        TargetFillAmount = Health / MaxHealth;
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
                    EngageWithSpaceNewtonianPhysics();
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
        // this is for SpaceCombatScene, not galaxy map 
        ShipController shipController = collider.gameObject.GetComponent<ShipController>();
        if (shipController != null) // it is a shipController 
        {
            OnShipEncounteredShip(shipController);
            Debug.Log("Controller collided with " + shipController.gameObject.name);
        }
    }

    private void EngageWithSpaceNewtonianPhysics()
    {
        #region Simplistic but mostly realistic Newtonian movement along a path in space
        // One time push simulating warp in residual velocity
        Vector3 move = currentVelocity * transform.forward * flipCombatSide;
        if (setSpeed)
        {
            rb.linearVelocity = Vector3.zero; //  0 linear momentum
            rb.angularVelocity = Vector3.zero; // 0 angular momentum
            rb.AddForce(move * Acceleration, ForceMode.Acceleration);
            setSpeed = false;
        }
        else
        {
            // Gradually slow down when approaching the stop point
            float distanceToCenter = Mathf.Abs(transform.position.x - 0f);

            if (distanceToCenter > StopDistance && rb.linearVelocity.magnitude > 0.1f)
            {
                Vector3 brakingForce = -rb.linearVelocity.normalized * Deceleration;
                rb.angularVelocity = Vector3.zero; // 0 angular momentum
                rb.AddForce(brakingForce, ForceMode.Acceleration);
                if (this.ShipData.ShipType == ShipType.Transport) // extra braking for transports
                {
                    rb.AddForce(brakingForce * 0.5f, ForceMode.Acceleration);
                }
            }
            else
            {
                rb.linearVelocity = Vector3.zero; // Full stop
            }
        }

        #endregion

    }
    private void MoveLikeAirplane()
    {
        #region How to make ships circle each other, move like airplanes
        //Instead of always moving towards the enemy group’s center, compute a circle vector around that center:
        // Ships move like banking airplanes and not like spaceships in a vacuum.
        if (TargetGroup != null)
        {
            // Direction to the enemy group
            Vector3 toTarget = (TargetGroup.position - rb.position).normalized;

            // Choose an "orbit axis" (here: world up for flat 2D circling)
            Vector3 orbitAxis = Vector3.up;

            // Rotate the direction vector 90° around the axis to get tangent direction
            Vector3 orbitDirection = Quaternion.AngleAxis(90, orbitAxis) * toTarget;

            // Blend between circling and moving toward the orbit distance
            Vector3 desiredPosition = TargetGroup.position - toTarget * OrbitDistance;
            Vector3 moveDir = (desiredPosition - rb.position).normalized;

            // Add orbiting movement
            Vector3 finalDir = (moveDir + orbitDirection * 0.5f).normalized;

            // Move
            Vector3 nextPosition = rb.position + finalDir * Velocity * Time.fixedUnscaledDeltaTime;
            rb.MovePosition(nextPosition);

            // Rotate to face movement
            if (finalDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(finalDir);
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, 200f * Time.fixedUnscaledDeltaTime));
            }
            //        What this does

            //TargetGroup = the other fleet’s center or leader GameObject.

            //Ships try to maintain a distance(OrbitDistance) from that point.

            //They add a tangential offset(orbitDirection) so they don’t collide head - on but instead circle.

            //Both groups, if given each other as TargetGroup, will end up orbiting each other like two swarms circling.

            //Options to tweak

            //Change orbitAxis: Vector3.up for flat 2D plane battles, or Vector3.Cross(toTarget, Vector3.up) for more dynamic 3D orbits.

            //Adjust OrbitDistance to avoid collisions between fleets.

            //Randomize OrbitSpeed slightly per ship for more natural motion.
        }
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
        yield return new WaitForSeconds(initialDelay);
        bool beam = true;
        while (true) // ToDo: not true when ship weapons are offline?
        {
            // Fire the ship's beam weapons
            FireWeapons(beam);
            if (beam)
                beam = false;
            else
                beam = true;
            // Wait for a random refire delay before next shot
            float refireDelay = UnityEngine.Random.Range(minRefireDelay, maxRefireDelay);
            yield return new WaitForSeconds(refireDelay);
        }
    }
    internal void FireWeapons(bool baem)
    {
        if (ShipData.TargetThisShipController != null)
        {
            if (this != null && transform != null)
            {
                if (baem && ShipData.BeamDamage > 0)
                {
                    var beamWeaponGo = Instantiate(beamWeaponPrefab, this.transform.position, Quaternion.identity);
                    beamWeaponGO = beamWeaponGo;
                    var lineRenderer = beamWeaponGo.GetComponent<LineRenderer>();
                    var beamWeaponScript = beamWeaponGo.GetComponent<BeamWeapon>();
                    beamWeaponScript.TargetTransform = ShipData.TargetThisShipController.ShipData.TargetOnThisShip.transform; // Set the target transform
                    beamWeaponScript.WeaponTransform = this.transform; // Set the weapon transform
                    beamWeaponScript.LineRenderer = lineRenderer;
                    beamWeaponScript.SetWeaponAndTarget(this.transform, ShipData.TargetThisShipController.ShipData.TargetOnThisShip.transform); // Set the weapon and target transforms
                    ShipData.TargetThisShipController.TakeDamage(ShipData.BeamDamage);
                    Destroy(beamWeaponGo, 0.5f); // Destroy the beam after so much time
                }
                else if (ShipData.TorpedoDamage > 0)
                {
                    var torpedoGo = Instantiate(torpedoPrefab, this.transform.position, Quaternion.identity);
                    var torpedoScript = torpedoGo.GetComponent<Torpedo>();
                    torpedoScript.TorpedoDamage = ShipData.TorpedoDamage;
                    if (ShipData.TargetThisShipController != null)
                    {
                        torpedoScript.Target = ShipData.TargetThisShipController.ShipData.TargetOnThisShip.transform; // ShipData.TargetForThisShip is GameObject and Torpedo.Target is Transform
                        torpedoScript.TargetCivEnum = ShipData.TargetThisShipController.ShipData.CivEnum; // Get the target ship's CivEnum
                    }
                }
            }
        }
    }
    public void TakeDamage(int weaponDamageInt)
    {
        if (Health != 0) 
        {
            Health -= (weaponDamageInt / 3);
            Health = Mathf.Max(Health, 0.0f ); // if Health goes below zero, set to zero
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
            // If both shields and hull are zero, destroy the ship
            var fleetController = this.ShipData.FleetController;
            if (fleetController != null && !ShipData.Distroyed)
            {
                ShipData.Distroyed = true;
                fleetController.RemoveShipFromFleet(this);
                CombatManager.Instance.RemoveThisShipController(this);

                ShipCombatCameraController.Instance.OnShipDestroyed(this);
                ShipData.TargetThisShipController = null; // Clear the target ship controller
                this.ShipData.FleetController.FleetData.ShipsList.Remove(this); // Remove this ship from the fleet's ship list
                                                                                // this can be problematic, FleetController can be null when its script is still running giving null reference exception
                                                                                // FleetManager.Instance.RemoveFleetConIfShipListIsEmpty(this); // Remove this ship from all ship lists in FleetManager
                Destroy(beamWeaponGO);
                Destroy(gameObject);

                this.ShipData.FleetController.IsTheFleetDestroyed();
                ShipManager.Instance.RemoveShipControllerFromList(this);
                FindAnyObjectByType<AudioManager>().Play("ShipDestroyed");
                
            }
        }
    }

    internal void SetWarpInOver()
    {
        WarpingInOver = true;
        rb.isKinematic = false; // enable physics
    }
}
