using System;

namespace Spring;

[AttributeUsage(AttributeTargets.Method)]

public sealed class ProxyAttribute : Attribute
{
    private readonly string m_BeanName;
    public ProxyAttribute(string beanName) => m_BeanName = beanName;
    public ProxyAttribute(){}
    public string GetBeanName() => m_BeanName;
}