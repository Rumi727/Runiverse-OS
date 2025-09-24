#nullable enable
using System;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(BoundsInt))]
    public class BoundsIntPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.boundsIntValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.boundsIntValue = (BoundsInt)(value ?? new BoundsInt());
    }
}