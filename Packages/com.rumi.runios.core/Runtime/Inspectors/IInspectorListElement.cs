#nullable enable
using System;

namespace RuniOS.Inspectors
{
    public interface IInspectorListElement : IInspectorVariableElement
    {
        Type? elementType { get; }
        int index { get; }
    }
}