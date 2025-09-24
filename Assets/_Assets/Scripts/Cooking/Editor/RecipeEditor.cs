#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(Recipe))]
[CanEditMultipleObjects]
public class RecipeEditor : Editor
{
    private SerializedProperty _resultItem;
    private SerializedProperty _resultQuantity;
    private SerializedProperty _ingredients;
    private SerializedProperty _startUnlocked;
    private SerializedProperty _persistentId;

    private ReorderableList _ingredientsList;
    private Texture2D _badgeOk;
    private Texture2D _badgeWarn;
    private GUIStyle _headerStyle;
    private bool _foldMeta = true;
    private bool _foldValidation = true;

    private readonly List<string> _validationBuffer = new();

    private void OnEnable()
    {
        _resultItem      = serializedObject.FindProperty("_resultItem");
        _resultQuantity  = serializedObject.FindProperty("_resultQuantity");
        _ingredients     = serializedObject.FindProperty("_ingredients");
        _startUnlocked   = serializedObject.FindProperty("_startUnlocked");
        _persistentId    = serializedObject.FindProperty("_persistentId");

        BuildList();
        LoadBadges();
        BuildStyles();
    }

    private void BuildStyles()
    {
        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft
        };
    }

    private void LoadBadges()
    {
        _badgeOk   = EditorGUIUtility.IconContent("TestPassed").image as Texture2D;
        _badgeWarn = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
    }

    private void BuildList()
    {
        _ingredientsList = new ReorderableList(serializedObject, _ingredients, true, true, true, true);
        _ingredientsList.drawHeaderCallback = r =>
        {
            EditorGUI.LabelField(r, $"Ingredients ({_ingredients.arraySize})");
        };
        _ingredientsList.elementHeight = EditorGUIUtility.singleLineHeight + 2f;
        _ingredientsList.drawElementCallback = (rect, index, active, focused) =>
        {
            rect.y += 1f;
            rect.height = EditorGUIUtility.singleLineHeight;
            var element = _ingredients.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, element, GUIContent.none);
        };
        _ingredientsList.onAddCallback = list =>
        {
            _ingredients.InsertArrayElementAtIndex(_ingredients.arraySize);
            var newEl = _ingredients.GetArrayElementAtIndex(_ingredients.arraySize - 1);
            newEl.FindPropertyRelative("_item").objectReferenceValue = null;
            newEl.FindPropertyRelative("_quantity").intValue = 1;
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeaderBar();
        EditorGUILayout.Space(4);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Result", _headerStyle);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_resultItem, new GUIContent("Item"));
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_resultQuantity, new GUIContent("Quantity"));
                if (GUILayout.Button("x2", GUILayout.Width(40)))
                    _resultQuantity.intValue = Mathf.Max(1, _resultQuantity.intValue * 2);
                if (GUILayout.Button("Clamp", GUILayout.Width(60)))
                    _resultQuantity.intValue = Mathf.Clamp(_resultQuantity.intValue, 1, 999);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _ingredientsList.DoLayoutList();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Normalize qty"))
                    NormalizeQuantities(1);
                if (GUILayout.Button("+1 all"))
                    OffsetQuantities(1);
                if (GUILayout.Button("-1 all"))
                    OffsetQuantities(-1);
                if (GUILayout.Button("Sort A-Z"))
                    SortIngredients();
            }
            AcceptDragItemsRect(GUILayoutUtility.GetLastRect());
        }

        EditorGUILayout.Space(4);

        _foldMeta = EditorGUILayout.BeginFoldoutHeaderGroup(_foldMeta, "Meta");
        if (_foldMeta)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_startUnlocked, new GUIContent("Start Unlocked"));
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(_persistentId, new GUIContent("Persistent ID"));
                    using (new EditorGUI.DisabledScope(true))
                        GUILayout.Label(string.IsNullOrEmpty(_persistentId.stringValue) ? "Auto" : "Stable", GUILayout.Width(40));
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Regenerate ID"))
                        _persistentId.stringValue = System.Guid.NewGuid().ToString("N");
                    if (GUILayout.Button("Clear ID"))
                        _persistentId.stringValue = string.Empty;
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        _foldValidation = EditorGUILayout.BeginFoldoutHeaderGroup(_foldValidation, "Validation");
        if (_foldValidation)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                RunValidation(_validationBuffer);
                bool ok = _validationBuffer.Count == 0;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(ok ? _badgeOk : _badgeWarn, GUILayout.Width(20), GUILayout.Height(20));
                    EditorGUILayout.LabelField(ok ? "No issues detected." : "Issues:", EditorStyles.boldLabel);
                }
                if (!ok)
                {
                    foreach (var line in _validationBuffer)
                        EditorGUILayout.HelpBox(line, MessageType.Warning);
                    if (GUILayout.Button("Auto-Fix"))
                        AutoFix();
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);
        DrawFooterBar();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeaderBar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label(target.name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(40)))
                EditorGUIUtility.PingObject(target);
            if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton))
                DuplicateRecipe();
            if (GUILayout.Button("Find In DB", EditorStyles.toolbarButton))
                RecipesOverviewWindow.OpenAndSelect((Recipe)target);
        }
    }

    private void DrawFooterBar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Open Recipes Overview", EditorStyles.toolbarButton))
                RecipesOverviewWindow.Open();
            if (GUILayout.Button("Force Validate", EditorStyles.toolbarButton))
                RunValidation(_validationBuffer, forceLog: true);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply & Save", EditorStyles.toolbarButton))
            {
                AssetDatabase.SaveAssets();
            }
        }
    }

    private void NormalizeQuantities(int to)
    {
        for (int i = 0; i < _ingredients.arraySize; i++)
            _ingredients.GetArrayElementAtIndex(i).FindPropertyRelative("_quantity").intValue = Mathf.Max(1, to);
    }

    private void OffsetQuantities(int delta)
    {
        for (int i = 0; i < _ingredients.arraySize; i++)
        {
            var qProp = _ingredients.GetArrayElementAtIndex(i).FindPropertyRelative("_quantity");
            qProp.intValue = Mathf.Max(1, qProp.intValue + delta);
        }
    }

    private void SortIngredients()
    {
        var list = new List<SerializedProperty>();
        for (int i = 0; i < _ingredients.arraySize; i++)
            list.Add(_ingredients.GetArrayElementAtIndex(i).Copy());

        list.Sort((a, b) =>
        {
            var aItem = a.FindPropertyRelative("_item").objectReferenceValue;
            var bItem = b.FindPropertyRelative("_item").objectReferenceValue;
            string an = aItem ? aItem.name : "~";
            string bn = bItem ? bItem.name : "~";
            return string.Compare(an, bn, System.StringComparison.OrdinalIgnoreCase);
        });

        _ingredients.ClearArray();
        for (int i = 0; i < list.Count; i++)
        {
            _ingredients.InsertArrayElementAtIndex(i);
            CopyIngredientProps(list[i], _ingredients.GetArrayElementAtIndex(i));
        }
    }

    private void CopyIngredientProps(SerializedProperty from, SerializedProperty to)
    {
        to.FindPropertyRelative("_item").objectReferenceValue =
            from.FindPropertyRelative("_item").objectReferenceValue;
        to.FindPropertyRelative("_quantity").intValue =
            from.FindPropertyRelative("_quantity").intValue;
    }

    private void RunValidation(List<string> buffer, bool forceLog = false)
    {
        buffer.Clear();
        if (_resultItem.objectReferenceValue == null)
            buffer.Add("Result item is missing.");
        if (_resultQuantity.intValue < 1)
            buffer.Add("Result quantity < 1.");
        HashSet<Object> seen = new();
        for (int i = 0; i < _ingredients.arraySize; i++)
        {
            var el = _ingredients.GetArrayElementAtIndex(i);
            var item = el.FindPropertyRelative("_item").objectReferenceValue;
            int qty = el.FindPropertyRelative("_quantity").intValue;
            if (item == null)
                buffer.Add($"Ingredient {i} is null.");
            else if (!seen.Add(item))
                buffer.Add($"Duplicate ingredient: {item.name}");
            if (qty < 1)
                buffer.Add($"Ingredient {i} quantity < 1.");
        }
        if (forceLog && buffer.Count > 0)
            Debug.LogWarning($"Recipe {target.name} validation issues:\n - {string.Join("\n - ", buffer)}", target);
    }

    private void AutoFix()
    {
        if (_resultQuantity.intValue < 1)
            _resultQuantity.intValue = 1;

        // Remove nulls and duplicates, clamp qty
        HashSet<Object> used = new();
        for (int i = _ingredients.arraySize - 1; i >= 0; i--)
        {
            var el = _ingredients.GetArrayElementAtIndex(i);
            var itemProp = el.FindPropertyRelative("_item");
            var qtyProp = el.FindPropertyRelative("_quantity");
            var item = itemProp.objectReferenceValue;
            if (item == null || !used.Add(item))
            {
                _ingredients.DeleteArrayElementAtIndex(i);
                continue;
            }
            if (qtyProp.intValue < 1)
                qtyProp.intValue = 1;
        }
    }

    private void DuplicateRecipe()
    {
        foreach (var t in targets)
        {
            var original = (Recipe)t;
            string path = AssetDatabase.GetAssetPath(original);
            string newPath = AssetDatabase.GenerateUniqueAssetPath(path);
            var clone = Instantiate(original);
            clone.name = original.name + "_Copy";
            AssetDatabase.CreateAsset(clone, newPath);
            EditorGUIUtility.PingObject(clone);
        }
        AssetDatabase.SaveAssets();
    }

    private void AcceptDragItemsRect(Rect lastRect)
    {
        var evt = Event.current;
        if (evt.type != EventType.DragPerform && evt.type != EventType.DragUpdated) return;
        if (!lastRect.Contains(evt.mousePosition)) return;

        var refs = DragAndDrop.objectReferences;
        bool any = false;
        foreach (var r in refs)
        {
            if (r is Item item)
            {
                any = true;
                if (!ContainsItem(item))
                {
                    _ingredients.InsertArrayElementAtIndex(_ingredients.arraySize);
                    var el = _ingredients.GetArrayElementAtIndex(_ingredients.arraySize - 1);
                    el.FindPropertyRelative("_item").objectReferenceValue = item;
                    el.FindPropertyRelative("_quantity").intValue = 1;
                }
            }
        }

        if (any)
        {
            DragAndDrop.AcceptDrag();
            Event.current.Use();
        }
    }

    private bool ContainsItem(Item item)
    {
        for (int i = 0; i < _ingredients.arraySize; i++)
        {
            var el = _ingredients.GetArrayElementAtIndex(i);
            if (el.FindPropertyRelative("_item").objectReferenceValue == item)
                return true;
        }
        return false;
    }
}
#endif
