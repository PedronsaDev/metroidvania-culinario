using System;
using NaughtyAttributes;
using UnityEngine;

public class PlayerPowerController : MonoBehaviour
{

    private PlayerMovement _movement;
    private PlayerHealth _health;
    private PlayerAttack _attack;
    private Rigidbody2D _rb;

    [Header("Equipped (Read Only)")]
    [SerializeField, ReadOnly] private PowerDefinition _equipped;

    [Header("Start Equipped (Optional)")]
    [SerializeField] private PowerDefinition _startEquipped;

    public event Action<PowerDefinition> PowerEquipped;
    public event Action<PowerDefinition> PowerUnequipped;

    public PlayerMovement Movement => _movement ? _movement : (_movement = GetComponent<PlayerMovement>());
    public PlayerHealth Health => _health ? _health : (_health = GetComponent<PlayerHealth>());
    public PlayerAttack Attack => _attack ? _attack : (_attack = GetComponent<PlayerAttack>());
    public Rigidbody2D Rb => _rb ? _rb : (_rb = GetComponent<Rigidbody2D>());

    private PowerRuntime _runtime;

    private void Awake()
    {
        if (!_movement) _movement = GetComponent<PlayerMovement>();
        if (!_health) _health = GetComponent<PlayerHealth>();
        if (!_attack) _attack = GetComponent<PlayerAttack>();
        if (!_rb) _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (_startEquipped)
            Equip(_startEquipped);
    }
    private void Equip(PowerDefinition def)
    {
        _runtime = def.CreateRuntime(this);
        _equipped = def;

        _runtime.OnEquip();
    }

    private void Unequip()
    {
        if (_runtime != null)
        {
            _runtime.OnUnequip();
            _runtime = null;
        }

        _equipped = null;
    }


    private void Update()
    {
        _runtime?.Tick();
    }

    private void FixedUpdate()
    {
        _runtime?.FixedTick();
    }
}
