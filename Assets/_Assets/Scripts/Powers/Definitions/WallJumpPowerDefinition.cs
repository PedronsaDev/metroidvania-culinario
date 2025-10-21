using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Powers/Wall Jump", fileName = "power_walljump")]
public class WallJumpPowerDefinition : PowerDefinition
{
    [Header("Input")]
    public InputActionReference MoveRef;

    [Header("Wall Detection")]
    public LayerMask WallMask;
    public float WallProbeDistance = 0.2f;

    [Header("Wall Slide")]
    [Range(0f, 20f)] public float WallSlideSpeed = 2f;
    public bool RequireInputToSlide = true;

    [Header("Wall Jump")]
    [Range(0f, 30f)] public float WallJumpVelocityX = 12f;
    [Range(0f, 30f)] public float WallJumpVelocityY = 18f;
    [Range(0f, 0.5f)] public float WallJumpInputLockTime = 0.2f;
    [Range(0f, 0.3f)] public float WallCoyoteTime = 0.1f;

    [Header("Debug")]
    public bool ShowDebugGizmos;

    public override PowerRuntime CreateRuntime(PlayerPowerController controller) =>
        new WallJumpPowerRuntime(controller, this);
}

public class WallJumpPowerRuntime : PowerRuntime
{
    private readonly WallJumpPowerDefinition _def;
    private PlayerMovement _move;
    private Rigidbody2D _rb;
    private InputAction _moveInput;
    private Collider2D _bodyCollider;

    private bool _onWallLeft;
    private bool _onWallRight;
    private float _wallCoyoteTimer;
    private float _wallJumpInputLockTimer;
    private bool _wallJumpPerformed;
    private bool _valid;

    public WallJumpPowerRuntime(PlayerPowerController controller, WallJumpPowerDefinition def) : base(controller)
    {
        _def = def;
    }

    public override void OnEquip()
    {
        base.OnEquip();
        _move = _controller.Movement;
        _rb = _controller.Rb;
        _bodyCollider = _move.BodyCollider;

        if (!_bodyCollider)
        {
            var colliders = _controller.GetComponents<Collider2D>();
            foreach (var col in colliders)
            {
                if (!col.isTrigger)
                {
                    _bodyCollider = col;
                    break;
                }
            }
        }

        if (_def.MoveRef)
            _moveInput = _def.MoveRef.action;

        if (_move)
        {
            _move.JumpInterceptor += TryInterceptJump;
            _move.Landed += OnPlayerLanded;
        }
    }

    public override void OnUnequip()
    {
        base.OnUnequip();
        if (_move)
        {
            _move.JumpInterceptor -= TryInterceptJump;
            _move.Landed -= OnPlayerLanded;
        }
    }

    public override void Tick()
    {
        CheckWalls();
        UpdateTimers();
    }

    public override void FixedTick()
    {
        UpdateWallJumpInputLock();
        ApplyWallSlide();
    }

    private void CheckWalls()
    {
        if (!_bodyCollider || !_rb)
            return;

        Vector2 bodyCenter = _bodyCollider.bounds.center;
        float height = _bodyCollider.bounds.size.y*0.8f;

        RaycastHit2D hitLeft = Physics2D.BoxCast(
            bodyCenter,
            new Vector2(_def.WallProbeDistance, height),
            0f,
            Vector2.left,
            _def.WallProbeDistance,
            _def.WallMask);
        _onWallLeft = hitLeft.collider;

        RaycastHit2D hitRight = Physics2D.BoxCast(
            bodyCenter,
            new Vector2(_def.WallProbeDistance, height),
            0f,
            Vector2.right,
            _def.WallProbeDistance,
            _def.WallMask);
        _onWallRight = hitRight.collider;

        _valid = !PlayerMovement.IsOneWayPlatformCollider(hitRight.collider) || !PlayerMovement.IsOneWayPlatformCollider(hitLeft.collider);

        if (_onWallLeft || _onWallRight && _valid)
            _wallCoyoteTimer = _def.WallCoyoteTime;

        if (_move && _move.Grounded)
            _wallCoyoteTimer = 0f;

        if (_def.ShowDebugGizmos)
        {
            Debug.DrawRay(bodyCenter, Vector2.left*_def.WallProbeDistance,
                _onWallLeft ? Color.magenta : Color.gray);
            Debug.DrawRay(bodyCenter, Vector2.right*_def.WallProbeDistance,
                _onWallRight ? Color.magenta : Color.gray);
        }
    }

    private void UpdateTimers()
    {
        if (_wallCoyoteTimer > 0f)
            _wallCoyoteTimer -= Time.deltaTime;
    }

    private void UpdateWallJumpInputLock()
    {
        if (_wallJumpInputLockTimer > 0f)
            _wallJumpInputLockTimer -= Time.fixedDeltaTime;
        else
            _wallJumpPerformed = false;
    }

    private void ApplyWallSlide()
    {
        if (!CanWallSlide())
            return;

        float yVel = _rb.linearVelocity.y;
        if (yVel < -_def.WallSlideSpeed)
        {
            yVel = -_def.WallSlideSpeed;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, yVel);
        }
    }

    private bool CanWallSlide()
    {
        if (!_valid)
            return false;
        if (!_move || !_rb)
            return false;
        if (_move.Grounded)
            return false;
        if (_rb.linearVelocity.y >= 0f)
            return false;
        if (!_onWallLeft && !_onWallRight)
            return false;

        if (_def.RequireInputToSlide && _moveInput != null)
        {
            Vector2 input = _moveInput.ReadValue<Vector2>();
            bool pressingLeft = input.x < -0.1f;
            bool pressingRight = input.x > 0.1f;

            if (_onWallLeft && !pressingLeft)
                return false;
            if (_onWallRight && !pressingRight)
                return false;
        }

        return true;
    }

    private bool TryInterceptJump()
    {
        if (CanWallJump() && !_move.Grounded)
        {
            PerformWallJump();
            return true;
        }
        return false;
    }

    private bool CanWallJump()
    {
        if (!_move || _move.Grounded || !CanWallSlide() || !_valid)
            return false;
        return _onWallLeft || _onWallRight || _wallCoyoteTimer > 0f;
    }

    private void PerformWallJump()
    {
        _wallCoyoteTimer = 0f;
        _wallJumpPerformed = true;
        _wallJumpInputLockTimer = _def.WallJumpInputLockTime;

        float jumpDirX = _onWallLeft ? 1f : -1f;
        Vector2 wallJumpVelocity = new(jumpDirX * _def.WallJumpVelocityX, _def.WallJumpVelocityY);

        _rb.linearVelocity = wallJumpVelocity;

        if (_move)
        {
            _move.ApplyRecoil(wallJumpVelocity, _def.WallJumpInputLockTime,
                overrideX: true, overrideY: true);
        }
    }

    private void OnPlayerLanded()
    {
        _wallJumpPerformed = false;
    }

    public bool IsOnWall() => _onWallLeft || _onWallRight;
    public bool IsWallSliding() => CanWallSlide();
}