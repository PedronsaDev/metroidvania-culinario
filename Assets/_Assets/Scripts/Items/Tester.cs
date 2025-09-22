using _Assets.Scripts.Drops;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tester : MonoBehaviour, IDamageable, IPointerDownHandler
{
    [SerializeField] private float _health = 100;
    [SerializeField] private float _maxHealth = 100;

    private Dropper _dropper;

    public float Health { get; set; }
    public float MaxHealth { get; set; }

    private void Start()
    {
        Health = _health;
        MaxHealth = _maxHealth;
        _dropper = GetComponent<Dropper>();
    }
    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            _dropper.TryDrop();
    }

    public void TakeDamage(float amount)
    {
        Health -= amount;
        if (IsDead())
            Die();

        Debug.Log("Took Damage, current health: " + Health);
    }

    public void Heal(float amount)
    {
        Health += amount;
        if (Health > MaxHealth)
            Health = MaxHealth;
    }

    public bool IsDead() => Health <= 0;

    public void Die() => Destroy(this);

    public void OnPointerDown(PointerEventData eventData)
    {
        TakeDamage(20);
        _dropper.TryDrop();
    }
}
