#nullable enable
namespace RuniOS.Inspectors.Attributes
{
    public abstract class InspectorAttributeDrawer(IInspectorAttribute attribute)
    {
        public IInspectorAttribute attribute { get; } = attribute;
    }
}