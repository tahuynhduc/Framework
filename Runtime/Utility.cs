using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

public static class EventExtensions
{
    private static readonly Dictionary<string, Coroutine> _timers = new();

    #region Public API

    public static void Invoke(this MonoBehaviour mono, Action action, float delay, bool realtime = false)
    {
        string key = GetKey(mono, action);
        mono.Cancel(key);
        _timers[key] = mono.StartCoroutine(RunOnce(mono, key, action, delay, realtime));
    }

    public static void InvokeRepeating(this MonoBehaviour mono, Action action, float repeat, bool realtime = false)
    {
        string key = GetKey(mono, action);
        mono.Cancel(key);
        _timers[key] = mono.StartCoroutine(RunRepeat(mono, key, action, repeat, realtime));
    }

    public static void InvokeWhen(this MonoBehaviour mono, Action action, Func<bool> condition)
    {
        string key = GetKey(mono, action);
        mono.Cancel(key);
        _timers[key] = mono.StartCoroutine(RunWhen(mono, key, action, condition));
    }
    public static void InvokeUntil(this MonoBehaviour mono, Action action, float repeat, Func<bool> stopCondition, bool realtime = false)
    {
        string key = GetKey(mono, action);
        mono.Cancel(key);
        _timers[key] = mono.StartCoroutine(RunUntil(mono, key, action, repeat, stopCondition, realtime));
    }

    public static void InvokeWhile(this MonoBehaviour mono, Action action, float repeat, Func<bool> keepCondition, bool realtime = false)
    {
        string key = GetKey(mono, action);
        mono.Cancel(key);
        _timers[key] = mono.StartCoroutine(RunWhile(mono, key, action, repeat, keepCondition, realtime));
    }

    public static void CancelInvoke(this MonoBehaviour mono, Action action)
    {
        string key = GetKey(mono, action);
        mono.Cancel(key);
    }

    #endregion

    #region Private Coroutines

    private static IEnumerator RunOnce(MonoBehaviour mono, string key, Action action, float delay, bool realtime)
    {
        if (realtime) yield return new WaitForSecondsRealtime(delay);
        else yield return new WaitForSeconds(delay);

        if (mono != null) action?.Invoke();
        _timers.Remove(key);
    }

    private static IEnumerator RunRepeat(MonoBehaviour mono, string key, Action action, float repeat, bool realtime)
    {
        while (mono != null)
        {
            if (realtime) yield return new WaitForSecondsRealtime(repeat);
            else yield return new WaitForSeconds(repeat);

            action?.Invoke();
        }
        _timers.Remove(key);
    }

    private static IEnumerator RunWhen(MonoBehaviour mono, string key, Action action, Func<bool> condition)
    {
        yield return new WaitUntil(condition);
        if (mono != null) action?.Invoke();
        _timers.Remove(key);
    }

    private static IEnumerator RunUntil(MonoBehaviour mono, string key, Action action, float repeat, Func<bool> stopCondition, bool realtime)
    {
        while (mono != null && !stopCondition())
        {
            if (realtime) yield return new WaitForSecondsRealtime(repeat);
            else yield return new WaitForSeconds(repeat);

            action?.Invoke();
        }
        _timers.Remove(key);
    }

    private static IEnumerator RunWhile(MonoBehaviour mono, string key, Action action, float repeat, Func<bool> keepCondition, bool realtime)
    {
        while (mono != null && keepCondition())
        {
            if (realtime) yield return new WaitForSecondsRealtime(repeat);
            else yield return new WaitForSeconds(repeat);

            action?.Invoke();
        }
        _timers.Remove(key);
    }

    #endregion

    #region Helpers

    private static string GetKey(MonoBehaviour mono, Action action)
    {
        return $"{mono.GetInstanceID()}_{action.Method.Name}_{action.Target?.GetHashCode()}";
    }

    private static void Cancel(this MonoBehaviour mono, string key)
    {
        if (_timers.TryGetValue(key, out var coroutine))
        {
            if (coroutine != null)
            {
                try { mono.StopCoroutine(coroutine); } catch { }
            }
            _timers.Remove(key);
        }
    }
    #endregion
}

public static class MonoBehaviourLogExtensions
{
  private static void Print(LogType type, string msg)
    {
        switch (type)
        {
            case LogType.Log: Debug.Log(msg); break;
            case LogType.Warning: Debug.LogWarning(msg); break;
            case LogType.Error: Debug.LogError(msg); break;
        }
    }

    private static string FormatFields(object obj)
    {
        if (obj == null) return "null";

        var type = obj.GetType();
        if (type.IsPrimitive || obj is string || obj is decimal)
            return obj.ToString();

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

        var sb = new StringBuilder();
        foreach (var f in fields)
        {
            var value = f.GetValue(obj);
            sb.Append($"{f.Name}={value}; ");
        }
        return sb.ToString();
    }

    // overload cho message
    private static void InternalLog(
        MonoBehaviour mono,
        string message,
        LogType logType,
        Color defaultColor)
    {
        string colorHex = $"#{ColorUtility.ToHtmlStringRGB(defaultColor)}";
        Print(logType,
            $"<color=cyan>{mono.name}</color>:\n<color={colorHex}>{message}</color>");
    }

    // overload cho array
    private static void InternalLog<T>(
        MonoBehaviour mono,
        IEnumerable<T> array,
        LogType logType,
        Color defaultColor)
    {
        string colorHex = $"#{ColorUtility.ToHtmlStringRGB(defaultColor)}";

        if (array == null)
        {
            Print(logType, $"<color=cyan>{mono.name}</color>: collection is null");
            return;
        }

        var sb = new StringBuilder();
        int index = 0;
        foreach (var item in array)
        {
            sb.AppendLine($"<color={colorHex}>{index}.{item}: {FormatFields(item)}</color>");
            index++;
        }

        Print(logType, $"<color=cyan>{mono.name}</color>:\n{sb}");
    }

    // ========= Public APIs =========

    // --- INFO ---
    public static void Log(this MonoBehaviour mono, string message, Color? messageColor = null) =>
        InternalLog(mono, message, LogType.Log, messageColor ?? Color.green);

    public static void Log<T>(this MonoBehaviour mono, IEnumerable<T> array, Color? messageColor = null) =>
        InternalLog(mono, array, LogType.Log, messageColor ?? Color.green);

    // --- WARNING ---
    public static void LogWarning(this MonoBehaviour mono, string message, Color? messageColor = null) =>
        InternalLog(mono, message, LogType.Warning, messageColor ?? Color.yellow);

    public static void LogWarning<T>(this MonoBehaviour mono, IEnumerable<T> array, Color? messageColor = null) =>
        InternalLog(mono, array, LogType.Warning, messageColor ?? Color.yellow);

    // --- ERROR ---
    public static void LogError(this MonoBehaviour mono, string message, Color? messageColor = null) =>
        InternalLog(mono, message, LogType.Error, messageColor ?? Color.red);

    public static void LogError<T>(this MonoBehaviour mono, IEnumerable<T> array, Color? messageColor = null) =>
        InternalLog(mono, array, LogType.Error, messageColor ?? Color.red);
    
}

public static class UnifiedLogExtensions
{
    private static void Print(LogType type, string msg)
    {
        switch (type)
        {
            case LogType.Log: Debug.Log(msg); break;
            case LogType.Warning: Debug.LogWarning(msg); break;
            case LogType.Error: Debug.LogError(msg); break;
        }
    }

    private static string FormatFields(object obj)
    {
        if (obj == null) return "null";

        var type = obj.GetType();
        if (type.IsPrimitive || obj is string || obj is decimal)
            return obj.ToString();

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

        var sb = new StringBuilder();
        foreach (var f in fields)
        {
            var value = f.GetValue(obj);
            sb.Append($"{f.Name}={value}; ");
        }
        return sb.ToString();
    }

    private static void InternalLog(
        object obj,
        string message,
        LogType logType,
        Color defaultColor)
    {
        string colorHex = $"#{ColorUtility.ToHtmlStringRGB(defaultColor)}";
        string objName = obj switch
        {
            MonoBehaviour mb => mb.name,
            null => "null",
            _ => obj.GetType().Name
        };
        Print(logType,
            $"<color=cyan>{objName}</color>:\n<color={colorHex}>{message}</color>");
    }

    private static void InternalLog<T>(
        object obj,
        IEnumerable<T> array,
        LogType logType,
        Color defaultColor)
    {
        string colorHex = $"#{ColorUtility.ToHtmlStringRGB(defaultColor)}";
        string objName = obj switch
        {
            MonoBehaviour mb => mb.name,
            null => "null",
            _ => obj.GetType().Name
        };

        if (array == null)
        {
            Print(logType, $"<color=cyan>{objName}</color>: collection is null");
            return;
        }

        var sb = new StringBuilder();
        int index = 0;
        foreach (var item in array)
        {
            sb.AppendLine($"<color={colorHex}>{index}.{item}: {FormatFields(item)}</color>");
            index++;
        }

        Print(logType, $"<color=cyan>{objName}</color>:\n{sb}");
    }

    // ========= Public APIs =========

    // --- INFO ---
    public static void Log(this object obj, string message, Color? messageColor = null) =>
        InternalLog(obj, message, LogType.Log, messageColor ?? Color.green);

    public static void Log<T>(this object obj, IEnumerable<T> array, Color? messageColor = null) =>
        InternalLog(obj, array, LogType.Log, messageColor ?? Color.green);

    // --- WARNING ---
    public static void LogWarning(this object obj, string message, Color? messageColor = null) =>
        InternalLog(obj, message, LogType.Warning, messageColor ?? Color.yellow);

    public static void LogWarning<T>(this object obj, IEnumerable<T> array, Color? messageColor = null) =>
        InternalLog(obj, array, LogType.Warning, messageColor ?? Color.yellow);

    // --- ERROR ---
    public static void LogError(this object obj, string message, Color? messageColor = null) =>
        InternalLog(obj, message, LogType.Error, messageColor ?? Color.red);

    public static void LogError<T>(this object obj, IEnumerable<T> array, Color? messageColor = null) =>
        InternalLog(obj, array, LogType.Error, messageColor ?? Color.red);
}

public static class RandomValue
{
    public static T Random<T>(this object obj, int max, int min = 0) => (T)(object)UnityEngine.Random.RandomRange(min, Mathf.Abs(max));

    public static T Random<T>(this object obj,float max, float min = 0) => (T)(object)UnityEngine.Random.RandomRange(min, Mathf.Abs(max));
    
    public static bool Random(this object obj,int rate,int distance) => UnityEngine.Random.Range(1, distance) <= rate;

    public static T Random<T>(this object obj,List<T> list,int start) => list[UnityEngine.Random.Range(start, list.Count)];
    
    public static T Random<T>(this object obj,T[] array) => array[UnityEngine.Random.Range(0, array.Length)];
    public static T Random<T>(this object obj,Enum max, Enum min) where T : Enum => (T)Enum.ToObject(typeof(T), UnityEngine.Random.Range(Convert.ToInt32(min), Convert.ToInt32(max))+1);
    public static T ConvertToEnum<T>(this object obj,string value) where T : Enum => (T)Enum.Parse(typeof(T), value);
}

public static class UtilityUserData
{
    public static void SaveData(this object obj)
    {
        PlayerPrefs.Save();
    }

    public static bool ContainKey(this object obj, string key)
    {
        return PlayerPrefs.HasKey(key);
    }
    public static void SetData(this object obj,string key, string data)
    {
        PlayerPrefs.SetString(key, data);
    }
    public static T GetData<T>(this object obj,string key,string defaultData)
    {
        var json = PlayerPrefs.GetString(key, defaultData);
        T data;

        try
        {
            data = JsonUtility.FromJson<T>(json);
    
            if (data == null)
            {
                // fallback nếu JsonUtility trả null
                data = JsonConvert.DeserializeObject<T>(defaultData);
            }
        }
        catch (ArgumentException e)
        {
            // fallback nếu JSON là dạng mảng, không hợp lệ với JsonUtility
            Debug.LogWarning($"Falling back to Json.NET: {e.Message}");
            data = JsonConvert.DeserializeObject<T>(json);
        }
        return data;
    }

    public static void SetData(this object obj, string key, string data, bool setJsonData = false)
    {
        if (!setJsonData)
        {
            SetData(obj, key, data);
            return;
        }
        
        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(Application.persistentDataPath, key);

        File.WriteAllText(path, json);
        Debug.Log("Saved to: " + path);
    }
    public static T GetData<T>(this object obj, string key, string defaultData,bool getJsonFolder = false)
    {
        if(!getJsonFolder) return GetData<T>(obj,key, defaultData);
        string path = Path.Combine(Application.persistentDataPath, key);

        if (!File.Exists(path))
        {
            // Debug.LogWarning("File not found: " + path);
            return GetData<T>(obj,key, defaultData);
        }

        string json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<T>(json);
    }
}