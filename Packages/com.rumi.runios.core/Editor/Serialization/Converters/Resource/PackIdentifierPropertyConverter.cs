#nullable enable
using RuniOS.Editor.Drawers;
using RuniOS.Editor.Drawers.Resource;
using RuniOS.Editor.Serialization.Converters.IO;
using RuniOS.IO;
using RuniOS.Resource;
using System;
using UnityEditor;

namespace RuniOS.Editor.Serialization.Converters.Resource
{
    [CustomPropertyConverter(typeof(PackIdentifier))]
    public class PackIdentifierPropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType)
        {
            (SerializedProperty nullableInternalID, SerializedProperty nullableLocalPath) = PackIdentifierPropertyDrawer.GetChildProperty(property);
            (SerializedProperty? internalID, SerializedProperty? internalIDToggle) = SerializableNullablePropertyDrawer.GetChildProperty(nullableInternalID);
            (SerializedProperty? localPath, SerializedProperty? localPathToggle) = SerializableNullablePropertyDrawer.GetChildProperty(nullableLocalPath);

            if ((internalIDToggle?.boolValue ?? false) && internalID != null)
                return PackIdentifier.CreateByID((Identifier)new IdentifierPropertyConverter().Read(internalID, typeof(Identifier)));
            else if ((localPathToggle?.boolValue ?? false) && localPath != null)
                return PackIdentifier.CreateByPath((FilePath)new FilePathPropertyConverter().Read(localPath, typeof(FilePath)));
            
            return PackIdentifier.CreateByID(Identifier.empty);
        }
        
        public override void Write(SerializedProperty property, Type propertyType, object? value)
        {
            if (value is PackIdentifier packIdentifier)
            {
                (SerializedProperty nullableInternalID, SerializedProperty nullableLocalPath) = PackIdentifierPropertyDrawer.GetChildProperty(property);
                
                new SerializableNullablePropertyConverter().Write(nullableInternalID, typeof(SerializableNullable<Identifier>), new SerializableNullable<Identifier>(packIdentifier.identifier));
                new SerializableNullablePropertyConverter().Write(nullableLocalPath, typeof(SerializableNullable<FilePath>), new SerializableNullable<FilePath>(packIdentifier.path));
            }
        }
    }
}