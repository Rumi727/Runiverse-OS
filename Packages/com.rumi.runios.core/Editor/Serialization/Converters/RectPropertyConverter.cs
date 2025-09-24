#nullable enable
using System;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(Rect))]
    public class RectPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.rectValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.rectValue = (Rect)(value ?? new Rect());
    }
}