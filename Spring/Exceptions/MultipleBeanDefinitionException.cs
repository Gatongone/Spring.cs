using System;

namespace Spring
{
    public class MultipleBeanDefinitionException : Exception
    {
        public MultipleBeanDefinitionException(string beanName) : base($"BeanDefinition named \"{beanName}\" already exists. ") { }

        public MultipleBeanDefinitionException(Type beanType) : base($"BeanDefinition typed \"{beanType}\" already exists. ") { }
    }
}