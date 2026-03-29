using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private PlayerRecoil _recoil;
    [SerializeField] private InputActionReference _attackRef;
    [SerializeField] private InputActionReference _moveRef;

    [Header("General")]
    [SerializeField, Range(0.02f, 0.6f)] private float _attackCooldown = 0.25f;
    [SerializeField] private int _damage = 1;
    [SerializeField] private LayerMask _hittableMask;

    [Header("Horizontal Slash Hitbox")]
    [SerializeField] private Vector2 _horizontalSize = new Vector2(1.4f, 1.0f);
    [SerializeField] private float _horizontalForwardOffset = 0.9f;

    [Header("Down (Pogo) Hitbox")]
    [SerializeField] private Vector2 _downSize = new Vector2(1.0f, 1.0f);
    [SerializeField] private float _downOffset = 0.9f;

    [SerializeField] private Vector2 _upSize = new Vector2(1.0f, 1.0f);
    [SerializeField] private float _upOffset = 0.9f;

    [Header("Attack Recoil Settings")]
    [SerializeField] private bool _respectTargetUpwardForceFlag = true;

    [Header("Debug")]
    [SerializeField] private bool _debugGizmos;
    [SerializeField, Range(0.01f, 0.5f)] private float _debugAttackVisualTime = 0.12f;

    [Header("Performance")]
    [SerializeField] private int _hitBufferCapacity = 8;

    [Header("Hit Filtering")]
    [SerializeField, Range(0f, 1f)] private float _verticalFilterTolerance = 0.05f;
    [SerializeField] private bool _useCenterForVerticalFilter = true;
    [SerializeField] private bool _skipVerticalFilter;

    [Header("Runtime Debug State (Read Only)")]
    [SerializeField, ReadOnly] private int _lastAcceptedCount;
    [SerializeField, ReadOnly] private int _lastRejectedCount;
    [SerializeField, ReadOnly] private int _lastEffectiveCount;
    [SerializeField, ReadOnly] private int _lastInvulnerableCount;

    private InputAction _attackAction;
    private InputAction _moveAction;
    private float _nextAttackTime;
    private bool _attackQueuedThisFrame;
    private Collider2D[] _hitBuffer;
    private ContactFilter2D _contactFilter;
    private bool _filterInitialized;

    private readonly List<Collider2D> _debugAccepted = new();
    private readonly List<Collider2D> _debugRejected = new();
    private readonly List<Collider2D> _debugInvulnerable = new();
    private bool _debugTruncated;

    public event Action AttackStarted;
    public event Action<bool> AttackHit;
    public event Action<bool, int> AttackResolved;

    private enum AttackDir { Left, Right, Down, Up }

    public enum VerticalFilterMode { None, Above, Below }

    public struct OverlapDamageResult
    {
        public bool AnyHit;
        public int AcceptedCount;
        public int RejectedCount;
        public int EffectiveCount;
        public int InvulnerableCount;
        public bool Truncated;
        public float MaxUpwardForce;
        public bool AnyGiveUpward;
    }

    private float _attackVisualTimer;
    private Vector2 _lastAttackCenter;
    private Vector2 _lastAttackSize;
    private bool _lastAttackValid;
    private bool _canAttack = true;

    private void Awake()
    {
        if (!_movement)
            _movement = GetComponent<PlayerMovement>();
        if (!_recoil)
            _recoil = GetComponent<PlayerRecoil>();

        if (_attackRef)
            _attackAction = _attackRef.action;

        if (_moveRef)
            _moveAction = _moveRef.action;

        AllocateBuffer();
    }

    private void OnEnable()
    {
        Enable(_attackAction, true);
        Enable(_moveAction, true);
        if (_attackAction != null)
            _attackAction.started += OnAttackStarted;
    }

    private void OnDisable()
    {
        if (_attackAction != null)
            _attackAction.started -= OnAttackStarted;
        Enable(_attackAction, false);
        Enable(_moveAction, false);
    }

    private static void Enable(InputAction action, bool on)
    {
        if (action == null) return;
        if (on && !action.enabled) action.Enable();
        else if (!on && action.enabled) action.Disable();
    }

    private void Update()
    {
        if (!_canAttack)
            return;

        _attackQueuedThisFrame = false;
        if (_attackVisualTimer > 0f)
        {
            _attackVisualTimer -= Time.deltaTime;
            if (_attackVisualTimer <= 0f)
                _lastAttackValid = false;
        }
    }

    private void OnAttackStarted(InputAction.CallbackContext ctx)
    {
        if (_attackQueuedThisFrame) return;
        _attackQueuedThisFrame = true;

        if (Time.time < _nextAttackTime) return;
        if (!_movement) return;

        Vector2 moveInput = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        bool tryDown = moveInput.y < -0.5f && !_movement.Grounded;
        bool tryUp = moveInput.y > 0.4f;

        AttackDir dir;

        if (tryDown)
            dir = AttackDir.Down;
        else if (tryUp)
            dir = AttackDir.Up;
        else
        {
            float hx = moveInput.x;

            if (Mathf.Abs(hx) > 0.2f)
                dir = hx > 0f ? AttackDir.Right : AttackDir.Left;
            else
                dir = _movement.FacingRight ? AttackDir.Right : AttackDir.Left;
        }

        PerformAttack(dir);
    }

    private void PerformAttack(AttackDir dir)
    {
        _nextAttackTime = Time.time + _attackCooldown;
        AttackStarted?.Invoke();
        _attackVisualTimer = _debugAttackVisualTime;

        _lastAttackValid = false;
        switch (dir)
        {
            case AttackDir.Down:
                DoDownAttack();
                break;
            case AttackDir.Right:
            case AttackDir.Left:
                DoHorizontalAttack(dir == AttackDir.Right);
                break;
            case AttackDir.Up:
                DoUpAttack();
                break;
        }
    }

    private void AllocateBuffer()
    {
        if (_hitBuffer == null || _hitBuffer.Length != _hitBufferCapacity)
            _hitBuffer = new Collider2D[_hitBufferCapacity];
    }

    private void EnsureContactFilter()
    {
        if (_filterInitialized) return;
        _contactFilter = new ContactFilter2D();
        _contactFilter.useLayerMask = true;
        _contactFilter.SetLayerMask(_hittableMask);
        _contactFilter.useTriggers = true;
        _filterInitialized = true;
    }

    private int OverlapBox(Vector2 center, Vector2 size)
    {
        if (_hitBuffer == null || _hitBuffer.Length == 0)
            AllocateBuffer();
        EnsureContactFilter();
        int count = Physics2D.OverlapBox(center, size, 0f, _contactFilter, _hitBuffer);
        return Mathf.Min(count, _hitBuffer.Length);
    }

    public OverlapDamageResult DealDamageInBox(Vector2 center, Vector2 size, VerticalFilterMode verticalMode = VerticalFilterMode.None, Vector3? fixedDirection = null, int? damageOverride = null)
    {
        return DealDamageInBoxInternal(transform.position, center, size, verticalMode, fixedDirection, damageOverride);
    }

    private OverlapDamageResult DealDamageInBoxInternal(Vector2 origin, Vector2 center, Vector2 size, VerticalFilterMode verticalMode, Vector3? fixedDirection, int? damageOverride)
    {
        if (_hitBuffer == null || _hitBuffer.Length == 0)
            AllocateBuffer();

        if (_debugGizmos)
        {
            _debugAccepted.Clear();
            _debugRejected.Clear();
            _debugInvulnerable.Clear();
            _debugTruncated = false;
        }

        int count = OverlapBox(center, size);
        bool truncated = count >= _hitBufferCapacity;
        HashSet<IHittable> processed = new();

        _lastAcceptedCount = 0;
        _lastRejectedCount = 0;
        _lastEffectiveCount = 0;
        _lastInvulnerableCount = 0;

        float threshYAbove = origin.y + _verticalFilterTolerance;
        float threshYBelow = origin.y - _verticalFilterTolerance;

        bool anyHit = false;
        float maxUpForce = 0f;
        bool anyGiveUp = false;
        int dmg = damageOverride ?? _damage;

        for (int i = 0; i < count; i++)
        {
            var col = _hitBuffer[i];
            if (!col)
                continue;

            if (col.attachedRigidbody && col.attachedRigidbody.gameObject == gameObject)
            {
                if (_debugGizmos) _debugRejected.Add(col);
                _lastRejectedCount++;
                continue;
            }

            if (!_skipVerticalFilter && verticalMode != VerticalFilterMode.None)
            {
                bool pass = true;
                if (verticalMode == VerticalFilterMode.Above)
                {
                    pass = _useCenterForVerticalFilter ? (col.bounds.center.y >= threshYAbove) : (col.bounds.min.y >= threshYAbove);
                }
                else if (verticalMode == VerticalFilterMode.Below)
                {
                    pass = _useCenterForVerticalFilter ? (col.bounds.center.y <= threshYBelow) : (col.bounds.max.y <= threshYBelow);
                }
                if (!pass)
                {
                    if (_debugGizmos) _debugRejected.Add(col);
                    _lastRejectedCount++;
                    continue;
                }
            }

            IHittable hittable = col.GetComponentInParent<IHittable>();
            if (hittable == null)
            {
                if (_debugGizmos) _debugRejected.Add(col);
                _lastRejectedCount++;
                continue;
            }
            if (!processed.Add(hittable))
            {
                if (_debugGizmos) _debugRejected.Add(col);
                continue;
            }

            Vector3 hitPoint = col.bounds.ClosestPoint(origin);
            Vector3 dir;
            if (fixedDirection.HasValue)
            {
                dir = fixedDirection.Value;
            }
            else
            {
                dir = (col.bounds.center - (Vector3)origin);
                if (dir.sqrMagnitude < 1e-6f)
                    dir = (_movement && _movement.FacingRight) ? Vector3.right : Vector3.left;

                dir.Normalize();
            }

            bool preWasHit = hittable.WasHit;
            hittable.Hit(hitPoint, dir, dmg);
            bool effective = !preWasHit && hittable.WasHit;
            if (!effective && preWasHit && _debugGizmos)
                _debugInvulnerable.Add(col);

            if (effective)
                _lastEffectiveCount++;
            else if (preWasHit)
                _lastInvulnerableCount++;

            if (hittable.GiveUpwardForce && hittable.UpwardForce > 0f)
            {
                anyGiveUp = true;
                if (hittable.UpwardForce > maxUpForce)
                    maxUpForce = hittable.UpwardForce;
            }

            anyHit = true;
            if (_debugGizmos)
                _debugAccepted.Add(col);

            _lastAcceptedCount++;
        }

        if (_debugGizmos && truncated)
            _debugTruncated = true;

        return new OverlapDamageResult
        {
            AnyHit = anyHit,
            AcceptedCount = _lastAcceptedCount,
            RejectedCount = _lastRejectedCount,
            EffectiveCount = _lastEffectiveCount,
            InvulnerableCount = _lastInvulnerableCount,
            Truncated = truncated,
            MaxUpwardForce = maxUpForce,
            AnyGiveUpward = anyGiveUp
        };
    }

    private void DoHorizontalAttack(bool right)
    {
        if (_hitBuffer == null || _hitBuffer.Length == 0)
            AllocateBuffer();

        Vector2 origin = transform.position;
        Vector2 center = origin + new Vector2((right ? 1f : -1f)*_horizontalForwardOffset, 0f);
        _lastAttackCenter = center;
        _lastAttackSize = _horizontalSize;
        _lastAttackValid = true;

        var result = DealDamageInBoxInternal(origin, center, _horizontalSize, VerticalFilterMode.None, null, null);

        if (result.AnyHit)
        {
            if (_recoil)
                _recoil.AttackHorizontalRecoil(right);
            AttackHit?.Invoke(false);
            AttackResolved?.Invoke(true, result.AcceptedCount);
        }
        else
        {
            AttackResolved?.Invoke(false, 0);
            AttackHit?.Invoke(false);
        }
#if UNITY_EDITOR
        if (_debugGizmos && result.Truncated)
            Debug.LogWarning($"Horizontal hit buffer truncated at capacity {_hitBufferCapacity}.");
#endif
    }

    private void DoDownAttack()
    {
        if (_hitBuffer == null || _hitBuffer.Length == 0)
            AllocateBuffer();

        Vector2 origin = transform.position;
        Vector2 center = origin + Vector2.down*_downOffset;
        _lastAttackCenter = center;
        _lastAttackSize = _downSize;
        _lastAttackValid = true;

        var result = DealDamageInBoxInternal(origin, center, _downSize, VerticalFilterMode.Below, Vector3.down, null);

        if (result.AnyHit)
        {
            float upVel = 0f;
            if (_respectTargetUpwardForceFlag && result.AnyGiveUpward)
            {
                upVel = Mathf.Max(0f, result.MaxUpwardForce);
            }
            if (_recoil)
                _recoil.PogoRecoil(upVel);
            AttackHit?.Invoke(true);
            AttackResolved?.Invoke(true, result.AcceptedCount);
        }
        else
        {
            AttackResolved?.Invoke(false, 0);
            AttackHit?.Invoke(false);
        }
#if UNITY_EDITOR
        if (_debugGizmos && result.Truncated)
            Debug.LogWarning($"Down attack hit buffer truncated at capacity {_hitBufferCapacity}.");
#endif
    }

    private void DoUpAttack()
    {
        if (_hitBuffer == null || _hitBuffer.Length == 0) AllocateBuffer();
        Vector2 origin = transform.position;
        Vector2 center = origin + Vector2.up*_upOffset;
        _lastAttackCenter = center;
        _lastAttackSize = _upSize;
        _lastAttackValid = true;

        var result = DealDamageInBoxInternal(origin, center, _upSize, VerticalFilterMode.Above, Vector3.up, null);

        AttackResolved?.Invoke(result.AnyHit, result.AcceptedCount);
#if UNITY_EDITOR
        if (_debugGizmos && result.Truncated)
            Debug.LogWarning($"Up attack hit buffer truncated at capacity {_hitBufferCapacity}.");
#endif
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!_debugGizmos) return;
        Vector2 origin = transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(origin + Vector2.right*_horizontalForwardOffset, _horizontalSize);
        Gizmos.DrawWireCube(origin + Vector2.left*_horizontalForwardOffset, _horizontalSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(origin + Vector2.down*_downOffset, _downSize);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(origin + Vector2.up*_upOffset, _upSize);

        if (Application.isPlaying && _lastAttackValid && _attackVisualTimer > 0f)
        {
            Color prev = Gizmos.color;
            Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
            Gizmos.DrawCube(_lastAttackCenter, _lastAttackSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(_lastAttackCenter, _lastAttackSize);
            Gizmos.color = prev;
        }

        if (Application.isPlaying && _lastAttackValid)
        {
            foreach (var col in _debugAccepted)
            {
                if (!col) continue;
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
            foreach (var col in _debugInvulnerable)
            {
                if (!col) continue;
                Gizmos.color = Color.gray;
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
            foreach (var col in _debugRejected)
            {
                if (!col) continue;
                Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
            if (_debugTruncated)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(_lastAttackCenter, 0.1f);
            }
        }
    }
#endif
    public void DisableAttack()
    {
        _canAttack = false;
    }

    public void EnableAttack()
    {
        _canAttack = true;
    }
}
