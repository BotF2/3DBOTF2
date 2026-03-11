using UnityEngine;

namespace BOTF3D.Core
{
    [CreateAssetMenu(fileName = "SoundSO", menuName = "Scriptable Objects/SoundSO")]
    public class SoundSO : ScriptableObject
    {
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;

        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

        public bool spatial = true;

        [Range(0, 10)]
        public int priority = 5;
    }
}
