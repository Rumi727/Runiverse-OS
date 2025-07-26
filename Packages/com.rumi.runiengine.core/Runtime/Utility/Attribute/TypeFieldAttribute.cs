#nullable enable
using System;
using UnityEngine;

namespace RuniEngine
{
    public sealed class TypeFieldAttribute : PropertyAttribute
    {
        public Type baseType;
        public TypeFieldAttribute(Type baseType) => this.baseType = baseType;
    }
}
