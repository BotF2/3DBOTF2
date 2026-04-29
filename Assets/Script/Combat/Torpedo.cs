using BOTF3D.Audio;
using BOTF3D.Core;
using BOTF3D.GamePlay;
using UnityEngine;

namespace BOTF3D.Combat
{
    public class Torpedo : MonoBehaviour
    {
        public float Velocity = 100f;
        public float TurnRatio = 10f;
        public Transform Target;
        public Rigidbody torpedoRigidbody;
        public CivEnum OwnerCivEnum;
        public CivEnum TargetCivEnum;
        public int TorpedoDamage;
        private AudioSource audioSource;
        [SerializeField] private WeaponSO weaponData;

        private void Awake()
        {
            torpedoRigidbody = GetComponent<Rigidbody>();
            if (torpedoRigidbody == null)
            {
                Debug.LogError("Torpedo Rigidbody is not assigned!");
            }
            else
            {
                // ✅ Make kinematic so physics doesn't interfere
                torpedoRigidbody.isKinematic = true;
                torpedoRigidbody.useGravity = false;
            }
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            // ✅ Start runs after target is assigned
            if (Target == null)
            {
                Debug.LogWarning($"🚀⚠️ Torpedo {gameObject.name} has NO TARGET in Start() - will destroy!");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"🚀✅ Torpedo {gameObject.name} initialized with target={Target.name}, velocity={Velocity}");
            }
        }

        // ✅ Use Update with transform.position instead of Rigidbody.MovePosition
        private void Update()
        {
            if (Target == null)
            {
                Debug.Log($"🚀⚠️ Torpedo {gameObject.name} target destroyed - destroying torpedo");
                Destroy(gameObject);
                return;
            }

            Vector3 currentPosition = transform.position;
            Vector3 targetPosition = Target.position;
            Vector3 direction = (targetPosition - currentPosition).normalized;

            // ✅ Use unscaledDeltaTime for movement during paused galaxy time
            float speedThisFrame = Velocity * Time.unscaledDeltaTime;

            // ✅ Move directly using transform.position (works better for kinematic with timeScale=0)
            Vector3 newPosition = Vector3.MoveTowards(currentPosition, targetPosition, speedThisFrame);
            transform.position = newPosition;

            // ✅ Rotate to face target
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, TurnRatio * 100f * Time.unscaledDeltaTime);
            }

            // ✅ Check if reached target
            float distanceToTarget = Vector3.Distance(newPosition, targetPosition);

            // ✅ Debug log every 30 frames to verify movement
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"🚀 Torpedo moving: pos={newPosition:F1}, target={targetPosition:F1}, distance={distanceToTarget:F2}, speed={speedThisFrame:F2}");
            }

            // ✅ Destroy if reached target (within 2 units)
            if (distanceToTarget < 2f)
            {
                Debug.Log($"💥 Torpedo reached target {Target.name} at distance {distanceToTarget:F2}");

                // Find and damage the target ship
                ShipController targetShip = Target.GetComponentInParent<ShipController>();
                if (targetShip != null && OwnerCivEnum != targetShip.ShipData.CivEnum)
                {
                    Debug.Log($"💥 Torpedo HIT {targetShip.ShipData.ShipName} for {TorpedoDamage} damage");
                    targetShip.TakeDamage(TorpedoDamage);

                    // Play explosion sound
                    if (weaponData?.impactSound != null)
                    {
                        AudioManager.Instance?.PlaySoundData3D(weaponData.impactSound, transform.position);
                    }
                }
                Destroy(gameObject);
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            Debug.Log($"🚀 Torpedo collided with {other.gameObject.name}");

            ShipController shipController = other.gameObject.GetComponent<ShipController>();
            if (shipController != null && OwnerCivEnum != shipController.ShipData.CivEnum)
            {
                Debug.Log($"💥 Torpedo TRIGGER HIT {shipController.ShipData.ShipName} for {TorpedoDamage} damage");
                shipController.TakeDamage(TorpedoDamage);

                // Play explosion sound
                if (weaponData?.impactSound != null)
                {
                    AudioManager.Instance?.PlaySoundData3D(weaponData.impactSound, transform.position);
                }

                Destroy(gameObject);
            }
        }
    }
}

