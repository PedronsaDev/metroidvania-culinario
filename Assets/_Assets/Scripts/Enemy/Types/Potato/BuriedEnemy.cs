using UnityEngine;
using NaughtyAttributes;

[RequireComponent(typeof(Rigidbody2D))]
public class BuriedEnemy : EnemyBase
{
    private enum State { Buried, Emerging, Chasing, Burying }

    [Header("Detection (Player Overhead)")]
    [SerializeField] private float _triggerWidth = 2.0f; // horizontal half-width of overhead trigger
    [SerializeField] private float _triggerHeight = 1.25f; // vertical height above enemy to detect player
    [SerializeField] private float _verticalTriggerOffset = 0.2f; // vertical offset above center for base of trigger

    [Header("Emerging / Burying")]
    [SerializeField, Min(0f)] private float _emergeTime = 0.4f;
    [SerializeField, Min(0f)] private float _buryTime = 0.45f;
    [SerializeField] private Transform _visualRoot; // sprite root
    [SerializeField] private bool _disableCollidersWhenBuried = true;
    [SerializeField] private bool _pauseSimulationWhenBuried = true; // new: stop physics so enemy doesn't fall
    [SerializeField] private float _buriedYOffset = -0.35f; // sink while buried

    [Header("Chase Movement")]
    [SerializeField, Min(0f)] private float _chaseSpeed = 3.6f;
    [SerializeField, Min(0f)] private float _acceleration = 14f;
    [SerializeField, Min(0f)] private float _maxChaseTime = 6f;
    [SerializeField, Min(0f)] private float _loseSightDistance = 7.5f;

    [Header("Other")]
    [SerializeField] private Collider2D _col;
    [SerializeField] private Animator _anim;

    [Foldout("Animation Params"), SerializeField] private string _animBuried = "Buried";
    [Foldout("Animation Params"), SerializeField] private string _animEmerging = "Emerging";
    [Foldout("Animation Params"), SerializeField] private string _animChasing = "Chasing";
    [Foldout("Animation Params"), SerializeField] private string _animBurying = "Burying";

    private State _state = State.Buried;
    private float _stateUntil; // Emerging/Burying end timer
    private float _chaseUntil; // chase max time

    private Transform _player;
    private Rigidbody2D _rb;
    private Collider2D[] _allColliders;
    private bool _facingRight = true; // now used to prevent redundant scale flips
    private Vector3 _visualRootDefaultLocalPos;
    private float _originalGravityScale; // store to restore after emerge

    protected override void Awake()
    {
        base.Awake();
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _rb = GetComponent<Rigidbody2D>();
        _originalGravityScale = _rb.gravityScale; // capture original gravity
        if (!_col) _col = GetComponent<Collider2D>();
        if (!_visualRoot) _visualRoot = transform;
        _visualRootDefaultLocalPos = _visualRoot.localPosition;

        _allColliders = GetComponentsInChildren<Collider2D>(true);

        ApplyBuriedVisual();
        SetAnimator(State.Buried);
    }

    protected override void Start()
    {
        base.Start();
        ChangeState(State.Buried);
    }

    private void Update()
    {
        if (!_player)
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;

        switch (_state)
        {
            case State.Buried:
                if (_player && PlayerOverhead()) BeginEmerge();
                break;
            case State.Emerging:
                if (Time.time >= _stateUntil) BeginChase();
                break;
            case State.Chasing:
                if (ShouldStopChase()) BeginBury();
                break;
            case State.Burying:
                if (Time.time >= _stateUntil) ChangeState(State.Buried);
                break;
        }
    }

    private void FixedUpdate()
    {
        if (_state == State.Chasing && _player)
        {
            float target = Mathf.Sign(_player.position.x - transform.position.x) * _chaseSpeed;
            float vx = Mathf.MoveTowards(_rb.linearVelocity.x, target, _acceleration * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector2(vx, _rb.linearVelocity.y);
            if (_rb.linearVelocity.x > 0.05f) FaceRight(true); else if (_rb.linearVelocity.x < -0.05f) FaceRight(false);
        }
        else
        {
            float vx = Mathf.MoveTowards(_rb.linearVelocity.x, 0f, _acceleration * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector2(vx, _rb.linearVelocity.y);
        }
    }

    protected override void OnDamageApplied(Vector3 hitDirection, int damage)
    {
        base.OnDamageApplied(hitDirection, damage);
        if (_state == State.Buried) BeginEmerge();
        else if (_state == State.Burying) BeginChase();
        else if (_state == State.Emerging) BeginChase();
    }

    private void BeginEmerge()
    {
        ChangeState(State.Emerging);
        _stateUntil = Time.time + _emergeTime;
        // Stay buried during emerge delay - only fully emerge when transitioning to chase
    }

    private void BeginChase()
    {
        ChangeState(State.Chasing);
        _chaseUntil = Time.time + _maxChaseTime;
        ApplyEmergedVisual(); // Now fully emerge and enable physics/colliders
    }

    private void BeginBury()
    {
        ChangeState(State.Burying);
        _stateUntil = Time.time + _buryTime;
    }

    private bool ShouldStopChase()
    {
        if (!_player) return true;
        if (Time.time >= _chaseUntil) return true;
        if (Vector2.Distance(_player.position, transform.position) > _loseSightDistance) return true;
        return false;
    }

    private bool PlayerOverhead()
    {
        if (!_player) return false;
        float centerY = GetBoundsCenterY();
        float centerX = transform.position.x;
        float px = _player.position.x; float py = _player.position.y;
        if (Mathf.Abs(px - centerX) > _triggerWidth * 0.5f) return false;
        float baseY = centerY + _verticalTriggerOffset;
        if (py < baseY) return false; // must be above
        if (py > baseY + _triggerHeight) return false; // too high
        return true;
    }

    private void ChangeState(State s)
    {
        _state = s;
        SetAnimator(s);

        if (s == State.Buried)
            ApplyBuriedVisual();
    }

    private void ApplyBuriedVisual()
    {
        if (_visualRoot)
        {
            Vector3 lp = _visualRootDefaultLocalPos; lp.y += _buriedYOffset; _visualRoot.localPosition = lp;
        }
        // Physics pause so we don't fall through ground while colliders disabled.
        if (_pauseSimulationWhenBuried && _rb)
        {
            _rb.gravityScale = 0f;
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = false; // fully pause physics interactions
        }
        if (_disableCollidersWhenBuried && _allColliders != null)
        {
            foreach (var c in _allColliders)
                if (c && c != _col) // keep root collider (optional) enabled to preserve ground position
                    c.enabled = false;
        }
    }
    private void ApplyEmergedVisual()
    {
        if (_visualRoot) _visualRoot.localPosition = _visualRootDefaultLocalPos;
        if (_pauseSimulationWhenBuried && _rb)
        {
            _rb.simulated = true;
            _rb.gravityScale = _originalGravityScale; // restore gravity
        }

        if (_disableCollidersWhenBuried && _allColliders != null)
        {
            foreach (var c in _allColliders) if (c) c.enabled = true;
        }
    }
    protected override void Die()
    {
        // Ensure physics restored for death effects
        if (_pauseSimulationWhenBuried && _rb)
        {
            _rb.simulated = true;
            _rb.gravityScale = _originalGravityScale;
        }
        if (_disableCollidersWhenBuried && _allColliders != null)
            foreach (var c in _allColliders) if (c) c.enabled = true;
        base.Die();
    }

    private void FaceRight(bool right)
    {
        if (_facingRight == right) return; // avoid unnecessary scale changes
        _facingRight = right;
        Vector3 s = transform.localScale; s.x = Mathf.Abs(s.x) * (right ? 1f : -1f); transform.localScale = s;
    }
    private float GetBoundsCenterY()
    {
        if (_col) return _col.bounds.center.y; return transform.position.y;
    }

    private void SetAnimator(State s)
    {
        if (!_anim) return;
        if (!string.IsNullOrEmpty(_animBuried)) _anim.SetBool(_animBuried, s == State.Buried);
        if (!string.IsNullOrEmpty(_animEmerging)) _anim.SetBool(_animEmerging, s == State.Emerging);
        if (!string.IsNullOrEmpty(_animChasing)) _anim.SetBool(_animChasing, s == State.Chasing);
        if (!string.IsNullOrEmpty(_animBurying)) _anim.SetBool(_animBurying, s == State.Burying);
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0.8f, 0.5f);
        float centerY = _col ? _col.bounds.center.y : transform.position.y;
        Vector3 center = new Vector3(transform.position.x, centerY + _verticalTriggerOffset + _triggerHeight * 0.5f, 0f);
        Vector3 size = new Vector3(_triggerWidth, _triggerHeight, 0.1f);
        Gizmos.DrawWireCube(center, size);
    }
}
