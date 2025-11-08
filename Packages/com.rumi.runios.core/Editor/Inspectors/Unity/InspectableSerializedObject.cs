#nullable enable
using RuniOS.APIBridge.UnityEditor;
using RuniOS.Inspectors;
using RuniOS.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor;

namespace RuniOS.Editor.Inspectors.Unity
{
    public class InspectableSerializedObject : IInspectableObject
    {
        IInspectorVariableElement? IInspectable.parentElement
        {
            get => null;
            set { }
        }
        
        public SerializedObject serializedObject { get; }
        public SerializedProperty targetProperty { get; }
        
        public bool instancesIsEmpty => serializedObject.targetObject == null;
        
        public bool instanceIsMultiple => serializedObject.isEditingMultipleObjects;
        
        public Type inspectionType { get; }
        public string inspectionDisplayName => inspectionType.GetTypeDisplayName();
        
        public int instanceCount => serializedObject.targetObjects.Length;

        public InspectableSerializedObject(SerializedObject serializedObject, SerializedProperty? targetProperty = null)
        {
            this.serializedObject = serializedObject;
            this.targetProperty = targetProperty ?? serializedObject.GetIterator();
            
            ScriptAttributeUtilityBridge.GetFieldInfoFromProperty(this.targetProperty, out Type type);
            inspectionType = type;
        }
        
        public bool TryGetInspectionType([NotNullWhen(true)] out Type? type)
        {
            type = inspectionType;
            return true;
        }

        public IEnumerable<IInspectorElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            if (!flags.HasFlagFast(InspectorFlags.Public) || !flags.HasFlagFast(InspectorFlags.Instance))
                return ImmutableArray<IInspectorElement>.Empty;
            
            SerializedProperty property = targetProperty.Copy();
            int depth = property.depth + 1;
            if (!property.Next(true))
                return ImmutableArray<IInspectorElement>.Empty;
            
            List<IInspectorElement?> elements = new List<IInspectorElement?>();
            do
            {
                if (depth != property.depth)
                    break;
                
                if (property.isArray)
                    elements.Add(new SerializedPropertyElement(property.Copy()));
            }
            while (property.Next(false));

            return elements.WhereNotNull().Where(x => x.HasFlags(flags));
        }
        
        public IInspectableObject Clone() => new InspectableSerializedObject(serializedObject, targetProperty);
        IInspectable IInspectable.Clone() => Clone();
        object ICloneable.Clone() => Clone();

        public static implicit operator SerializedObject(InspectableSerializedObject inspectableObject) => inspectableObject.serializedObject;
        public static implicit operator InspectableSerializedObject(SerializedObject serializedObject) => new InspectableSerializedObject(serializedObject);
    }
}