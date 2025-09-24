#nullable enable
using System;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(Quaternion))]
    public class QuaternionPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.quaternionValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.quaternionValue = (Quaternion)(value ?? new Quaternion());
    }
}