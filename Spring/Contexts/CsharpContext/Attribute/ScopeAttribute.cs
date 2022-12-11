using System;

namespace Spring
{
    
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ScopeAttribute : Attribute
    {
        public ScopeType scope { get; }

        public ScopeAttribute(ScopeType scopeType) => scope = scopeType;
    }
}