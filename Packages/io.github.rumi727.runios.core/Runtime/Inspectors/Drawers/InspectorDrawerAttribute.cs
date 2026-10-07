#nullable enable
using RuniOS.Reflection;

namespace RuniOS.Inspectors.Drawers
{
    public class InspectorDrawerAttribute : TypeRegistrationAttribute
    {
        public InspectorDrawerAttribute(Type targetType, bool useForChildren = false) : base(targetType) => this.useForChildren = useForChildren;

        public bool allowInDebug { get; init; }
    }
}