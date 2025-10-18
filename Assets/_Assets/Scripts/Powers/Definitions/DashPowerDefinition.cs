using TheBlackCat.TrailEffect2D;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Powers/Dash", fileName = "power_dash")]
public class DashPowerDefinition : PowerDefinition
{
    public enum DashDirectionMode { HorizontalOnly, EightWay }

    [Header("Input")]
    public InputActionReference ActivateRef;
    public InputActionReference DirectionRef;

    [Header("Dash Tuning")]
    [Min(0.01f)] public float Duration = 0.15f;
    [Min(0.1f)] public float Speed = 22f;
    [Min(0f)] public float DashCooldown = 0.35f;
    public AnimationCurve SpeedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("Air/Charges")]
    [Min(0)] public int AirDashes = 1;
    [Min(0f)] public float CoyoteTime = 0.1f;
    public bool DisableGravityDuringDash;

    [Header("Input Feel")]
    public DashDirectionMode DirectionMode = DashDirectionMode.HorizontalOnly;
    [Range(0f, 1f)] public float InputDeadzone = 0.2f;
    [Min(0f)] public float BufferTime = 0.1f;

    [Header("Damage")]
    public bool DamageOnDash;
    public int DamageAmount = 1;
    public float DamageKnockback = 5f;

    [Header("Behaviour")]
    public bool InvulnerabilityDuringDash;

    public override PowerRuntime CreateRuntime(PlayerPowerController controller) =>
        new DashPowerRuntime(controller, this);
}

public class DashPowerRuntime : PowerRuntime
{
    private readonly DashPowerDefinition _def;
    private InputAction _activateAction;
    private InputAction _dirAction;

    private bool _isDashing;
    private float _dashTimer;
    private float _cooldownTimer;
    private float _bufferTimer;
    private float _coyoteTimer;
    private int _airDashesRemaining;

    private PlayerMovement _move;
    private PlayerHealth _health;
    private PlayerAttack _attack;

    private Vector2 _dashDir;
    private float _dashElapsed;
    private bool _overrideYDuringDash;

    public DashPowerRuntime(PlayerPowerController controller, DashPowerDefinition def) : base(controller)
    {
        _def = def;
    }

    public override void OnEquip()
    {
        base.OnEquip();
        _move = _controller.Movement;
        _airDashesRemaining = _def.AirDashes;

        if (_def.InvulnerabilityDuringDash)
            _health = _controller.Health;

        if (_def.DamageOnDash)
            _attack = _controller.Attack;

        if (_def.ActivateRef)
        {
            _activateAction = _def.ActivateRef.action;
            _activateAction.started += OnActivateTriggered;
            if (!_activateAction.enabled)
                _activateAction.Enable();
        }

        if (_def.DirectionRef)
        {
            _dirAction = _def.DirectionRef.action;
            if (!_dirAction.enabled)
                _dirAction.Enable();
        }

        if (_move)
            _move.Landed += OnLanded;
    }

    public override void OnUnequip()
    {
        if (_activateAction != null)
        {
            _activateAction.started -= OnActivateTriggered;
            _activateAction.Disable();
            _activateAction = null;
        }

        if (_dirAction != null)
        {
            _dirAction.Disable();
            _dirAction = null;
        }

        if (_move)
            _move.Landed -= OnLanded;
    }

    public override void Tick()
    {
        float dt = Time.deltaTime;

        if (_cooldownTimer > 0f)
            _cooldownTimer -= dt;
        if (_bufferTimer > 0f)
            _bufferTimer -= dt;

        if (_move)
            _coyoteTimer = !_move.Grounded ? Mathf.Max(_coyoteTimer - dt, 0f) : _def.CoyoteTime;

        if (_bufferTimer > 0f && CanDash())
        {
            _bufferTimer = 0f;
            BeginDash();
        }

        if (_isDashing && _def.DamageOnDash)
        {
            _attack.DealDamageInBox(_attack.transform.position, Vector2.one * 2f,
                PlayerAttack.VerticalFilterMode.None, damageOverride: _def.DamageAmount);
        }
    }

    public override void FixedTick()
    {
        if (!_isDashing)
            return;

        _dashTimer -= Time.fixedDeltaTime;
        _dashElapsed += Time.fixedDeltaTime;

        float t = _def.Duration > 0f ? Mathf.Clamp01(_dashElapsed / _def.Duration) : 1f;
        float mult = _def.SpeedCurve != null ? Mathf.Max(0f, _def.SpeedCurve.Evaluate(t)) : 1f;
        float targetSpeed = Mathf.Max(0f, _def.Speed * mult);
        float wallCheckSpeed = Mathf.Max(targetSpeed, _def.Speed * 0.1f);

        if (_dashElapsed > 0.02f && _controller?.Rb)
        {
            Vector2 vel = _controller.Rb.linearVelocity;
            float along = Vector2.Dot(vel, _dashDir);
            if (along < (wallCheckSpeed * 0.2f))
            {
                _controller.Movement?.CancelRecoil();
                EndDash();
                return;
            }
        }

        if (_controller?.Rb)
        {
            Vector2 desired = _dashDir * targetSpeed;
            if (_overrideYDuringDash)
            {
                _controller.Rb.linearVelocity = desired;
            }
            else
            {
                var v = _controller.Rb.linearVelocity;
                v.x = desired.x;
                _controller.Rb.linearVelocity = v;
            }
        }

        if (_dashTimer <= 0f)
            EndDash();
    }

    public override void ActivatePower()
    {
        if (!CanDash())
        {
            _bufferTimer = Mathf.Max(_bufferTimer, _def.BufferTime);
            return;
        }

        BeginDash();
    }

    private void OnActivateTriggered(InputAction.CallbackContext obj)
    {
        ActivatePower();
    }

    private void OnLanded()
    {
        _airDashesRemaining = _def.AirDashes;
        _coyoteTimer = 0f;
    }

    private bool CanDash()
    {
        if (_isDashing || _cooldownTimer > 0f || !_move)
            return false;

        if (_move.Grounded || _airDashesRemaining > 0)
            return true;

        return _coyoteTimer > 0f;
    }

    private void BeginDash()
    {
        if (_def.InvulnerabilityDuringDash)
            _health.SetInvunerability(true);

        TrailManager.Instance.StartTrail(_controller.Trail.gameObject);

        Vector2 dir = Vector2.zero;
        if (_def.DirectionMode == DashPowerDefinition.DashDirectionMode.EightWay && _dirAction != null)
        {
            Vector2 input = _dirAction.ReadValue<Vector2>();
            if (input.sqrMagnitude >= _def.InputDeadzone * _def.InputDeadzone)
            {
                Vector2 n = input.normalized;
                dir = SnapEightWay(n);
            }
        }

        if (dir == Vector2.zero)
        {
            bool right = _move && _move.FacingRight;
            dir = right ? Vector2.right : Vector2.left;
        }

        _dashDir = dir.sqrMagnitude > 0f ? dir.normalized : Vector2.right;
        float speed = Mathf.Max(0.01f, _def.Speed);
        Vector2 dashVel = _dashDir * speed;

        bool overrideY = false;
        if (_def.DirectionMode == DashPowerDefinition.DashDirectionMode.EightWay)
        {
            overrideY = true;
        }
        else if (_def.DisableGravityDuringDash)
        {
            overrideY = true;
            dashVel.y = 0f;
            _move.DisableGravity();
        }

        _overrideYDuringDash = overrideY;
        _controller.Movement.ApplyRecoil(dashVel, _def.Duration, overrideX: true, overrideY: overrideY);

        _isDashing = true;
        _dashTimer = _def.Duration;
        _dashElapsed = 0f;
        _cooldownTimer = _def.DashCooldown;

        if (!_move.Grounded && _airDashesRemaining > 0)
            _airDashesRemaining--;
    }

    private void EndDash()
    {
        if (_def.DisableGravityDuringDash)
            _move.EnableGravity();

        _isDashing = false;
        _dashTimer = 0f;

        if (_def.InvulnerabilityDuringDash)
            _health.SetInvunerability(false);

        TrailManager.Instance.StopTrail(_controller.Trail.gameObject);
    }

    private static Vector2 SnapEightWay(Vector2 n)
    {
        Vector2[] dirs = new Vector2[]
        {
            new(1, 0), new Vector2(1, 1).normalized, new(0, 1), new Vector2(-1, 1).normalized,
            new(-1, 0), new Vector2(-1, -1).normalized, new(0, -1), new Vector2(1, -1).normalized
        };

        int best = 0;
        float bestDot = -999f;
        for (int i = 0; i < dirs.Length; i++)
        {
            float d = Vector2.Dot(n, dirs[i]);
            if (d > bestDot)
            {
                bestDot = d;
                best = i;
            }
        }
        return dirs[best];
    }
}
