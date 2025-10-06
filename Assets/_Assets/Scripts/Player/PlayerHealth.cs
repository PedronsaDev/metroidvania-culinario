using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _maxHealth = 5;

    private PlayerMovement _movement;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();

        SetHealth(_maxHealth);
    }

    private void SetHealth(int health)
    {
        _currentHealth = Mathf.Clamp(health, 0, _maxHealth);
        CheckHealth();
    }

    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        CheckHealth();
        CameraManager.Instance.ShakeCamera(1f);
    }

    private void CheckHealth()
    {
        if (_currentHealth <= 0)
            Die();
        else if(_currentHealth > _maxHealth)
            _currentHealth = _maxHealth;
    }

    private void Die()
    {

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Triggered with " + other.gameObject.name);

        if (other.gameObject.TryGetComponent<EnemyBase>( out var enemy))
        {
            Vector2 direction = other.transform.position - transform.position;
            if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
            {
                if (direction.y < 0)
                    _movement.ApplyRecoil(new Vector2(_movement.HorizontalSpeed, 23f), 0.04f, overrideX: false, overrideY: true);
            }
            else
            {
                if (direction.x > 0)
                    Debug.Log("Enemy is to the right");
                else
                    Debug.Log("Enemy is to the left");
            }

            TakeDamage(1);
        }
    }
}
