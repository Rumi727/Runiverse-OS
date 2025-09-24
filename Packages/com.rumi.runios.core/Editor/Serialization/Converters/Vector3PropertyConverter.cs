#nullable enable
using System;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Serialization.Converters
{
    [CustomPropertyConverter(typeof(Vector3))]
    public class Vector3PropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.vector3Value;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.vector3Value = (Vector3)(value ?? new Vector3());
    }
}