using System;

namespace Spring
{
    public interface IBeanPipeline
    {
        object GenerateProxy(Type bean);
        object RequirePrototypeBean(string beanName, Type type, BeanDefinition beanDefinition, params object[] parameters);
        object RequireSingletonBean(string beanName, Type type, BeanDefinition beanDefinition);
    }
}