using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage, Transform source = null);

    bool Invulnerable { get; }
}

