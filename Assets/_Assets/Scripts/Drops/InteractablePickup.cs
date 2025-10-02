using System;
using DG.Tweening;
using UnityEngine;
using _Assets.Scripts.Drops;

[DisallowMultipleComponent]
public class InteractablePickup : InteractableBase
{
    [SerializeField] private DroppedItem _dropped;
    [SerializeField, Min(1)] private int _quantity = 1;
    [SerializeField] private bool _destroyOnPickup = true;
    [SerializeField] private bool _autoSetNameFromItem = true;

    [Header("Auto Pickup")]
    [SerializeField] private bool _autoPickupOnEnter = true;
    [SerializeField] private LayerMask _collectorMask;
    [SerializeField] private bool _onlyPickupIfInventoryAccepts = true;
    [SerializeField] private float _pickupRadius = 0.6f;

    private bool _collected;

    private void Awake()
    {
        if (!_dropped) _dropped = GetComponent<DroppedItem>();
        if (_autoSetNameFromItem && _dropped && _dropped.Payload)
            _displayName = _dropped.Payload.name;

        GetComponentInChildren<CircleCollider2D>().radius = _pickupRadius;
    }

    private void OnEnable()
    {
        _dropped.OnCanBePickedUp += OnCanBePickedUp;
    }

    private void OnDisable()
    {
        if (_dropped)
            _dropped.OnCanBePickedUp -= OnCanBePickedUp;
    }

    private void OnCanBePickedUp()
    {
        if (!_autoPickupOnEnter || _collected || !_dropped)
            return;

        if (!_dropped.IsPickable)
            return;

        Collider2D hit = Physics2D.OverlapCircle(transform.position, _pickupRadius, _collectorMask);

        if (!hit)
            return;

        hit.TryGetComponent<IItemCollector>(out var collector);

        if (_onlyPickupIfInventoryAccepts && (collector == null || !collector.CanAccept(_dropped.Payload, _quantity)))
            return;

        var ctx = new InteractionContext(hit.gameObject, (Vector2)transform.position, Time.time);
        Interact(in ctx);
    }

    public override bool CanInteract(in InteractionContext context)
    {
        if (_collected || !_dropped || !_dropped.Payload)
            return false;

        if (!_dropped.IsPickable)
            return false;

        return IsEnabled;
    }

    public override void Interact(in InteractionContext context)
    {
        if (!CanInteract(context)) return;

        var collector = context.Initiator ? context.Initiator.GetComponent<IItemCollector>() : null;
        if (collector != null && collector.CanAccept(_dropped.Payload, _quantity))
        {
            collector.AddItem(_dropped.Payload, _quantity);
        }
        else if (!_onlyPickupIfInventoryAccepts)
        {
            Debug.Log($"Picked up {_quantity}x {_dropped.Payload.name} (no inventory component found).");
        }
        else
        {
            return;
        }

        _collected = true;

        if (_destroyOnPickup && _dropped)
        {

            transform.DOJump(context.Initiator.transform.position, 0.5f, 1, 0.25f)
                .SetEase(Ease.InCubic)
                .OnComplete(() => ObjectPoolManager.ReturnObjectToPool(this.gameObject));
        }
        else
        {
            _displayName = "(Collected)";
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_autoPickupOnEnter || _collected || !_dropped)
            return;

        if (!_dropped.IsPickable)
            return;

        if (((1 << other.gameObject.layer) & _collectorMask) == 0)
            return;

        var collector = other.GetComponent<IItemCollector>();

        if (_onlyPickupIfInventoryAccepts && (collector == null || !collector.CanAccept(_dropped.Payload, _quantity)))
            return;

        var ctx = new InteractionContext(other.gameObject, (Vector2)transform.position, Time.time);
        Interact(in ctx);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _pickupRadius);
    }

  #endif

    public void Reset()
    {
        _quantity = 1;
        _destroyOnPickup = true;
        _autoPickupOnEnter = true;
        _onlyPickupIfInventoryAccepts = true;
        _collected = false;
    }
}
