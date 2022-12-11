using System;

namespace Spring
{
    public class ResolveObjectException : Exception
    {
        public ResolveObjectException(string msg) : base(msg) { }
    }
}