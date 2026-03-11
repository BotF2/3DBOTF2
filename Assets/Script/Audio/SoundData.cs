using UnityEngine;

namespace BOTF3D.Audio
{
    [CreateAssetMenu(fileName = "New Sound", menuName = "Audio/Sound Data")]
    public class SoundData : ScriptableObject
    {
        [Header("Audio Clip")]
        public AudioClip clip;

        [Header("Playback Settings")]
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        [Range(0f, 0.3f)] public float randomPitchVariation = 0.1f;

        [Header("Spatial Settings")]
        public bool is3D = false;
        public float minDistance = 1f;
        public float maxDistance = 500f;

        [Header("Categorization")]
        public AudioCategory category = AudioCategory.SFX;
        public bool loop = false;
    }

    public enum AudioCategory
    {
        Music,
        SFX,
        UI,
        Weapon,
        Ambient,
        Voice
    }
}