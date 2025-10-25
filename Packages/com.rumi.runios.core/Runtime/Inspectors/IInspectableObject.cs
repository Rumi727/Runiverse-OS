#nullable enable
namespace RuniOS.Inspectors
{
    public interface IInspectableObject : IInspectable
    {
        new IInspectableObject Clone();
    }
}