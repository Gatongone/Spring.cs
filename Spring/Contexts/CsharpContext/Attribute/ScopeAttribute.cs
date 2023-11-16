using System;

namespace Spring;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ScopeAttribute(ScopeType scope) : Attribute
{
    public readonly ScopeType scope = scope;
}