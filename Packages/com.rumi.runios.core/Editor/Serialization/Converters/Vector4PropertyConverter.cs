#nullable enable
using System;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(Vector4))]
    public class Vector4PropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.vector4Value;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.vector4Value = (Vector4)(value ?? new Vector4());
    }
}