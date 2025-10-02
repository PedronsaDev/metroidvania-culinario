using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerMovement : MonoBehaviour
{
    [Header("Config")]
    [SerializeField, Expandable] private MovementConfig _config;
    [Header("Colliders")]
    [SerializeField] private Collider2D _bodyCollider;
    [SerializeField] private Collider2D _feetCollider;
    [Header("Input")]
    [SerializeField] private InputActionReference _moveRef;
    [SerializeField] private InputActionReference _jumpRef;
    [SerializeField] private InputActionReference _runRef;

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _runAction;
    private Rigidbody2D _rb;

    private enum VerticalState { Grounded, Rising, Falling }
    private VerticalState _vState = VerticalState.Grounded;

    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private float _apexTimer;
    private int _airJumpsUsed;
    private bool _jumpHeld;
    private bool _facingRight = true;
    private float _lastYVel;
    private bool _grounded;
    private bool _headBlocked;
    private int _suppressGroundFrames;

    private bool _ledgeFallActive;
    private float _ledgeFallTimer;

    private bool _inputsSuspended;

    private float _recoilTimer;

    public event Action Jumped;
    public event Action Landed;

    public bool Grounded => _grounded;
    public float HorizontalSpeed => _rb != null ? _rb.linearVelocity.x : 0f;
    public float VerticalSpeed => _rb != null ? _rb.linearVelocity.y : 0f;
    public int VerticalStateId => (int)_vState; // 0 grounded, 1 rising, 2 falling
    public bool FacingRight => _facingRight;
    public bool LedgeFallEasing => _ledgeFallActive;
    public float MaxHorizontalSpeed => _config != null ? Mathf.Max(_config.WalkSpeed, _config.RunSpeed) : 1f;
    public float NormalizedHorizontalSpeed => MaxHorizontalSpeed > 0f ? Mathf.Abs(HorizontalSpeed) / MaxHorizontalSpeed : 0f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_moveRef) _moveAction = _moveRef.action;
        if (_jumpRef) _jumpAction = _jumpRef.action;
        if (_runRef) _runAction = _runRef.action;

        _rb.gravityScale = 0f;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void OnEnable()
    {
        Enable(_moveAction, true);
        Enable(_jumpAction, true);
        Enable(_runAction, true);

        if (_jumpAction != null)
        {
            _jumpAction.started += OnJumpStarted;
            _jumpAction.canceled += OnJumpCanceled;
        }
    }

    private void OnDisable()
    {
        if (_jumpAction != null)
        {
            _jumpAction.started -= OnJumpStarted;
            _jumpAction.canceled -= OnJumpCanceled;
        }
        Enable(_moveAction, false);
        Enable(_jumpAction, false);
        Enable(_runAction, false);
    }

    private static void Enable(InputAction action, bool on)
    {
        if (action == null) return;
        if (on && !action.enabled) action.Enable();
        else if (!on && action.enabled) action.Disable();
    }

    private void Update()
    {
        HandleGameplayBlock();

        _jumpHeld = _jumpAction != null && _jumpAction.IsPressed();

        GroundAndCeilingChecks();
        VerticalStateUpdate();
        TryConsumeBufferedJump();
        TickTimers();
    }

    private void FixedUpdate()
    {
        TryConsumeBufferedJump();
        HorizontalMove();
        ApplyVerticalPhysics();
        CommitVelocity();
    }

    private void GroundAndCeilingChecks()
    {
        bool wasGrounded = _grounded;
        Vector2 feetCenter = new(_feetCollider.bounds.center.x, _feetCollider.bounds.min.y);
        float width = _feetCollider.bounds.size.x*_config.GroundProbeWidthMultiplier;

        bool hitGround = Physics2D.BoxCast(
            feetCenter,
            new Vector2(width, _config.GroundProbeDistance),
            0f,
            Vector2.down,
            _config.GroundProbeDistance,
            _config.GroundMask).collider;

        _grounded = (_suppressGroundFrames <= 0) && hitGround;

        Vector2 headCenter = new(_bodyCollider.bounds.center.x, _bodyCollider.bounds.max.y);
        _headBlocked = Physics2D.BoxCast(
            headCenter,
            new Vector2(width, _config.CeilingProbeDistance),
            0f,
            Vector2.up,
            _config.CeilingProbeDistance,
            _config.GroundMask).collider;

        if (!_ledgeFallActive && wasGrounded && !_grounded && _suppressGroundFrames == 0 && Mathf.Abs(_rb.linearVelocity.y) < 0.02f)
        {
            StartLedgeFall();
        }
        if (_grounded && _ledgeFallActive)
        {
            _ledgeFallActive = false;
            _ledgeFallTimer = 0f;
        }

        if (_config.DebugProbes)
        {
            Color g = _grounded ? Color.green : Color.red;
            Debug.DrawLine(feetCenter + Vector2.left*(width*0.5f), feetCenter + Vector2.right*(width*0.5f), g);
            Debug.DrawLine(headCenter + Vector2.left*(width*0.5f), headCenter + Vector2.right*(width*0.5f), _headBlocked ? Color.yellow : Color.cyan);
        }

        if (_grounded && !wasGrounded && _lastYVel <= 0f)
        {
            Landed?.Invoke();
        }
    }

    private void HorizontalMove()
    {
        if (_recoilTimer > 0f)
        {
            _recoilTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector2 input = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        float baseSpeed = (_runAction != null && _runAction.IsPressed()) ? _config.RunSpeed : _config.WalkSpeed;
        float targetSpeed = input.x * baseSpeed;
        float currentX = _rb.linearVelocity.x;

        float apexAssist = 1f;
        if (_vState != VerticalState.Grounded && Mathf.Abs(_rb.linearVelocity.y) < 1.5f)
        {
            float t = 1f - Mathf.Clamp01(Mathf.Abs(_rb.linearVelocity.y) / 1.5f);
            apexAssist = Mathf.Lerp(1f, 1f + _config.ApexHorizontalAssist, t);
        }

        bool hasInput = Mathf.Abs(targetSpeed) > 0.01f;
        bool reversing = hasInput && Mathf.Abs(currentX) > 0.01f && !Mathf.Approximately(Mathf.Sign(targetSpeed), Mathf.Sign(currentX));

        if (reversing)
        {
            float turnDecel = (_grounded ? _config.GroundDeceleration : _config.AirDeceleration) * _config.TurnDecelMultiplier;
            currentX = Mathf.MoveTowards(currentX, 0f, turnDecel * Time.fixedDeltaTime);
            if (Mathf.Abs(currentX) < 0.01f)
            {
                float accel = (_grounded ? _config.GroundAcceleration : _config.AirAcceleration);
                currentX = Mathf.MoveTowards(currentX, targetSpeed, accel * apexAssist * Time.fixedDeltaTime);
            }
        }
        else
        {
            float accel = hasInput
                ? (_grounded ? _config.GroundAcceleration : _config.AirAcceleration)
                : (_grounded ? _config.GroundDeceleration : _config.AirDeceleration);
            currentX = Mathf.MoveTowards(currentX, targetSpeed, accel * apexAssist * Time.fixedDeltaTime);
        }

        float maxMag = baseSpeed;
        currentX = Mathf.Clamp(currentX, -maxMag, maxMag);

        if (Mathf.Abs(input.x) > 0.05f)
            Face(input.x > 0f);

        _rb.linearVelocity = new Vector2(currentX, _rb.linearVelocity.y);
    }

    private void Face(bool right)
    {
        if (right == _facingRight)
            return;

        _facingRight = right;
        var s = transform.localScale;
        s.x *= -1f;
        transform.localScale = s;
    }

    private void TryConsumeBufferedJump()
    {
        if (_jumpBufferTimer <= 0f) return;

        if (CanGroundJump())
        {
            PerformJump(true);
            return;
        }
        if (CanAirJump())
        {
            PerformJump(false);
            return;
        }
    }

    private void PerformJump(bool groundContext)
    {
        _jumpBufferTimer = 0f;
        if (groundContext)
            _airJumpsUsed = 0;
        else
            _airJumpsUsed++;

        _suppressGroundFrames = 2;
        _grounded = false;

        _vState = VerticalState.Rising;
        _apexTimer = 0f;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _config.JumpVelocity);

        _ledgeFallActive = false;
        _ledgeFallTimer = 0f;
        Jumped?.Invoke();
    }

    private void EarlyReleaseCheck()
    {
        if (_vState == VerticalState.Rising && _rb.linearVelocity.y > _config.MinReleaseUpVelocity)
        {
            float clipped = Mathf.Max(_config.MinReleaseUpVelocity, _rb.linearVelocity.y*0.55f);
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, clipped);
        }
    }

    private void VerticalStateUpdate()
    {
        if (_grounded)
        {
            _vState = VerticalState.Grounded;
            _coyoteTimer = _config.CoyoteTime;
            _airJumpsUsed = 0;
            return;
        }

        _vState = _rb.linearVelocity.y > 0.01f ? VerticalState.Rising : VerticalState.Falling;
        if (_vState == VerticalState.Rising && _ledgeFallActive)
        {
            _ledgeFallActive = false;
            _ledgeFallTimer = 0f;
        }
    }

    private void ApplyVerticalPhysics()
    {
        if (!_grounded && _vState == VerticalState.Grounded)
            _vState = VerticalState.Falling;

        float yVel = _rb.linearVelocity.y;

        if (_headBlocked && yVel > 0f)
            yVel = 0f;

        if (_vState == VerticalState.Rising)
        {
            if (yVel is > 0f and < 2f)
            {
                _apexTimer += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(_apexTimer/_config.ApexEaseTime);
                yVel = Mathf.Lerp(yVel, 0f, t);
            }
            else
            {
                yVel += _config.Gravity*Time.fixedDeltaTime;
            }
        }
        else if (_vState == VerticalState.Falling)
        {
            float baseMultiplier = (_jumpHeld ? 1f : _config.GravityReleaseMultiplier);
            float gravityEaseMultiplier = 1f;
            if (_ledgeFallActive)
            {
                float rampT = _config.LedgeWalkGravityRampTime <= 0f ? 1f : Mathf.Clamp01(_ledgeFallTimer / _config.LedgeWalkGravityRampTime);

                gravityEaseMultiplier = Mathf.SmoothStep(_config.LedgeWalkInitialGravityMultiplier, 1f, rampT);
                _ledgeFallTimer += Time.fixedDeltaTime;
                if (rampT >= 1f) _ledgeFallActive = false;
            }
            float grav = _config.Gravity * baseMultiplier * gravityEaseMultiplier;
            yVel += grav*Time.fixedDeltaTime;
        }
        else
        {
            if (yVel < 0f) yVel = 0f;
        }

        if (!_jumpHeld && _vState == VerticalState.Rising)
            yVel += _config.Gravity*(_config.GravityReleaseMultiplier - 1f)*Time.fixedDeltaTime;

        float maxDown = _jumpHeld ? _config.MaxFallSpeed : _config.FastFallSpeed;
        yVel = Mathf.Clamp(yVel, -maxDown, _config.JumpVelocity*1.2f);

        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, yVel);
    }

    private void CommitVelocity() => _lastYVel = _rb.linearVelocity.y;

    private void TickTimers()
    {
        if (_jumpBufferTimer > 0f) _jumpBufferTimer -= Time.deltaTime;
        if (!_grounded) _coyoteTimer -= Time.deltaTime;
        if (_suppressGroundFrames > 0) _suppressGroundFrames--;
    }

    private bool CanGroundJump() => _grounded || _coyoteTimer > 0f;
    private bool CanAirJump() => !_grounded && _airJumpsUsed < _config.MaxAirJumps;
    private void OnJumpStarted(InputAction.CallbackContext ctx)
    {
        if (CanGroundJump())
        {
            PerformJump(true);
            return;
        }
        if (CanAirJump())
        {
            PerformJump(false);
            return;
        }
        _jumpBufferTimer = _config.JumpBufferTime;
    }
    private void OnJumpCanceled(InputAction.CallbackContext ctx) => EarlyReleaseCheck();

    private void HandleGameplayBlock()
    {
        bool blocked = UIManager.Instance && UIManager.Instance.IsGameplayBlocked;
        if (blocked == _inputsSuspended)
            return;

        _inputsSuspended = blocked;
        if (blocked)
        {
            Enable(_moveAction, false);
            Enable(_jumpAction, false);
            Enable(_runAction, false);
            _jumpHeld = false;
            _jumpBufferTimer = 0f;
        }
        else
        {
            Enable(_moveAction, true);
            Enable(_jumpAction, true);
            Enable(_runAction, true);
        }
    }

    private void StartLedgeFall()
    {
        _ledgeFallActive = true;
        _ledgeFallTimer = 0f;
    }

    public void ApplyRecoil(Vector2 recoilVelocity, float duration, bool overrideX = true, bool overrideY = false)
    {
        if (overrideX)
            _rb.linearVelocity = new Vector2(recoilVelocity.x, _rb.linearVelocity.y);
        if (overrideY)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, recoilVelocity.y);
            if (recoilVelocity.y > 0f)
            {
                _grounded = false;
                _vState = VerticalState.Rising;
            }
        }
        _recoilTimer = Mathf.Max(_recoilTimer, duration);
    }
}
