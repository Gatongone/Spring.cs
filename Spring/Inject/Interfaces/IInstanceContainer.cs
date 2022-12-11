using System;

namespace Spring
{
    public interface IInstanceContainer
    {
        void BindSingleton(Type type, object? obj);

        void BindSingleton(string name, object? obj);

        void BindPrototype(string name, Type type);
        void BindPrototype(Type type, Type MappingType);

        bool RemoveSingletonBinding(string name);

        bool RemoveSingletonBinding(Type type);

        bool RemovePrototypeBinding(string name);

        bool RemovePrototypeBinding(Type type);

        bool ContainsSingleton(string name);

        bool ContainsSingleton(Type type);

        bool ContainsPrototype(string name);

        bool ContainsPrototype(Type type);

        object? RequireSingleton(string name, Type type);

        object? RequireSingleton(Type type);

        object RequirePrototype(string name, params object[] args);

        object RequirePrototype(Type type, params object[] args);
    }
}