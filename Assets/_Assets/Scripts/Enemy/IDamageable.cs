
public interface IDamageable
{
    float Health { get; set; }
    float MaxHealth { get; set; }

    void SetHealth(float amount)
    {
        Health = amount;
    }

    void CheckHealth()
    {
        if (Health > MaxHealth)
            Health = MaxHealth;
        else if (Health <= 0)
            Die();
    }

    void TakeDamage(float amount);

    void Heal(float amount);

    bool IsDead();

    void Die();
}
