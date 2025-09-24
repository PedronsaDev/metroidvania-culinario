using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RecipesDatabase))]
public class RecipesDatabaseEditor : Editor
{
    private const string DefaultFolder = "Assets/_Assets/Resources/Recipes";
    private DefaultAsset _folderAsset;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Auto Population", EditorStyles.boldLabel);

        //_folderAsset = (DefaultAsset)EditorGUILayout.ObjectField("Folder", _folderAsset, typeof(DefaultAsset), false);

        using (new EditorGUILayout.HorizontalScope())
        {
            // if (GUILayout.Button("Scan Folder"))
            //     Scan(ResolveFolderPath());
            if (GUILayout.Button("Scan Folder"))
                Scan(DefaultFolder);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Sort A-Z"))
                SortCurrent();
            if (GUILayout.Button("Clear"))
                Replace(new List<Recipe>());
        }

        if (GUILayout.Button("Validate (De-dup & Remove Nulls)"))
            ValidateCurrent();

        //EditorGUILayout.HelpBox("Pick a folder (project view) then Scan Folder, or use Scan Default. Uses t:Recipe search.", MessageType.Info);
    }

    private string ResolveFolderPath()
    {
        if (!_folderAsset)
            return DefaultFolder;
        var path = AssetDatabase.GetAssetPath(_folderAsset);
        return AssetDatabase.IsValidFolder(path) ? path : DefaultFolder;
    }

    private void Scan(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"RecipesDatabaseEditor: Invalid folder '{folderPath}'.");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Recipe", new[] { folderPath });
        var list = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<Recipe>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(r => r)
            .Distinct()
            .OrderBy(r => r.name)
            .ToList();

        Replace(list);
        Debug.Log($"RecipesDatabaseEditor: Collected {list.Count} recipes from '{folderPath}'.");
    }

    private void SortCurrent()
    {
        var db = (RecipesDatabase)target;
        var sorted = db.Recipes.Where(r => r).OrderBy(r => r.name).ToList();
        Replace(sorted);
    }

    private void ValidateCurrent()
    {
        var db = (RecipesDatabase)target;
        var filtered = db.Recipes.Where(r => r).Distinct().OrderBy(r => r.name).ToList();
        Replace(filtered);
    }

    private void Replace(List<Recipe> newList)
    {
        var db = (RecipesDatabase)target;
        db.SetRecipes(newList);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Repaint();
    }
}
