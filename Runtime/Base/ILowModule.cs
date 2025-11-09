using UnityEngine;

// Giao diện cơ bản cho các Low Module
public interface ILowModule
{
    // Không cần phương thức nào, chỉ dùng để đánh dấu và ràng buộc kiểu (type constraint)
}

// Giao diện cho các High Module (Container/Context)
public interface IHighModule
{
    void RegisterLowModule(ILowModule module);
    void UnregisterLowModule(ILowModule module);
}