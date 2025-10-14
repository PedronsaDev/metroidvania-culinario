using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BeeEnemy : EnemyBase
{
    [Header("Movement")]
    [SerializeField] private float _patrolSpeed = 2f;
    [SerializeField] private float _chaseSpeed = 3f;
    [SerializeField] private float _acceleration = 12f;
    [SerializeField] private float _hoverAmplitude = 0.5f;
    [SerializeField] private float _hoverFrequency = 2f;
    [SerializeField] private float _verticalFollowGain = 6f;
    [SerializeField] private float _moveRange = 5f;
    [SerializeField] private float _detectionRange = 6f;
    [SerializeField] private float _stopDistance = 1.5f;

    [Header("Ataque")]
    private readonly float _attackRange = 2f;
    private readonly float _attackCooldown = 2f;
    private GameObject _lancaHitbox;

    [Header("Knockback/Hit")]
    private readonly float _knockbackPauseDuration = 0.2f;

    private Vector2 _startPos;
    private bool _movingRight = true;
    private Animator _animator;
    private Transform _player;
    private float _lastAttackTime;

    private float _baseY;
    private float _hoverPhaseOffset;
    private float _stunUntil;
    private bool _prevWasHit;

    private static readonly int AnimVoando = Animator.StringToHash("Voando");
    private static readonly int AnimAtacar = Animator.StringToHash("Atacar");

    protected override void Start()
    {
        base.Start();

        _startPos = transform.position;
        _baseY = _startPos.y;
        _hoverPhaseOffset = Random.Range(0f, Mathf.PI*2f);

        _animator = GetComponentInChildren<Animator>();
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        Rb = GetComponent<Rigidbody2D>();

        if (_lancaHitbox)
            _lancaHitbox.SetActive(false);

        _animator?.SetBool(AnimVoando, true);
    }

    void Update()
    {
        if (!_player)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found) _player = found.transform;
        }

        if (!_prevWasHit && WasHit)
            _stunUntil = Mathf.Max(_stunUntil, Time.time + _knockbackPauseDuration);

        _prevWasHit = WasHit;

        float distanceToPlayer = _player ? Vector2.Distance(transform.position, _player.position) : Mathf.Infinity;

        if (_player && distanceToPlayer <= _attackRange && Time.time >= _lastAttackTime + _attackCooldown && Time.time >= _stunUntil)
        {
            Attack();
        }
    }

    void FixedUpdate()
    {
        if (Time.time < _stunUntil)
            return;

        bool canSeePlayer = _player && Vector2.Distance(Rb.position, _player.position) <= _detectionRange;

        if (canSeePlayer)
            DoChase();
        else
            DoPatrol();

        DoHoverVertical();

        if (Rb.linearVelocity.x > 0.05f)
            FaceRight(true);
        else if (Rb.linearVelocity.x < -0.05f)
            FaceRight(false);
    }

    void DoPatrol()
    {
        float left = _startPos.x - _moveRange;
        float right = _startPos.x + _moveRange;
        float targetSpeed = (_movingRight ? 1f : -1f)*_patrolSpeed;

        if (_movingRight && Rb.position.x >= right)
            _movingRight = false;
        else if (!_movingRight && Rb.position.x <= left)
            _movingRight = true;

        float vx = Mathf.MoveTowards(Rb.linearVelocity.x, targetSpeed, _acceleration*Time.fixedDeltaTime);
        Rb.linearVelocity = new Vector2(vx, Rb.linearVelocity.y);
    }

    void DoChase()
    {
        Vector2 toPlayer = (_player.position - transform.position);
        float dir = Mathf.Sign(toPlayer.x);
        float distX = Mathf.Abs(toPlayer.x);

        float targetSpeed = 0f;
        if (distX > _stopDistance)
            targetSpeed = dir*_chaseSpeed;
        else if (distX < _stopDistance*0.8f)
            targetSpeed = -dir*(_chaseSpeed*0.5f);

        float vx = Mathf.MoveTowards(Rb.linearVelocity.x, targetSpeed, _acceleration*Time.fixedDeltaTime);
        Rb.linearVelocity = new Vector2(vx, Rb.linearVelocity.y);
    }

    void DoHoverVertical()
    {
        float targetY = _baseY + Mathf.Sin(Time.time*_hoverFrequency + _hoverPhaseOffset)*_hoverAmplitude;
        float vy = (targetY - Rb.position.y)*_verticalFollowGain;
        Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, vy);
    }

    void Attack()
    {
        _lastAttackTime = Time.time;
        _animator?.SetTrigger(AnimAtacar);
    }

    public void EnableLancaHitbox()
    {
        if (_lancaHitbox)
            _lancaHitbox.SetActive(true);
    }

    public void DisableLancaHitbox()
    {
        if (_lancaHitbox)
            _lancaHitbox.SetActive(false);
    }

    void FaceRight(bool right)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x)*(right ? 1f : -1f);
        transform.localScale = scale;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
        Gizmos.color = Color.cyan;
        Vector3 left = new Vector3((Application.isPlaying ? _startPos.x : transform.position.x) - _moveRange, transform.position.y, 0f);
        Vector3 right = new Vector3((Application.isPlaying ? _startPos.x : transform.position.x) + _moveRange, transform.position.y, 0f);
        Gizmos.DrawLine(left, right);
    }
}
