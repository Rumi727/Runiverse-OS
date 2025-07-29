#nullable enable
using System;

namespace RuniEngine.Editor.UIElements.Bindings
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CustomPropertyBinderAttribute : Attribute
    {
        public CustomPropertyBinderAttribute(Type targetType, bool isSubtypeCompatible = false)
        {
            this.targetType = targetType;
            this.isSubtypeCompatible = isSubtypeCompatible;
        }

        public Type targetType { get; }
        public bool isSubtypeCompatible { get; }
    }
}