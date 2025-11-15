using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

// T kế thừa từ chính nó để đảm bảo HighModule con có thể được truyền vào ràng buộc
public abstract class HighModule<T> : ServiceBase, IHighModule 
    where T : HighModule<T>
{
    // Dictionary lưu trữ LowModule theo Type
    protected Dictionary<Type, ILowModule> lowModules = new();
    
    public bool TryGetLowModule<TConcreteLow>(out TConcreteLow module) where TConcreteLow : ILowModule
    {
        if (lowModules.TryGetValue(typeof(TConcreteLow), out var lowModule))
        {
            if (lowModule is TConcreteLow concreteModule)
            {
                module = concreteModule;
                return true;
            }
        }
        module = default;
        return false;
    }
    
    public void RegisterLowModule(ILowModule module)
    {
        var moduleType = module.GetType();
        lowModules.TryAdd(moduleType, module);
    }

    public void UnregisterLowModule(ILowModule module)
    {
        lowModules.Remove(module.GetType());
    }
}