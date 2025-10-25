#nullable enable
using System;

namespace RuniOS.Inspectors
{
    public interface IInspectorActionElement : IInspectorElement
    {
        Type? returnType { get; }
        RuniNullabilityInfo? returnNullabilityInfo { get; }

        void Execute(object?[] parameters);
    }
}