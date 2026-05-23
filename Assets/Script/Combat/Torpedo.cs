using BOTF3D.Audio;
using BOTF3D.Core;
using UnityEngine;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Torpedo projectile with velocity-based accuracy.
    /// Higher relative velocity between torpedo and target = lower hit chance.
    /// Initialized with damage and sounds from the firing ship.
    /// No longer uses WeaponSO - uses ship's TorpedoDamage and audio clips.
    /// </summary>
    public class Torpedo : MonoBehaviour
    {
        [Header("Torpedo Movement")]
        [Tooltip("Torpedo flight speed (units per second)")]
        public float Velocity = 100f;

        [Tooltip("How fast torpedo can turn to track target")]
        public float TurnRatio = 10f;

        public Transform Target;
        public Rigidbody torpedoRigidbody;
        Vector3 currentPosition;
        Vector3 direction;
        [Header("Torpedo Identity")]
        public CivEnum OwnerCivEnum;
        public CivEnum TargetCivEnum;

        [Header("Damage (Set by Ship)")]
        public int TorpedoDamage; // Assigned from ShipData.TorpedoDamage

        private AudioClip torpedoFireSound;
        private AudioClip torpedoImpactSound;

        [Header("Velocity-Based Accuracy")]
        [Tooltip("Max relative speed for accuracy calculation (m/s)")]
        [SerializeField] private float maxRelativeSpeed = 200f;

        [Tooltip("Minimum hit chance at max relative speed (0.2 = 20%)")]
        [SerializeField] private float minAccuracy = 0.2f;

        private bool hasCheckedAccuracy = false;
        private bool willMiss = false;
        private float missDestructionTimer = 0f;
        private const float MISS_FLYBY_TIME = 5f; // Seconds to fly past target before destruction

        private void Awake()
        {
            torpedoRigidbody = GetComponent<Rigidbody>();
            if (torpedoRigidbody == null)
            {
                Debug.LogError("Torpedo: Rigidbody component not found!");
            }
            else
            {
                // Make kinematic so physics doesn't interfere with manual movement
                torpedoRigidbody.isKinematic = true;
                torpedoRigidbody.useGravity = false;
            }
        }

        /// <summary>
        /// Initialize torpedo with damage and sounds from the firing ship.
        /// Call this immediately after instantiating the torpedo prefab.
        /// </summary>
        /// <param name="damage">Torpedo damage from ShipData.TorpedoDamage</param>
        /// <param name="fireSound">Fire sound from ShipController.clipTorpedoFire</param>
        /// <param name="impactSound">Optional impact/explosion sound</param>
        public void Initialize(int damage, AudioClip fireSound, AudioClip impactSound = null)
        {
            TorpedoDamage = damage;
            torpedoFireSound = fireSound;
            torpedoImpactSound = impactSound;

            Debug.Log($"🚀 Torpedo initialized: damage={damage}, hasFireSound={fireSound != null}");
        }

        private void Start()
        {
            if (Target == null)
            {
                Debug.LogWarning($"🚀⚠️ Torpedo {gameObject.name} has NO TARGET - will self-destruct!");
                Destroy(gameObject);
                return;
            }

            // ✅ Play fire sound once at launch position
            if (torpedoFireSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX3DClip(torpedoFireSound, transform.position);
            }

            Debug.Log($"🚀 Torpedo launched: target={Target.name}, velocity={Velocity}, damage={TorpedoDamage}");
        }

        private void Update()
        {
            // If torpedo will miss, continue flyby behavior even if target is destroyed
            if (willMiss)
            {
                missDestructionTimer += Time.unscaledDeltaTime;
                if (missDestructionTimer >= MISS_FLYBY_TIME)
                {
                    Debug.Log($"🚀 Missed torpedo flyby complete - self-destructing");
                    Destroy(gameObject);
                    return;
                }

                // Continue flying in miss direction
                currentPosition = transform.position;
                direction = transform.forward; // Use forward direction since we already calculated it
                float speedThisFrame = Velocity * Time.unscaledDeltaTime;
                transform.position += direction * speedThisFrame;
                return;
            }

            // If target destroyed and torpedo hasn't determined hit/miss yet, destroy immediately
            if (Target == null)
            {
                Debug.Log($"🚀 Torpedo target destroyed - self-destructing");
                Destroy(gameObject);
                return;
            }

            currentPosition = transform.position;
            Vector3 targetPosition = Target.position;
            direction = (targetPosition - currentPosition).normalized;

            // ✅ Velocity-based accuracy check (one-time check at launch)
            if (!hasCheckedAccuracy)
            {
                PerformAccuracyCheck(ref direction, targetPosition, currentPosition);
            }

            // ✅ Normal tracking behavior (will hit)
            TrackTarget(currentPosition, targetPosition, direction);
        }

        /// <summary>
        /// Perform one-time accuracy check based on relative velocity.
        /// Higher relative speed = lower accuracy = higher miss chance.
        /// </summary>
        private void PerformAccuracyCheck(ref Vector3 direction, Vector3 targetPosition, Vector3 currentPosition)
        {
            hasCheckedAccuracy = true;

            ShipController targetShip = Target.GetComponentInParent<ShipController>();

            // Calculate relative velocity
            Vector3 torpedoVelocity = direction * Velocity;
            Vector3 targetVelocity = GetShipVelocity(targetShip);
            Vector3 relativeVelocity = targetVelocity - torpedoVelocity;
            float relativeSpeed = relativeVelocity.magnitude;

            // Calculate accuracy factor (1.0 = 100% at low speed, 0.2 = 20% at high speed)
            float accuracyFactor = Mathf.Lerp(1.0f, minAccuracy, Mathf.Clamp01(relativeSpeed / maxRelativeSpeed));

            // Random roll to determine hit/miss
            float hitRoll = Random.value;

            if (hitRoll > accuracyFactor)
            {
                // ✅ MISS - torpedo veers off course
                willMiss = true;
                Debug.Log($"🚀 MISS! Torpedo will miss {Target.name} (relSpeed={relativeSpeed:F1}, accuracy={accuracyFactor:F2}, roll={hitRoll:F2})");

                // Add random offset to make torpedo miss
                Vector3 missOffset = Random.insideUnitSphere * 50f;
                direction = (targetPosition + missOffset - currentPosition).normalized;

                // Set rotation to face miss direction
                transform.rotation = Quaternion.LookRotation(direction);

                // Timer will handle destruction after MISS_FLYBY_TIME seconds
                missDestructionTimer = 0f;
            }
            else
            {
                // ✅ HIT - torpedo will track target accurately
                Debug.Log($"🚀 HIT trajectory locked! Tracking {Target.name} (relSpeed={relativeSpeed:F1}, accuracy={accuracyFactor:F2})");
            }
        }

        /// <summary>
        /// Track target with homing behavior
        /// </summary>
        private void TrackTarget(Vector3 currentPosition, Vector3 targetPosition, Vector3 direction)
        {
            // Move toward target
            float speedThisFrame = Velocity * Time.unscaledDeltaTime;
            Vector3 newPosition = Vector3.MoveTowards(currentPosition, targetPosition, speedThisFrame);
            transform.position = newPosition;

            // Rotate to face target
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    TurnRatio * 100f * Time.unscaledDeltaTime
                );
            }

            // Check if reached target (within 2 units)
            float distanceToTarget = Vector3.Distance(newPosition, targetPosition);

            if (distanceToTarget < 2f)
            {
                Debug.Log($"💥 Torpedo reached target {Target.name} at distance {distanceToTarget:F2}");

                ShipController targetShip = Target.GetComponentInParent<ShipController>();
                if (targetShip != null && OwnerCivEnum != targetShip.ShipData.CivEnum)
                {
                    Debug.Log($"💥 Torpedo HIT {targetShip.ShipData.ShipName} for {TorpedoDamage} damage");
                    targetShip.TakeDamage(TorpedoDamage);

                    // Play explosion sound
                    if (torpedoImpactSound != null && AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySFX3DClip(torpedoImpactSound, transform.position);
                    }
                }
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Get ship velocity from Rigidbody or estimate from movement speed
        /// </summary>
        private Vector3 GetShipVelocity(ShipController ship)
        {
            if (ship == null) return Vector3.zero;

            // Try to get velocity from Rigidbody
            Rigidbody rb = ship.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                return rb.linearVelocity;
            }

            // Fallback: estimate from combat order movement
            var orderStateMachine = ship.GetComponent<CombatOrderStateMachine>();
            if (orderStateMachine != null)
            {
                float speed = ship.ShipData.maxWarpFactor * orderStateMachine.GetOrderSpeedFactor();
                return ship.transform.forward * speed;
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Collision-based hit detection (backup to distance check)
        /// </summary>
        public void OnTriggerEnter(Collider other)
        {
            if (willMiss) return; // Don't apply damage if torpedo is missing

            Debug.Log($"🚀 Torpedo collided with {other.gameObject.name}");

            ShipController shipController = other.gameObject.GetComponent<ShipController>();
            if (shipController != null && OwnerCivEnum != shipController.ShipData.CivEnum)
            {
                Debug.Log($"💥 Torpedo COLLISION HIT {shipController.ShipData.ShipName} for {TorpedoDamage} damage");
                shipController.TakeDamage(TorpedoDamage);

                // Play explosion sound
                if (torpedoImpactSound != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX3DClip(torpedoImpactSound, transform.position);
                }

                Destroy(gameObject);
            }
        }
    }
}

