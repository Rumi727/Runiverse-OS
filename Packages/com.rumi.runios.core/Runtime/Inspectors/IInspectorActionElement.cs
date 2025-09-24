#nullable enable
using System;
using System.Reflection;

namespace RuniOS.Inspectors
{
    public interface IInspectorActionElement : IInspectorElement
    {
        Type? returnType { get; }
        NullabilityInfo? returnNullabilityInfo { get; }

        void Execute(object?[] parameters);
    }
}