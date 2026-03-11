using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace BOTF3D.Audio
{
    /// <summary>
    /// Centralized audio manager with pooling, crossfading, transitions, global volume, volume groups, and randomization.
    /// Persistent across scenes.Handles global or shared audio systems, audio settings, background music, sound effects, and UI sounds.
    /// !! Weapon sounds, combat, are not handled here - they should be managed by the weapon system for better performance and spatialization control.
    /// Weapons prefabs have attached AudioSources for their sounds, and the weapon system triggers those directly.
    /// This keeps the AudioManager focused on shared and global audio needs.
    /// Recommended Setup (Common Game Dev Practice)
    //Audio Type        Format Compression in Unity
    //UI                sounds WAV ADPCM
    //Short SFX         WAV ADPCM
    //Ambient loops     OGG Vorbis
    //Background music  OGG Vorbis
    //Voice lines       WAV or OGG  Vorbis
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music")]
        [SerializeField] private AudioSource musicSource1;
        [SerializeField] private AudioSource musicSource2;
        private AudioSource activeMusicSource;
        private AudioSource inactiveMusicSource;
        private bool isCrossfading;

        [Header("Sound Pool Settings")]
        [SerializeField] private int sfxPoolSize = 10;
        [SerializeField] private int uiPoolSize = 5;
        IObjectPool<SoundEmitter> soundEmitterPool;
        readonly List<SoundEmitter> activeSoundEmitters = new List<SoundEmitter>();
        [Header("Sound Library")]
        [SerializeField] private SoundData[] soundLibrary; // ✅ Changed from Sound[] to SoundData[]
        private Dictionary<string, SoundData> soundDictionary;
        public readonly Dictionary<SoundData, int> soundEmitterUsage = new Dictionary<SoundData, int>();
        [SerializeField] private SoundEmitter soundEmitterPrefab;
        [SerializeField] private bool collectionCheck = true;
        [SerializeField] private int defaultCapacity = 10;
        [SerializeField] private int maxSize = 160;
        [SerializeField] private int maxSoundInstances = 30;

        [SerializeField] private Sound[] sounds;
        private Dictionary<string, List<AudioClip>> randomSoundGroups; // For sound variations

        // Audio Source Pools
        private Queue<AudioSource> sfxPool;
        private Queue<AudioSource> uiPool;
        private List<AudioSource> allPooledSources;

        // Volume Settings (0-1 range)
        private float masterVolume = 1f;
        private float musicVolume = 1f;
        private float sfxVolume = 1f;
        private float uiVolume = 1f;

        // PlayerPrefs Keys
        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string MUSIC_VOLUME_KEY = "MusicVolume";
        private const string SFX_VOLUME_KEY = "SFXVolume";
        private const string UI_VOLUME_KEY = "UIVolume";

        // Crossfade Settings
        [Header("Crossfade Settings")]
        [SerializeField] private float crossfadeDuration = 2f;

        #region Usage Examples (for documentation)
        //// ✅ Play music (with crossfade)
        //AudioManager.Instance.PlayMusic("CombatTheme");

        //// ✅ Play SFX
        //AudioManager.Instance.PlaySFX("LaserShot");

        //// ✅ Play randomized SFX (plays LaserShot_1, LaserShot_2, etc randomly)
        //AudioManager.Instance.PlayRandomSFX("LaserShot");

        //// ✅ Play 3D positioned SFX
        //AudioManager.Instance.PlaySFX3D("Explosion", explosionPosition);

        //// ✅ Play UI sound
        //AudioManager.Instance.PlayUI("ButtonClick");

        //// ✅ Stop music with fade
        //AudioManager.Instance.StopMusic(fade: true);

        //// ✅ Set volumes (saves to PlayerPrefs)
        //AudioManager.Instance.SetMasterVolume(0.8f);
        //AudioManager.Instance.SetMusicVolume(0.5f);
        //AudioManager.Instance.SetSFXVolume(0.7f);
        //AudioManager.Instance.SetUIVolume(1f);
        #endregion audio usage examples

        void Awake()
        {
            // ✅ Singleton Pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("✅ AudioManager: Instance created and set to DontDestroyOnLoad");
            }
            else
            {
                Debug.LogWarning("⚠️ Duplicate AudioManager detected - destroying");
                Destroy(gameObject);
                return;
            }

            InitializeAudioManager();
        }

        private void InitializeAudioManager()
        {
            // ✅ Initialize dictionary with SoundData
            soundDictionary = new Dictionary<string, SoundData>();
            foreach (var soundData in soundLibrary)
            {
                if (!soundDictionary.ContainsKey(soundData.name))
                {
                    soundDictionary.Add(soundData.name, soundData);
                }
            }

            // Initialize music sources
            if (musicSource1 == null)
                musicSource1 = gameObject.AddComponent<AudioSource>();
            if (musicSource2 == null)
                musicSource2 = gameObject.AddComponent<AudioSource>();

            musicSource1.loop = true;
            musicSource2.loop = true;
            musicSource1.playOnAwake = false;
            musicSource2.playOnAwake = false;

            activeMusicSource = musicSource1;
            inactiveMusicSource = musicSource2;

            // Initialize SFX Pool
            sfxPool = new Queue<AudioSource>();
            allPooledSources = new List<AudioSource>();

            for (int i = 0; i < sfxPoolSize; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                sfxPool.Enqueue(source);
                allPooledSources.Add(source);
            }

            // Initialize UI Pool
            uiPool = new Queue<AudioSource>();

            for (int i = 0; i < uiPoolSize; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                uiPool.Enqueue(source);
                allPooledSources.Add(source);
            }

            Debug.Log($"AudioManager: Initialized with {sfxPoolSize} SFX sources and {uiPoolSize} UI sources");
        }
        SoundEmitter CreatSoundEmitter()
        {
            SoundEmitter emitter = Instantiate(soundEmitterPrefab);
            emitter.gameObject.SetActive(false);
            return emitter;
        }
        void OnTakeFromPool(SoundEmitter emitter)
        {
            emitter.gameObject.SetActive(true);
            activeSoundEmitters.Add(emitter);
        }
        void OnReturnedToPool(SoundEmitter emitter)
        {
            emitter.gameObject.SetActive(false);
            activeSoundEmitters.Remove(emitter);
        }
        void OnDestroyPoolObject(SoundEmitter emitter)
        {
            Destroy(emitter.gameObject);
        }
        private void InitializePool()
        {
            soundEmitterPool = new ObjectPool<SoundEmitter>(
                        CreatSoundEmitter,
                        OnTakeFromPool,
                        OnReturnedToPool,
                        OnDestroyPoolObject,
                        collectionCheck,
                        defaultCapacity,
                        maxSize);
        }



        void Start()
        {
            // Auto-play main theme if exists
            PlayMusic("MainTheme");
        }

        #region Volume Controls

        /// <summary>
        /// Load volume settings from PlayerPrefs
        /// </summary>
        private void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
            musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
            sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
            uiVolume = PlayerPrefs.GetFloat(UI_VOLUME_KEY, 1f);

            Debug.Log($"Loaded volumes: Master={masterVolume}, Music={musicVolume}, SFX={sfxVolume}, UI={uiVolume}");
        }

        /// <summary>
        /// Save volume settings to PlayerPrefs
        /// </summary>
        private void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
            PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolume);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
            PlayerPrefs.SetFloat(UI_VOLUME_KEY, uiVolume);
            PlayerPrefs.Save();
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
            SaveVolumeSettings();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateMusicVolume();
            SaveVolumeSettings();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            SaveVolumeSettings();
        }

        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            SaveVolumeSettings();
        }

        public float GetMasterVolume() => masterVolume;
        public float GetMusicVolume() => musicVolume;
        public float GetSFXVolume() => sfxVolume;
        public float GetUIVolume() => uiVolume;

        private void UpdateAllVolumes()
        {
            UpdateMusicVolume();
            // SFX and UI volumes are applied when played
        }

        private void UpdateMusicVolume()
        {
            float finalVolume = masterVolume * musicVolume;
            if (musicSource1 != null)
                musicSource1.volume = finalVolume;
            if (musicSource2 != null)
                musicSource2.volume = finalVolume;
        }

        #endregion

        #region Music Playback with Crossfading
        /// <summary>
        /// Play SoundData by reference (recommended)
        /// </summary>
        public void PlaySoundData(SoundData soundData, float volumeMultiplier = 1f)
        {
            if (soundData == null || soundData.clip == null) return;

            float finalVolume = masterVolume * GetCategoryVolume(soundData.category) * soundData.volume * volumeMultiplier;
            float finalPitch = soundData.pitch + Random.Range(-soundData.randomPitchVariation, soundData.randomPitchVariation);

            if (soundData.is3D)
            {
                // Use SoundEmitter pool for 3D
                PlaySoundData3D(soundData, Camera.main.transform.position);
            }
            else
            {
                // Use simple AudioSource pool for 2D
                PlaySoundData2D(soundData, finalVolume, finalPitch);
            }
        }
        /// <summary>
        /// Play 3D positional sound using SoundEmitter pool
        /// </summary>
        public void PlaySoundData3D(SoundData soundData, Vector3 position, float volumeMultiplier = 1f)
        {
            if (soundData == null || soundData.clip == null) return;

            SoundEmitter emitter = soundEmitterPool.Get();
            emitter.transform.position = position;

            float finalVolume = masterVolume * GetCategoryVolume(soundData.category) * soundData.volume * volumeMultiplier;
            float finalPitch = soundData.pitch + Random.Range(-soundData.randomPitchVariation, soundData.randomPitchVariation);

            emitter.Initialize(soundData.clip, finalVolume, finalPitch, soundData.loop, soundData.minDistance, soundData.maxDistance);
            emitter.OnFinished += () => soundEmitterPool.Release(emitter);
        }
        private void PlaySoundData2D(SoundData soundData, float volume, float pitch)
        {
            AudioSource source = GetAvailableSFXSource();
            if (source == null) return;

            source.clip = soundData.clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = 0f;
            source.Play();

            StartCoroutine(ReturnToPool(source, sfxPool, soundData.clip.length));
        }
        /// <summary>
        /// Play music by name with optional crossfade
        /// </summary>
        public void PlayMusic(string musicName, bool crossfade = true)
        {
            if (!soundDictionary.ContainsKey(musicName))
            {
                Debug.LogWarning($"Music '{musicName}' not found in soundData library!");
                return;
            }

            AudioClip clip = soundDictionary[musicName].clip;
            PlayMusicClip(clip, crossfade);
        }

        /// <summary>
        /// Play music by AudioClip with optional crossfade
        /// </summary>
        public void PlayMusicClip(AudioClip clip, bool crossfade = true)
        {
            if (clip == null)
            {
                Debug.LogWarning("PlayMusicClip: clip is null!");
                return;
            }

            // If same clip is already playing, do nothing
            if (activeMusicSource.clip == clip && activeMusicSource.isPlaying)
            {
                Debug.Log($"Music '{clip.name}' is already playing");
                return;
            }

            if (crossfade && activeMusicSource.isPlaying && !isCrossfading)
            {
                StartCoroutine(CrossfadeMusic(clip));
            }
            else
            {
                // Immediate switch
                activeMusicSource.Stop();
                activeMusicSource.clip = clip;
                activeMusicSource.volume = masterVolume * musicVolume;
                activeMusicSource.Play();
            }
        }

        private IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            isCrossfading = true;

            // Setup inactive source with new clip
            inactiveMusicSource.clip = newClip;
            inactiveMusicSource.volume = 0f;
            inactiveMusicSource.Play();

            float elapsed = 0f;
            float startVolume = activeMusicSource.volume;

            // Crossfade
            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / crossfadeDuration;

                activeMusicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                inactiveMusicSource.volume = Mathf.Lerp(0f, masterVolume * musicVolume, t);

                yield return null;
            }

            // Finish
            activeMusicSource.Stop();
            activeMusicSource.volume = masterVolume * musicVolume;

            // Swap active/inactive
            (activeMusicSource, inactiveMusicSource) = (inactiveMusicSource, activeMusicSource);

            isCrossfading = false;
        }

        public void StopMusic(bool fade = true)
        {
            if (fade)
            {
                StartCoroutine(FadeOutMusic());
            }
            else
            {
                activeMusicSource.Stop();
                inactiveMusicSource.Stop();
            }
        }

        private IEnumerator FadeOutMusic()
        {
            float startVolume = activeMusicSource.volume;
            float elapsed = 0f;

            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                activeMusicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / crossfadeDuration);
                yield return null;
            }

            activeMusicSource.Stop();
            activeMusicSource.volume = masterVolume * musicVolume;
        }

        #endregion

        #region SFX Playback (Pooled)

        // ✅ Legacy string-based lookup (keep for backwards compatibility)
        public void PlaySFX(string soundName)
        {
            if (soundDictionary.TryGetValue(soundName, out SoundData soundData))
            {
                PlaySoundData(soundData);
            }
            else
            {
                Debug.LogWarning($"Sound '{soundName}' not found!");
            }
        }

        /// <summary>
        /// Play sound effect by AudioClip
        /// </summary>
        public void PlaySFXClip(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetAvailableSFXSource();
            if (source == null)
            {
                Debug.LogWarning("No available SFX AudioSource in pool!");
                return;
            }

            source.clip = clip;
            source.volume = masterVolume * sfxVolume * volumeMultiplier;
            source.Play();

            StartCoroutine(ReturnToPool(source, sfxPool, clip.length));
        }

        /// <summary>
        /// Play randomized SFX from a group (e.g., "Explosion" plays random Explosion_1, Explosion_2, etc.)
        /// </summary>
        public void PlayRandomSFX(string groupName, float volumeMultiplier = 1f)
        {
            if (!randomSoundGroups.ContainsKey(groupName))
            {
                // Fallback: try exact name
                PlaySFX(groupName);
                return;
            }

            List<AudioClip> clips = randomSoundGroups[groupName];
            if (clips.Count == 0) return;

            AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Count)];
            PlaySFXClip(randomClip, volumeMultiplier);
        }

        /// <summary>
        /// Play 3D positional sound effect
        /// </summary>
        public void PlaySFX3D(string sfxName, Vector3 position, float volumeMultiplier = 1f)
        {
            if (!soundDictionary.ContainsKey(sfxName))
            {
                Debug.LogWarning($"SFX '{sfxName}' not found!");
                return;
            }

            AudioClip clip = soundDictionary[sfxName].clip;
            PlaySFX3DClip(clip, position, volumeMultiplier);
        }

        public void PlaySFX3DClip(AudioClip clip, Vector3 position, float volumeMultiplier = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetAvailableSFXSource();
            if (source == null)
            {
                Debug.LogWarning("No available SFX AudioSource in pool!");
                return;
            }

            source.clip = clip;
            source.volume = masterVolume * sfxVolume * volumeMultiplier;
            source.spatialBlend = 1f; // Full 3D
            source.transform.position = position;
            source.Play();

            StartCoroutine(ReturnToPool(source, sfxPool, clip.length));
        }

        #endregion

        #region UI Sound Playback (Pooled)

        /// <summary>
        /// Play UI sound by name
        /// </summary>
        public void PlayUI(string uiSoundName)
        {
            if (!soundDictionary.ContainsKey(uiSoundName))
            {
                Debug.LogWarning($"UI Sound '{uiSoundName}' not found!");
                return;
            }

            AudioClip clip = soundDictionary[uiSoundName].clip;
            PlayUIClip(clip);
        }

        /// <summary>
        /// Play UI sound by AudioClip
        /// </summary>
        public void PlayUIClip(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetAvailableUISource();
            if (source == null)
            {
                Debug.LogWarning("No available UI AudioSource in pool!");
                return;
            }

            source.clip = clip;
            source.volume = masterVolume * uiVolume * volumeMultiplier;
            source.spatialBlend = 0f; // 2D sound
            source.Play();

            StartCoroutine(ReturnToPool(source, uiPool, clip.length));
        }

        #endregion

        #region Audio Source Pooling
        private float GetCategoryVolume(AudioCategory category)
        {
            return category switch
            {
                AudioCategory.Music => musicVolume,
                AudioCategory.SFX => sfxVolume,
                AudioCategory.UI => uiVolume,
                AudioCategory.Weapon => sfxVolume, // Or add weaponVolume if needed
                AudioCategory.Ambient => musicVolume,
                AudioCategory.Voice => sfxVolume,
                _ => 1f
            };
        }
        private AudioSource GetAvailableSFXSource()
        {
            // Try to get from pool
            if (sfxPool.Count > 0)
            {
                return sfxPool.Dequeue();
            }

            // Find any inactive source
            AudioSource inactive = allPooledSources.FirstOrDefault(s => !s.isPlaying && !uiPool.Contains(s));
            if (inactive != null)
            {
                return inactive;
            }

            // Expand pool if needed
            Debug.Log("Expanding SFX pool...");
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newSource.loop = false;
            allPooledSources.Add(newSource);
            return newSource;
        }

        private AudioSource GetAvailableUISource()
        {
            // Try to get from pool
            if (uiPool.Count > 0)
            {
                return uiPool.Dequeue();
            }

            // Find any inactive source
            AudioSource inactive = allPooledSources.FirstOrDefault(s => !s.isPlaying && !sfxPool.Contains(s));
            if (inactive != null)
            {
                return inactive;
            }

            // Expand pool
            Debug.Log("Expanding UI pool...");
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newSource.loop = false;
            allPooledSources.Add(newSource);
            return newSource;
        }

        private IEnumerator ReturnToPool(AudioSource source, Queue<AudioSource> pool, float delay)
        {
            yield return new WaitForSeconds(delay + 0.1f); // Small buffer

            if (source != null && !source.isPlaying)
            {
                source.spatialBlend = 0f; // Reset to 2D
                pool.Enqueue(source);
            }
        }

        #endregion

        #region Legacy Compatibility

        /// <summary>
        /// Legacy Play method for backward compatibility
        /// </summary>
        public void Play(string name)
        {
            if (soundDictionary.ContainsKey(name))
            {
                SoundData soundData = soundDictionary[name];
                if (soundData.is3D)
                {
                    PlayMusic(name, crossfade: false);
                }
                else
                {
                    PlaySFX(name);
                }
            }
            else
            {
                Debug.LogWarning($"Sound '{name}' not found!");
            }
        }

        #endregion
    }

    /// <summary>
    /// Sound definition for inspector
    /// </summary>
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop;
        public bool isMusic; // ✅ NEW: Flag to identify music vs SFX

        [HideInInspector] public AudioSource source; // Legacy compatibility
    }
}

