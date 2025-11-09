using System;
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class LowModule<T> : SerializedMonoBehaviour, ILowModule 
    where T : class, IHighModule // Bắt buộc T phải là HighModule
{
    private T _highLevel;
    
    protected T HighLevel => _highLevel;
    
    protected virtual void Start()
    {
        _highLevel = ServiceRegistry.Resolve<T>(gameObject); 
        // 2. Đăng ký chính nó vào HighModule nếu thành công
        if (_highLevel != null)
        {
            _highLevel.RegisterLowModule(this);
            OnHighLevelReady(_highLevel);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] LowModule<{typeof(T).Name}> could not resolve HighModule dependency. Is it a Service/Component?");
        }
    }

    protected virtual void OnDestroy()
    {
        // 3. Hủy đăng ký khi bị hủy
        if (_highLevel != null)
        {
            _highLevel.UnregisterLowModule(this);
        }
    }
    
    /// <summary>
    /// Hook method được gọi khi HighLevel đã được Resolve và LowModule đã đăng ký thành công.
    /// Lớp con nên override để thực hiện logic khởi tạo.
    /// </summary>
    protected virtual void OnHighLevelReady(T high) { }
}