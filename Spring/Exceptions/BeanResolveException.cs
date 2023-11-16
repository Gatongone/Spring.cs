using System;

namespace Spring;

public class BeanResolveException : Exception
{
    public BeanResolveException(string msg) : base(msg) { }
}