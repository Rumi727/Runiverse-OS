#nullable enable
using System;
using UnityEngine;

namespace RuniOS
{
    public sealed class TypeFieldAttribute : PropertyAttribute
    {
        public TypeFieldAttribute(Type baseType) => this.baseType = baseType;
        
        public Type baseType { get; }
    }
}
