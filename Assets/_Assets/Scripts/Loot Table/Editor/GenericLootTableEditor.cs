#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace _Assets.Scripts.Loot_Table.Editor
{
    [CustomEditor(typeof(LootTableBase), true)]
    [CanEditMultipleObjects]
    public class GenericLootTableEditor : UnityEditor.Editor
    {
        private SerializedProperty _guaranteedProp;
        private SerializedProperty _additionalProp;

        private ReorderableList _guaranteedList;
        private ReorderableList _additionalList;

        private GUIStyle _boxStyle;

        private void OnEnable()
        {
            _guaranteedProp = serializedObject.FindProperty("_garanteedItems");
            _additionalProp = serializedObject.FindProperty("_additionalItems");

            _guaranteedList = BuildGuaranteedList(_guaranteedProp, "Guaranteed Drops");
            _additionalList = BuildAdditionalList(_additionalProp, "Additional Drops (weighted)");
        }

        public override void OnInspectorGUI()
        {
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 8, 8) };
            }

            serializedObject.Update();

            using (new EditorGUILayout.VerticalScope(_boxStyle))
            {
                _guaranteedList.DoLayoutList();
            }

            EditorGUILayout.Space(6);

            using (new EditorGUILayout.VerticalScope(_boxStyle))
            {
                _additionalList.DoLayoutList();

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(_additionalProp == null || _additionalProp.arraySize == 0))
                    {
                        if (GUILayout.Button(new GUIContent("Normalize Weights", "Scale weights so they sum to 100"), GUILayout.Width(150)))
                        {
                            NormalizeWeights(scaleTo: 100f);
                        }
                        if (GUILayout.Button(new GUIContent("Sort by Weight", "Sort items descending by weight"), GUILayout.Width(130)))
                        {
                            SortByWeightDescending();
                        }
                    }
                }
            }

            EditorGUILayout.Space(8);
            DrawChancesSection();

            if (serializedObject.ApplyModifiedProperties())
            {
                InvokeValidateTable();
                EditorUtility.SetDirty(target);
            }
        }

        private ReorderableList BuildGuaranteedList(SerializedProperty listProp, string header)
        {
            var rl = new ReorderableList(serializedObject, listProp, true, true, true, true);
            rl.elementHeight = EditorGUIUtility.singleLineHeight + 6;
            rl.headerHeight = EditorGUIUtility.singleLineHeight * 2f + 6f;
            rl.drawHeaderCallback = rect =>
            {
                var titleRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(titleRect, header, EditorStyles.boldLabel);

                var colsRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight, rect.width, EditorGUIUtility.singleLineHeight);
                DrawColumnsHeader(colsRect, showWeight: false);
            };
            rl.drawElementCallback = (rect, index, _, _) =>
            {
                var element = listProp.GetArrayElementAtIndex(index);
                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;

                var itemProp = element.FindPropertyRelative("Item");
                var quantityProp = element.FindPropertyRelative("QuantityRange");

                DrawItemQuantityRow(rect, itemProp, quantityProp, showWeight: false, weightProp: null);
            };
            return rl;
        }

        private ReorderableList BuildAdditionalList(SerializedProperty listProp, string header)
        {
            var rl = new ReorderableList(serializedObject, listProp, true, true, true, true);
            rl.elementHeight = EditorGUIUtility.singleLineHeight + 6;
            rl.headerHeight = EditorGUIUtility.singleLineHeight * 2f + 6f;
            rl.drawHeaderCallback = rect =>
            {
                var titleRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(titleRect, header, EditorStyles.boldLabel);

                var colsRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight, rect.width, EditorGUIUtility.singleLineHeight);
                DrawColumnsHeader(colsRect, showWeight: true);
            };
            rl.drawElementCallback = (rect, index, _, _) =>
            {
                var element = listProp.GetArrayElementAtIndex(index);
                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;

                var itemProp = element.FindPropertyRelative("Item");
                var qtyProp = element.FindPropertyRelative("QuantityRange");
                var weightProp = element.FindPropertyRelative("ProbabilityWeight");

                DrawItemQuantityRow(rect, itemProp, qtyProp, showWeight: true, weightProp: weightProp);
            };
            return rl;
        }

        private void DrawColumnsHeader(Rect rect, bool showWeight)
        {
            float pad = 4f;
            float itemWidth = rect.width * (showWeight ? 0.45f : 0.6f);
            float minFieldWidth = 40f;
            float dashWidth = 12f;
            float maxFieldWidth = 40f;

            var rItem = new Rect(rect.x, rect.y, itemWidth, rect.height);
            EditorGUI.LabelField(rItem, "Item to Drop", EditorStyles.miniBoldLabel);

            float afterItemX = rItem.xMax + pad;
            if (showWeight)
            {
                var rWeight = new Rect(afterItemX, rect.y, 80f, rect.height);
                EditorGUI.LabelField(rWeight, "Weight", EditorStyles.miniBoldLabel);
                afterItemX = rWeight.xMax + pad * 2f;
            }
            else
            {
                afterItemX = rItem.xMax + pad * 2f;
            }

            var rMin = new Rect(afterItemX, rect.y, minFieldWidth, rect.height);
            var rDash = new Rect(rMin.xMax + pad, rect.y, dashWidth, rect.height);
            var rMax = new Rect(rDash.xMax + pad, rect.y, maxFieldWidth, rect.height);

            EditorGUI.LabelField(rMin, " Min", EditorStyles.miniBoldLabel);
            //EditorGUI.LabelField(rDash, "", EditorStyles.miniBoldLabel);
            EditorGUI.LabelField(rMax, "Max", EditorStyles.miniBoldLabel);
        }

        private void DrawItemQuantityRow(Rect rect, SerializedProperty itemProp, SerializedProperty quantityProp, bool showWeight, SerializedProperty weightProp)
        {
            float pad = 4f;
            float itemWidth = rect.width * (showWeight ? 0.45f : 0.6f);
            float minFieldWidth = 40f;
            float dashWidth = 12f;
            float maxFieldWidth = 40f;

            // Item
            var rItem = new Rect(rect.x, rect.y, itemWidth, rect.height);
            EditorGUI.PropertyField(rItem, itemProp, GUIContent.none);

            // Weight
            float afterItemX = rItem.xMax + pad;
            if (showWeight && weightProp != null)
            {
                var rWeight = new Rect(afterItemX, rect.y, 80f, rect.height);
                EditorGUI.BeginChangeCheck();
                float w = EditorGUI.FloatField(rWeight, weightProp.floatValue);
                if (EditorGUI.EndChangeCheck())
                {
                    weightProp.floatValue = Mathf.Max(0f, w);
                }
                afterItemX = rWeight.xMax + pad * 2f;
            }
            else
            {
                afterItemX = rItem.xMax + pad * 2f;
            }

            var rMin = new Rect(afterItemX, rect.y, minFieldWidth, rect.height);
            var rDash = new Rect(rMin.xMax + pad, rect.y, dashWidth, rect.height);
            var rMax = new Rect(rDash.xMax + pad, rect.y, maxFieldWidth, rect.height);

            var minProp = quantityProp.FindPropertyRelative("x");
            var maxProp = quantityProp.FindPropertyRelative("y");

            EditorGUI.BeginChangeCheck();
            int newMin = EditorGUI.IntField(rMin, minProp.intValue);
            EditorGUI.LabelField(rDash, "-");
            int newMax = EditorGUI.IntField(rMax, maxProp.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                newMin = Mathf.Clamp(newMin, 1, 10);
                newMax = Mathf.Clamp(newMax, 1, 10);
                if (newMax < newMin) newMax = newMin;

                minProp.intValue = newMin;
                maxProp.intValue = newMax;
            }
        }

        private void DrawChancesSection()
        {
            EditorGUILayout.LabelField("Drop Chances", EditorStyles.boldLabel);

            if (_additionalProp == null)
            {
                EditorGUILayout.HelpBox("No additional weighted items.", MessageType.Info);
                return;
            }

            float total = 0f;
            for (int i = 0; i < _additionalProp.arraySize; i++)
            {
                var el = _additionalProp.GetArrayElementAtIndex(i);
                var w = el.FindPropertyRelative("ProbabilityWeight").floatValue;
                if (w > 0f) total += w;
            }

            if (total <= 0f)
            {
                EditorGUILayout.HelpBox("Coloque pesos > 0 para visualizar as porcentagens.", MessageType.Info);
                return;
            }

            for (int i = 0; i < _additionalProp.arraySize; i++)
            {
                var el = _additionalProp.GetArrayElementAtIndex(i);
                var item = el.FindPropertyRelative("Item");
                var w = Mathf.Max(0f, el.FindPropertyRelative("ProbabilityWeight").floatValue);
                float pct = (w / total);

                string label = BuildItemLabel(item);
                Rect r = GUILayoutUtility.GetRect(18, 18);
                EditorGUI.ProgressBar(r, pct, $"{label} — {(pct * 100f):F1}%");
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField($"Total Weight: {total:F2}", EditorStyles.miniBoldLabel);
        }

        private void NormalizeWeights(float scaleTo)
        {
            if (_additionalProp == null || _additionalProp.arraySize == 0) return;
            float total = 0f;
            for (int i = 0; i < _additionalProp.arraySize; i++)
            {
                var w = _additionalProp.GetArrayElementAtIndex(i).FindPropertyRelative("ProbabilityWeight").floatValue;
                if (w > 0f) total += w;
            }
            if (total <= 0f) return;

            for (int i = 0; i < _additionalProp.arraySize; i++)
            {
                var weightProp = _additionalProp.GetArrayElementAtIndex(i).FindPropertyRelative("ProbabilityWeight");
                float w = Mathf.Max(0f, weightProp.floatValue);
                weightProp.floatValue = (w / total) * scaleTo;
            }
            serializedObject.ApplyModifiedProperties();
            InvokeValidateTable();
            Repaint();
        }

        private void SortByWeightDescending()
        {
            if (_additionalProp == null || _additionalProp.arraySize <= 1) return;
            var items = new List<(int index, float weight)>();
            for (int i = 0; i < _additionalProp.arraySize; i++)
            {
                float w = _additionalProp.GetArrayElementAtIndex(i).FindPropertyRelative("ProbabilityWeight").floatValue;
                items.Add((i, w));
            }
            var order = items.OrderByDescending(t => t.weight).Select(t => t.index).ToList();

            for (int sortedPos = 0; sortedPos < order.Count; sortedPos++)
            {
                int currentIndex = order[sortedPos];
                _additionalProp.MoveArrayElement(currentIndex, sortedPos);
                for (int j = sortedPos + 1; j < order.Count; j++)
                {
                    if (order[j] < currentIndex) order[j]++;
                }
            }
            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        private string BuildItemLabel(SerializedProperty itemProp)
        {
            if (itemProp.propertyType == SerializedPropertyType.ObjectReference)
            {
                var obj = itemProp.objectReferenceValue;
                if (obj != null) return obj.name;
                return "Nothing";
            }
            return itemProp.displayName;
        }

        private void InvokeValidateTable()
        {
            try
            {
                var m = target.GetType().GetMethod("ValidateTable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                m?.Invoke(target, null);
            }
            catch (Exception)
            {
                // Ignore
            }
        }
    }
}
#endif
