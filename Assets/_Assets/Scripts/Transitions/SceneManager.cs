using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class SceneManager : MonoBehaviour
{
    public static SceneManager Instance { get; private set; }

    [Header("Lifecycle")]
    [SerializeField] private bool _dontDestroyOnLoad = true;
    [SerializeField] private bool _fadeInOnStart = true;

    [Header("Transition Visual")]
    [SerializeField] private CanvasGroup _transitionCanvasGroup;
    [SerializeField] private float _fadeOutDuration = 0.35f;
    [SerializeField] private float _fadeInDuration = 0.35f;
    [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _minBlackHoldTime = 0.1f;

    [Header("Events")]
    [SerializeField] private UnityEvent _onTransitionStarted;
    [SerializeField] private UnityEvent _onTransitionCompleted;

    public bool IsTransitioning => _transitionRoutine != null;

    public static event Action OnSceneLoaded;

    private Coroutine _transitionRoutine;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        PrepareTransitionCanvas();
    }

    private IEnumerator Start()
    {
        if (_fadeInOnStart && _transitionCanvasGroup)
        {
            _transitionCanvasGroup.alpha = 1f;
            yield return Fade(1f, 0f, _fadeInDuration);
        }
    }

    public void ReloadCurrentScene()
    {
        var activeScene = UnitySceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            Debug.LogWarning("ReloadCurrentScene called, but the active scene is invalid.");
            return;
        }

        LoadScene(activeScene.buildIndex);
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("LoadScene called with an empty scene name.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"Scene '{sceneName}' cannot be loaded. Ensure it's added to Build Settings.");
            return;
        }

        BeginTransition(() => UnitySceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single));
    }

    public void LoadScene(int buildIndex)
    {
        if (!IsValidBuildIndex(buildIndex))
        {
            Debug.LogWarning($"Build index {buildIndex} is not valid. Add the scene to Build Settings.");
            return;
        }

        BeginTransition(() => UnitySceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single));
    }

    private void BeginTransition(Func<AsyncOperation> loaderFactory)
    {
        if (_transitionRoutine != null)
        {
            Debug.LogWarning("A scene transition is already running.");
            return;
        }

        _transitionRoutine = StartCoroutine(PerformTransition(loaderFactory));
    }

    private IEnumerator PerformTransition(Func<AsyncOperation> loaderFactory)
    {
        _onTransitionStarted?.Invoke();

        AsyncOperation loadOperation = null;
        try
        {
            loadOperation = loaderFactory?.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to start scene load: {exception.Message}\n{exception.StackTrace}");
        }

        if (loadOperation == null)
        {
            _transitionRoutine = null;
            yield break;
        }

        loadOperation.allowSceneActivation = false;

        if (_transitionCanvasGroup)
        {
            yield return Fade(_transitionCanvasGroup.alpha, 1f, _fadeOutDuration);
        }
        else
        {
            yield return null;
        }

        var targetTime = Time.unscaledTime + Mathf.Max(_minBlackHoldTime, 0f);
        OnSceneLoaded?.Invoke();
        while (loadOperation.progress < 0.9f)
        {
            yield return null;
        }

        while (Time.unscaledTime < targetTime)
        {
            yield return null;
        }

        loadOperation.allowSceneActivation = true;
        while (!loadOperation.isDone)
        {
            yield return null;
        }

        if (_transitionCanvasGroup)
        {
            yield return Fade(1f, 0f, _fadeInDuration);
        }

        _onTransitionCompleted?.Invoke();
        _transitionRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (!_transitionCanvasGroup)
        {
            yield break;
        }

        duration = Mathf.Max(0f, duration);
        if (Mathf.Approximately(duration, 0f))
        {
            _transitionCanvasGroup.alpha = to;
            yield break;
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var curveValue = _fadeCurve?.Evaluate(t) ?? t;
            _transitionCanvasGroup.alpha = Mathf.Lerp(from, to, curveValue);
            yield return null;
        }

        _transitionCanvasGroup.alpha = to;
    }

    private bool IsValidBuildIndex(int buildIndex)
    {
        return buildIndex >= 0 && buildIndex < UnitySceneManager.sceneCountInBuildSettings;
    }

    private void PrepareTransitionCanvas()
    {
        if (!_transitionCanvasGroup)
        {
            return;
        }

        var canvasObject = _transitionCanvasGroup.gameObject;
        if (!canvasObject.activeSelf)
        {
            canvasObject.SetActive(true);
        }

        _transitionCanvasGroup.alpha = _fadeInOnStart ? 1f : 0f;
    }
}
