using _Assets.Scripts.Drops;
using UnityEngine;

[RequireComponent(typeof(Dropper))]
public class EnemyBase : Damageable
{
    private Dropper _dropper;

    protected override void Awake()
    {
        base.Awake();
        _dropper = GetComponent<Dropper>();
    }

    protected override void Die()
    {
        _dropper.DropNow();
        base.Die();
    }
}
