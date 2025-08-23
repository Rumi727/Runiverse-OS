#nullable enable
using RuniOS.Editor.Drawers;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Bindings
{
    [CustomPropertyBinder(typeof(SerializableType))]
    public class SerializableTypePropertyBinder : PropertyBinder
    {
        public override object Read(VisualElement element, SerializedProperty property, Type propertyType) => new SerializableType(TypeUtility.DeserializeFromString(SerializableTypePropertyDrawer.GetChildProperty(property).stringValue));
        
        public override void Write(VisualElement element, SerializedProperty property, Type propertyType, object? value)
        {
            if (value is SerializableType serializableType)
                SerializableTypePropertyDrawer.GetChildProperty(property).stringValue = serializableType.value?.SerializeToString() ?? string.Empty;
        }
    }
}