using System;

namespace Spring
{
    public interface IBeanFactory : IBeanMaps<string>, IBeanMaps<Type>
    {
        object GetBean(string beanName, params object[] args);
        object GetBean(Type beanType, string beanName);
        object GetBean(Type beanType, object[] args);
        T GetBean<T>(string beanName) => (T) GetBean(typeof(T), beanName);
        T GetBean<T>() => (T) GetBean(typeof(T));
        T GetBean<T>(object[] args) => (T) GetBean(typeof(T), args);
        bool IsSingleton(string beanName);
        bool IsPrototype(string beanName);
        bool IsSingleton(Type beanType);
        bool IsPrototype(Type beanType);
        bool IsSingleton<T>() => IsSingleton(typeof(T));
        bool IsPrototype<T>() => IsPrototype(typeof(T));
        bool IsTypeMatch(string beanName, Type type);
        bool IsTypeMatch<T>(string beanName) => IsTypeMatch(beanName, typeof(T));
    }
}