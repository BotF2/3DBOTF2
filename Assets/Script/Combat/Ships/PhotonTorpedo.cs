using UnityEngine;

namespace Assets.SpaceCombat.AutoBattle.Scripts.Starships
{
    public class PhotonTorpedo : MonoBehaviour
    {
        private float _velocity = 2.5f;
        private float _turnRate = 3f;
        private Transform _target;
        [SerializeField] private GameObject[] _children;

        public void SetCurrentTarget(Transform targetTransform)
        {
            _target = targetTransform;
        }

        private void FixedUpdate()
        {
            if (_target == null)
            {
                transform.Translate(Vector3.forward * _velocity * Time.deltaTime);
                return;
            }

            // TODO: Fire in-front first, then home to _destination, use a co-routine

            //TODO: slow down to battle speed

            var targetRotation = Quaternion.LookRotation(_target.position - transform.position);
            var turningStrength = _turnRate * Time.deltaTime;
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turningStrength);

            transform.Translate(Vector3.forward * _velocity * Time.deltaTime);
        }

        public void OnCollisionEnter(Collision collision)
        {
            // TODO, need to add explosion and sound effects (add new a object for that)

            Destroy(gameObject);
        }
    }
}

