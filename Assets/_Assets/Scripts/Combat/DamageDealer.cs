using UnityEngine;
using NaughtyAttributes;

public class DamageDealer : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField, Min(0)] private int _damage = 1;
    [SerializeField] private bool _oneShot;
    [SerializeField, Min(0f)] private float _repeatCooldown = 0.5f;

    [Header("Continuous")]
    [SerializeField] private bool _continuous;

    [Header("Source Override")]
    [SerializeField] private Transform _customSource;

    [Header("Filtering")]
    [SerializeField] private LayerMask _targetLayers = ~0;

    [Header("Debug (Read Only)")]
    [ReadOnly] [SerializeField] private bool _consumed;

    private float _lastTickTime = -999f;

    public int Damage => _damage;
    public Transform SourceTransform => _customSource ? _customSource : transform;
    public bool Consumed => _consumed;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_oneShot && _consumed) return;
        if (!TargetLayerMatches(other.gameObject.layer)) return;

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            if (!damageable.Invulnerable)
            {
                damageable.TakeDamage(_damage, SourceTransform);
                _lastTickTime = Time.time;
                if (_oneShot) _consumed = true;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!_continuous || _oneShot && _consumed) return;
        if (!TargetLayerMatches(other.gameObject.layer)) return;

        if (Time.time < _lastTickTime + _repeatCooldown) return;

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            if (!damageable.Invulnerable)
            {
                damageable.TakeDamage(_damage, SourceTransform);
                _lastTickTime = Time.time;
                if (_oneShot)
                    _consumed = true;
            }
        }
    }

    private bool TargetLayerMatches(int layer)
    {
        return (_targetLayers.value & (1 << layer)) != 0;
    }
}
