#nullable enable
using System;

namespace RuniOS.Inspectors.Drawers
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public class CustomInspectorDrawerAttribute : Attribute
    {
        public Type targetType { get; }
        
        public CustomInspectorDrawerAttribute(Type targetType) => this.targetType = targetType;
    }
}