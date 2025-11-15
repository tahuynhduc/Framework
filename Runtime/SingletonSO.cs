using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "SingletonSO", menuName = "SO/SingletonSO")]
public class SingletonSO : ScriptableObject
{
    private static SingletonSO _instance;
    public static SingletonSO Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<SingletonSO>("SingletonSO");
#if UNITY_EDITOR
                if (_instance == null)
                {
                    Debug.LogError("❌ Không tìm thấy SingletonSO trong Resources!");
                }
#endif
            }
            return _instance;
        }
    }

    [Header("📂 Thư mục chứa ScriptableObjects")]
    [SerializeField] private string targetFolder = "Assets/ScriptableObjects";
    [SerializeField] private string assetType;
    [SerializeField] private SerializableDictionary<string, ScriptableObject> _map;

#if UNITY_EDITOR
    [SerializeField, HideInInspector]
    private List<ScriptableObject> _editorCache = new();
#endif

    // 🧠 Chạy mỗi khi asset thay đổi (Editor only)
#if UNITY_EDITOR
    [Button]
    private void OnValidate()
    {
        AutoLoadAssetsInEditor();
        BuildMap();
    }
#endif

    private void BuildMap()
    {
        if (_map == null)
            _map = new SerializableDictionary<string, ScriptableObject>();
        else
            _map.Clear();

#if UNITY_EDITOR
        foreach (var so in _editorCache)
        {
            if (so == null) continue;
            string key = so.GetType().FullName;
            if (!_map.ContainsKey(key))
            {
                _map.Add(key, so);
            }
            else
            {
                Debug.LogWarning($"⚠️ Trùng ScriptableObject loại {key}, chỉ giữ cái đầu tiên.");
            }
        }
#endif
    }

    /// <summary>
    /// ✅ Trả về bản sao (runtime instance) của ScriptableObject
    /// </summary>
    public T Get<T>() where T : ScriptableObject
    {
        EnsureMapBuilt();

        string key = typeof(T).FullName;

        if (_map != null && _map.TryGetValue(key, out var so))
        {
            return Instantiate(so) as T;
        }

        Debug.LogError($"❌ Không tìm thấy ScriptableObject loại {key}");
        return null;
    }

    /// <summary>
    /// 🧭 Lấy bản gốc (asset trong project hoặc Resources)
    /// </summary>
    public T GetOriginal<T>() where T : ScriptableObject
    {
        EnsureMapBuilt();

        string key = typeof(T).FullName;

        if (_map != null && _map.TryGetValue(key, out var so))
        {
            return so as T;
        }

        Debug.LogError($"❌ Không tìm thấy ScriptableObject loại {key}");
        return null;
    }

    private void EnsureMapBuilt()
    {
        if (_map == null || _map.Count == 0)
        {
#if UNITY_EDITOR
            AutoLoadAssetsInEditor();
            BuildMap();
#else
            // 🔹 Runtime: load tất cả ScriptableObjects từ Resources
            _map = new SerializableDictionary<string, ScriptableObject>();
            var allSOs = Resources.LoadAll<ScriptableObject>("");
            foreach (var so in allSOs)
            {
                if (so == null) continue;
                string key = so.GetType().FullName;
                if (!_map.ContainsKey(key))
                    _map.Add(key, so);
            }
#endif
        }
    }

#if UNITY_EDITOR
    [ContextMenu("🔄 Auto Load ScriptableObjects")]
    public void AutoLoadAssetsInEditor()
    {
        _editorCache.Clear();
        _editorCache = GetAssetsReader.GetAssets<ScriptableObject>(targetFolder,assetType);
        Debug.Log($"✅ Đã load {_editorCache.Count} ScriptableObjects từ {targetFolder}");
    }
#endif
}

public static class GetAssetsReader
{
    public static List<T> GetAssets<T>(string targetFolder,string type) where T : Object
    {
        List<T> assets = new List<T>();
        string[] guids = AssetDatabase.FindAssets($"t:{type}", new[] { targetFolder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                assets.Add(asset);
            }
        }

        return assets;
    }
}