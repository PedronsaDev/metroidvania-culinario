using UnityEngine;

[RequireComponent(typeof(DamageFlash))]
public class Damageable : MonoBehaviour, IHittable
{
    [Header("Pogo Settings")]
    [SerializeField] private bool _giveUpwardForce;
    [SerializeField] private float _upwardForce = 22f;

    public bool GiveUpwardForce { get => _giveUpwardForce; set => _giveUpwardForce = value; }
    public bool WasHit { get; set; }
    public float UpwardForce { get => _upwardForce; set => _upwardForce = value; }

    [SerializeField] protected int _currentHealth;
    [SerializeField] protected int _maxHealth = 3;
    [SerializeField] protected float _invincibilityDuration = 0.2f;

    private DamageFlash _damageFlash;


    protected virtual void Awake()
    {
        _damageFlash = GetComponent<DamageFlash>();
    }

    private void Start()
    {
        _currentHealth = _maxHealth;
    }

    public void Hit(Vector3 hitPoint, Vector3 hitDirection, int damage = 1) => TakeDamage(damage);

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
            }
        }
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
