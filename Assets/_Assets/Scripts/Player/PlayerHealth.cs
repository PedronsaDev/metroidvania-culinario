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

    [Header("(Read Only)")]
    [SerializeField, ReadOnly] private bool _invulnerable;
    [SerializeField, ReadOnly] private bool _dead;
    [SerializeField, ReadOnly] private float _invulnRemaining;

    private PlayerMovement _movement;
    private PlayerRecoil _recoil;
    private float _invulnTimer;
    private SpriteRenderer[] _renderers;
    private DamageFlash _damageFlash;

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
        _recoil = GetComponent<PlayerRecoil>();
        _damageFlash = GetComponent<DamageFlash>();
        SetHealth(_maxHealth);
    }

    private void Update()
    {
        TickInvulnerability();
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
        _damageFlash.Flash();

        HitPause.Instance?.Do(0.06f);

        Damaged?.Invoke();
        if (source && _recoil)
            _recoil.ApplyHitRecoilFromSource(source.position);

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

    private void CheckHealth()
    {
        if (_currentHealth <= 0 && !_dead)
            Die();
        else if (_currentHealth > _maxHealth)
            _currentHealth = _maxHealth;
    }

    private void Die()
    {
        if (_dead)
            return;
        _dead = true;
        EndInvulnerability();

        HitPause.Instance?.Do(0.12f);

        Death?.Invoke();
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
        dmg = dealer ? dealer.Damage : 1;

        Transform source = dealer ? dealer.SourceTransform : (enemy ? enemy.transform : other.transform);
        TakeDamage(dmg, source);
    }
}
