using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour, IInventory
{
    private readonly Dictionary<Item, int> _inventory = new();

    public event Action<Item,int> ItemAdded;
    public event Action<Item,int> ItemRemoved;


    public IReadOnlyDictionary<Item, int> Inventory => _inventory;

    public void AddItem(Item item, int quantity = 1)
    {
        if (!item || quantity < 1) return;

        if (!_inventory.TryAdd(item, quantity))
            _inventory[item] += quantity;

        ItemAdded?.Invoke(item, quantity);
    }

    public bool Consume(Item item, int quantity = 1) => RemoveItem(item, quantity);

    public bool RemoveItem(Item item, int quantity = 1)
    {
        if (!item || quantity < 1 || !_inventory.TryGetValue(item, out int currentQuantity) || currentQuantity < quantity)
            return false;

        if (currentQuantity == quantity)
            _inventory.Remove(item);
        else
            _inventory[item] = currentQuantity - quantity;

        ItemRemoved?.Invoke(item, quantity);
        return true;
    }

    public bool HasItem(Item item, int quantity = 1)
    {
        if (!item || quantity < 1) return false;
        return _inventory.TryGetValue(item, out int currentQuantity) && currentQuantity >= quantity;
    }

    public bool HasIngredients(IEnumerable<Ingredient> ingredients)
    {
        if (ingredients == null) return false;
        foreach (var ing in ingredients)
        {
            if (!HasItem(ing.Item, ing.Quantity))
                return false;
        }
        return true;
    }

    public bool ConsumeIngredients(IEnumerable<Ingredient> ingredients)
    {
        if (!HasIngredients(ingredients))
            return false;

        foreach (var ing in ingredients)
            RemoveItem(ing.Item, ing.Quantity);

        return true;
    }

    public void Clear() => _inventory.Clear();
}
