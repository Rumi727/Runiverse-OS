#nullable enable
using System;

namespace RuniEngine.Resource
{
    public sealed class InvalidIdentifierException : Exception
    {
        public InvalidIdentifierException() { }
        public InvalidIdentifierException(string message) : base(message) { }
        public InvalidIdentifierException(string message, Exception innerException) : base(message, innerException) { }
    }
}
