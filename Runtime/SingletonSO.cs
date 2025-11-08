using System;
using System.Collections.Generic;
using UnityEngine;

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

    private Dictionary<Type, ScriptableObject> _map;

#if UNITY_EDITOR
    [SerializeField, HideInInspector]
    private List<ScriptableObject> _editorCache = new();
#endif

    // 🧠 Gọi mỗi khi asset có thay đổi (Editor only)
#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoLoadAssetsInEditor();
        BuildMap();
    }
#endif

    private void BuildMap()
    {
        if (_map == null)
            _map = new Dictionary<Type, ScriptableObject>();
        else
            _map.Clear();

#if UNITY_EDITOR
        foreach (var so in _editorCache)
        {
            if (so == null) continue;
            var type = so.GetType();
            if (!_map.ContainsKey(type))
            {
                _map.Add(type, so);
            }
            else
            {
                Debug.LogWarning($"⚠️ Trùng ScriptableObject loại {type.Name}, chỉ giữ cái đầu tiên.");
            }
        }
#endif
    }

    /// <summary>
    /// ✅ Trả về bản sao (runtime instance) của ScriptableObject
    /// </summary>
    public T Get<T>() where T : ScriptableObject
    {
#if UNITY_EDITOR
        if (_map == null || _map.Count == 0)
        {
            AutoLoadAssetsInEditor();
            BuildMap();
        }
#endif

        if (_map.TryGetValue(typeof(T), out var so))
        {
            return Instantiate(so) as T;
        }

        Debug.LogError($"❌ Không tìm thấy ScriptableObject loại {typeof(T).Name}");
        return null;
    }

    /// <summary>
    /// 🧭 Lấy bản gốc (asset trong project)
    /// </summary>
    public T GetOriginal<T>() where T : MonoBehaviour
    {
#if UNITY_EDITOR
        if (_map == null || _map.Count == 0)
        {
            AutoLoadAssetsInEditor();
            BuildMap();
        }
#endif

        if (_map.TryGetValue(typeof(T), out var so))
        {
            return so as T;
        }

        Debug.LogError($"❌ Không tìm thấy ScriptableObject loại {typeof(T).Name}");
        return null;
    }

#if UNITY_EDITOR
    [ContextMenu("🔄 Auto Load ScriptableObjects")]
    public void AutoLoadAssetsInEditor()
    {
        _editorCache.Clear();
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { targetFolder });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (so != null)
            {
                _editorCache.Add(so);
            }
        }

        Debug.Log($"✅ Đã load {_editorCache.Count} ScriptableObjects từ {targetFolder}");
    }
#endif
}
