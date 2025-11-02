#nullable enable
using System;

namespace RuniOS.Inspectors.Drawers
{
    public sealed class CustomInspectorDrawerAttribute : CustomAttributeDrawerAttribute
    {
        public override bool isSubtypeCompatible => true;
        
        public CustomInspectorDrawerAttribute(Type targetType) : base(targetType) { }
    }
}