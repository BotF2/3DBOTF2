using BOTF3D.Core;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BOTF3D.Audio
{
    [CreateAssetMenu(fileName = "New Sound", menuName = "Audio/Sound Data")]
    public class SoundData : ScriptableObject
    {
        [Header("Audio Clip")]
        [Tooltip("Single clip - leave empty if using random clips")]
        public AudioClip clip;

        [Tooltip("Random clips - pick one at random each play. Takes priority over single clip.")]
        public AudioClip[] randomClips;

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

        /// <summary>
        /// Gets the clip to play - returns random clip if available, otherwise single clip
        /// </summary>
        public AudioClip GetClip()
        {
            if (randomClips != null && randomClips.Length > 0)
            {
                return randomClips[Random.Range(0, randomClips.Length)];
            }
            return clip;
        }

        /// <summary>
        /// Gets the final pitch with random variation applied
        /// </summary>
        public float GetPitchWithVariation()
        {
            return pitch + Random.Range(-randomPitchVariation, randomPitchVariation);
        }

        #region Editor Preview (Free - Not Odin!)
#if UNITY_EDITOR
        private AudioSource previewSource;

        private void OnEnable()
        {
            StopPreview(); // Cleanup any existing preview
        }

        private void OnDisable()
        {
            StopPreview();
        }

        /// <summary>
        /// Play preview of this sound in the editor (Inspector)
        /// Right-click this asset → "Preview Sound"
        /// </summary>
        [ContextMenu("Preview Sound")]
        public void PlayPreview()
        {
            StopPreview();

            AudioClip clipToPlay = GetClip();
            if (clipToPlay == null)
            {
                Debug.LogWarning($"⚠️ No audio clip assigned to {name}");
                return;
            }

            // Create temporary preview GameObject
            GameObject previewObj = EditorUtility.CreateGameObjectWithHideFlags(
                $"PREVIEW_{name}",
                HideFlags.HideAndDontSave,
                typeof(AudioSource)
            );

            previewSource = previewObj.GetComponent<AudioSource>();
            previewSource.clip = clipToPlay;
            previewSource.volume = volume;
            previewSource.pitch = GetPitchWithVariation();
            previewSource.loop = loop;
            previewSource.spatialBlend = is3D ? 1f : 0f;
            previewSource.minDistance = minDistance;
            previewSource.maxDistance = maxDistance;
            previewSource.Play();

            Debug.Log($"🔊 Previewing: {name} ({category}) - Clip: {clipToPlay.name}");
        }

        /// <summary>
        /// Stop preview playback
        /// Right-click this asset → "Stop Preview"
        /// </summary>
        [ContextMenu("Stop Preview")]
        public void StopPreview()
        {
            if (previewSource != null)
            {
                if (previewSource.isPlaying)
                    previewSource.Stop();

                if (previewSource.gameObject != null)
                    DestroyImmediate(previewSource.gameObject);

                previewSource = null;
            }
        }
#endif
        #endregion

        /// <summary>
        /// ⚠️ LEGACY METHOD - Use AudioManager.PlaySoundData() instead
        /// Direct play method for quick testing or special cases
        /// </summary>
        public AudioSource Play(AudioSource sourceParam = null)
        {
            AudioClip clipToPlay = GetClip();

            // ✅ Fixed null checks
            if (clipToPlay == null)
            {
                Debug.LogWarning($"⚠️ No audio clip assigned to {name}");
                return null;
            }

            // Use provided source or create temporary one
            AudioSource source = sourceParam;
            bool createdNewSource = false;

            if (source == null)
            {
                GameObject tempObj = new GameObject($"SOUND_{name}");
                source = tempObj.AddComponent<AudioSource>();
                createdNewSource = true;
            }

            source.clip = clipToPlay;
            source.volume = volume;
            source.pitch = GetPitchWithVariation();
            source.loop = loop;
            source.spatialBlend = is3D ? 1f : 0f;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.Play();

            // Destroy temporary GameObject after clip finishes
            if (createdNewSource && !loop)
            {
                Object.Destroy(source.gameObject, clipToPlay.length / source.pitch + 0.1f);
            }

            return source;
        }
    }
}