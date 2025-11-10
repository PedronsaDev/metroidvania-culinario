using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance
    {
        get
        {
            if (!_instance) Bootstrap();
            return _instance;
        }
    }
    private static AudioManager _instance;

    [Range(0f, 1f)] [SerializeField] private float _masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float _musicVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float _uiVolume = 1f;

    [SerializeField] private bool _muteAll = false;

    [Header("Optional Mixer Routing")]
    [SerializeField] private AudioMixerGroup _musicGroup;
    [SerializeField] private AudioMixerGroup _sfxGroup;
    [SerializeField] private AudioMixerGroup _uiGroup;

    [Header("SFX Pool Settings")]
    [SerializeField] private int _initialSfxPool = 16;
    [SerializeField] private int _maxSfxPool = 64;
    [Tooltip("Minimum seconds between the same clip plays to avoid spam.")]
    [SerializeField] private float _sameClipThrottleSeconds = 0.03f;

    private AudioSource _musicA;
    private AudioSource _musicB;
    private bool _musicAToggle = true;
    private Coroutine _musicFadeRoutine;

    private Transform sfxRoot;
    private readonly Queue<AudioSource> sfxAvailable = new Queue<AudioSource>();
    private readonly HashSet<AudioSource> sfxInUse = new HashSet<AudioSource>();
    private readonly HashSet<AudioSource> sfxInUseManual = new HashSet<AudioSource>();
    private readonly HashSet<AudioSource> sfxUIInUse = new HashSet<AudioSource>();
    private int totalSFXSources = 0;

    private float _lastAppliedMaster = 1f;
    private float _lastAppliedMusic = 1f;
    private float _lastAppliedSfx = 1f;
    private float _lastAppliedUi = 1f;

    private readonly Dictionary<AudioClip, float> _lastPlayTime = new Dictionary<AudioClip, float>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance) return;
        var go = new GameObject("[AUDIO MANAGER]");
        _instance = go.AddComponent<AudioManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        _musicA = CreateChildSource("Music A", _musicGroup);
        _musicB = CreateChildSource("Music B", _musicGroup);
        _musicA.loop = true;
        _musicB.loop = true;
        _musicA.playOnAwake = false;
        _musicB.playOnAwake = false;
        _musicA.spatialBlend = 0f;
        _musicB.spatialBlend = 0f;

        var sfxRootGo = new GameObject("SFX Pool");
        sfxRootGo.transform.SetParent(transform);
        sfxRoot = sfxRootGo.transform;

        for (int i = 0; i < Mathf.Max(1, _initialSfxPool); i++)
            sfxAvailable.Enqueue(CreatePooledSFXSource());

        _lastAppliedMaster = EffectiveMaster();
        _lastAppliedMusic = _musicVolume;
        _lastAppliedSfx = _sfxVolume;
        _lastAppliedUi = _uiVolume;

        ApplyVolumeToAllActive();
    }

    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = Mathf.Clamp01(value);
            ApplyVolumeToAllActive();
        }
    }
    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = Mathf.Clamp01(value);
            ApplyVolumeToAllActive();
        }
    }
    public float SFXVolume
    {
        get => _sfxVolume;
        set
        {
            _sfxVolume = Mathf.Clamp01(value);
            ApplyVolumeToAllActive();
        }
    }
    public float UIVolume
    {
        get => _uiVolume;
        set
        {
            _uiVolume = Mathf.Clamp01(value);
            ApplyVolumeToAllActive();
        }
    }
    public bool MuteAll
    {
        get => _muteAll;
        set
        {
            _muteAll = value;
            ApplyVolumeToAllActive();
        }
    }

    public void PlayMusic(AudioClip clip, float fadeDuration = 1f, float targetVolume = 1f, bool loop = true)
    {
        if (!clip) return;
        var pair = GetMusicSwapSources();
        var from = pair.from;
        var to = pair.to;
        to.clip = clip;
        to.loop = loop;
        to.volume = 0f;
        to.Play();

        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(CrossfadeMusic(from, to, fadeDuration, Mathf.Clamp01(targetVolume)));
    }

    public void StopMusic(float fadeOutDuration = 0.5f)
    {
        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(FadeOutBothMusic(fadeOutDuration));
    }

    public void PauseMusic()
    {
        _musicA.Pause();
        _musicB.Pause();
    }

    public void ResumeMusic()
    {
        if (_musicA.clip) _musicA.UnPause();
        if (_musicB.clip) _musicB.UnPause();
    }

    public AudioSource PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f, float pitchVariance = 0f, float volumeVariance = 0f)
    {
        if (!CanPlayClip(clip)) return null;
        var src = GetSFXSource();
        Configure2D(src);
        ConfigureAndPlay(src, clip, volume, pitch, pitchVariance, volumeVariance, false);
        return src;
    }

    public AudioSource PlayUISFX(AudioClip clip, float volume = 1f, float pitch = 1f, float pitchVariance = 0f, float volumeVariance = 0f)
    {
        if (!CanPlayClip(clip)) return null;
        var src = GetSFXSource();
        Configure2D(src);
        if (_uiGroup) src.outputAudioMixerGroup = _uiGroup;
        ConfigureAndPlay(src, clip, volume, pitch, pitchVariance, volumeVariance, false, isUI: true);
        return src;
    }

    public AudioSource PlaySFX3D(AudioClip clip, Vector3 position, float volume = 1f, float spatialBlend = 1f, float minDistance = 1f, float maxDistance = 20f, float pitch = 1f, float pitchVariance = 0f, float volumeVariance = 0f)
    {
        if (!CanPlayClip(clip)) return null;
        var src = GetSFXSource();
        Configure3D(src, position, spatialBlend, minDistance, maxDistance);
        ConfigureAndPlay(src, clip, volume, pitch, pitchVariance, volumeVariance, false);
        return src;
    }

    public AudioSource PlayLoopSFX3D(AudioClip clip, Vector3 position, float volume = 1f, float spatialBlend = 1f, float minDistance = 1f, float maxDistance = 20f, float pitch = 1f)
    {
        if (!clip) return null;
        var src = GetSFXSource(manual: true);
        Configure3D(src, position, spatialBlend, minDistance, maxDistance);
        src.clip = clip;
        src.loop = true;
        src.pitch = pitch;
        float basePreMaster = Mathf.Clamp01(volume)*_sfxVolume;
        src.volume = basePreMaster*EffectiveMaster();
        sfxUIInUse.Remove(src);
        src.Play();
        return src;
    }

    public AudioSource PlayLoopSFX(AudioClip clip, float volume = 1f, float pitch = 1f, bool isUI = false)
    {
        if (!clip) return null;
        var src = GetSFXSource(manual: true);
        Configure2D(src);
        if (isUI)
            sfxUIInUse.Add(src);
        else
            sfxUIInUse.Remove(src);
        src.clip = clip;
        src.loop = true;
        src.pitch = pitch;
        float categoryScale = isUI ? _uiVolume : _sfxVolume;
        float basePreMaster = Mathf.Clamp01(volume)*categoryScale;
        src.volume = basePreMaster*EffectiveMaster();
        src.Play();
        return src;
    }

    public void StopLoopSFX(AudioSource loopingSource)
    {
        if (!loopingSource) return;
        loopingSource.Stop();
        ReturnSFXToPool(loopingSource);
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector2 position)
    {
        if (!clip) return;
        PlaySFX3D(clip, new Vector3(position.x, position.y, 0f));
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position)
    {
        if (!clip) return;
        PlaySFX3D(clip, position);
    }

    public AudioSource PlaySFX(string key)
    {
        var db = SFXDatabase.Instance;
        if (!db)
        {
            Debug.LogWarning($"SFXDatabase not found when trying to play key '{key}'");
            return null;
        }
        return db.Play(key);
    }

    public AudioSource PlaySFX(string key, Vector3 position)
    {
        var db = SFXDatabase.Instance;
        if (!db)
        {
            Debug.LogWarning($"SFXDatabase not found when trying to play key '{key}'");
            return null;
        }
        return db.Play(key, position);
    }

    public AudioSource PlayLoopSFX(string key, Vector3? position = null)
    {
        var db = SFXDatabase.Instance;
        if (!db)
        {
            Debug.LogWarning($"SFXDatabase not found when trying to loop key '{key}'");
            return null;
        }
        return db.Play(key, position, loopOverride: true);
    }

    private enum VolumeCategory { Music, SFX, UI }

    private (AudioSource from, AudioSource to) GetMusicSwapSources()
    {
        var currentIsA = _musicAToggle;
        var from = currentIsA ? _musicA : _musicB;
        var to = currentIsA ? _musicB : _musicA;
        _musicAToggle = !_musicAToggle;
        return (from, to);
    }

    private IEnumerator CrossfadeMusic(AudioSource from, AudioSource to, float duration, float targetVolume)
    {
        float t = 0f;
        float fromStart = from ? from.volume : 0f;
        float toStart = to.volume;
        ApplyMusicVolume(from);
        ApplyMusicVolume(to);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = duration > 0f ? Mathf.Clamp01(t/duration) : 1f;
            if (from) from.volume = Mathf.Lerp(fromStart, 0f, k)*EffectiveMaster();
            if (to) to.volume = Mathf.Lerp(toStart, targetVolume*_musicVolume, k)*EffectiveMaster();
            yield return null;
        }

        if (from)
        {
            from.Stop();
            from.clip = null;
            from.volume = 0f;
        }
        if (to)
        {
            to.volume = targetVolume*_musicVolume*EffectiveMaster();
        }
    }

    private IEnumerator FadeOutBothMusic(float duration)
    {
        float t = 0f;
        float a0 = _musicA.volume;
        float b0 = _musicB.volume;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = duration > 0f ? Mathf.Clamp01(t/duration) : 1f;
            _musicA.volume = Mathf.Lerp(a0, 0f, k);
            _musicB.volume = Mathf.Lerp(b0, 0f, k);
            yield return null;
        }
        _musicA.Stop();
        _musicA.clip = null;
        _musicA.volume = 0f;
        _musicB.Stop();
        _musicB.clip = null;
        _musicB.volume = 0f;
    }

    private void Configure2D(AudioSource src)
    {
        src.spatialBlend = 0f;
        src.transform.position = transform.position;
        if (_sfxGroup) src.outputAudioMixerGroup = _sfxGroup;
    }

    private void Configure3D(AudioSource src, Vector3 position, float spatialBlend, float minDistance, float maxDistance)
    {
        src.transform.position = position;
        src.spatialBlend = Mathf.Clamp01(spatialBlend);
        src.minDistance = Mathf.Max(0.01f, minDistance);
        src.maxDistance = Mathf.Max(src.minDistance + 0.01f, maxDistance);
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        if (_sfxGroup) src.outputAudioMixerGroup = _sfxGroup;
    }

    private void ConfigureAndPlay(AudioSource src, AudioClip clip, float volume, float pitch, float pitchVariance, float volumeVariance, bool loop, bool isUI = false)
    {
        src.clip = clip;
        src.loop = loop;

        float p = pitch + Random.Range(-pitchVariance, pitchVariance);
        float v = volume + Random.Range(-volumeVariance, volumeVariance);
        src.pitch = Mathf.Clamp(p, -3f, 3f);

        if (isUI)
            sfxUIInUse.Add(src);
        else
            sfxUIInUse.Remove(src);
        float categoryScale = isUI ? _uiVolume : _sfxVolume;
        float basePreMaster = Mathf.Clamp01(v)*categoryScale;
        src.volume = basePreMaster*EffectiveMaster();
        src.Play();
        if (!loop)
            StartCoroutine(ReturnToPoolWhenDone(src));
    }

    private float FinalVolume(float local, VolumeCategory category)
    {
        float cat = 1f;
        switch (category)
        {
            case VolumeCategory.Music: cat = _musicVolume; break;
            case VolumeCategory.SFX: cat = _sfxVolume; break;
            case VolumeCategory.UI: cat = _uiVolume; break;
        }
        return Mathf.Clamp01(local)*cat*EffectiveMaster();
    }

    private float EffectiveMaster() => _muteAll ? 0f : _masterVolume;

    private AudioSource CreateChildSource(string childName, AudioMixerGroup group)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        if (group) src.outputAudioMixerGroup = group;
        return src;
    }

    private AudioSource CreatePooledSFXSource()
    {
        var go = new GameObject($"SFX Source #{totalSFXSources}");
        go.transform.SetParent(sfxRoot);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;
        if (_sfxGroup) src.outputAudioMixerGroup = _sfxGroup;
        totalSFXSources++;
        return src;
    }

    private AudioSource GetSFXSource(bool manual = false)
    {
        AudioSource src = null;
        if (sfxAvailable.Count > 0)
        {
            src = sfxAvailable.Dequeue();
        }
        else if (totalSFXSources < _maxSfxPool)
        {
            src = CreatePooledSFXSource();
        }
        else
        {
            foreach (var s in sfxInUse)
            {
                src = s;
                break;
            }
            if (src)
            {
                src.Stop();
                ReturnSFXToPool(src);
                src = sfxAvailable.Count > 0 ? sfxAvailable.Dequeue() : CreatePooledSFXSource();
            }
            else
            {
                src = CreatePooledSFXSource();
            }
        }

        if (manual)
            sfxInUseManual.Add(src);
        else
            sfxInUse.Add(src);

        return src;
    }

    private IEnumerator ReturnToPoolWhenDone(AudioSource src)
    {
        while (src && src.isPlaying)
            yield return null;
        if (src)
            ReturnSFXToPool(src);
    }

    private void ReturnSFXToPool(AudioSource src)
    {
        if (!src) return;
        src.clip = null;
        src.loop = false;
        src.volume = 0f;
        src.pitch = 1f;
        src.transform.SetParent(sfxRoot, false);
        sfxInUse.Remove(src);
        sfxInUseManual.Remove(src);
        sfxUIInUse.Remove(src);
        sfxAvailable.Enqueue(src);
    }

    private bool CanPlayClip(AudioClip clip)
    {
        if (!clip) return false;
        if (_sameClipThrottleSeconds <= 0f) return true;
        float now = Time.unscaledTime;
        if (_lastPlayTime.TryGetValue(clip, out var last))
        {
            if (now - last < _sameClipThrottleSeconds) return false;
        }
        _lastPlayTime[clip] = now;
        return true;
    }

    private void ApplyVolumeToAllActive()
    {
        float newMaster = EffectiveMaster();
        float masterRatio = _lastAppliedMaster > 0f ? newMaster/_lastAppliedMaster : newMaster;
        float musicRatio = _lastAppliedMusic > 0f ? (_musicVolume/_lastAppliedMusic) : _musicVolume;
        float sfxRatio = _lastAppliedSfx > 0f ? (_sfxVolume/_lastAppliedSfx) : _sfxVolume;
        float uiRatio = _lastAppliedUi > 0f ? (_uiVolume/_lastAppliedUi) : _uiVolume;

        if (_musicA && _musicA.isPlaying && _musicFadeRoutine == null)
            _musicA.volume *= masterRatio*musicRatio;
        if (_musicB && _musicB.isPlaying && _musicFadeRoutine == null)
            _musicB.volume *= masterRatio*musicRatio;

        foreach (var s in sfxInUse)
        {
            if (!s) continue;
            bool isUi = sfxUIInUse.Contains(s);
            float ratio = masterRatio*(isUi ? uiRatio : sfxRatio);
            s.volume *= ratio;
        }
        foreach (var s in sfxInUseManual)
        {
            if (!s) continue;
            s.volume *= masterRatio*sfxRatio;
        }

        _lastAppliedMaster = newMaster;
        _lastAppliedMusic = _musicVolume;
        _lastAppliedSfx = _sfxVolume;
        _lastAppliedUi = _uiVolume;
    }

    private void ApplyMusicVolume(AudioSource src)
    {
        if (!src) return;
        if (!src.isPlaying)
        {
            src.volume = 0f;
            return;
        }
        if (_musicFadeRoutine == null)
        {
            float newMaster = EffectiveMaster();
            float masterRatio = _lastAppliedMaster > 0f ? newMaster/_lastAppliedMaster : newMaster;
            float musicRatio = _lastAppliedMusic > 0f ? (_musicVolume/_lastAppliedMusic) : _musicVolume;
            src.volume *= masterRatio*musicRatio;
        }
    }
}
