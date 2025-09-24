#nullable enable
using System;
using UnityEditor;

namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(ulong))]
    public class ULongPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.ulongValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.ulongValue = (ulong)(value ?? 0uL);
    }
}