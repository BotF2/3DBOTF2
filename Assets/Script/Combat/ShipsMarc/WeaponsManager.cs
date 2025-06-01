using Assets.SpaceCombat.AutoBattle.Scripts.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SpaceCombat.AutoBattle.Scripts.Starships
{
    public class WeaponsManager : MonoBehaviour
    {
        [SerializeField] private WeaponHardPoints _weaponHardPoints;
        //public WeaponHardPoints WeaponHardPoints => _weaponHardPoints;

        [SerializeField] private float _weaponsRange = 5f;
        public float WeaponsRange => _weaponsRange;

        public Collider StarshipCollider { get; set; }

        private IDictionary<TorpedoHardPointInfo, bool> _hardPointFireCoroutinesRunning = new Dictionary<TorpedoHardPointInfo, bool>();
        private AudioPlayer _audioPlayer;
        private Coroutine _fireAllTorpedosCoroutine;

        public void Awake()
        {
            foreach (var photonTorpedoHardPoint in _weaponHardPoints.PhotonTorpedoHardPoints)
            {
                _hardPointFireCoroutinesRunning.Add(photonTorpedoHardPoint, false);

                photonTorpedoHardPoint.Initialize();
                StartCoroutine(photonTorpedoHardPoint.ReloadStep());
            }

            _audioPlayer = GetComponentInChildren<AudioPlayer>();
        }

        public void FireEverything(Transform target)
        {
            foreach (var photonTorpedoHardPoint in _weaponHardPoints.PhotonTorpedoHardPoints)
            {
                if (!_hardPointFireCoroutinesRunning[photonTorpedoHardPoint])
                {
                    _fireAllTorpedosCoroutine = StartCoroutine(FireAllTorpedos(target, photonTorpedoHardPoint));
                }


                //if (photonTorpedoHardPoint.LoadedTorpedos > 0)
                //{
                //    while (photonTorpedoHardPoint.LoadedTorpedos != 0)
                //    {
                //        var gameObject = Instantiate(photonTorpedoHardPoint.WeaponPrefab, photonTorpedoHardPoint.HardPoint.transform.position, Quaternion.identity);

                //        var photonTorpedoScript = gameObject.GetComponent<PhotonTorpedo>();
                //        photonTorpedoScript.SetCurrentTarget(_destination);
                //        Physics.IgnoreCollision(gameObject.GetComponent<Collider>(), StarshipCollider);

                //        photonTorpedoHardPoint.LoadedTorpedos--;
                //    }
                //}

                //if (photonTorpedoHardPoint.WeaponRecharge >= photonTorpedoHardPoint.FireRate)
                //{
                //    var gameObject = Instantiate(photonTorpedoHardPoint.WeaponPrefab, photonTorpedoHardPoint.HardPoint.transform.position, Quaternion.identity);

                //    //var photonTorpedoAudioSource = gameObject.AddComponent<AudioSource>();
                //    //photonTorpedoAudioSource.playOnAwake = false;
                //    //photonTorpedoAudioSource.clip = photonTorpedoHardPoint.AudioClip;
                //    //photonTorpedoAudioSource.Play();

                //    var photonTorpedoScript = gameObject.GetComponent<PhotonTorpedo>();
                //    photonTorpedoScript.SetCurrentTarget(_destination);
                //    Physics.IgnoreCollision(gameObject.GetComponent<Collider>(), StarshipCollider);
                //    photonTorpedoHardPoint.WeaponRecharge = 0;
                //}
                //else
                //{
                //    photonTorpedoHardPoint.WeaponRecharge += photonTorpedoHardPoint.RechargeRate * Time.deltaTime;
                //}
            }
        }

        public void CeaseFire()
        {
            StopCoroutine(_fireAllTorpedosCoroutine);
        }

        IEnumerator FireAllTorpedos(Transform target, TorpedoHardPointInfo torpedoHardPointInfo)
        {
            _hardPointFireCoroutinesRunning[torpedoHardPointInfo] = true;

            while (torpedoHardPointInfo.LoadedTorpedos != 0)
            {
                var gameObject = Instantiate(torpedoHardPointInfo.WeaponPrefab, torpedoHardPointInfo.HardPoint.transform.position, Quaternion.identity);

                var photonTorpedoScript = gameObject.GetComponent<PhotonTorpedo>();
                photonTorpedoScript.SetCurrentTarget(target);
                Physics.IgnoreCollision(gameObject.GetComponent<Collider>(), StarshipCollider);

                _audioPlayer.PlayClip(torpedoHardPointInfo.AudioClip);

                torpedoHardPointInfo.LoadedTorpedos--;

                yield return torpedoHardPointInfo.WaitForFireAction;
            }

            _hardPointFireCoroutinesRunning[torpedoHardPointInfo] = false;
        }
    }
}
