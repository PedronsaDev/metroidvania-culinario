using System;
using UnityEngine;
using NaughtyAttributes;


[RequireComponent(typeof(DamageFlash))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _maxHealth = 5;

    [Header("I-Frames")]
    [SerializeField, Range(0.05f, 2f)] private float _postHitInvuln = 0.6f;
    [SerializeField] private bool _flashDuringIFrames = true;

    [Header("Recoil")]
    [SerializeField] private float _horizontalHitRecoilSpeed = 14f;
    [SerializeField] private float _horizontalHitVerticalBoost = 6f;
    [SerializeField] private float _horizontalHitRecoilDuration = 0.12f;
    [SerializeField] private float _verticalBounceVelocity = 23f;
    [SerializeField] private float _verticalBounceHorizontalNudge = 6f;
    [SerializeField] private float _verticalBounceRecoilDuration = 0.08f;
    [SerializeField, Range(1f, 3f)] private float _minSeparationPushMultiplier = 1.6f;
    [SerializeField, Range(0f, 1f)] private float _tinySeparationThreshold = 0.15f;

    [Header("(Read Only)")]
    [SerializeField, ReadOnly] private bool _invulnerable;
    [SerializeField, ReadOnly] private bool _dead;
    [SerializeField, ReadOnly] private float _invulnRemaining;

    private PlayerMovement _movement;
    private float _invulnTimer;
    private SpriteRenderer[] _renderers;
    private Color[] _originalColors;

    public event Action Damaged;
    public event Action<int, int> HealthChanged;
    public event Action Death;
    public event Action IFramesStarted;
    public event Action IFramesEnded;

    public bool Invulnerable => _invulnTimer > 0f;
    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        CacheRenderers();
        SetHealth(_maxHealth);
    }

    private void Update()
    {
        TickInvulnerability();
    }

    private void CacheRenderers()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            _originalColors[i] = _renderers[i].color;
    }

    private void SetHealth(int health)
    {
        _currentHealth = Mathf.Clamp(health, 0, _maxHealth);
        CheckHealth();
        HealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    public void TakeDamage(int damage, Transform source = null)
    {
        if (damage <= 0)
            return;

        if (_dead)
            return;

        if (Invulnerable)
            return;

        _currentHealth -= damage;
        HealthChanged?.Invoke(_currentHealth, _maxHealth);
        CameraManager.Instance?.ShakeCamera(1f);

        StartInvulnerability();
        Damaged?.Invoke();

        if (source && _movement)
            ApplyHitRecoil(source.position);

        CheckHealth();
    }

    private void StartInvulnerability()
    {
        _invulnTimer = _postHitInvuln;
        _invulnerable = true;
        _invulnRemaining = _invulnTimer;
        IFramesStarted?.Invoke();
    }

    private void EndInvulnerability()
    {
        _invulnTimer = 0f;
        _invulnerable = false;
        _invulnRemaining = 0f;
        IFramesEnded?.Invoke();
        RestoreRendererColors();
    }

    private void TickInvulnerability()
    {
        if (_invulnTimer <= 0f) return;
        _invulnTimer -= Time.deltaTime;
        _invulnRemaining = Mathf.Max(0f, _invulnTimer);

        if (_invulnTimer <= 0f)
        {
            EndInvulnerability();
            return;
        }
    }

    private void RestoreRendererColors()
    {
        if (_renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i])
                _renderers[i].color = _originalColors[i];
        }
    }

    private void CheckHealth()
    {
        if (_currentHealth <= 0 && !_dead)
            Die();
        else if (_currentHealth > _maxHealth)
            _currentHealth = _maxHealth;
    }

    private void Die()
    {
        if (_dead) return;
        _dead = true;
        EndInvulnerability();
        Death?.Invoke();
    }

    private void ApplyHitRecoil(Vector2 sourcePosition)
    {
        if (!_movement) return;

        Vector2 toPlayer = (Vector2)transform.position - sourcePosition;
        if (toPlayer.sqrMagnitude < 0.0001f)
            toPlayer = _movement.FacingRight ? Vector2.right : Vector2.left;

        float absX = Mathf.Abs(toPlayer.x);
        float absY = Mathf.Abs(toPlayer.y);

        bool verticalBounce = absY > absX && toPlayer.y > 0f;
        if (verticalBounce)
        {
            float horizontalSign = Mathf.Sign(toPlayer.x);
            if (Mathf.Abs(toPlayer.x) < _tinySeparationThreshold)
            {
                horizontalSign = _movement.FacingRight ? 1f : -1f;
                horizontalSign *= _minSeparationPushMultiplier;
            }

            Vector2 recoil = new Vector2(horizontalSign*_verticalBounceHorizontalNudge, _verticalBounceVelocity);
            _movement.ApplyRecoil(recoil, _verticalBounceRecoilDuration, overrideX: true, overrideY: true);
        }
        else
        {
            float horizontalSign = Mathf.Sign(toPlayer.x);
            if (horizontalSign == 0f)
                horizontalSign = _movement.FacingRight ? -1f : 1f;

            float hSpeed = _horizontalHitRecoilSpeed;
            if (Mathf.Abs(toPlayer.x) < _tinySeparationThreshold)
                hSpeed *= _minSeparationPushMultiplier;

            float yVel = Mathf.Max(_movement.VerticalSpeed, _horizontalHitVerticalBoost);
            Vector2 recoil = new Vector2(horizontalSign*hSpeed, yVel);
            _movement.ApplyRecoil(recoil, _horizontalHitRecoilDuration, overrideX: true, overrideY: true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_dead || Invulnerable) return;

        DamageDealer dealer = other.GetComponent<DamageDealer>();
        EnemyBase enemy = null;

        if (!dealer)
        {
            dealer = other.GetComponentInParent<DamageDealer>();
        }
        if (!dealer)
        {
            other.gameObject.TryGetComponent<EnemyBase>(out enemy);
            if (!enemy)
                enemy = other.GetComponentInParent<EnemyBase>();
        }

        if (!dealer && !enemy)
            return;

        int dmg = 1;
        if (dealer)
            dmg = dealer.Damage;
        else
        {
            dmg = 1;
        }

        Transform source = dealer ? dealer.SourceTransform : (enemy ? enemy.transform : other.transform);
        TakeDamage(dmg, source);
    }
}
