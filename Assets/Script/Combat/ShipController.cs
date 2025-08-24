using Assets.Core;
using System;
using System.Linq;
using UnityEngine;
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
    public float OrbitSpeed = 30f; // how fast to orbit (degrees per second)
    public float MaxSpeed = 50f; // top speed
    public float Acceleration = 20f; // units per second^2
    public float Deceleration = 20f; // units per second^2
    public float TurnSpeed = 60f; // deg/sec rotation speed
    public Transform PathStart; // start of flight path
    public Transform PathEnd;
    public Transform StopPoint;
    private Rigidbody rb;
    private bool warpingInOver = false;
    private bool goingForward = true;
    private bool isStopping = false;
    public CombatOrders Order; // orders for the ship, e.g. attack, defend, patrol


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;            // space, microgravity set to zero
        rb.linearDamping = 0f;            // no air drag
        rb.angularDamping = 0.5f;         // small resistance to rotation
    }
    public void Init(ShipManager shipManager)
    {
        ShipManager.Instance = shipManager;
    }
    private void Update()
    {
       // move ship
        if (ShipData != null && ShipData.TargetThisShipController != null)
        {

        }
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

    private void FixedUpdate()
    {
        if (warpingInOver)
        {
            switch (Order)
            {
                case CombatOrders.None:
                    // No orders, do nothing
                    break;
                case CombatOrders.Engage:
                    EngageWithSpaceNewtonianPhysics();
                    // MoveLikeAirplane
                    break;
                case CombatOrders.Formation:
                    // Stay in position or move to a defensive position
                    break;
                case CombatOrders.Retreat:
                    // Patrol logic can be implemented here
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
    private void EngageWithSpaceNewtonianPhysics()
    {
        // Move towards the target group
        #region How to make realistic movement along a path in space
        // move like a spaceship in space, Newtonian-style “thrust + coasting + braking” system.
        if (StopPoint == null) return;

        Vector3 toTarget = (StopPoint.position - transform.position);
        float distance = toTarget.magnitude;

        // Calculate stopping distance = v² / (2a)
        float stoppingDistance = (rb.linearVelocity.sqrMagnitude) / (2f * Deceleration);

        // Decide whether to accelerate or decelerate
        if (distance > stoppingDistance)
        {
            // Accelerate forward until max speed
            if (rb.linearVelocity.magnitude < MaxSpeed)
                rb.AddForce(transform.forward * Acceleration, ForceMode.Acceleration);
        }
        else
        {
            // Start decelerating
            isStopping = true;
            if (rb.linearVelocity.magnitude > 0.1f)
                rb.AddForce(-rb.linearVelocity.normalized * Deceleration, ForceMode.Acceleration);
            else
                rb.linearVelocity = Vector3.zero; // Full stop
        }

        // Rotate ship to face velocity while moving
        //if (rb.linearVelocity.sqrMagnitude > 0.1f)
        //{
        //    Quaternion targetRot = Quaternion.LookRotation(rb.linearVelocity.normalized, Vector3.up);
        //    rb.rotation = Quaternion.RotateTowards(rb.rotation, targetRot, TurnSpeed * Time.fixedDeltaTime);
        //}
        //else if (isStopping)
        //{
        //    // Face the enemy stop point after coming to a halt
        //    Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        //    rb.rotation = Quaternion.RotateTowards(rb.rotation, targetRot, TurnSpeed * Time.fixedDeltaTime);
        //}
        #endregion

    }
    private void MoveLikeAirplane()
    {
        #region How to make ships circle each other
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
                StopPoint = new GameObject("StopPoint").transform; // Create a stop point for the ship
                StopPoint.Translate(transform.position + transform.forward * 350f); // Set the stop point ahead of the ship
                break;
            case CombatOrders.Rush:
                break;
            case CombatOrders.Retreat:
                break;
            case CombatOrders.Formation:
                break;
            case CombatOrders.TargetTransports:
                break;
            case CombatOrders.None:
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

    internal void FireWeapons(bool baem)
    {
        if (ShipData.TargetThisShipController != null)
        { 
            if (baem && ShipData.BeamDamage > 0)
            {
                var beamWeaponGo = Instantiate(beamWeaponPrefab, this.transform.position, Quaternion.identity);
                var lineRenderer = beamWeaponGo.GetComponent<LineRenderer>();
                var beamWeaponScript = beamWeaponGo.GetComponent<BeamWeapon>();
                beamWeaponScript.TargetTransform = ShipData.TargetThisShipController.ShipData.TargetOnThisShip.transform; // Set the target transform
                beamWeaponScript.WeaponTransform = this.transform; // Set the weapon transform
                beamWeaponScript.LineRenderer = lineRenderer;
                beamWeaponScript.SetWeaponAndTarget(this.transform, ShipData.TargetThisShipController.ShipData.TargetOnThisShip.transform); // Set the weapon and target transforms
                TakeDamage(ShipData.BeamDamage); 
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
    public void TakeDamage(int weaponDamageInt)
    {
        if (ShipData.ShieldHealth > 0)
        {
            //If the ship has shields, damage the shields first
            ShipData.ShieldHealth -= (weaponDamageInt / 2);
            return;
        }
        else if (ShipData.HullHealth > 0)
        {
            ShipData.HullHealth -= (weaponDamageInt  / 3);
            return;
        }
        else
        {
            // If both shields and hull are destroyed, destroy the ship
            var fleetController = this.ShipData.FleetController;
            if (fleetController != null)
            {
                fleetController.RemoveShipFromFleet(this);
            }
            ShipCombatCameraController.Instance.OnShipDestroyed(this);
            ShipData.TargetThisShipController = null; // Clear the target ship controller
            Destroy(gameObject);
            FindAnyObjectByType<AudioManager>().Play("ShipDestroyed");
        }

    }

    internal void SetWarpInOver()
    {
        warpingInOver = true;
    }
}
