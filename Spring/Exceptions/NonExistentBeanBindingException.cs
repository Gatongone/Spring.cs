using System;

namespace Spring;

public class NonExistentBeanBindingException : Exception
{
    public NonExistentBeanBindingException(string beanName) : base($"No bean named \"{beanName}\" has bound. ") { }

    public NonExistentBeanBindingException(Type beanType) : base($"No bean typed \"{beanType}\" has bound. ") { }
}