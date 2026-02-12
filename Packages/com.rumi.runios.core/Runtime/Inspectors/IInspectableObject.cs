#nullable enable
namespace RuniOS.Inspectors
{
    public interface IInspectableObject : IInspectable
    {
        /// <inheritdoc cref="IInspectable.Clone"/>
        new IInspectableObject Clone();
        IInspectable IInspectable.Clone() => Clone();
    }
}