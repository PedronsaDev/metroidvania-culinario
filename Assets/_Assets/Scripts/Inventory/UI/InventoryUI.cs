using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : BaseUIWindow
{
    [Header("References")]
    [SerializeField] private PlayerInventory _inventorySource;
    [SerializeField] private Transform _listContainer;
    [SerializeField] private InventoryItemEntry _entryPrefab;

    private readonly Dictionary<Item, InventoryItemEntry> _entries = new();

    protected override void OnEnable()
    {
        base.OnEnable();

        Attach(_inventorySource);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        Detach(_inventorySource);
    }

    public void SetInventory(PlayerInventory inv)
    {
        if (inv == _inventorySource) return;
        Detach(_inventorySource);
        _inventorySource = inv;
        Attach(_inventorySource);
    }

    private void Attach(PlayerInventory inv)
    {
        if (!inv) return;
        inv.ItemAdded += HandleItemAdded;
        inv.ItemRemoved += HandleItemRemoved;
        Rebuild(inv);
    }

    private void Detach(PlayerInventory inv)
    {
        if (!inv) return;
        inv.ItemAdded -= HandleItemAdded;
        inv.ItemRemoved -= HandleItemRemoved;
        ClearVisuals();
    }

    private void Rebuild(PlayerInventory inv)
    {
        ClearVisuals();
        foreach (var kvp in inv.Inventory)
            CreateOrUpdate(kvp.Key, kvp.Value);
    }

    private void ClearVisuals()
    {
        foreach (var entry in _entries.Values)
            if (entry) Destroy(entry.gameObject);
        _entries.Clear();
    }

    private void HandleItemAdded(Item item, int qty)
    {
        if (!item) return;
        if (_entries.TryGetValue(item, out var entry))
        {
            int total = _inventorySource.Inventory[item];
            entry.UpdateQuantity(total);
        }
        else
        {
            int total = _inventorySource.Inventory[item];
            CreateOrUpdate(item, total);
        }
    }

    private void HandleItemRemoved(Item item, int qty)
    {
        if (!item) return;
        if (!_entries.TryGetValue(item, out var entry))
            return;

        if (!_inventorySource.Inventory.TryGetValue(item, out int remaining))
        {
            _entries.Remove(item);
            if (entry) Destroy(entry.gameObject);
            return;
        }

        entry.UpdateQuantity(remaining);
    }

    private void CreateOrUpdate(Item item, int quantity)
    {
        var entry = Instantiate(_entryPrefab, _listContainer);
        entry.Bind(item, quantity);
        _entries[item] = entry;
    }
}
