using System;

namespace Spring
{
    public class InvalidPatternException : Exception
    {
        public InvalidPatternException() : base("Pattern parsing failed. ")
        {
        }
    }
}