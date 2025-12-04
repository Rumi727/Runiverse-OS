#nullable enable
using RuniOS.Reflection;

namespace RuniOS.Inspectors
{
    public interface IInspectorActionElement : IInspectorElement
    {
        Type? returnType { get; }
        NullabilityInfo? returnNullabilityInfo { get; }

        void Execute(object?[] parameters);
    }
}