using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class SFXEntry
{
    [FormerlySerializedAs("key")]
    [Tooltip("Unique key used to play this SFX.")] public string Key;

    [FormerlySerializedAs("clip")]
    [Tooltip("Audio clip to play.")] public AudioClip Clip;

    [Header("Category & Mode")]
    [FormerlySerializedAs("isUI")] public bool IsUI = false;
    [FormerlySerializedAs("is3D")] public bool Is3D = false;

    [FormerlySerializedAs("defaultVolume")] [Range(0f,1f)] public float DefaultVolume = 1f;
    [FormerlySerializedAs("defaultPitch")] public float DefaultPitch = 1f;

    [Header("Variance")]
    [FormerlySerializedAs("pitchVariance")] [Range(0f, 3f)] public float PitchVariance = 0f;
    [FormerlySerializedAs("volumeVariance")] [Range(0f, 1f)] public float VolumeVariance = 0f;

    [Header("3D Settings")]
    [FormerlySerializedAs("spatialBlend")] [Tooltip("Only if is3D true")] [Range(0f,1f)] public float SpatialBlend = 1f;
    [FormerlySerializedAs("minDistance")] public float MinDistance = 1f;
    [FormerlySerializedAs("maxDistance")] public float MaxDistance = 20f;

    [Header("Loop Settings")] [FormerlySerializedAs("loop")] public bool Loop = false;
}

public class SFXDatabase : ScriptableObject
{
    private static SFXDatabase _instance;
    public static SFXDatabase Instance
    {
        get
        {
            if (!_instance)
            {
                _instance = Resources.Load<SFXDatabase>("SFX");
                if (!_instance)
                {
#if UNITY_EDITOR
                    var found = UnityEditor.AssetDatabase.FindAssets("t:SFXDatabase");
                    if (found != null && found.Length > 0)
                    {
                        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(found[0]);
                        _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<SFXDatabase>(path);
                    }
#endif
                }
            }
            return _instance;
        }
        set { _instance = value; }
    }

    [Tooltip("List of SFX entries. Keys must be unique.")] public List<SFXEntry> Entries = new List<SFXEntry>();
    private readonly Dictionary<string, SFXEntry> _cache = new Dictionary<string, SFXEntry>();

    private void OnEnable()
    {
        RebuildCache();
        if (!_instance) _instance = this;
    }

    public void RebuildCache()
    {
        _cache.Clear();
        foreach (var e in Entries)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.Key) || !e.Clip) continue;
            if (_cache.ContainsKey(e.Key))
            {
                Debug.LogWarning($"Duplicate SFX key '{e.Key}' ignored in database {name}.");
                continue;
            }
            _cache.Add(e.Key, e);
        }
    }

    public bool TryGet(string key, out SFXEntry entry)
    {
        if (string.IsNullOrWhiteSpace(key)) { entry = null; return false; }
        if (_cache.Count != Entries.Count) RebuildCache();
        return _cache.TryGetValue(key, out entry);
    }

    public AudioSource Play(string key, Vector3? position = null, bool loopOverride = false)
    {
        if (!TryGet(key, out var entry))
        {
            Debug.LogWarning($"SFX key '{key}' not found.");
            return null;
        }
        var am = AudioManager.Instance;
        if (entry.Is3D)
        {
            var pos = position ?? Vector3.zero;
            if (loopOverride || entry.Loop)
            {
                return am.PlayLoopSFX3D(entry.Clip, pos, entry.DefaultVolume, entry.SpatialBlend, entry.MinDistance, entry.MaxDistance, entry.DefaultPitch);
            }
            return am.PlaySFX3D(entry.Clip, pos, entry.DefaultVolume, entry.SpatialBlend, entry.MinDistance, entry.MaxDistance, entry.DefaultPitch, entry.PitchVariance, entry.VolumeVariance);
        }
        else
        {
            if (loopOverride || entry.Loop)
            {
                return am.PlayLoopSFX(entry.Clip, entry.DefaultVolume, entry.DefaultPitch, entry.IsUI);
            }
            if (entry.IsUI)
            {
                return am.PlayUISFX(entry.Clip, entry.DefaultVolume, entry.DefaultPitch, entry.PitchVariance, entry.VolumeVariance);
            }
            return am.PlaySFX(entry.Clip, entry.DefaultVolume, entry.DefaultPitch, entry.PitchVariance, entry.VolumeVariance);
        }
    }
}

#if UNITY_EDITOR
public static class SFXDatabaseEditorUtilities
{
    [UnityEditor.MenuItem("Tools/Audio/Rebuild SFX Database Cache", priority = 201)]
    private static void RebuildFromMenu()
    {
        var guids = UnityEditor.AssetDatabase.FindAssets("t:SFXDatabase");
        foreach (var g in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
            var db = UnityEditor.AssetDatabase.LoadAssetAtPath<SFXDatabase>(path);
            if (db)
            {
                db.RebuildCache();
                UnityEditor.EditorUtility.SetDirty(db);
                Debug.Log($"Rebuilt SFX cache for '{db.name}'.");
            }
        }
        UnityEditor.AssetDatabase.SaveAssets();
    }
}
#endif

#if UNITY_EDITOR
public static class SFXDatabaseAssetMenu
{
    [UnityEditor.MenuItem("Assets/Create/Audio/SFX Database", priority = 300)]
    private static void CreateAsset()
    {
        var asset = ScriptableObject.CreateInstance<SFXDatabase>();
        string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath("Assets/Resources/SFX.asset");
        var dir = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
        UnityEditor.AssetDatabase.CreateAsset(asset, path);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.EditorUtility.FocusProjectWindow();
        UnityEditor.Selection.activeObject = asset;
        Debug.Log("Created SFXDatabase at " + path + ". Place audio clips via inspector.");
    }
}
#endif
