#nullable enable
using System;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(Gradient))]
    public class GradientPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.gradientValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.gradientValue = (Gradient)(value ?? 0);
    }
}