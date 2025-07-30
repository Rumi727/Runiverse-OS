#nullable enable
using RuniOS.Editor.Drawers;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Bindings
{
    [CustomPropertyBinder(typeof(HexColor))]
    public class HexColorPropertyBinder : PropertyBinder
    {
        public override object? Read(VisualElement element, SerializedProperty property, Type propertyType) => new HexColor(HexColorPropertyDrawer.GetChildProperty(property).stringValue);
        public override void Write(VisualElement element, SerializedProperty property, Type propertyType, object? value)
        {
            if (value is HexColor hexColor)
                HexColorPropertyDrawer.GetChildProperty(property).stringValue = hexColor.value;
        }
    }
}