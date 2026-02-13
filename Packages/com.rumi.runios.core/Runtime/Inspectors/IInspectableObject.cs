#nullable enable
namespace RuniOS.Inspectors
{
    public interface IInspectableObject : IInspectable
    {
        IInspectorVariableElement GetVariableElement(string name, InspectorFlags flags = InspectorFlags.All);
        
        /// <inheritdoc cref="IInspectable.Clone"/>
        new IInspectableObject Clone();
        IInspectable IInspectable.Clone() => Clone();
    }
}