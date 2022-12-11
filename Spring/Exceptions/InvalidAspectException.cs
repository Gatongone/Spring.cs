using System;

namespace Spring
{
    public class InvalidAspectException : ScanningException
    {
        public InvalidAspectException(string message) : base(message) { }
    }
}