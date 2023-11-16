using System;

namespace Spring;

public class MultipleBindingException : Exception
{
    public MultipleBindingException(string beanName) : base($"Bean named \"{beanName}\" has been bound. ") { }

    public MultipleBindingException(Type beanType) : base($"Bean typed \"{beanType}\" has been bound. ") { }
}