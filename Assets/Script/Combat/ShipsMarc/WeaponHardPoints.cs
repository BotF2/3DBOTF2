using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SpaceCombat.AutoBattle.Scripts.Starships
{
    [Serializable]
    public class WeaponHardPoints
    {
        [SerializeField] private List<TorpedoHardPointInfo> _photonTorpedoHardPoints;

        public List<TorpedoHardPointInfo> PhotonTorpedoHardPoints => _photonTorpedoHardPoints;
    }

    [Serializable]
    public class TorpedoHardPointInfo
    {
        [SerializeField] public GameObject _hardPoint;
        [SerializeField] public GameObject _weaponPrefab;
        [SerializeField] public float _fireRate;
        [SerializeField] public float _maxTorpedoTubes;
        [SerializeField] public float _loadedTorpedos;
        [SerializeField] public float _reloadRate;
        [SerializeField] private AudioClip _audioClip;

        WaitForSeconds _waitForReloadAction;
        WaitForSeconds _waitForFireAction;

        public GameObject HardPoint => _hardPoint;
        public GameObject WeaponPrefab => _weaponPrefab;
        public float MaxTorpedoTubes => _maxTorpedoTubes;
        public AudioClip AudioClip => _audioClip;
        public float LoadedTorpedos
        {
            get => _loadedTorpedos;
            set => _loadedTorpedos = value;
        }

        public WaitForSeconds WaitForFireAction => _waitForFireAction;

        public void Initialize()
        {
            _waitForReloadAction = new(_reloadRate);
            _waitForFireAction = new(_fireRate);
        }

        public IEnumerator ReloadStep()
        {
            while (true)
            {
                if (LoadedTorpedos < MaxTorpedoTubes)
                {
                    LoadedTorpedos++;
                }

                yield return _waitForReloadAction;
            }
        }
    }
}
