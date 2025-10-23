#nullable enable
using RuniOS.APIBridge.UnityEditor;
using RuniOS.Inspectors;
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
        IInspectorVariableElement? IInspectableObject.parentElement => null;
        
        public SerializedObject serializedObject { get; }
        public SerializedProperty targetProperty { get; }
        
        public bool instancesIsEmpty => serializedObject.targetObject == null;
        
        public Type inspectionType { get; }
        public string inspectionDisplayName => inspectionType.GetTypeDisplayName();
        
        IEnumerable<object> IInspectableObject.instances => serializedObject.targetObjects;

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

        public IReadOnlyList<IInspectorElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
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
                
                if ((property.isArray && flags.HasFlagFast(InspectorFlags.List)) || flags.HasFlagFast(InspectorFlags.Field))
                    elements.Add(new SerializedPropertyElement(property.Copy()));
            }
            while (property.Next(false));

            return elements.WhereNotNull().ToImmutableArray();
        }

        public static implicit operator SerializedObject(InspectableSerializedObject inspectableObject) => inspectableObject.serializedObject;
        public static implicit operator InspectableSerializedObject(SerializedObject serializedObject) => new InspectableSerializedObject(serializedObject);
    }
}