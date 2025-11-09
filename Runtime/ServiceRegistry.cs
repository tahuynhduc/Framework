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
#endregion

#region 📌 Service Registry

public static class ServiceRegistry
{
    private static readonly Dictionary<GameObject, List<object>> servicesByObject = new();

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
}

#endregion
