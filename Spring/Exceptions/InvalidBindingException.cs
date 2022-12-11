using System;

namespace Spring
{
    public class InvalidBindingException : Exception
    {
        public InvalidBindingException(string msg) : base(msg) { }
    }
}