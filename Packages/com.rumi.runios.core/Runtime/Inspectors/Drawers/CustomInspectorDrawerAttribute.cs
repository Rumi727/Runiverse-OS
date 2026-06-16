#nullable enable
namespace RuniOS.Inspectors.Drawers
{
    public class CustomInspectorDrawerAttribute(Type targetType, bool useForChildren = false) : TypeHandlerAttribute(targetType)
    {
        public bool allowInDebug { get; set; }
        
        public override bool isSubtypeCompatible { get; } = useForChildren;
    }
}