using System;
using System.Reflection;

namespace Spring
{
    public struct BeanDefinition
    {
        public ScopeType scope;
        public Type type;
        public MethodInfo proxyFactory;
    }
}