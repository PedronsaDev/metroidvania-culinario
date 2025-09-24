using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "new_recipe", menuName = "Cooking/New Recipe")]
public class Recipe : ScriptableObject
{
    [Header("Result")]
    [SerializeField] private Item _resultItem;
    [SerializeField] private int _resultQuantity = 1;

    [Header("Ingredients")]
    [SerializeField] private List<Ingredient> _ingredients = new();

    [Header("Meta")]
    [SerializeField] private bool _startUnlocked = false;
    [SerializeField, Tooltip("Optional stable ID for save/persistence; leave blank to auto-generate."), ReadOnly]
    private string _persistentId;

    public Item ResultItem => _resultItem;
    public int ResultQuantity => _resultQuantity < 1 ? 1 : _resultQuantity;
    public bool StartUnlocked => _startUnlocked;
    public string PersistentId => string.IsNullOrWhiteSpace(_persistentId) ? name : _persistentId;
    public IReadOnlyList<Ingredient> Ingredients => _ingredients;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_ingredients == null)
            _ingredients = new List<Ingredient>();

        var seen = new HashSet<Item>();
        for (int i = _ingredients.Count - 1; i >= 0; i--)
        {
            var ing = _ingredients[i];
            var item = ing.Item;

            if (item == null)
            {
                continue;
            }

            if (!seen.Add(item))
            {
                _ingredients.RemoveAt(i);
                continue;
            }

            if (ing.Quantity < 1)
                _ingredients[i] = new Ingredient(item, 1);
        }

        if (_resultQuantity < 1)
            _resultQuantity = 1;

        if (string.IsNullOrWhiteSpace(_persistentId))
            _persistentId = Guid.NewGuid().ToString("N");

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
