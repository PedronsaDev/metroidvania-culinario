#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(SFXDatabase))]
public class SFXDatabaseEditor : Editor
{
    private SerializedProperty _entriesProp;
    private ReorderableList _list;
    private string _search = string.Empty;
    private GUIStyle _searchStyle;
    private int _selectedIndex = -1;

    // Preview helpers via internal AudioUtil
    private static Type _audioUtilType;
    private static MethodInfo _playPreviewMethod;
    private static MethodInfo _playClipMethod;
    private static MethodInfo _stopAllPreviewMethod;

    private void OnEnable()
    {
        _entriesProp = serializedObject.FindProperty("Entries");
        BuildList();
    }

    private void BuildList()
    {
        _list = new ReorderableList(serializedObject, _entriesProp, true, true, true, true)
        {
            drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(new Rect(rect.x + 6, rect.y, rect.width*0.45f, rect.height), "Key");
                EditorGUI.LabelField(new Rect(rect.x + rect.width*0.47f, rect.y, rect.width*0.38f, rect.height), "Clip");
            },
            drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var e = _entriesProp.GetArrayElementAtIndex(index);
                var key = e.FindPropertyRelative("Key");
                var clip = e.FindPropertyRelative("Clip");
                bool matches = MatchesSearch(key.stringValue, clip.objectReferenceValue as AudioClip);
                bool duplicate = IsDuplicateKey(index);
                bool missingClip = clip.objectReferenceValue == null;

                // Row background highlighting
                Color bg = Color.clear;
                if (missingClip) bg = new Color(1f, 0.95f, 0.6f);
                else if (duplicate) bg = new Color(1f, 0.6f, 0.6f);
                else if (!matches) bg = new Color(0.4f, 0.4f, 0.4f, 0.2f);
                if (bg.a > 0f || bg.r + bg.g + bg.b > 0f)
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y + 1, rect.width, rect.height - 2), bg);

                float line = EditorGUIUtility.singleLineHeight;
                float y = rect.y + 2;
                float x = rect.x + 6;
                float wKey = rect.width*0.45f - 12;
                float wClip = rect.width*0.45f - 36;

                key.stringValue = EditorGUI.TextField(new Rect(x, y, wKey, line), key.stringValue);
                EditorGUI.PropertyField(new Rect(x + wKey + 8, y, wClip, line), clip, GUIContent.none);

                using (new EditorGUI.DisabledScope(clip.objectReferenceValue == null))
                {
                    if (GUI.Button(new Rect(rect.x + rect.width - 44, y, 18, line), "▶")) PlayPreview(clip.objectReferenceValue as AudioClip);
                    if (GUI.Button(new Rect(rect.x + rect.width - 22, y, 18, line), "■")) StopAllPreview();
                }
            },
            elementHeightCallback = index => EditorGUIUtility.singleLineHeight + 6,
            onSelectCallback = l => _selectedIndex = l.index,
            onAddCallback = l =>
            {
                int i = _entriesProp.arraySize;
                _entriesProp.InsertArrayElementAtIndex(i);
                var e = _entriesProp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Key").stringValue = "new_key";
                e.FindPropertyRelative("Clip").objectReferenceValue = null;
                e.FindPropertyRelative("IsUI").boolValue = false;
                e.FindPropertyRelative("Is3D").boolValue = false;
                e.FindPropertyRelative("DefaultVolume").floatValue = 1f;
                e.FindPropertyRelative("DefaultPitch").floatValue = 1f;
                e.FindPropertyRelative("PitchVariance").floatValue = 0f;
                e.FindPropertyRelative("VolumeVariance").floatValue = 0f;
                e.FindPropertyRelative("SpatialBlend").floatValue = 1f;
                e.FindPropertyRelative("MinDistance").floatValue = 1f;
                e.FindPropertyRelative("MaxDistance").floatValue = 20f;
                e.FindPropertyRelative("Loop").boolValue = false;
                _selectedIndex = i;
            },
            onRemoveCallback = l =>
            {
                if (l.index >= 0 && l.index < _entriesProp.arraySize)
                {
                    _entriesProp.DeleteArrayElementAtIndex(l.index);
                    _selectedIndex = Mathf.Clamp(l.index - 1, 0, _entriesProp.arraySize - 1);
                }
            }
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var db = (SFXDatabase)target;

        // Toolbar
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            _searchStyle ??= EditorStyles.toolbarSearchField ?? EditorStyles.toolbarTextField;
            _search = GUILayout.TextField(_search, _searchStyle, GUILayout.MinWidth(160));
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(48)))
            {
                _search = string.Empty;
                GUI.FocusControl(null);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Auto-Fill Empty Keys", EditorStyles.toolbarButton)) AutoFillKeysFromClips(db);
            if (GUILayout.Button("Sort By Key", EditorStyles.toolbarButton)) SortByKey(db);
            if (GUILayout.Button("Validate & Rebuild", EditorStyles.toolbarButton))
            {
                db.RebuildCache();
                EditorUtility.SetDirty(db);
            }
        }

        // Info box
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            int dupes = CountDuplicateKeys();
            EditorGUILayout.LabelField($"Entries: {_entriesProp.arraySize}    Duplicate Keys: {dupes}");
        }

        // List
        _list.DoLayoutList();

        // Details panel
        if (_selectedIndex >= 0 && _selectedIndex < _entriesProp.arraySize)
        {
            var e = _entriesProp.GetArrayElementAtIndex(_selectedIndex);
            EditorGUILayout.Space(6);
            DrawDetailsPanel(e);
        }

        if (GUI.changed)
        {
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void DrawDetailsPanel(SerializedProperty e)
    {
        var key = e.FindPropertyRelative("Key");
        var clip = e.FindPropertyRelative("Clip");
        var isUI = e.FindPropertyRelative("IsUI");
        var is3D = e.FindPropertyRelative("Is3D");
        var vol = e.FindPropertyRelative("DefaultVolume");
        var pitch = e.FindPropertyRelative("DefaultPitch");
        var pVar = e.FindPropertyRelative("PitchVariance");
        var vVar = e.FindPropertyRelative("VolumeVariance");
        var sBlend = e.FindPropertyRelative("SpatialBlend");
        var minD = e.FindPropertyRelative("MinDistance");
        var maxD = e.FindPropertyRelative("MaxDistance");
        var loop = e.FindPropertyRelative("Loop");

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Selected Entry", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(key);
            EditorGUILayout.PropertyField(clip);

            using (new EditorGUILayout.HorizontalScope())
            {
                isUI.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("UI"), isUI.boolValue, GUILayout.Width(60));
                is3D.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("3D"), is3D.boolValue, GUILayout.Width(60));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Set Key From Clip Name", GUILayout.Width(180)))
                {
                    if (clip.objectReferenceValue is AudioClip ac) key.stringValue = ac.name;
                }
                using (new EditorGUI.DisabledScope(clip.objectReferenceValue == null))
                {
                    if (GUILayout.Button("▶", GUILayout.Width(28))) PlayPreview(clip.objectReferenceValue as AudioClip);
                    if (GUILayout.Button("■", GUILayout.Width(28))) StopAllPreview();
                }
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.Slider(vol, 0f, 1f, new GUIContent("Volume"));
            EditorGUILayout.Slider(pitch, -3f, 3f, new GUIContent("Pitch"));

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Variance", EditorStyles.miniBoldLabel);
            EditorGUILayout.Slider(pVar, 0f, 3f, new GUIContent("Pitch Var"));
            EditorGUILayout.Slider(vVar, 0f, 1f, new GUIContent("Vol Var"));

            if (is3D.boolValue)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("3D Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.Slider(sBlend, 0f, 1f, new GUIContent("Spatial Blend"));
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(minD, new GUIContent("Min Distance"));
                    EditorGUILayout.PropertyField(maxD, new GUIContent("Max Distance"));
                }
            }

            EditorGUILayout.Space(2);
            loop.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Loop"), loop.boolValue);

            // Warnings
            if (string.IsNullOrWhiteSpace(key.stringValue))
                EditorGUILayout.HelpBox("Key is empty.", MessageType.Warning);
            if (clip.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Clip is missing.", MessageType.Warning);
            if (IsDuplicateKey(_selectedIndex))
                EditorGUILayout.HelpBox("Duplicate key detected.", MessageType.Error);
        }
    }

    private bool MatchesSearch(string key, AudioClip clip)
    {
        if (string.IsNullOrEmpty(_search)) return true;
        var k = key ?? string.Empty;
        var c = clip != null ? clip.name : string.Empty;
        return k.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0 ||
               c.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private int CountDuplicateKeys()
    {
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < _entriesProp.arraySize; i++)
        {
            var e = _entriesProp.GetArrayElementAtIndex(i);
            var key = e.FindPropertyRelative("Key").stringValue ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (!seen.ContainsKey(key)) seen[key] = 0;
            seen[key]++;
        }
        return seen.Values.Count(v => v > 1);
    }

    private bool IsDuplicateKey(int index)
    {
        var e = _entriesProp.GetArrayElementAtIndex(index);
        var key = e.FindPropertyRelative("Key").stringValue ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key)) return false;
        for (int i = 0; i < _entriesProp.arraySize; i++)
        {
            if (i == index) continue;
            var other = _entriesProp.GetArrayElementAtIndex(i);
            var ok = other.FindPropertyRelative("Key").stringValue ?? string.Empty;
            if (string.Equals(ok, key, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private void AutoFillKeysFromClips(SFXDatabase db)
    {
        Undo.RecordObject(db, "Auto Fill Keys");
        bool changed = false;
        foreach (var entry in db.Entries)
        {
            if (entry == null) continue;
            if (string.IsNullOrWhiteSpace(entry.Key) && entry.Clip != null)
            {
                entry.Key = entry.Clip.name;
                changed = true;
            }
        }
        if (changed)
        {
            db.RebuildCache();
            EditorUtility.SetDirty(db);
        }
    }

    private void SortByKey(SFXDatabase db)
    {
        Undo.RecordObject(db, "Sort SFX Entries");
        db.Entries.Sort((a, b) => string.Compare(a?.Key ?? string.Empty, b?.Key ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        EditorUtility.SetDirty(db);
        serializedObject.Update();
    }

    private static void EnsureAudioUtil()
    {
        if (_audioUtilType != null) return;
        _audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        if (_audioUtilType == null) return;
        _playPreviewMethod = _audioUtilType.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
                             ?? _audioUtilType.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        _playClipMethod = _audioUtilType.GetMethod("PlayClip", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
                          ?? _audioUtilType.GetMethod("PlayClip", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        _stopAllPreviewMethod = _audioUtilType.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public)
                                ?? _audioUtilType.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.NonPublic);
    }

    private static void PlayPreview(AudioClip clip)
    {
        if (!clip) return;
        EnsureAudioUtil();
        if (_audioUtilType == null) return;
        if (_playPreviewMethod != null)
            _playPreviewMethod.Invoke(null, new object[] { clip, 0, false });
        else if (_playClipMethod != null)
            _playClipMethod.Invoke(null, new object[] { clip, 0, false });
    }

    private static void StopAllPreview()
    {
        EnsureAudioUtil();
        _stopAllPreviewMethod?.Invoke(null, null);
    }
}
#endif
