using UnityEngine;

public abstract class SingletonCustom<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool _applicationIsQuitting = false;

    [SerializeField] 
    private bool destroyOnLoad = false;

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting) return null;

            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();

                if (_instance == null)
                {
                    var obj = new GameObject(typeof(T).Name);
                    _instance = obj.AddComponent<T>();
                    // _instance.Log($"[SingletonCustom<{typeof(T)}>] Auto-created instance: {obj.name}");
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // _instance.LogWarning($"[SingletonCustom<{typeof(T)}>] Duplicate detected, destroying {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        _instance = this as T;

        if (!destroyOnLoad)
            DontDestroyOnLoad(gameObject);

        Init();
    }

    protected virtual void Init() { }

    private void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }
}