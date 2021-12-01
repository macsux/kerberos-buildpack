using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace KerberosCommon;

public class ValidationException : Exception
{
    public ValidationException()
    {
    }

    protected ValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ValidationException(string? message) : base(message)
    {
    }

    public ValidationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public List<string> Errors { get; set; } = new();
    public override string Message => string.Join("\n", Errors);
}