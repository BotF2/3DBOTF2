using System;
using UnityEngine;

namespace BOTF3D.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : MonoBehaviour
    {
        private AudioSource audioSource;
        public event Action OnFinished;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        public void Initialize(AudioClip clip, float volume, float pitch, bool loop, float minDist, float maxDist)
        {
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.loop = loop;
            audioSource.spatialBlend = 1f; // Full 3D
            audioSource.minDistance = minDist;
            audioSource.maxDistance = maxDist;
            audioSource.Play();

            if (!loop)
            {
                Invoke(nameof(Finish), clip.length / pitch);
            }
        }

        private void Finish()
        {
            OnFinished?.Invoke();
            OnFinished = null; // Clear listeners
        }

        public void Stop()
        {
            audioSource.Stop();
            CancelInvoke(nameof(Finish));
            Finish();
        }
    }
}