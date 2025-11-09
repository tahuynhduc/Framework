#region Base Service

using System;
using UnityEngine;

public abstract class ServiceBase : MonoBehaviour
{
    protected virtual void Awake()
    {
            ServiceRegistry.Register(gameObject, this);
    }

    protected virtual void OnDestroy()
    {
            ServiceRegistry.Unregister(gameObject, this);
    }
}

#endregion