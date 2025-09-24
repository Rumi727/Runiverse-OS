#nullable enable
using System;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(Color32))]
    public class Color32PropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => (Color32)property.colorValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.colorValue = (Color32)(value ?? new Color32());
    }
}