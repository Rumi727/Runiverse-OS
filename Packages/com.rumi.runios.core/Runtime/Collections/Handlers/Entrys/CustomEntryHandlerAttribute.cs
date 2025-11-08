#nullable enable
using System;

namespace RuniOS.Collections.Handlers.Entrys
{
    public sealed class CustomEntryHandlerAttribute : CustomAttributeDrawerAttribute
    {
        public override bool isSubtypeCompatible => true;
        
        public CustomEntryHandlerAttribute(Type targetType) : base(targetType) { }
    }
}