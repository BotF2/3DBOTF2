using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BOTF3D.SpaceCombat.AutoBattle.Scripts.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        private List<AudioSource> _audioSources;

        // Start is called before the first frame update
        void Awake()
        {
            _audioSources = new List<AudioSource>(GetComponents<AudioSource>());
        }

        public void PlayClip(AudioClip clip)
        {
            var audioSource = _audioSources.FirstOrDefault(x => !x.isPlaying);

            if (audioSource == null)
            {
                Debug.Log("No Audio Sources Available");
                return;
            }

            audioSource.clip = clip;
            audioSource.Play();
        }

    }
}
