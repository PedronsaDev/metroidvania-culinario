using System;
using NaughtyAttributes;
using TheBlackCat.TrailEffect2D;
using UnityEngine;

public class PlayerPowerController : MonoBehaviour
{
    [Header("Equipped (Read Only)")]
    [SerializeField, ReadOnly] private PowerDefinition _equipped;

    [Header("Start Equipped (Optional)")]
    [SerializeField] private PowerDefinition _startEquipped;

    private PlayerMovement _movement;
    private PlayerHealth _health;
    private PlayerAttack _attack;
    private TrailInstance _trail;
    private Rigidbody2D _rb;
    private PowerRuntime _runtime;
    private PlayerAnimationController _animation;

    public event Action<PowerDefinition> PowerEquipped;
    public event Action<PowerDefinition> PowerUnequipped;

    public PlayerMovement Movement => _movement ? _movement : (_movement = GetComponent<PlayerMovement>());
    public PlayerHealth Health => _health ? _health : (_health = GetComponent<PlayerHealth>());
    public PlayerAttack Attack => _attack ? _attack : (_attack = GetComponent<PlayerAttack>());
    public TrailInstance Trail => _trail ? _trail : (_trail = GetComponentInChildren<TrailInstance>());
    public Rigidbody2D Rb => _rb ? _rb : (_rb = GetComponent<Rigidbody2D>());
    public PlayerAnimationController Animation => _animation ? _animation : (_animation = GetComponent<PlayerAnimationController>());

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _health = GetComponent<PlayerHealth>();
        _attack = GetComponent<PlayerAttack>();
        _trail = GetComponentInChildren<TrailInstance>();
        _rb = GetComponent<Rigidbody2D>();
        _animation = GetComponent<PlayerAnimationController>();
    }

    private void Start()
    {
        if (_startEquipped)
            Equip(_startEquipped);

        PowerRuntime.OnPowerEnded += Unequip;
    }

    private void OnDestroy()
    {
        PowerRuntime.OnPowerEnded -= Unequip;
    }

    private void Update()
    {
        _runtime?.Tick();
    }

    private void FixedUpdate()
    {
        _runtime?.FixedTick();
    }

    private void Equip(PowerDefinition def)
    {
        _runtime = def.CreateRuntime(this);
        _equipped = def;
        def.OnEquip(this);
        _runtime.OnEquip();
        PowerEquipped?.Invoke(def);
    }

    private void Unequip()
    {
        if (_runtime != null)
        {
            _runtime.OnUnequip();
            _runtime = null;
        }

        _equipped?.OnUnequip(this);
        PowerUnequipped?.Invoke(_equipped);
        _equipped = null;
    }
}
