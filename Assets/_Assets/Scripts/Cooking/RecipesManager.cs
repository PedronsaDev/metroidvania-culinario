using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public static class RecipesManager
{
    private static bool _initialized;
    private static readonly List<Recipe> _allRecipes = new();
    private static readonly HashSet<Recipe> _unlocked = new();
    private static RecipesDatabase _database;

    public static event Action<Recipe> OnRecipeUnlocked;

    public static IReadOnlyList<Recipe> AllRecipes
    {
        get
        {
            EnsureInitialized();
            return _allRecipes;
        }
    }

    public static IEnumerable<Recipe> UnlockedRecipes
    {
        get
        {
            EnsureInitialized();
            return _unlocked;
        }
    }

    public static void SetDatabase(RecipesDatabase db)
    {
        _database = db;
        Reset();
        EnsureInitialized();
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
            return;

        if (!_database)
        {
            _database = Resources.Load<RecipesDatabase>("Recipes/Database/recipes_database");

            if (!_database)
            {
                Debug.LogWarning("RecipesManager: Recipes database missing.");
                _initialized = true;
                return;
            }
        }

        _allRecipes.Clear();
        _unlocked.Clear();

        var list = _database.Recipes;
        for (int i = 0; i < list.Count; i++)
        {
            var recipe = list[i];
            if (!recipe)
                continue;

            _allRecipes.Add(recipe);
            if (recipe.StartUnlocked)
                _unlocked.Add(recipe);
        }

        _initialized = true;
    }

    public static bool IsUnlocked(Recipe recipe)
    {
        if (!recipe)
            return false;

        EnsureInitialized();
        return _unlocked.Contains(recipe);
    }

    public static bool Unlock(Recipe recipe)
    {
        if (!recipe)
            return false;

        EnsureInitialized();

        if (!_unlocked.Add(recipe))
            return false;

        OnRecipeUnlocked?.Invoke(recipe);

        return true;
    }

    public static void Reset()
    {
        _allRecipes.Clear();
        _unlocked.Clear();
        _initialized = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void DomainReset() => Reset();
}
