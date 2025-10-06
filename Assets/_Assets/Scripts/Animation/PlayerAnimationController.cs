using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private Animator _animator;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Thresholds")]
    [SerializeField] private float _movingSpeedThreshold = 0.1f;

    [Range(0f, 1f)]
    [SerializeField] private float _horizontalSmoothing = 0.15f;

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

    private void Reset()
    {
        if (!_movement) _movement = GetComponent<PlayerMovement>();
        if (!_animator) _animator = GetComponentInChildren<Animator>();
        if (!_spriteRenderer) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (!_movement) _movement = GetComponent<PlayerMovement>();
        if (!_animator) _animator = GetComponentInChildren<Animator>();
        if (!_spriteRenderer) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        CacheHashes();
    }

    private void OnEnable()
    {
        if (_movement)
        {
            _movement.Jumped += OnJumped;
            _movement.Landed += OnLanded;
        }
    }

    private void OnDisable()
    {
        if (_movement)
        {
            _movement.Jumped -= OnJumped;
            _movement.Landed -= OnLanded;
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
    }

    private void OnLanded()
    {
        if (!_animator) return;
        _animator.ResetTrigger(_tJump);
        _animator.SetTrigger(_tLand);
        if (_debugLogEvents) Debug.Log("[Anim] Land trigger");
    }
}
