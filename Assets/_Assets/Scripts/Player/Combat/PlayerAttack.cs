using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement _movement;
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

    [Header("Recoil")]
    [SerializeField] private float _horizontalRecoilSpeed = 12f;
    [SerializeField] private float _horizontalRecoilDuration = 0.08f;
    [SerializeField] private float _pogoUpVelocity = 22f;
    [SerializeField] private float _pogoRecoilDuration = 0.04f;
    [SerializeField] private bool _respectTargetUpwardForceFlag = true;
    [SerializeField, Range(0f,1f)] private float _horizontalRecoilVsVerticalRatio = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool _debugGizmos;
    [SerializeField, Range(0.01f, 0.5f)] private float _debugAttackVisualTime = 0.12f;

    [Header("Performance")]
    [SerializeField] private int _hitBufferCapacity = 3;

    private InputAction _attackAction;
    private InputAction _moveAction;
    private float _nextAttackTime;
    private bool _attackQueuedThisFrame;
    private Collider2D[] _hitBuffer;
    private ContactFilter2D _contactFilter;
    private bool _filterInitialized;


    public event Action AttackStarted;
    public event Action<bool> AttackHit;

    private enum AttackDir { Left, Right, Down, Up }

    private AttackDir _lastAttackDir;
    private float _attackVisualTimer;
    private Vector2 _lastAttackCenter;
    private Vector2 _lastAttackSize;
    private bool _lastAttackValid;

    private void Awake()
    {
        if (!_movement)
            _movement = GetComponent<PlayerMovement>();
        if (_attackRef) _attackAction = _attackRef.action;
        if (_moveRef) _moveAction = _moveRef.action;
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
        _attackQueuedThisFrame = false;
        if (_attackVisualTimer > 0f)
        {
            _attackVisualTimer -= Time.deltaTime;
            if (_attackVisualTimer <= 0f)
            {
                _lastAttackValid = false;
            }
        }
    }

    private void OnAttackStarted(InputAction.CallbackContext ctx)
    {
        if (_attackQueuedThisFrame) return;
        _attackQueuedThisFrame = true;

        if (Time.time < _nextAttackTime) return;
        if (!_movement) return;

        Vector2 moveInput = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        bool tryDown = moveInput.y < -0.3f && !_movement.Grounded;
        bool tryUp = moveInput.y > 0.3f;

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
        _lastAttackDir = dir;
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

    private void DoHorizontalAttack(bool right)
    {
        if (_hitBuffer == null || _hitBuffer.Length == 0) AllocateBuffer();
        Vector2 origin = transform.position;
        Vector2 center = origin + new Vector2((right ? 1f : -1f)*_horizontalForwardOffset, 0f);
        _lastAttackCenter = center;
        _lastAttackSize = _horizontalSize;
        _lastAttackValid = true;

        int count = OverlapBox(center, _horizontalSize);

        bool anyHit = false;
        HashSet<IHittable> processed = new();
        for (int i = 0; i < count; i++)
        {
            var col = _hitBuffer[i];
            if (!col) continue;
            if (col.attachedRigidbody && col.attachedRigidbody.gameObject == gameObject) continue;
            IHittable hittable = col.GetComponentInParent<IHittable>();
            if (hittable == null) continue;
            if (!processed.Add(hittable)) continue;

            Vector3 hitPoint = col.bounds.ClosestPoint(origin);
            Vector3 dir = (col.bounds.center - (Vector3)origin).normalized;
            hittable.Hit(hitPoint, dir, _damage);
            anyHit = true;
        }

        if (anyHit)
        {
            float recoilSign = right ? -1f : 1f;
            float cappedHorizontalSpeed = _horizontalRecoilSpeed;
            float maxAllowed = _pogoUpVelocity * _horizontalRecoilVsVerticalRatio;
            if (cappedHorizontalSpeed > maxAllowed)
                cappedHorizontalSpeed = maxAllowed;
            _movement.ApplyRecoil(new Vector2(recoilSign*cappedHorizontalSpeed, _movement.VerticalSpeed), _horizontalRecoilDuration, overrideX: true, overrideY: false);
            AttackHit?.Invoke(false);
        }
        else
        {
            AttackHit?.Invoke(false);
        }
    }

    private void DoDownAttack()
    {
        if (_hitBuffer == null || _hitBuffer.Length == 0) AllocateBuffer();
        Vector2 origin = transform.position;
        Vector2 center = origin + Vector2.down*_downOffset;
        _lastAttackCenter = center;
        _lastAttackSize = _downSize;
        _lastAttackValid = true;

        int count = OverlapBox(center, _downSize);

        bool pogo = false;
        HashSet<IHittable> processed = new();
        for (int i = 0; i < count; i++)
        {
            var col = _hitBuffer[i];
            if (col == null) continue;
            if (col.attachedRigidbody && col.attachedRigidbody.gameObject == gameObject) continue;
            if (col.bounds.max.y > origin.y - 0.05f) continue;
            IHittable hittable = col.GetComponentInParent<IHittable>();
            if (hittable == null) continue;
            if (!processed.Add(hittable)) continue;
            Vector3 hitPoint = col.bounds.ClosestPoint(origin);
            hittable.Hit(hitPoint, Vector3.down, _damage);
            pogo = true;
        }

        if (pogo)
        {
            float upVel = _pogoUpVelocity;
            if (_respectTargetUpwardForceFlag)
            {
                foreach (var h in processed)
                {
                    if (h.GiveUpwardForce && h.UpwardForce > 0f)
                        upVel = Mathf.Max(upVel, h.UpwardForce);
                }
            }
            _movement.ApplyRecoil(new Vector2(_movement.HorizontalSpeed, upVel), _pogoRecoilDuration, overrideX: false, overrideY: true);
            AttackHit?.Invoke(true);
        }
        else
        {
            AttackHit?.Invoke(true);
        }
    }

    private void DoUpAttack()
    {
        if (_hitBuffer == null || _hitBuffer.Length == 0) AllocateBuffer();
        Vector2 origin = transform.position;
        Vector2 center = origin + Vector2.up * _upOffset;
        _lastAttackCenter = center;
        _lastAttackSize = _upSize;
        _lastAttackValid = true;

        int count = OverlapBox(center, _upSize);

        bool anyHit = false;

        HashSet<IHittable> processed = new();
        for (int i = 0; i < count; i++)
        {
            var col = _hitBuffer[i];
            if (col == null) continue;
            if (col.attachedRigidbody != null && col.attachedRigidbody.gameObject == gameObject) continue;
            if (col.bounds.min.y < origin.y + 0.05f) continue;
            IHittable hittable = col.GetComponentInParent<IHittable>();
            if (hittable == null) continue;
            if (!processed.Add(hittable)) continue;
            Vector3 hitPoint = col.bounds.ClosestPoint(origin);
            hittable.Hit(hitPoint, Vector3.up, _damage);
            anyHit = true;
        }
        AttackHit?.Invoke(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!_debugGizmos)
            return;

        Gizmos.color = Color.cyan;
        Vector2 origin = transform.position;
        Vector2 rightCenter = origin + Vector2.right*_horizontalForwardOffset;
        Vector2 leftCenter = origin + Vector2.left*_horizontalForwardOffset;
        Gizmos.DrawWireCube(rightCenter, _horizontalSize);
        Gizmos.DrawWireCube(leftCenter, _horizontalSize);
        Gizmos.color = Color.yellow;
        Vector2 downCenter = origin + Vector2.down*_downOffset;
        Gizmos.DrawWireCube(downCenter, _downSize);
        Gizmos.color = Color.magenta;
        Vector2 upCenter = origin + Vector2.up*_upOffset;
        Gizmos.DrawWireCube(upCenter, _upSize);


        if (Application.isPlaying && _lastAttackValid && _attackVisualTimer > 0f)
        {
            Color prev = Gizmos.color;
            Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
            Gizmos.DrawCube(_lastAttackCenter, _lastAttackSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(_lastAttackCenter, _lastAttackSize);
            Gizmos.color = prev;
        }
    }
#endif
}
