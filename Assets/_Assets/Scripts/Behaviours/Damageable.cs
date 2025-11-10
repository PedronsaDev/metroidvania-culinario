using NaughtyAttributes;
using UnityEngine;

[RequireComponent(typeof(DamageFlash))]
public class Damageable : MonoBehaviour, IHittable
{
    [Header("Pogo Settings")]
    [SerializeField] private bool _giveUpwardForce;
    [SerializeField] private float _upwardForce = 30f;

    public bool GiveUpwardForce { get => _giveUpwardForce; set => _giveUpwardForce = value; }
    public bool WasHit { get; set; }
    public float UpwardForce { get => _upwardForce; set => _upwardForce = value; }

    [SerializeField, ReadOnly] protected int _currentHealth;
    [SerializeField] protected int _maxHealth = 3;
    [SerializeField] protected float _invincibilityDuration = 0.2f;

    [Header("Knockback Settings")]
    [SerializeField] private bool _enableKnockback = true;
    [SerializeField, Min(0f), ShowIf("_enableKnockback")] private float _knockbackForce = 8f;
    [SerializeField, ShowIf("_enableKnockback")] private ForceMode2D _knockbackForceMode = ForceMode2D.Impulse;
    [SerializeField, ShowIf("_enableKnockback")] private bool _searchRigidbodyInParents = true;
    [SerializeField, Min(0f), ShowIf("_enableKnockback")] private float _minUpwardComponent;

    private DamageFlash _damageFlash;
    protected Rigidbody2D Rb;


    protected virtual void Awake()
    {
        _damageFlash = GetComponent<DamageFlash>();
        Rb = GetComponent<Rigidbody2D>();
        if (!Rb && _searchRigidbodyInParents)
            Rb = GetComponentInParent<Rigidbody2D>();
    }

    protected virtual void Start()
    {
        _currentHealth = _maxHealth;
    }

    public void Hit(Vector3 hitPoint, Vector3 hitDirection, int damage = 1) => TakeDamage(damage, hitDirection);

    public Transform GetTransform() => this.transform;

    public virtual void TakeDamage(int damage)
    {
        if (!WasHit && _currentHealth > 0)
        {
            _currentHealth -= damage;
            if (_currentHealth <= 0)
            {
                Die();
            }
            else
            {
                WasHit = true;
                Invoke(nameof(ResetCanBeHit), _invincibilityDuration);
                _damageFlash.Flash();
                OnDamageApplied(Vector3.zero, damage);
            }
        }
    }

    public virtual void TakeDamage(int damage, Vector3 hitDirection)
    {
        if (_currentHealth <= 0)
            return;

        bool applied = false;
        if (!WasHit)
        {
            _currentHealth -= damage;
            if (_currentHealth <= 0)
            {
                Die();
                return;
            }
            else
            {
                WasHit = true;
                Invoke(nameof(ResetCanBeHit), _invincibilityDuration);
                _damageFlash.Flash();
                applied = true;
            }
        }

        if (applied)
        {
            OnDamageApplied(hitDirection, damage);
            ApplyKnockback(hitDirection);
        }
    }

    protected virtual void OnDamageApplied(Vector3 hitDirection, int damage) { }

    protected virtual void ApplyKnockback(Vector3 hitDirection)
    {
        if (!_enableKnockback)
            return;

        if (!Rb)
        {
            Rb = GetComponent<Rigidbody2D>();
            if (!Rb && _searchRigidbodyInParents)
                Rb = GetComponentInParent<Rigidbody2D>();
        }
        if (!Rb || !Rb || _knockbackForce <= 0f)
            return;

        Vector2 dir = new Vector2(hitDirection.x, hitDirection.y);
        if (dir.sqrMagnitude < 1e-6f)
        {
            dir = Vector2.right;
        }
        dir.Normalize();

        if (_minUpwardComponent > 0f)
        {
            if (dir.y < _minUpwardComponent)
            {
                dir.y = _minUpwardComponent;
                dir = dir.normalized;
            }
        }

        Rb.AddForce(dir * _knockbackForce, _knockbackForceMode);
    }

    protected virtual void ResetCanBeHit()
    {
        WasHit = false;
    }

    protected virtual void Die()
    {
        WasHit = true;
        _currentHealth = 0;
        this.enabled = false;
        this.gameObject.SetActive(false);
    }
}
