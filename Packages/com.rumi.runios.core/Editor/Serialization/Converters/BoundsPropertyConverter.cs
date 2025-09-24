#nullable enable
using System;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(Bounds))]
    public class BoundsPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.boundsValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.boundsValue = (Bounds)(value ?? new Bounds());
    }
}