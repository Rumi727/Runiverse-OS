#nullable enable
namespace RuniOS.Inspectors.Attributes
{
    public abstract class InspectorAttributeDrawer
    {
        public IInspectorAttribute attribute { get; }

        protected InspectorAttributeDrawer(IInspectorAttribute attribute) => this.attribute = attribute;
    }
}