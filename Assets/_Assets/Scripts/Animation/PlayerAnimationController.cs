using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private Animator animator;
    [Tooltip("Automatically flip SpriteRenderer by scale instead of using animator mirror.")]
    [SerializeField] private bool flipByScale = true;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Thresholds")]
    [Tooltip("Horizontal speed magnitude above which 'Moving' becomes true.")]
    [SerializeField] private float movingSpeedThreshold = 0.1f;

    [Tooltip("Filter factor (0 = instant) for horizontal speed smoothing for animation.")]
    [Range(0f, 1f)]
    [SerializeField] private float horizontalSmoothing = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool debugLogEvents;

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
        if (!movement) movement = GetComponent<PlayerMovement>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (!movement) movement = GetComponent<PlayerMovement>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        CacheHashes();
    }

    private void OnEnable()
    {
        if (movement)
        {
            movement.Jumped += OnJumped;
            movement.Landed += OnLanded;
        }
    }

    private void OnDisable()
    {
        if (movement)
        {
            movement.Jumped -= OnJumped;
            movement.Landed -= OnLanded;
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
        if (!movement || !animator) return;

        float targetAbsX = Mathf.Abs(movement.HorizontalSpeed);
        _smoothedAbsX = Mathf.Lerp(_smoothedAbsX, targetAbsX, 1f - Mathf.Pow(1f - Mathf.Clamp01(1f - horizontalSmoothing), Time.deltaTime*60f));

        animator.SetFloat(_pSpeedXAbs, _smoothedAbsX);
        animator.SetFloat(_pSpeedY, movement.VerticalSpeed);
        animator.SetBool(_pGrounded, movement.Grounded);
        animator.SetInteger(_pVerticalState, movement.VerticalStateId);
        animator.SetBool(_pLedgeEase, movement.LedgeFallEasing);
        animator.SetBool(_pMoving, targetAbsX > movingSpeedThreshold && movement.Grounded);
        animator.SetFloat(_pSpeedX01, movement.NormalizedHorizontalSpeed);

        if (flipByScale)
        {
            var tr = transform;
            Vector3 scale = tr.localScale;
            float sign = movement.FacingRight ? 1f : -1f;
            if (scale.x*sign < 0f) scale.x = -scale.x;

            tr.localScale = scale;
        }
        else if (spriteRenderer)
        {
            spriteRenderer.flipX = !movement.FacingRight;
        }
    }

    private void OnJumped()
    {
        if (!animator) return;
        animator.ResetTrigger(_tLand);
        animator.SetTrigger(_tJump);
        if (debugLogEvents) Debug.Log("[Anim] Jump trigger");
    }

    private void OnLanded()
    {
        if (!animator) return;
        animator.ResetTrigger(_tJump);
        animator.SetTrigger(_tLand);
        if (debugLogEvents) Debug.Log("[Anim] Land trigger");
    }
}
