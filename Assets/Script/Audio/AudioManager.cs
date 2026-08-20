// Ignore Spelling: BOTF sfx

using BOTF3D.Core;
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
    public class AudioManager : MonoBehaviour, IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
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
        //public readonly Dictionary<SoundData, int> soundEmitterUsage = new Dictionary<SoundData, int>();
        [SerializeField] private SoundEmitter soundEmitterPrefab;
        [SerializeField] private bool collectionCheck = true;
        [SerializeField] private int defaultCapacity = 10;
        [SerializeField] private int maxSize = 160;

        // Audio Source Pools Simple 2D audio on AudioManager itself, UI clicks, background music, non-positional SFX
        private Queue<AudioSource> sfxPool;
        private Queue<AudioSource> uiPool;
        private List<AudioSource> allPooledSources;

        // Volume Settings (0-1 range)
        private float masterVolume = 0.6f;
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
            ServiceLocator.Register<AudioManager>(this);
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
        void Start()
        {
            // ✅ Volume already loaded in InitializeAudioManager()
            Debug.Log($"AudioManager: Ready - Volumes: Master={masterVolume}, Music={musicVolume}, SFX={sfxVolume}, UI={uiVolume}");
        }
        private void InitializeAudioManager()
        {
            // ✅ Load saved volume settings FIRST
            LoadVolumeSettings();

            // Only add a listener if none exists anywhere in the scene - avoids duplicate
            // AudioListener warnings when a scene camera already has one.
            if (FindAnyObjectByType<AudioListener>(FindObjectsInactive.Include) == null)
            {
                gameObject.AddComponent<AudioListener>();
                Debug.Log("✅ Added AudioListener to AudioManager");
            }

            // ✅ CRITICAL: Initialize music sources BEFORE anything else
            if (musicSource1 == null)
            {
                musicSource1 = gameObject.AddComponent<AudioSource>();
                Debug.Log("✅ Created musicSource1");
            }
            if (musicSource2 == null)
            {
                musicSource2 = gameObject.AddComponent<AudioSource>();
                Debug.Log("✅ Created musicSource2");
            }

            musicSource1.loop = true;
            musicSource2.loop = true;
            musicSource1.playOnAwake = false;
            musicSource2.playOnAwake = false;

            // ✅ Set initial volume from loaded settings
            musicSource1.volume = masterVolume * musicVolume;
            musicSource2.volume = masterVolume * musicVolume;

            // ✅ CRITICAL: Set active/inactive AFTER sources are created
            activeMusicSource = musicSource1;
            inactiveMusicSource = musicSource2;

            Debug.Log("✅ Music sources initialized");

            // ✅ SAFETY: Disable playOnAwake on ALL existing AudioSources (prevents accidents)
            AudioSource[] existingSources = GetComponents<AudioSource>();
            foreach (var source in existingSources)
            {
                if (source.playOnAwake)
                {
                    Debug.LogWarning($"⚠️ AudioManager: Found AudioSource with playOnAwake=true (clip: {source.clip?.name}) - disabling!");
                    source.playOnAwake = false;

                    // Also stop if it's already playing
                    if (source.isPlaying)
                    {
                        source.Stop();
                    }
                }
            }

            // ✅ Initialize dictionary with SoundData (skip null entries)
            soundDictionary = new Dictionary<string, SoundData>();
            foreach (var soundData in soundLibrary)
            {
                // ✅ CRITICAL: Skip null entries (empty Inspector slots)
                if (soundData == null)
                {
                    Debug.LogWarning("⚠️ AudioManager: soundLibrary contains null entry - skipping");
                    continue;
                }

                if (!soundDictionary.ContainsKey(soundData.name))
                {
                    soundDictionary.Add(soundData.name, soundData);
                    Debug.Log($"✅ Registered sound: {soundData.name} ({soundData.category})");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Duplicate sound name in library: {soundData.name}");
                }
            }

            Debug.Log($"AudioManager: Loaded {soundDictionary.Count} sounds from library");

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

            // ✅ Initialize SoundEmitter pool
            InitializePool();

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

        #region Volume Controls

        /// <summary>
        /// Load volume settings from PlayerPrefs
        /// </summary>
        private void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 0.6f);
            musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
            sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
            uiVolume = PlayerPrefs.GetFloat(UI_VOLUME_KEY, 1f);

            // Safety check: if master is 0 but was never set, default to 0.6 (60%)
            if (!PlayerPrefs.HasKey(MASTER_VOLUME_KEY) || masterVolume < 0.001f)
            {
                if (!PlayerPrefs.HasKey(MASTER_VOLUME_KEY))
                {
                    masterVolume = 0.6f;
                    Debug.Log("AudioManager: No master volume found in PlayerPrefs, defaulting to 0.6f");
                }
            }

            Debug.Log($"📊 Loaded volumes: Master={masterVolume:F3}, Music={musicVolume:F3}, SFX={sfxVolume:F3}, UI={uiVolume:F3}");
            // ✅ Apply music volume immediately if sources exist
            UpdateMusicVolume();
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
            {
                musicSource1.volume = finalVolume;
            }

            if (musicSource2 != null)
            {
                musicSource2.volume = finalVolume;
            }

            Debug.Log($"🔊 Updated music volume to: {finalVolume} (master={masterVolume} × music={musicVolume})");
        }

        #endregion

        #region Music Playback with Crossfading
        /// <summary>
        /// Play SoundData by reference (recommended)
        /// </summary>
        // Returns the pooled AudioSource actually used for 2D playback (null for 3D, which is
        // played through the separate SoundEmitter pool instead) so callers that need to cut a
        // one-shot short - e.g. MainMenuUIController stopping the previous civ selection sting
        // when the player picks a new civ or saves before it finishes - have a handle to Stop().
        public AudioSource PlaySoundData(SoundData soundData, float volumeMultiplier = 1f)
        {
            if (soundData == null || soundData.GetClip() == null) return null;

            float finalVolume = masterVolume * GetCategoryVolume(soundData.category) * soundData.volume * volumeMultiplier;
            float finalPitch = soundData.pitch + Random.Range(-soundData.randomPitchVariation, soundData.randomPitchVariation);

            if (soundData.is3D)
            {
                // Use SoundEmitter pool for 3D
                PlaySoundData3D(soundData, Camera.main.transform.position);
                return null;
            }
            else
            {
                // Use simple AudioSource pool for 2D
                return PlaySoundData2D(soundData, finalVolume, finalPitch);
            }
        }
        /// <summary>
        /// Play 3D positional sound using SoundEmitter pool
        /// </summary>
        public void PlaySoundData3D(SoundData soundData, Vector3 position, float volumeMultiplier = 1f)
        {
            if (soundData == null || soundData.GetClip() == null) return;

            SoundEmitter emitter = soundEmitterPool.Get();
            emitter.transform.position = position;

            float finalVolume = masterVolume * GetCategoryVolume(soundData.category) * soundData.volume * volumeMultiplier;
            float finalPitch = soundData.pitch + Random.Range(-soundData.randomPitchVariation, soundData.randomPitchVariation);

            emitter.Initialize(soundData.GetClip(), finalVolume, finalPitch, soundData.loop, soundData.minDistance, soundData.maxDistance);
            emitter.OnFinished += () => soundEmitterPool.Release(emitter);
        }
        private AudioSource PlaySoundData2D(SoundData soundData, float volume, float pitch)
        {
            AudioSource source = GetAvailableSFXSource();
            if (source == null) return null;

            AudioClip clip = soundData.GetClip();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = 0f;
            source.Play();

            StartCoroutine(ReturnToPool(source, sfxPool, clip.length));
            return source;
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

            SoundData soundData = soundDictionary[musicName];
            AudioClip clip = soundData.clip;

            // ✅ Calculate final volume INCLUDING SoundData volume
            float finalVolume = masterVolume * musicVolume * soundData.volume;

            Debug.Log($"🎵 PlayMusic: master={masterVolume} × music={musicVolume} × SoundData={soundData.volume} = {finalVolume}");
            Debug.Log($"🎵 AudioClip assigned: {(clip != null ? clip.name : "NULL")}");

            PlayMusicClip(clip, finalVolume, crossfade);
        }
        /// <summary>
        /// Play music by AudioClip with specified volume and optional crossfade
        /// </summary>
        public void PlayMusicClip(AudioClip clip, float volume, bool crossfade = true)
        {
            if (clip == null)
            {
                Debug.LogError("❌ PlayMusicClip: clip is NULL!");
                return;
            }

            Debug.Log($"🎵 PlayMusicClip: Playing '{clip.name}' at volume {volume} (crossfade={crossfade})");

            // If same clip is already playing, do nothing
            if (activeMusicSource.clip == clip && activeMusicSource.isPlaying)
            {
                Debug.Log($"🎵 Music '{clip.name}' is already playing");
                return;
            }

            if (crossfade && activeMusicSource.isPlaying && !isCrossfading)
            {
                Debug.Log($"🎵 Crossfading from '{activeMusicSource.clip?.name}' to '{clip.name}'");
                StartCoroutine(CrossfadeMusic(clip, volume));
            }
            else
            {
                // Immediate switch
                activeMusicSource.Stop();
                activeMusicSource.clip = clip;
                activeMusicSource.volume = volume;
                activeMusicSource.Play();

                Debug.Log($"🎵 Started music: {clip.name} (isPlaying={activeMusicSource.isPlaying})");
            }
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
        private IEnumerator CrossfadeMusic(AudioClip newClip, float targetVolume)
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
                inactiveMusicSource.volume = Mathf.Lerp(0f, targetVolume, t);

                yield return null;
            }

            // Finish
            activeMusicSource.Stop();
            activeMusicSource.volume = targetVolume;

            // Swap active/inactive
            (activeMusicSource, inactiveMusicSource) = (inactiveMusicSource, activeMusicSource);

            isCrossfading = false;
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
            // ✅ Safety: Check if music sources exist
            if (activeMusicSource == null || inactiveMusicSource == null)
            {
                Debug.LogWarning("StopMusic: Music sources are NULL - cannot stop music");
                return;
            }

            if (fade)
            {
                // ✅ Only fade if music is actually playing
                if (activeMusicSource.isPlaying)
                {
                    StartCoroutine(FadeOutMusic());
                }
                else
                {
                    Debug.Log("StopMusic: No music playing - skipping fade");
                }
            }
            else
            {
                activeMusicSource.Stop();
                inactiveMusicSource.Stop();
            }
        }

        private IEnumerator FadeOutMusic()
        {
            // ✅ CRITICAL: Check if music source exists
            if (activeMusicSource == null)
            {
                Debug.LogWarning("FadeOutMusic: activeMusicSource is NULL - nothing to fade out");
                yield break;
            }

            // ✅ Check if music is actually playing
            if (!activeMusicSource.isPlaying)
            {
                Debug.Log("FadeOutMusic: No music playing - skipping fade");
                yield break;
            }

            float startVolume = activeMusicSource.volume;
            float elapsed = 0f;

            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;

                // ✅ Safety check in case source gets destroyed mid-fade
                if (activeMusicSource != null)
                {
                    activeMusicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / crossfadeDuration);
                }
                else
                {
                    Debug.LogWarning("FadeOutMusic: activeMusicSource became NULL during fade!");
                    yield break;
                }

                yield return null;
            }

            // ✅ Final null check before stopping
            if (activeMusicSource != null)
            {
                activeMusicSource.Stop();
                activeMusicSource.volume = masterVolume * musicVolume;
                Debug.Log("🎵 FadeOutMusic: Complete");
            }
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
        /// Play randomized SFX - uses randomClips[] array from SoundData if available
        /// </summary>
        public void PlayRandomSFX(string soundName, float volumeMultiplier = 1f)
        {
            if (!soundDictionary.TryGetValue(soundName, out SoundData soundData))
            {
                Debug.LogWarning($"Sound '{soundName}' not found!");
                return;
            }

            // GetClip() automatically handles random selection from randomClips[]
            AudioClip clipToPlay = soundData.GetClip();
            if (clipToPlay == null)
            {
                Debug.LogWarning($"Sound '{soundName}' has no clips assigned!");
                return;
            }

            float finalVolume = masterVolume * GetCategoryVolume(soundData.category) * soundData.volume * volumeMultiplier;
            float finalPitch = soundData.GetPitchWithVariation();

            if (soundData.is3D)
            {
                // Use 3D positioned sound at camera location
                PlaySoundData3D(soundData, Camera.main.transform.position, volumeMultiplier);
            }
            else
            {
                // Use 2D sound
                AudioSource source = GetAvailableSFXSource();
                if (source == null)
                {
                    Debug.LogWarning("No available SFX AudioSource in pool!");
                    return;
                }

                source.clip = clipToPlay;
                source.volume = finalVolume;
                source.pitch = finalPitch;
                source.spatialBlend = 0f;
                source.Play();

                StartCoroutine(ReturnToPool(source, sfxPool, clipToPlay.length / finalPitch));
            }
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
            if (clip == null)
            {
                Debug.LogWarning("⚠️ PlaySFX3DClip: clip is NULL!");
                return;
            }

            AudioSource source = GetAvailableSFXSource();
            if (source == null)
            {
                Debug.LogWarning("❌ No available SFX AudioSource in pool!");
                return;
            }

            // ✅ Calculate final volume
            float finalVolume = masterVolume * sfxVolume * volumeMultiplier;

            // ✅ Configure 3D audio settings
            source.clip = clip;
            source.volume = finalVolume;
            source.spatialBlend = 1f; // Full 3D
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 10f;    // Sound is full volume within 10 units
            source.maxDistance = 500f;   // Sound fades to 0 at 500 units
            source.dopplerLevel = 0f;    // Disable doppler for combat sounds
            source.transform.position = position;

            // ✅ Find AudioListener
            AudioListener listener = FindAnyObjectByType<AudioListener>(FindObjectsInactive.Include);

            // ✅ Detailed logging
            Debug.Log($"🔊 PlaySFX3DClip: '{clip.name}' at {position}");
            Debug.Log($"   Volume: {finalVolume:F3} (master={masterVolume:F3} × sfx={sfxVolume:F3} × mult={volumeMultiplier:F3})");
            Debug.Log($"   3D Settings: spatialBlend={source.spatialBlend}, minDist={source.minDistance}, maxDist={source.maxDistance}");
            Debug.Log($"   AudioListener position: {(listener != null ? listener.transform.position.ToString() : "NONE FOUND")}");

            if (finalVolume <= 0.001f)
            {
                Debug.Log($"❌ VOLUME VERY LOW! Final volume is {finalVolume}");
            }

            if (listener == null)
            {
                Debug.LogError("❌ NO AUDIO LISTENER IN SCENE!");
            }

            source.Play();

            if (!source.isPlaying)
            {
                Debug.LogError($"❌ AudioSource.Play() called but isPlaying=false! Check clip: {clip.name}");
            }

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
        /// <summary>
        /// Get volume for a specific audio category
        /// </summary>
        public float GetCategoryVolume(AudioCategory category)
        {
            switch (category)
            {
                case AudioCategory.Music:
                    return musicVolume;
                case AudioCategory.SFX:
                case AudioCategory.Weapon:
                case AudioCategory.Ambient:
                    return sfxVolume;
                case AudioCategory.UI:
                case AudioCategory.Voice:
                    return uiVolume;
                default:
                    return 1f;
            }
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
        public void PlaySound(SoundData soundData, Vector3 position)
        {
            AudioClip clipToPlay = soundData.GetClip();
            float finalPitch = soundData.GetPitchWithVariation();
            float finalVolume = masterVolume * GetCategoryVolume(soundData.category) * soundData.volume;

            SoundEmitter emitter = soundEmitterPool.Get(); // ✅ Fixed
            emitter.transform.position = position;
            emitter.Initialize(clipToPlay, finalVolume, finalPitch, soundData.loop,
                              soundData.minDistance, soundData.maxDistance);

            // ✅ Return to pool when finished
            emitter.OnFinished += () => soundEmitterPool.Release(emitter);
        }
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
#if UNITY_EDITOR
        private void OnValidate()
        {
            // ✅ Warn about null entries in Inspector
            if (soundLibrary != null)
            {
                for (int i = 0; i < soundLibrary.Length; i++)
                {
                    if (soundLibrary[i] == null)
                    {
                        Debug.LogWarning($"⚠️ AudioManager: soundLibrary[{i}] is NULL - assign a SoundData asset or reduce array size");
                    }
                }
            }
        }
#endif
    

        private void OnDestroy()
        {
            ServiceLocator.Unregister<AudioManager>(); }
    }
}