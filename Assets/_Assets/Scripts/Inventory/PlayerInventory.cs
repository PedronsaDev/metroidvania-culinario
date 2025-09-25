using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private Dictionary<Item, int> _inventory = new();

    public IReadOnlyDictionary<Item, int> Inventory => _inventory;

    public static event Action<Item,int> OnItemAdded;
    public static event Action<Item,int> OnItemRemoved;

    public void AddItem(Item item, int quantity = 1)
    {
        if (!item || quantity < 1)
            return;

        if (!_inventory.TryAdd(item, quantity))
            _inventory[item] += quantity;

        OnItemAdded?.Invoke(item, quantity);
    }

    public bool RemoveItem(Item item, int quantity = 1)
    {
        if (!item || quantity < 1 || !_inventory.TryGetValue(item, out int currentQuantity) || currentQuantity < quantity)
            return false;

        if (currentQuantity == quantity)
            _inventory.Remove(item);
        else
            _inventory[item] = currentQuantity - quantity;

        OnItemRemoved?.Invoke(item, quantity);

        return true;
    }

    public bool HasItem(Item item, int quantity = 1)
    {
        if (!item || quantity < 1)
            return false;

        return _inventory.TryGetValue(item, out int currentQuantity) && currentQuantity >= quantity;
    }

    public void Clear()
    {
        _inventory.Clear();
    }
}
