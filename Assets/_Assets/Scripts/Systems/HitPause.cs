using System.Collections;
using UnityEngine;
using DG.Tweening;

public sealed class HitPause : MonoBehaviour
{
    public static HitPause Instance { get; private set; }

    [Header("Pause")]
    [SerializeField] private float _defaultDuration = 0.06f;

    [Header("Resume (Smooth)")]
    [SerializeField] private float _resumeDuration = 0.08f;
    [SerializeField] private Ease _resumeEase = Ease.OutCubic;

    private float _baseFixedDeltaTime;
    private float _prevTimeScale = 1f;
    private Coroutine _pauseRoutine;
    private Tween _resumeTween;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (!Instance)
        {
            var go = new GameObject("HitPause");
            go.AddComponent<HitPause>();
        }
    }

    private void Awake()
    {
        if (Instance && !Equals(Instance, this))
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _baseFixedDeltaTime = Time.fixedDeltaTime;
    }

    public void Do(float duration = -1f)
    {
        if (duration <= 0f) duration = _defaultDuration;

        if (_pauseRoutine != null) StopCoroutine(_pauseRoutine);
        if (_resumeTween != null && _resumeTween.IsActive()) _resumeTween.Kill();

        _pauseRoutine = StartCoroutine(PauseRoutine(duration));
    }

    private IEnumerator PauseRoutine(float duration)
    {
        _prevTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;

        Time.timeScale = 0f;
        Time.fixedDeltaTime = _baseFixedDeltaTime * Time.timeScale;

        yield return new WaitForSecondsRealtime(duration);

        float targetScale = _prevTimeScale <= 0f ? 1f : _prevTimeScale;

        _resumeTween = DOTween
            .To(() => Time.timeScale, v =>
            {
                Time.timeScale = v;
                Time.fixedDeltaTime = _baseFixedDeltaTime * v;
            }, targetScale, _resumeDuration)
            .SetEase(_resumeEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                Time.timeScale = targetScale;
                Time.fixedDeltaTime = _baseFixedDeltaTime * targetScale;
                _resumeTween = null;
                _pauseRoutine = null;
            });
    }

    private void OnDisable()
    {
        if (_resumeTween != null && _resumeTween.IsActive()) _resumeTween.Kill();
        _pauseRoutine = null;
    }
}
