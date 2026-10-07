#nullable enable
using UnityEngine;

namespace RuniOS.Utility.Attributes
{
    public sealed class TypeFieldAttribute(Type baseType) : PropertyAttribute
    {
        public Type baseType { get; } = baseType;
    }
}