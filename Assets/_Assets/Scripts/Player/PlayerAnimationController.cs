using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private Animator _animator;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private PlayerHealth _health;

    [Header("Thresholds")]
    [SerializeField] private float _movingSpeedThreshold = 0.1f;

    [Range(0f, 1f)]
    [SerializeField] private float _horizontalSmoothing = 0.15f;

    [Header("Squash & Stretch")]
    [SerializeField] private Transform _squashTarget;

    [Header("Jump Squash")]
    [SerializeField, Min(0.2f)] private float _jumpXMult = 0.9f;
    [SerializeField, Min(0.2f)] private float _jumpYMult = 1.1f;
    [SerializeField, Min(0.01f)] private float _jumpInDuration = 0.06f;
    [SerializeField, Min(0.01f)] private float _jumpOutDuration = 0.12f;
    [SerializeField] private Ease _jumpInEase = Ease.OutQuad;
    [SerializeField] private Ease _jumpOutEase = Ease.OutBack;
    [SerializeField] private bool _jumpUnscaledTime;

    [Header("Land Squash")]
    [SerializeField, Min(0.2f)] private float _landXMult = 1.15f;
    [SerializeField, Min(0.2f)] private float _landYMult = 0.85f;
    [SerializeField, Min(0.01f)] private float _landInDuration = 0.05f;
    [SerializeField, Min(0.01f)] private float _landOutDuration = 0.12f;
    [SerializeField] private Ease _landInEase = Ease.OutQuad;
    [SerializeField] private Ease _landOutEase = Ease.OutBack;
    [SerializeField] private bool _landUnscaledTime;

    [Header("Hit Squash")]
    [SerializeField, Min(0.2f)] private float _hitXMult = 1.2f;
    [SerializeField, Min(0.2f)] private float _hitYMult = 0.8f;
    [SerializeField, Min(0.01f)] private float _hitInDuration = 0.06f;
    [SerializeField, Min(0.01f)] private float _hitOutDuration = 0.12f;
    [SerializeField] private Ease _hitInEase = Ease.OutQuad;
    [SerializeField] private Ease _hitOutEase = Ease.OutBack;
    [SerializeField] private bool _hitUnscaledTime = true;

    [Header("Debug")]
    [SerializeField] private bool _debugLogEvents;

    private int _pSpeedXAbs;
    private int _pSpeedY;
    private int _pGrounded;
    private int _pVerticalState;
    private int _pLedgeEase;
    private int _pMoving;
    private int _tJump;
    private int _tLand;
    private int _pSpeedX01;

    private float _smoothedAbsX;

    private Tween _squashTween;

    private void Reset()
    {
        if (!_movement) _movement = GetComponent<PlayerMovement>();
        if (!_animator) _animator = GetComponentInChildren<Animator>();
        if (!_spriteRenderer) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!_health) _health = GetComponent<PlayerHealth>();
        if (!_squashTarget && _spriteRenderer) _squashTarget = _spriteRenderer.transform;
    }

    private void Awake()
    {
        if (!_movement) _movement = GetComponent<PlayerMovement>();
        if (!_animator) _animator = GetComponentInChildren<Animator>();
        if (!_spriteRenderer) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!_health) _health = GetComponent<PlayerHealth>();
        if (!_squashTarget && _spriteRenderer) _squashTarget = _spriteRenderer.transform;
        CacheHashes();
    }

    private void OnEnable()
    {
        if (_movement)
        {
            _movement.Jumped += OnJumped;
            _movement.Landed += OnLanded;
        }
        if (_health)
        {
            _health.Damaged += OnDamaged;
        }
    }

    private void OnDisable()
    {
        if (_movement)
        {
            _movement.Jumped -= OnJumped;
            _movement.Landed -= OnLanded;
        }
        if (_health)
        {
            _health.Damaged -= OnDamaged;
        }

        if (_squashTween != null && _squashTween.IsActive())
        {
            _squashTween.Kill();
            TryRestoreScale();
        }
    }

    private void CacheHashes()
    {
        _pSpeedXAbs = Animator.StringToHash("SpeedXAbs");
        _pSpeedY = Animator.StringToHash("SpeedY");
        _pGrounded = Animator.StringToHash("Grounded");
        _pVerticalState = Animator.StringToHash("VerticalState");
        _pLedgeEase = Animator.StringToHash("LedgeEase");
        _pMoving = Animator.StringToHash("Moving");
        _tJump = Animator.StringToHash("Jump");
        _tLand = Animator.StringToHash("Land");
        _pSpeedX01 = Animator.StringToHash("SpeedX01");
    }

    private void Update()
    {
        if (!_movement || !_animator) return;

        float targetAbsX = Mathf.Abs(_movement.HorizontalSpeed);
        _smoothedAbsX = Mathf.Lerp(_smoothedAbsX, targetAbsX, 1f - Mathf.Pow(1f - Mathf.Clamp01(1f - _horizontalSmoothing), Time.deltaTime*60f));

        _animator.SetFloat(_pSpeedXAbs, _smoothedAbsX);
        _animator.SetFloat(_pSpeedY, _movement.VerticalSpeed);
        _animator.SetBool(_pGrounded, _movement.Grounded);
        _animator.SetInteger(_pVerticalState, _movement.VerticalStateId);
        _animator.SetBool(_pLedgeEase, _movement.LedgeFallEasing);
        _animator.SetBool(_pMoving, targetAbsX > _movingSpeedThreshold && _movement.Grounded);
        _animator.SetFloat(_pSpeedX01, _movement.NormalizedHorizontalSpeed);

        FlipByRotation();
    }

    public Animator GetCurrentAnimator() => _animator;

    private void FlipBySprite() => _spriteRenderer.flipX = !_movement.FacingRight;

    private void FlipByScale()
    {
        var tr = transform;
        Vector3 scale = tr.localScale;
        float sign = _movement.FacingRight ? 1f : -1f;
        if (scale.x*sign < 0f)
            scale.x = -scale.x;

        tr.localScale = scale;
    }

    private void FlipByRotation()
    {
        var tr = _spriteRenderer.transform;
        Vector3 rot = tr.eulerAngles;
        rot.y = _movement.FacingRight ? 0f : -180f;
        tr.eulerAngles = rot;
    }

    private void OnJumped()
    {
        if (!_animator) return;
        _animator.ResetTrigger(_tLand);
        _animator.SetTrigger(_tJump);
        if (_debugLogEvents) Debug.Log("[Anim] Jump trigger");

        DoSquash(_jumpXMult, _jumpYMult, _jumpInDuration, _jumpOutDuration, _jumpInEase, _jumpOutEase, _jumpUnscaledTime);
    }

    private void OnLanded()
    {
        if (!_animator) return;
        _animator.ResetTrigger(_tJump);
        _animator.SetTrigger(_tLand);
        if (_debugLogEvents) Debug.Log("[Anim] Land trigger");

        DoSquash(_landXMult, _landYMult, _landInDuration, _landOutDuration, _landInEase, _landOutEase, _landUnscaledTime);
    }

    private void OnDamaged()
    {
        DoSquash(_hitXMult, _hitYMult, _hitInDuration, _hitOutDuration, _hitInEase, _hitOutEase, _hitUnscaledTime);
    }

    private void DoSquash(float xMult, float yMult, float inDuration, float outDuration, Ease inEase, Ease outEase, bool useUnscaled)
    {
        if (!_squashTarget)
            _squashTarget = _spriteRenderer ? _spriteRenderer.transform : transform;

        _squashTween?.Kill();

        Vector3 s = Vector3.one;
        float signX = Mathf.Approximately(s.x, 0f) ? 1f : Mathf.Sign(s.x);
        Vector3 abs = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));

        Vector3 peak = new Vector3(Mathf.Max(0.01f, xMult)*abs.x*signX,
                                   Mathf.Max(0.01f, yMult)*abs.y,
                                   abs.z);
        Vector3 rest = new Vector3(abs.x*signX, abs.y, abs.z);

        var seq = DOTween.Sequence();
        seq.Append(_squashTarget.DOScale(peak, inDuration).SetEase(inEase));
        seq.Append(_squashTarget.DOScale(rest, outDuration).SetEase(outEase));
        seq.SetUpdate(useUnscaled);
        _squashTween = seq;
    }

    private void TryRestoreScale()
    {
        if (!_squashTarget) return;
        Vector3 s = _squashTarget.localScale;
        float signX = Mathf.Approximately(s.x, 0f) ? 1f : Mathf.Sign(s.x);
        Vector3 abs = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        _squashTarget.localScale = new Vector3(abs.x*signX, abs.y, abs.z);
    }
}
