using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Transform _target;

    [Header("Strength (per axis)")]
    [SerializeField] private Vector2 _strength = new Vector2(0.5f, 0.5f);
    [SerializeField] private float _strengthZ = 0f;

    [Header("Axes")]
    [SerializeField] private bool _affectX = true;
    [SerializeField] private bool _affectY = true;
    [SerializeField] private bool _affectZ = false;

    [Header("Smoothing")]
    [Min(0f)] public float SmoothTime = 0f;
    [SerializeField] private bool _useUnscaledTime = false;

    [Header("Advanced")]
    [SerializeField] private bool _previewInEditMode = true;
    [SerializeField] private bool _fallbackWithoutManager = true;

    private Vector3 _velocity;
    private Vector3 _pendingOffset;
    private Vector3 _lastCamPos;
    private bool _hasCamPos;

    Transform _camTransform;

    void Reset()
    {
        _target = transform;
    }

    void OnEnable()
    {
        if (!_target) _target = transform;
        ParallaxManager.CameraMoved += OnCameraMoved;
        CacheCamera();
        _hasCamPos = false;
        _pendingOffset = Vector3.zero;
        _velocity = Vector3.zero;
    }

    void OnDisable()
    {
        ParallaxManager.CameraMoved -= OnCameraMoved;
    }

    void CacheCamera()
    {
        var cam = Camera.main;
        if (!cam)
        {
            var cams = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (cams is { Length: > 0 }) cam = cams[0];
        }
        _camTransform = cam ? cam.transform : null;
    }

    void OnCameraMoved(Vector3 delta, float dt)
    {
        if (!Application.isPlaying && !_previewInEditMode)
            return;

        ApplyDelta(delta, dt);
    }

    void ApplyDelta(Vector3 camDelta, float dt)
    {
        Vector3 offset = Vector3.zero;
        if (_affectX) offset.x = camDelta.x*_strength.x;
        if (_affectY) offset.y = camDelta.y*_strength.y;
        if (_affectZ) offset.z = camDelta.z*_strengthZ;

        if (SmoothTime <= 0f || (!Application.isPlaying && dt <= 0f))
        {
            _target.position += offset;
        }
        else
        {
            _pendingOffset += offset;
        }
    }

    void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        if (_fallbackWithoutManager && !_camTransform)
            CacheCamera();

        if (_fallbackWithoutManager && _camTransform)
        {
            var camPos = _camTransform.position;
            if (!_hasCamPos)
            {
                _lastCamPos = camPos;
                _hasCamPos = true;
            }
            else
            {
                var delta = camPos - _lastCamPos;
                if (delta.sqrMagnitude > 0f)
                {
                    _lastCamPos = camPos;
                    ApplyDelta(delta, _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
                }
            }
        }

        if (SmoothTime > 0f)
        {
            var dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (dt > 0f)
            {
                var targetPos = _target.position + _pendingOffset;
                _target.position = Vector3.SmoothDamp(_target.position, targetPos, ref _velocity, SmoothTime, Mathf.Infinity, dt);

                var residual = targetPos - _target.position;
                _pendingOffset = residual;
            }
            else
            {
                _target.position += _pendingOffset;
                _pendingOffset = Vector3.zero;
            }
        }
    }
}
