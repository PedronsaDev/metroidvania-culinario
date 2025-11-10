using UnityEngine;
[RequireComponent(typeof(Rigidbody2D))]
public class MushroomGroundEnemy : EnemyBase
{
    [Header("Patrol")]
    [SerializeField] private float _patrolSpeed = 1.6f;
    [SerializeField] private float _acceleration = 12f;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private float _wallCheckDistance = 0.25f;
    [SerializeField] private float _ledgeCheckDistance = 0.35f;
    [SerializeField] private float _groundCheckDistance = 0.1f;

    [Header("Detection")]
    [SerializeField] private LayerMask _playerMask;
    [SerializeField] private float _detectionRadius = 5f;
    [SerializeField] private float _attackCooldown = 2.25f;
    [SerializeField] private Collider2D _col;

    [Header("Ground Pound")]
    [SerializeField] private float _windupTime = 0.35f;
    [SerializeField] private int _attackDamage = 1;
    [SerializeField] private float _attackDuration = 0.4f;
    [SerializeField] private float _dizzyDuration = 1.1f;
    [SerializeField] private Collider2D _attackCollider;

    [Header("Hit / Stun")]
    [SerializeField] private float _hitStunPause = 0.2f;

    private enum State { Patrol, Windup, Attack, Dizzy }

    private State _state = State.Patrol;
    private float _stateUntil;
    private float _attackUntil;
    private float _nextAttackTime;
    private bool _facingRight = true;

    private Animator _anim;
    private Transform _player;

    private bool _prevWasHit;
    private float _stunUntil;

    private static readonly int _animWalking = Animator.StringToHash("Walking");
    private static readonly int _animWindup = Animator.StringToHash("Windup");
    private static readonly int _animDashing = Animator.StringToHash("Dashing");
    private static readonly int _animDizzy = Animator.StringToHash("Dizzy");

    protected override void Awake()
    {
        base.Awake();
        _anim = GetComponentInChildren<Animator>();
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        Rb = GetComponent<Rigidbody2D>();
    }

    protected override void Start()
    {
        base.Start();

        SetAnimator(State.Patrol);
    }

    private void Update()
    {
        if (!_prevWasHit && WasHit)
            _stunUntil = Mathf.Max(_stunUntil, Time.time + _hitStunPause);
        _prevWasHit = WasHit;

        if (Time.time < _stunUntil)
            return;

        switch (_state)
        {
            case State.Patrol:
                TryBeginAttack();
                break;
            case State.Windup:
                if (Time.time >= _stateUntil)
                    BeginAttack();
                break;

            case State.Attack:
                if (Time.time >= _attackUntil)
                    EndAttack();
                break;

            case State.Dizzy:
                if (Time.time >= _stateUntil)
                {
                    ChangeState(State.Patrol);
                }
                break;
        }
    }

    private void FixedUpdate()
    {
        if (Time.time < _stunUntil)
            return;

        switch (_state)
        {
            case State.Patrol:
                DoPatrolMovement();
                break;
            case State.Windup:
                float vx = Mathf.MoveTowards(Rb.linearVelocity.x, 0f, _acceleration*Time.fixedDeltaTime);
                Rb.linearVelocity = new Vector2(vx, Rb.linearVelocity.y);
                break;
            case State.Attack:
                break;
            case State.Dizzy:
                break;
        }

        if (_state == State.Patrol)
        {
            if (Rb.linearVelocity.x > 0.05f) FaceRight(true);
            else if (Rb.linearVelocity.x < -0.05f) FaceRight(false);
        }
    }

    protected override void OnDamageApplied(Vector3 hitDirection, int damage)
    {
        base.OnDamageApplied(hitDirection, damage);

        if (_state == State.Attack)
            EndAttack();
    }

    private void ChangeState(State s)
    {
        _state = s;
        SetAnimator(s);

        _attackCollider.enabled = _state == State.Attack;
    }

    private void SetAnimator(State s)
    {
        if (!_anim) return;
        _anim.SetBool(_animWalking, s == State.Patrol);
        _anim.SetBool(_animWindup, s == State.Windup);
        _anim.SetBool(_animDashing, s == State.Attack);
        _anim.SetBool(_animDizzy, s == State.Dizzy);
    }

    private void TryBeginAttack()
    {
        if (!_player) return;
        if (Time.time < _nextAttackTime) return;
        if (!IsGrounded()) return;

        if (PlayerDetectedOnSide())
        {
            FaceRight(_player.position.x >= transform.position.x);
            ChangeState(State.Windup);
            _stateUntil = Time.time + _windupTime;
        }
    }

    private bool PlayerDetectedOnSide()
    {
        if (!_player)
            return false;

        float horizontalDist = Mathf.Abs(_player.position.x - transform.position.x);
        if (horizontalDist > _detectionRadius) return false;

        float verticalDist = Mathf.Abs(_player.position.y - GetBoundsCenterY());
        float vTolerance = _col ? (_col.bounds.extents.y + 0.25f) : 0.6f;
        if (verticalDist > vTolerance) return false;

        Vector2 dir = _player.position.x >= transform.position.x ? Vector2.right : Vector2.left;
        Vector2 origin = new Vector2(GetFrontX(), GetBoundsCenterY());

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, _detectionRadius, ~0);
        return hit.collider && hit.collider.CompareTag("Player");
    }

    private void BeginAttack()
    {
        ChangeState(State.Attack);
        _attackUntil = Time.time + _attackDuration;
    }

    private void EndAttack()
    {
        ChangeState(State.Dizzy);
        _stateUntil = Time.time + _dizzyDuration;

        _nextAttackTime = Time.time + _attackCooldown;
    }

    private void DoPatrolMovement()
    {
        if (WallAhead() || !GroundAhead()) FaceRight(!_facingRight);

        float target = (_facingRight ? 1f : -1f)*_patrolSpeed;
        float vx = Mathf.MoveTowards(Rb.linearVelocity.x, target, _acceleration*Time.fixedDeltaTime);
        Rb.linearVelocity = new Vector2(vx, Rb.linearVelocity.y);
    }

    private bool IsGrounded()
    {
        if (!_col)
            _col = GetComponent<Collider2D>();

        Vector2 pos = transform.position;
        float cast = Mathf.Max(0.02f, _groundCheckDistance);
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(pos.x, GetBoundsMinY() + 0.02f), Vector2.down, cast, _groundMask);
        return hit.collider;
    }

    private bool GroundAhead()
    {
        Vector2 origin = new Vector2(GetFrontX(), GetBoundsMinY() + 0.05f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, _ledgeCheckDistance, _groundMask);
        return hit.collider;
    }

    private bool WallAhead()
    {
        Vector2 dir = _facingRight ? Vector2.right : Vector2.left;
        Vector2 origin = new Vector2(GetFrontX(), GetBoundsCenterY());
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, _wallCheckDistance, _groundMask);
        return hit.collider;
    }

    private bool PlayerAhead()
    {
        if (!_player) return false;
        Vector2 dir = _facingRight ? Vector2.right : Vector2.left;
        Vector2 origin = new Vector2(GetFrontX(), GetBoundsCenterY());
        float dist = Mathf.Max(_wallCheckDistance, 0.35f);
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, ~0);
        if (hit.collider && hit.collider.CompareTag("Player"))
            return true;
        return false;
    }

    private void FaceRight(bool right)
    {
        _facingRight = right;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x)*(right ? 1f : -1f);
        transform.localScale = scale;
    }

    private float GetBoundsMinY()
    {
        if (_col)
            return _col.bounds.min.y;
        return transform.position.y - 0.5f;
    }

    private float GetBoundsCenterY()
    {
        if (_col)
            return _col.bounds.center.y;
        return transform.position.y;
    }

    private float GetFrontX()
    {
        float cx = transform.position.x;
        float ext = 0.4f;
        if (_col)
            ext = _col.bounds.extents.x + 0.02f;
        return cx + (_facingRight ? ext : -ext);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_state != State.Attack)
            return;

        if (collision.collider.CompareTag("Player") || ((_groundMask.value & (1 << collision.collider.gameObject.layer)) != 0))
            EndAttack();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);

        Gizmos.color = Color.red;
        Vector2 dir = _facingRight ? Vector2.right : Vector2.left;
        Vector3 w0 = new(GetFrontX(), GetBoundsCenterY(), 0f);
        Gizmos.DrawLine(w0, w0 + new Vector3(dir.x, dir.y, 0f)*_wallCheckDistance);

        Gizmos.color = Color.cyan;
        Vector3 l0 = new(GetFrontX(), GetBoundsMinY() + 0.05f, 0f);
        Gizmos.DrawLine(l0, l0 + Vector3.down*_ledgeCheckDistance);
    }
}
