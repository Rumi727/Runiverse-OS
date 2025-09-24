#nullable enable
using System;
using UnityEditor;

namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(long))]
    public class LongPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.longValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.longValue = (long)(value ?? 0L);
    }
}