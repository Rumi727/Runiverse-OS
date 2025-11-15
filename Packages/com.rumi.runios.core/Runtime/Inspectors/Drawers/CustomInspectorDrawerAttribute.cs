#nullable enable
namespace RuniOS.Inspectors.Drawers
{
    public sealed class CustomInspectorDrawerAttribute : CustomAttributeDrawerAttribute
    {
        public bool allowInDebug { get; set; }
        
        public override bool isSubtypeCompatible { get; }

        public CustomInspectorDrawerAttribute(Type targetType, bool useForChildren = false) : base(targetType) => isSubtypeCompatible = useForChildren;
    }
}