using System;

namespace Spring;

public class NonExistentBeanDefinitionException : Exception
{
    public NonExistentBeanDefinitionException(string beanName) : base($"BeanDefinition named \"{beanName}\" does not exist. ") { }

    public NonExistentBeanDefinitionException(Type beanType) : base($"BeanDefinition typed \"{beanType}\" does not exist. ") { }
}