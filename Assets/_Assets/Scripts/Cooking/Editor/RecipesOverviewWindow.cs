#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class RecipesOverviewWindow : EditorWindow
{
    private Vector2 _scroll;
    private string _search = "";
    private bool _showLocked = true;
    private bool _showUnlocked = true;
    private RecipesDatabase _db;
    private readonly List<Recipe> _filtered = new();
    private GUIStyle _badge;
    private int _columnCount = 2;

    [MenuItem("Tools/Cooking/Recipes Overview")]
    public static void Open() => GetWindow<RecipesOverviewWindow>("Recipes Overview").Refresh();

    public static void OpenAndSelect(Recipe r)
    {
        var w = GetWindow<RecipesOverviewWindow>("Recipes Overview");
        w.Refresh();
        w.Select(r);
    }

    private void OnEnable()
    {
        _badge = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        Refresh();
    }

    private void OnGUI()
    {
        DrawToolbar();
        if (_db == null)
        {
            EditorGUILayout.HelpBox("No RecipesDatabase found. Place one in Resources or drag one here.", MessageType.Info);
            _db = (RecipesDatabase)EditorGUILayout.ObjectField("Database", _db, typeof(RecipesDatabase), false);
            if (GUILayout.Button("Refresh"))
                Refresh();
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawGrid();
        EditorGUILayout.EndScrollView();

        GUILayout.FlexibleSpace();
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label($"Total: {_db.Recipes.Count} | Filtered: {_filtered.Count}");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Validate All", GUILayout.Width(100)))
                ValidateAll();
            if (GUILayout.Button("Select DB", GUILayout.Width(80)))
                EditorGUIUtility.PingObject(_db);
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            _db = (RecipesDatabase)EditorGUILayout.ObjectField(_db, typeof(RecipesDatabase), false, GUILayout.Width(180));
            _search = GUILayout.TextField(_search, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField, GUILayout.MinWidth(140));
            if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
                _search = "";
            _showUnlocked = GUILayout.Toggle(_showUnlocked, "StartUnlocked", EditorStyles.toolbarButton);
            _showLocked = GUILayout.Toggle(_showLocked, "Locked", EditorStyles.toolbarButton);
            _columnCount = Mathf.Clamp(EditorGUILayout.IntField(_columnCount, GUILayout.Width(40)), 1, 6);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                Refresh();
        }
    }

    private void DrawGrid()
    {
        int col = Mathf.Max(1, _columnCount);
        float pad = 6f;
        float width = (position.width - (pad * (col + 1))) / col;
        int index = 0;
        EditorGUILayout.Space(4);
        foreach (var r in _filtered)
        {
            if (index % col == 0)
                EditorGUILayout.BeginHorizontal();
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(width)))
            {
                DrawCard(r, width);
            }
            if (index % col == col - 1)
                EditorGUILayout.EndHorizontal();
            index++;
        }
        if (index % col != 0)
            EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

    private void DrawCard(Recipe r, float width)
    {
        var rect = GUILayoutUtility.GetRect(width, 90f);
        GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

        var nameRect = new Rect(rect.x + 6, rect.y + 4, rect.width - 12, 18);
        EditorGUI.LabelField(nameRect, r.name, EditorStyles.boldLabel);

        var unlocked = r.StartUnlocked;
        var badgeRect = new Rect(rect.x + rect.width - 60, rect.y + 6, 54, 16);
        var prev = GUI.color;
        GUI.color = unlocked ? new Color(0.25f, 0.6f, 0.25f, 1f) : new Color(0.4f, 0.4f, 0.4f, 1f);
        GUI.Box(badgeRect, unlocked ? "START" : "LOCKED", _badge);
        GUI.color = prev;

        var item = r.ResultItem;
        if (item)
        {
            var itemRect = new Rect(rect.x + 6, rect.y + 26, rect.width - 12, 16);
            EditorGUI.LabelField(itemRect, $"Result: {item.name} x{r.ResultQuantity}");
        }

        var ingRect = new Rect(rect.x + 6, rect.y + 46, rect.width - 12, 34);
        var ingredients = r.Ingredients;
        string ingLine = string.Join(", ", ingredients.Take(3).Select(i => i.Item ? i.Item.name : "?"));
        if (ingredients.Count > 3) ingLine += $" (+{ingredients.Count - 3})";
        EditorGUI.LabelField(ingRect, "Ing: " + ingLine, EditorStyles.miniLabel);

        var btnRect = new Rect(rect.x + 6, rect.yMax - 22, rect.width - 12, 18);
        if (GUI.Button(btnRect, "Select"))
            Select(r);
    }

    private void Select(Recipe r)
    {
        Selection.activeObject = r;
        EditorGUIUtility.PingObject(r);
    }

    public void Refresh()
    {
        if (_db == null)
        {
            _db = Resources.Load<RecipesDatabase>("RecipesDatabase");
            if (_db == null)
                return;
        }

        _filtered.Clear();
        foreach (var r in _db.Recipes)
        {
            if (!r) continue;
            if (!string.IsNullOrEmpty(_search) &&
                !r.name.ToLower().Contains(_search.ToLower()))
                continue;
            if (r.StartUnlocked && !_showUnlocked) continue;
            if (!r.StartUnlocked && !_showLocked) continue;
            _filtered.Add(r);
        }
        Repaint();
    }

    private void ValidateAll()
    {
        List<string> issues = new();
        foreach (var r in _filtered)
        {
            if (!r.ResultItem)
                issues.Add($"{r.name}: missing result item");
            if (r.ResultQuantity < 1)
                issues.Add($"{r.name}: result qty < 1");
            HashSet<Object> ingSet = new();
            foreach (var ing in r.Ingredients)
            {
                if (!ing.Item)
                    issues.Add($"{r.name}: null ingredient");
                else if (!ingSet.Add(ing.Item))
                    issues.Add($"{r.name}: duplicate ingredient {ing.Item.name}");
                if (ing.Quantity < 1)
                    issues.Add($"{r.name}: ingredient qty < 1");
            }
        }

        if (issues.Count == 0)
            EditorUtility.DisplayDialog("Validation", "All filtered recipes OK.", "Close");
        else
            EditorUtility.DisplayDialog("Validation", string.Join("\n", issues.Take(40)) + (issues.Count > 40 ? "\n..." : ""), "Close");
    }
}
#endif
