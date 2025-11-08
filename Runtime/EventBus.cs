using System;
using System.Collections.Generic;

/// <summary>
/// EventBus: hệ thống Publish/Subscribe kiểu type-safe
/// Không phụ thuộc Unity, dễ tái sử dụng.
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> Listeners = new();

    /// <summary>
    /// Đăng ký lắng nghe một sự kiện T
    /// </summary>
    public static void Subscribe<T>(Action<T> callback)
    {
        var eventType = typeof(T);

        if (!Listeners.ContainsKey(eventType))
            Listeners[eventType] = new List<Delegate>();

        if (!Listeners[eventType].Contains(callback))
            Listeners[eventType].Add(callback);
    }

    /// <summary>
    /// Hủy lắng nghe sự kiện T
    /// </summary>
    public static void Unsubscribe<T>(Action<T> callback)
    {
        var eventType = typeof(T);

        if (Listeners.TryGetValue(eventType, out var list))
        {
            list.Remove(callback);
            if (list.Count == 0)
                Listeners.Remove(eventType);
        }
    }

    /// <summary>
    /// Publish event đến tất cả listeners T
    /// </summary>
    public static void Publish<T>(T eventData)
    {
        var eventType = typeof(T);

        if (Listeners.TryGetValue(eventType, out var list))
        {
            var invokeList = list.ToArray(); // tránh modify khi đang iterate
            foreach (Delegate del in invokeList)
                (del as Action<T>)?.Invoke(eventData);
        }
    }
}

/// <summary>
/// Ví dụ Event cụ thể (bạn có thể tạo nhiều event khác theo mẫu này)
/// </summary>
public struct PlayerDied
{
    public string playerName;
    public int score;

    public PlayerDied(string name, int score)
    {
        playerName = name;
        this.score = score;
    }
}