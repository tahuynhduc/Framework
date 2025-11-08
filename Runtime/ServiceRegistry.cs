using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#region 📌 Attributes & Scope

/// <summary>
/// Scope dùng cho [Resolve] (auto inject trên cùng GameObject/children)
/// </summary>
public enum ResolveLocalScope
{
    LocalOnly, // Trên cùng GameObject
    AutoAdd    // Nếu là Component, tự AddComponent
}

/// <summary>
/// Scope dùng cho runtime Resolve<T> (không liên quan parent)
/// </summary>
public enum ResolveRuntimeScope
{
    LocalOnly,  // Tìm trên requester GameObject
    GlobalOnly, // Tìm tất cả GameObject trong scene
    AutoAdd     // Nếu là Component, tự AddComponent trên requester
}

/// <summary>
/// This attribute to get a type with type declared has scope  
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ResolveAttribute : Attribute
{
    public ResolveLocalScope Scope { get; }
    public ResolveAttribute(ResolveLocalScope scope = ResolveLocalScope.LocalOnly) => Scope = scope;
}
/// <summary>
/// This attribute to get many type with type declared has scope 
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ResolvesAttribute : Attribute
{
    public ResolveLocalScope Scope { get; }
    public ResolvesAttribute(ResolveLocalScope scope = ResolveLocalScope.LocalOnly) => Scope = scope;
}
/// <summary>
/// This attribute to tick this class as the attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ServiceAttribute : Attribute { }

#endregion

#region 📌 Service Registry

public static class ServiceRegistry
{
    private static readonly Dictionary<GameObject, List<object>> servicesByObject = new();
    private static readonly Dictionary<Type, FieldInfo[]> cachedResolveFields = new();

    #region Register / Unregister

    public static void Register(GameObject owner, object service)
    {
        if (!servicesByObject.TryGetValue(owner, out var list))
        {
            list = new List<object>();
            servicesByObject[owner] = list;
        }

        if (!list.Contains(service))
        {
            list.Add(service);
            AutoResolveAll(owner); // auto inject fields local
        }
    }

    public static void Unregister(GameObject owner, object service)
    {
        if (servicesByObject.TryGetValue(owner, out var list))
            list.Remove(service);
    }

    #endregion

    #region Resolve API (runtime)

    public static T Resolve<T>(GameObject requester, ResolveRuntimeScope scope = ResolveRuntimeScope.GlobalOnly) where T : class
        => Resolve(requester, typeof(T), scope) as T;

    public static List<T> Resolves<T>(GameObject requester, ResolveRuntimeScope scope = ResolveRuntimeScope.GlobalOnly) where T : class
    {
        var objects = Resolves(requester, typeof(T), scope);
        return objects.ConvertAll(x => x as T);
    }

    #endregion

    #region Core Resolve

    private static object Resolve(GameObject requester, Type type, ResolveRuntimeScope scope)
    {
        return scope switch
        {
            ResolveRuntimeScope.LocalOnly => ResolveLocal(requester, type),
            ResolveRuntimeScope.GlobalOnly => ResolveGlobal(type),
            ResolveRuntimeScope.AutoAdd => ResolveAutoAdd(requester, type),
            _ => null
        };
    }

    private static List<object> Resolves(GameObject requester, Type type, ResolveRuntimeScope scope)
    {
        List<object> result = new();

        switch (scope)
        {
            case ResolveRuntimeScope.LocalOnly:
                result.AddRange(ResolveManyLocal(requester, type));
                break;
            case ResolveRuntimeScope.GlobalOnly:
                result.AddRange(ResolveManyGlobal(type));
                break;
            case ResolveRuntimeScope.AutoAdd:
                result.AddRange(ResolveManyLocal(requester, type));
                if (result.Count == 0 && typeof(Component).IsAssignableFrom(type))
                {
                    Component auto = requester.AddComponent(type) as Component;
                    if (auto != null)
                    {
                        Register(requester, auto);
                        result.Add(auto);
                    }
                }
                break;
        }

        return result;
    }

    private static object ResolveAutoAdd(GameObject go, Type type)
    {
        var resolved = Resolve(go, type, ResolveRuntimeScope.LocalOnly);
        if (resolved == null && typeof(Component).IsAssignableFrom(type))
        {
            Component auto = go.AddComponent(type) as Component;
            if (auto != null)
            {
                Register(go, auto);
                resolved = auto;
            }
        }
        return resolved;
    }

    #region Helpers

    private static object ResolveLocal(GameObject go, Type type)
    {
        if (servicesByObject.TryGetValue(go, out var list))
            foreach (var s in list)
                if (type.IsInstanceOfType(s)) return s;
        return null;
    }

    private static object ResolveGlobal(Type type)
    {
        foreach (var pair in servicesByObject)
            foreach (var s in pair.Value)
                if (type.IsInstanceOfType(s)) return s;
        return null;
    }

    private static List<object> ResolveManyLocal(GameObject go, Type type)
    {
        List<object> res = new();
        if (servicesByObject.TryGetValue(go, out var list))
            foreach (var s in list)
                if (type.IsInstanceOfType(s)) res.Add(s);
        return res;
    }

    private static List<object> ResolveManyGlobal(Type type)
    {
        List<object> res = new();
        foreach (var pair in servicesByObject)
            foreach (var s in pair.Value)
                if (type.IsInstanceOfType(s)) res.Add(s);
        return res;
    }

    #endregion

    #endregion

    #region AutoResolve (local only)

    private static void AutoResolveAll(GameObject owner)
    {
        foreach (var comp in owner.GetComponentsInChildren<Component>(true))
            ResolveFields(comp);
    }

    public static void ResolveFields(object target)
    {
        if (!(target is Component comp)) return;

        var type = target.GetType();
        if (!cachedResolveFields.TryGetValue(type, out var fields))
        {
            fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            cachedResolveFields[type] = fields;
        }

        foreach (var f in fields)
        {
            var singleAttr = f.GetCustomAttribute<ResolveAttribute>();
            var manyAttr = f.GetCustomAttribute<ResolvesAttribute>();
            if (singleAttr == null && manyAttr == null) continue;

            if (manyAttr != null)
            {
                if (f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementType = f.FieldType.GetGenericArguments()[0];
                    var resolvedList = Resolves(comp.gameObject, elementType, ResolveRuntimeScope.GlobalOnly); // runtime resolve for List
                    var listInstance = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                    var addMethod = listInstance.GetType().GetMethod("Add");
                    foreach (var obj in resolvedList) addMethod.Invoke(listInstance, new[] { obj });
                    f.SetValue(target, listInstance);
                }
                continue;
            }

            if (singleAttr != null)
            {
                object resolved = null;
                if (singleAttr.Scope == ResolveLocalScope.LocalOnly || singleAttr.Scope == ResolveLocalScope.AutoAdd)
                    resolved = ResolveLocal(comp.gameObject, f.FieldType);
                f.SetValue(target, resolved);
            }
        }
    }

    #endregion
}

#endregion

#region Base Service

// Tự register/unregister nếu class có [Service]
[Service]
public abstract class ServiceBase : MonoBehaviour
{
    protected virtual void Awake()
    {
        if (Attribute.IsDefined(GetType(), typeof(ServiceAttribute)))
            ServiceRegistry.Register(gameObject, this);
    }

    protected virtual void OnDestroy()
    {
        if (Attribute.IsDefined(GetType(), typeof(ServiceAttribute)))
            ServiceRegistry.Unregister(gameObject, this);
    }
}

#endregion


#region Example to use

public class ExampleChacracter : MonoBehaviour
{
    [Resolve] private ExampleHead _exampleHead;

    [Resolves] private List<ExampleHand> _exampleHands;
    
    private ExampleHead ExampleHead => ServiceRegistry.Resolve<ExampleHead>(gameObject);
    
    private List<ExampleHand> ExampleHands =>  ServiceRegistry.Resolves<ExampleHand>(gameObject);
    private void Start()
    {
        
        if (_exampleHead != null)
            _exampleHead?.RotateHead();

        foreach (var e in _exampleHands)
            e?.Attack();
        
        ExampleHead?.RotateHead();
        
        foreach (var e in ExampleHands)
            e?.Attack();
    }
}

public class ExampleHead : ServiceBase
{
    public void RotateHead()
    {
        Debug.Log($"{gameObject.name}");
    }
}

public class ExampleHand : ServiceBase
{
    public void Attack()
    {
        Debug.Log($"{gameObject.name}");
    }
}

#endregion
