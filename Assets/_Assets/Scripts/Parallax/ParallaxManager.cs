using System;
using UnityEngine;

[DefaultExecutionOrder(-5000)]
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class ParallaxManager : MonoBehaviour
{
    public static event Action<Vector3, float> CameraMoved;

    [SerializeField] private Camera _targetCamera;

    [SerializeField] private bool _enableInEditMode = true;

    Vector3 _lastCamPos;
    bool _initialized;
    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;
    }
    Camera ResolveCamera()
    {
        if (_targetCamera) return _targetCamera;
        if (!_cam)
        {
            var cams = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (cams is { Length: > 0 }) _cam = cams[0];
        }
        return _cam;
    }

    void OnEnable() => TryInit(true);

    void OnDisable() => _initialized = false;

    void TryInit(bool force)
    {
        var cam = ResolveCamera();
        if (!cam)
        {
            _initialized = false;
            return;
        }

        if (force || !_initialized)
        {
            _lastCamPos = cam.transform.position;
            _initialized = true;
        }
    }

    void LateUpdate()
    {
        if (!Application.isPlaying && !_enableInEditMode)
            return;

        var cam = ResolveCamera();
        if (!cam)
        {
            _initialized = false;
            return;
        }

        if (!_initialized)
        {
            TryInit(true);
            return;
        }

        var current = cam.transform.position;
        var delta = current - _lastCamPos;
        if (delta.sqrMagnitude > 0f)
        {
            _lastCamPos = current;
            var dt = Application.isPlaying ? Time.deltaTime : 0f;
            CameraMoved?.Invoke(delta, dt);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            TryInit(true);
        }
    }
#endif
}
