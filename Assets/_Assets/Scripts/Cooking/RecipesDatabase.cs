using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cooking/Recipes Database", fileName = "RecipesDatabase")]
public class RecipesDatabase : ScriptableObject
{
    [SerializeField] private List<Recipe> _recipes = new();

    public IReadOnlyList<Recipe> Recipes => _recipes;

#if UNITY_EDITOR

    public void SetRecipes(List<Recipe> newList)
    {
        _recipes = newList;
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private void OnValidate()
    {
        var recipes = new HashSet<Recipe>();
        for (int i = _recipes.Count - 1; i >= 0; i--)
        {
            var recipe = _recipes[i];
            if (!recipe || !recipes.Add(recipe))
                _recipes.RemoveAt(i);
        }
    }
#endif
}
