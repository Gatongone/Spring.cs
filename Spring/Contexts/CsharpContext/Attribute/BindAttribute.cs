using System;

namespace Spring;

public sealed class BindAttribute : Attribute
{
    private readonly Type m_ImplementType;

    public BindAttribute(Type implementType) => m_ImplementType = implementType;

    public Type GetImplementType() => m_ImplementType;
}