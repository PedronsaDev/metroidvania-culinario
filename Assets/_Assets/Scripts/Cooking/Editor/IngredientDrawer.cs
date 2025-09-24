#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(Ingredient))]
public class IngredientDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight;

    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        var itemProp = property.FindPropertyRelative("_item");
        var qtyProp  = property.FindPropertyRelative("_quantity");

        float itemWidth = rect.width * 0.70f;
        float qtyWidth  = rect.width - itemWidth - 4f;

        var itemRect = new Rect(rect.x, rect.y, itemWidth, rect.height);
        var qtyRect  = new Rect(itemRect.xMax + 4f, rect.y, qtyWidth, rect.height);

        EditorGUI.PropertyField(itemRect, itemProp, GUIContent.none);

        using (new EditorGUIUtility.IconSizeScope(Vector2.one * 12f))
        {
            using (new EditorGUI.ChangeCheckScope())
            {
                int v = Mathf.Max(1, EditorGUI.IntField(qtyRect, qtyProp.intValue));
                if (v != qtyProp.intValue)
                    qtyProp.intValue = v;
            }
        }
    }
}
#endif
