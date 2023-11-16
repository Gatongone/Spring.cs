using System;

namespace Spring;

public interface IBeanDefinitionsCollection
{
    void Add(Type beanType, BeanDefinition beanDefinition);
    void Add(string beanName, BeanDefinition beanDefinition);
    BeanDefinition Get(string beanName);
    BeanDefinition Get(Type beanType);
    bool TryGet(string beanName, out BeanDefinition beanDefinition);
    bool TryGet(Type beanType, out BeanDefinition beanDefinition);
    bool Contains(string beanName);
    bool Contains(Type beanType);
    bool Remove(string beanName);
    bool Remove(Type type);
}