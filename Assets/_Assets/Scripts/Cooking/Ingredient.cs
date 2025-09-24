using System;
using UnityEngine;
[Serializable]
public struct Ingredient
{
    [SerializeField] private Item _item;
    [SerializeField, Min(1)] private int _quantity;

    public Item Item => _item;
    public int Quantity => _quantity < 1 ? 1 : _quantity;

    public Ingredient(Item item, int quantity)
    {
        _item = item;
        _quantity = quantity < 1 ? 1 : quantity;
    }
}
