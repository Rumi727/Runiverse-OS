#nullable enable
using RuniOS.Reflection;

namespace RuniOS.Inspectors
{
    public interface IInspectorActionElement : IInspectorElement
    {
        Type? returnType { get; }
        NullabilityInfo? returnNullabilityInfo { get; }

        void Execute(object?[] parameters);
        
        /// <inheritdoc cref="IInspectorElement.Clone"/>
        new IInspectorActionElement Clone();
        IInspectorElement IInspectorElement.Clone() => Clone();
    }
}