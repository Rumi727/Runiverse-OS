#nullable enable
using RuniOS.Editor.Drawers;
using RuniOS.Editor.Drawers.Resource;
using RuniOS.Editor.UIElements.Bindings.IO;
using RuniOS.IO;
using RuniOS.Resource;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Bindings.Resource
{
    [CustomPropertyBinder(typeof(PackIdentifier))]
    public class PackIdentifierPropertyBinder : PropertyBinder
    {
        public override object Read(VisualElement element, SerializedProperty property, Type propertyType)
        {
            (SerializedProperty nullableInternalID, SerializedProperty nullableLocalPath) = PackIdentifierPropertyDrawer.GetChildProperty(property);
            (SerializedProperty? internalID, SerializedProperty? internalIDToggle) = SerializableNullablePropertyDrawer.GetChildProperty(nullableInternalID);
            (SerializedProperty? localPath, SerializedProperty? localPathToggle) = SerializableNullablePropertyDrawer.GetChildProperty(nullableLocalPath);

            if ((internalIDToggle?.boolValue ?? false) && internalID != null)
                return PackIdentifier.CreateByID((Identifier)new IdentifierPropertyBinder().Read(element, internalID, typeof(Identifier)));
            else if ((localPathToggle?.boolValue ?? false) && localPath != null)
                return PackIdentifier.CreateByPath((FilePath)new FilePathPropertyBinder().Read(element, localPath, typeof(FilePath)));
            
            return PackIdentifier.CreateByID(Identifier.empty);
        }
        
        public override void Write(VisualElement element, SerializedProperty property, Type propertyType, object? value)
        {
            if (value is PackIdentifier packIdentifier)
            {
                (SerializedProperty nullableInternalID, SerializedProperty nullableLocalPath) = PackIdentifierPropertyDrawer.GetChildProperty(property);
                
                new SerializableNullablePropertyBinder().Write(element, nullableInternalID, typeof(SerializableNullable<Identifier>), new SerializableNullable<Identifier>(packIdentifier.identifier));
                new SerializableNullablePropertyBinder().Write(element, nullableLocalPath, typeof(SerializableNullable<FilePath>), new SerializableNullable<FilePath>(packIdentifier.path));
            }
        }
    }
}