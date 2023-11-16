using System;
using System.Collections.Generic;

namespace Spring;

public class BeanDefinitionsCollection : IBeanDefinitionsCollection
{
    private readonly Dictionary<string, BeanDefinition> m_BeanNameDefinitionsMap = new();
    private readonly Dictionary<Type, BeanDefinition> m_BeanTypeDefinitionsMap = new();

    public void Add(Type beanType, BeanDefinition beanDefinition)
    {
        if (m_BeanTypeDefinitionsMap.ContainsKey(beanType))
            throw new MultipleBeanDefinitionException(beanType);
        m_BeanTypeDefinitionsMap.Add(beanType, beanDefinition);
    }

    public void Add(string beanName, BeanDefinition beanDefinition)
    {
        if (m_BeanNameDefinitionsMap.ContainsKey(beanName))
            throw new MultipleBeanDefinitionException(beanName);
        m_BeanNameDefinitionsMap.Add(beanName, beanDefinition);
    }

    public BeanDefinition Get(Type beanType)
    {
        if (m_BeanTypeDefinitionsMap.TryGetValue(beanType, out var beanDefinition))
        {
            return beanDefinition;
        }

        throw new NonExistentBeanDefinitionException(beanType);
    }

    public bool TryGet(string beanName, out BeanDefinition beanDefinition) => m_BeanNameDefinitionsMap.TryGetValue(beanName, out beanDefinition);

    public bool TryGet(Type beanType, out BeanDefinition beanDefinition) => m_BeanTypeDefinitionsMap.TryGetValue(beanType, out beanDefinition);

    public BeanDefinition Get(string beanName)
    {
        if (m_BeanNameDefinitionsMap.TryGetValue(beanName, out var beanDefinition))
        {
            return beanDefinition;
        }

        throw new NonExistentBeanDefinitionException(beanName);
    }

    public bool Contains(string beanName) => m_BeanNameDefinitionsMap.ContainsKey(beanName);

    public bool Contains(Type beanType) => m_BeanTypeDefinitionsMap.ContainsKey(beanType);

    public bool Remove(string beanName) => m_BeanNameDefinitionsMap.Remove(beanName);

    public bool Remove(Type type) => m_BeanTypeDefinitionsMap.Remove(type);
}