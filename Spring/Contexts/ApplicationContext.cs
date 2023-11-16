using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Spring;

public abstract class ApplicationContext : IApplicationHierarchy
{
    private const BindingFlags INJECT_FLAGS = BindingFlags.Instance | BindingFlags.Public;
    private readonly Dictionary<string, Type> m_NamesBindingMap = new();
    private readonly Dictionary<Type, Type> m_TypesBindingMap = new();
    private readonly InstanceContainer m_EarlyBeanFactory = new(); //Has generated proxy but never inject
    private readonly InstanceContainer m_FinalBeanFactory = new(); //Has generated proxy and injected
    private readonly Lazy<IBeanDefinitionsCollection> m_BeanDefinitions;
    private readonly Lazy<IApplicationHierarchy> m_Child;
    protected readonly ICutpointMaps cutpointMaps = new CutpointMaps();

    protected ApplicationContext()
    {
        m_Child = new Lazy<IApplicationHierarchy>(GetImplement);
        m_BeanDefinitions = new Lazy<IBeanDefinitionsCollection>(GetEnvironment);
    }

    #region Hierarchy

    public abstract IApplicationHierarchy GetImplement();

    #endregion

    #region BeanBinder

    public void Bind(string beanName, Type implementType)
    {
        if (m_NamesBindingMap.ContainsKey(beanName))
        {
            throw new MultipleBindingException(beanName);
        }

        var beanType = m_Child.Value.GetEnvironment().Get(beanName).type;
        if (!implementType.IsAssignableTo(beanType))
            throw new InvalidBindingException($"{implementType} is not assignable to {beanType}");
        m_NamesBindingMap[beanName] = implementType;
    }

    public void Bind(Type beanType, Type implementType)
    {
        if (m_TypesBindingMap.ContainsKey(beanType))
        {
            throw new MultipleBindingException(beanType);
        }

        if (!implementType.IsAssignableTo(beanType))
            throw new InvalidBindingException($"{implementType} is not assignable to {beanType}");
        m_TypesBindingMap[beanType] = implementType;
    }

    public Type GetImplementType(string beanName)
    {
        if (m_NamesBindingMap.TryGetValue(beanName, out var type))
        {
            return type;
        }

        throw new NonExistentBeanBindingException(beanName);
    }

    public Type GetImplementType(Type beanType)
    {
        if (m_TypesBindingMap.TryGetValue(beanType, out var type))
        {
            return type;
        }

        throw new NonExistentBeanBindingException(beanType);
    }

    public bool TryGetImplementType(string beanName, [MaybeNullWhen(false)] out Type implementType)
    {
        return m_NamesBindingMap.TryGetValue(beanName, out implementType);
    }

    public bool TryGetImplementType(Type beanType, [MaybeNullWhen(false)] out Type implementType)
    {
        return m_TypesBindingMap.TryGetValue(beanType, out implementType);
    }

    #endregion

    #region BeanPipeline

    public abstract IBeanDefinitionsCollection GetEnvironment();

    public object GenerateProxy(Type beanType)
    {
        var methodContext = RuntimeAssembly.CreateProxy(beanType);
        foreach (var info in cutpointMaps.GetCutpointInfos())
        {
            var cutpoint = info.cutpoint;
            if (!cutpoint.MatchType(beanType) || info.advices == null) continue;
            foreach (var method in beanType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!method.IsVirtual || method.IsFinal || method.Name.Equals("Equals") || method.Name.Equals("ToString") || method.Name.Equals("GetHashCode"))
                    continue;
                if (cutpoint.MatchMethod(method))
                {
                    methodContext.WithCutpointAdvices(method, info.advices);
                }
            }
        }

        return methodContext.GetResult();
    }

    public object RequireSingletonBean(string beanName, Type type, BeanDefinition beanDefinition)
    {
        object result;

        //Create bean by name
        if (!string.IsNullOrEmpty(beanName))
        {
            //Has injected
            if (m_FinalBeanFactory.ContainsSingleton(beanName))
                return m_FinalBeanFactory.RequireSingleton(beanName, type);
            //Has generated proxy
            if (m_EarlyBeanFactory.ContainsSingleton(beanName))
            {
                result = m_EarlyBeanFactory.RequireSingleton(beanName, type);
                return result;
            }

            result = beanDefinition.proxyFactory != null ? beanDefinition.proxyFactory.Invoke(null, null) : GenerateProxy(type);

            if (result == null) return null;

            //Callback before initialization
            if (result is IBeanInitializationPreprocessor preprocessor_name)
                preprocessor_name.OnBeforeInitialization(result, beanName);

            m_EarlyBeanFactory.BindSingleton(beanName, result);

            InjectMembers(result, beanName);

            //Callback after initialization
            if (result is IBeanInitializationPostprocessor postprocessor_name)
                postprocessor_name.OnAfterInitialization(result, beanName);

            m_EarlyBeanFactory.RemoveSingletonBinding(beanName);
            if (!m_FinalBeanFactory.ContainsSingleton(beanName))
                return result;

            m_FinalBeanFactory.BindSingleton(beanName, result);
            return result;
        }

        //Create bean by type
        //Has injected
        if (m_FinalBeanFactory.ContainsSingleton(type))
            return m_FinalBeanFactory.RequireSingleton(type);

        //Has generated proxy
        if (m_EarlyBeanFactory.ContainsSingleton(type))
        {
            result = m_EarlyBeanFactory.RequireSingleton(type);
            return result;
        }

        result = beanDefinition.proxyFactory != null ? beanDefinition.proxyFactory.Invoke(null, null) : GenerateProxy(type);

        if (result == null) return null;

        //Callback before initialization
        if (result is IBeanInitializationPreprocessor preprocessor_type)
            preprocessor_type.OnBeforeInitialization(result, null);

        m_EarlyBeanFactory.BindSingleton(type, result);

        InjectMembers(result, null);

        //Callback after initialization
        if (result is IBeanInitializationPostprocessor postprocessor_type)
            postprocessor_type.OnAfterInitialization(result, beanName);

        m_EarlyBeanFactory.RemoveSingletonBinding(type);
        if (m_FinalBeanFactory.ContainsSingleton(type))
            return result;
        m_FinalBeanFactory.BindSingleton(type, result);
        return result;
    }

    public object RequirePrototypeBean(string beanName, Type type, BeanDefinition beanDefinition, params object[] parameters)
    {
        object result;

        //Create bean by name
        if (!string.IsNullOrEmpty(beanName))
        {
            //Has injected
            if (m_FinalBeanFactory.ContainsPrototype(beanName))
            {
                result = m_FinalBeanFactory.RequirePrototype(beanName, parameters);
                return result;
            }

            //result = m_FinalBeanFactory.RequirePrototype(type, parameters);

            result = GenerateProxy(type);

            //Callback before initialization
            if (result is IBeanInitializationPreprocessor preprocessor_name)
                preprocessor_name.OnBeforeInitialization(result, beanName);

            if (result == null) return null;

            InjectMembers(result, null);

            //Callback after initialization
            if (result is IBeanInitializationPostprocessor postprocessor_name)
                postprocessor_name.OnAfterInitialization(result, beanName);

            m_FinalBeanFactory.BindPrototype(beanName, result.GetType());
            return result;
        }
        //Create bean by type

        //Has injected
        if (m_FinalBeanFactory.ContainsPrototype(type))
        {
            result = m_FinalBeanFactory.RequirePrototype(type, parameters);
            return result;
        }

        result = beanDefinition.proxyFactory != null ? beanDefinition.proxyFactory.Invoke(null, parameters) : GenerateProxy(type);

        //Callback before initialization
        if (result is IBeanInitializationPreprocessor preprocessor_type)
            preprocessor_type.OnBeforeInitialization(result, null);

        if (result == null) return null;

        InjectMembers(result, null);

        //Callback after initialization
        if (result is IBeanInitializationPostprocessor postprocessor_type)
            postprocessor_type.OnAfterInitialization(result, beanName);

        m_FinalBeanFactory.BindPrototype(type, result.GetType());
        return result;
    }

    #endregion

    #region BeanFactory

    public object GetBean(string beanName)
    {
        if (string.IsNullOrEmpty(beanName) || !m_BeanDefinitions.Value.TryGet(beanName, out var beanDefinition))
        {
            return null;
        }

        var beanType = m_NamesBindingMap.ContainsKey(beanName) ? m_NamesBindingMap[beanName] : beanDefinition.type;

        return beanDefinition.scope == ScopeType.Singleton ? RequireSingletonBean(beanName, beanType, beanDefinition) : RequirePrototypeBean(beanName, beanType, beanDefinition);
    }

    public object GetBean(string beanName, params object[] args)
    {
        if (string.IsNullOrEmpty(beanName) || !m_BeanDefinitions.Value.TryGet(beanName, out var beanDefinition))
        {
            return null;
        }

        var beanType = m_NamesBindingMap.ContainsKey(beanName) ? m_NamesBindingMap[beanName] : beanDefinition.type;

        return beanDefinition.scope != ScopeType.Prototype ? throw new BeanResolveException("The Bean with args must be prototype.") : RequirePrototypeBean(beanName, beanType, beanDefinition, args);
    }

    public object GetBean(Type beanType)
    {
        if (!m_BeanDefinitions.Value.TryGet(beanType, out var beanDefinition))
        {
            return null;
        }

        beanType = m_TypesBindingMap.ContainsKey(beanType) ? m_TypesBindingMap[beanType] : beanDefinition.type;

        return beanDefinition.scope == ScopeType.Singleton ? RequireSingletonBean(null, beanType, beanDefinition) : RequirePrototypeBean(null, beanType, beanDefinition);
    }

    public object GetBean(Type beanType, object[] args)
    {
        if (!m_BeanDefinitions.Value.TryGet(beanType, out var beanDefinition))
        {
            return null;
        }

        beanType = m_TypesBindingMap.ContainsKey(beanType) ? m_TypesBindingMap[beanType] : beanDefinition.type;

        return beanDefinition.scope != ScopeType.Prototype ? throw new BeanResolveException("The Bean with args must be prototype.") : RequirePrototypeBean(null, beanType, beanDefinition, args);
    }

    public object GetBean(Type beanType, string beanName)
    {
        return GetBean(beanName) ?? GetBean(beanType);
    }

    public bool IsSingleton(string beanName) => m_BeanDefinitions.Value.Contains(beanName) && m_BeanDefinitions.Value.Get(beanName).scope == ScopeType.Singleton;

    public bool IsSingleton(Type beanType) => m_BeanDefinitions.Value.Contains(beanType) && m_BeanDefinitions.Value.Get(beanType).scope == ScopeType.Singleton;

    public bool IsPrototype(string beanName) => m_BeanDefinitions.Value.Contains(beanName) && m_BeanDefinitions.Value.Get(beanName).scope == ScopeType.Prototype;

    public bool IsPrototype(Type beanType) => m_BeanDefinitions.Value.Contains(beanType) && m_BeanDefinitions.Value.Get(beanType).scope == ScopeType.Prototype;

    public bool IsTypeMatch(string beanName, Type type) => type.IsAssignableTo(m_BeanDefinitions.Value.Get(beanName).type);

    #endregion

    #region Protected

    protected abstract bool NeedMemberInject(MemberInfo member);

    protected abstract object OnMemberInjection(MemberInfo member, object bean);

    #endregion

    #region Private

    private void InjectMembers(object bean, string? beanName)
    {
        ForeachMembers(bean, memberInfo =>
        {
            if (bean is IMemberInjectionPreprocessor preprocessor)
                preprocessor.OnBeforeInjection(memberInfo, bean, beanName);

            switch (memberInfo)
            {
                case PropertyInfo propertyInfo:
                    propertyInfo.SetValue(bean, OnMemberInjection(memberInfo, bean));
                    break;
                case FieldInfo fieldInfo:
                    fieldInfo.SetValue(bean, OnMemberInjection(memberInfo, bean));
                    break;
            }

            if (bean is IMemberInjectionPostprocessor postprocessor)
                postprocessor.OnAfterInjection(memberInfo, bean, beanName);
        });
    }

    private void ForeachMembers(object bean, Action<MemberInfo> injectAction)
    {
        var beanType = bean.GetType();

        //Inject fields
        foreach (var field in beanType.GetFields(INJECT_FLAGS))
        {
            if (!NeedMemberInject(field))
                continue;
            injectAction?.Invoke(field);
        }

        //Inject properties
        foreach (var property in beanType.GetProperties(INJECT_FLAGS))
        {
            if (!NeedMemberInject(property))
                continue;
            injectAction?.Invoke(property);
        }
    }

    #endregion
}