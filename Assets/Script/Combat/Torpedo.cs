using BOTF3D.Core;
using UnityEngine;

namespace BOTF3D.GamePlay
{
    public class Torpedo : MonoBehaviour
    {
        public float Velocity = 100f; // the velocity used appears to be set in the prefab and not this value.
        public float TurnRatio = 10f; // same, set in prefab, not this value. keep it for now, have options different torpedos by civ
        public Transform Target;
        public Rigidbody torpedoRigidbody;
        public CivEnum OwnerCivEnum;
        public CivEnum TargetCivEnum;
        public int TorpedoDamage;
        private AudioSource audioSource;

        private void Awake()
        {
            torpedoRigidbody = GetComponent<Rigidbody>();
            if (torpedoRigidbody == null)
            {
                Debug.LogError("Torpedo Rigidbody is not assigned!");
            }
            audioSource = GetComponent<AudioSource>();
        }
        public void SetCurrentTarget(Transform targetTransform)
        {
            Target = targetTransform;
        }

        private void FixedUpdate()
        {
            if (Target == null)
            {
                Destroy(gameObject); // Destroy the torpedo if no target is set
                return;
            }
            Vector3 currentPosition = torpedoRigidbody.position;
            Vector3 direction = (Target.position - currentPosition).normalized;
            float speedWhileGameTimePaused = Velocity * Time.fixedUnscaledDeltaTime;
            Vector3 nextPosition = Vector3.MoveTowards(currentPosition, Target.position, speedWhileGameTimePaused);
            torpedoRigidbody.MovePosition(nextPosition);
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                torpedoRigidbody.MoveRotation(Quaternion.RotateTowards(torpedoRigidbody.rotation, targetRotation, TurnRatio * Time.fixedUnscaledDeltaTime));
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            // TODO, need to add explosion and sound effects (add new a object for that)

            ShipController shipController = other.gameObject.GetComponent<ShipController>();
            if (shipController != null && OwnerCivEnum != shipController.ShipData.CivEnum)
            {
                shipController.TakeDamage(TorpedoDamage);
            }
            if (shipController != null && TargetCivEnum == shipController.ShipData.CivEnum)
            {
                Destroy(gameObject); // Destroy the torpedo after it hits something
            }
        }
        private void DoDamage(ShipController shipController)
        {
            //if (shipController.ShipData.ShieldHealth > 0)
            //{
            //    // If the ship has shields, damage the shields first
            //    shipController.ShipData.ShieldHealth -= (TorpedoDamage/2);
            //    return;
            //}
            //else if (shipController.ShipData.HullHealth > 0)
            //{
            //    shipController.ShipData.HullHealth -= (TorpedoDamage/3); // Example damage value
            //    return;
            //}
            //else         
            //{
            //    // If both shields and hull are destroyed, destroy the ship
            //    Destroy(shipController.gameObject);
            //    ShipCombatCameraController.Instance.OnShipDestroyed(shipController);
            //}
            //Destroy(gameObject); // Destroy the torpedo after it hits something
        }
    }
}

