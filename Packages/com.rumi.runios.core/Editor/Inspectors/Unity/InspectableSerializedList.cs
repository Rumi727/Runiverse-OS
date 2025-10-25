#nullable enable
using RuniOS.APIBridge.UnityEditor;
using RuniOS.Collections.Generic;
using RuniOS.Editor.Serialization;
using RuniOS.Inspectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;

namespace RuniOS.Editor.Inspectors.Unity
{
    public class InspectableSerializedList : IInspectableList
    {
        public InspectableSerializedList(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.String || !property.isArray)
                throw new ArgumentException($"Provided property '{property.propertyPath}' is not a array type.", nameof(property));
            
            ScriptAttributeUtilityBridge.GetFieldInfoFromProperty(property, out Type type);
            
            inspectionType = type;
            inspectionElementType = CollectionGenericUtility.GetListElementType(type) ?? throw new ArgumentException($"Provided property '{property.propertyPath}' is not a list type.", nameof(property));
            
            this.property = property;
            converter = PropertyConverter.FindConverter(inspectionElementType);
        }
        
        IInspectorVariableElement? IInspectable.parentElement => null;
        
        public Type inspectionType { get; }
        public string inspectionDisplayName => inspectionType.GetTypeDisplayName();
        
        public bool instancesIsEmpty => false;
        
        public Type inspectionElementType { get; }
        public string inspectionElementDisplayName => inspectionElementType.GetTypeDisplayName();

        public SerializedProperty property { get; }
        public PropertyConverter? converter { get; }

        RuniNullabilityInfo? IInspectableList.nullabilityInfo => null;

        bool IList.IsReadOnly => false;
        bool IList.IsFixedSize => false;
        
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;
        
        public object? this[int index]
        {
            get
            {
                if (index < 0 || index >= count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return converter?.Read(property.GetArrayElementAtIndex(index), inspectionElementType);
            }
            set
            {
                if (index < 0 || index >= count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                converter?.Write(property.GetArrayElementAtIndex(index), inspectionElementType, value);
            }
        }
        
        public int count
        {
            get => property.arraySize;
            set => property.arraySize = value;
        }
        int ICollection.Count => count;
        
        

        public int Add(object? value)
        {
            int index = count;
            property.InsertArrayElementAtIndex(index);
            return index;
        }
        
        public void Insert(int index, object value)
        {
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));
            
            property.InsertArrayElementAtIndex(index);
        }

        public void Remove(object value) => throw new NotImplementedException();
        
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));
            
            property.DeleteArrayElementAtIndex(index);
        }

        public void Clear() => property.arraySize = 0;

        public bool Contains(object? value) => throw new NotImplementedException();
        
        public int IndexOf(object? value) => throw new NotImplementedException();

        public IEnumerator GetEnumerator() => throw new NotImplementedException();
        
        public void CopyTo(Array array, int index) => throw new NotSupportedException("CopyTo is not implemented for multi-object editing.");
        
        
        
        public bool TryGetInspectionType([NotNullWhen(true)] out Type? type)
        {
            type = inspectionType;
            return true;
        }
        
        public bool TryGetInspectionElementType(out Type? type)
        {
            type = inspectionElementType;
            return true;
        }
        
        
        
        List<IInspectorElement>? cachedElements;
        IReadOnlyList<IInspectorElement>? readOnlyCachedElements;
        public IReadOnlyList<IInspectorElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            if (!flags.HasFlagFast(InspectorFlags.List))
                return ImmutableArray<IInspectorElement>.Empty;
            
            cachedElements ??= new List<IInspectorElement>();
            readOnlyCachedElements ??= cachedElements.AsReadOnly();
            
            if (cachedElements.Count < count)
            {
                // 0, 1, 2 : 3
                // 0 : 1
                
                // i = 1
                // 1 < 3 : true
                // 0, 1 : 1
                
                // i = 2
                // 2 < 3 : true
                // 0, 1, 2 : 2
                
                // i = 3
                // 3 < 3 : false
                
                for (int i = cachedElements.Count; i < count; i++)
                    cachedElements.Add(new SerializedListElement(this, property.GetArrayElementAtIndex(i), i));
            }
            else if (cachedElements.Count > count)
            {
                // 0 : 1
                // 0, 1, 2 : 3
                
                // i = 2
                // 2 >= 1 : true
                // 0, 1 : 2
                
                // i = 1
                // 1 >= 1 : true
                // 0 : 1
                
                // i = 0
                // 0 >= 1 : false
                
                for (int i = cachedElements.Count - 1; i >= count; i--)
                    cachedElements.RemoveAt(i);
            }

            return readOnlyCachedElements;
        }

        public IInspectorListElement? GetElement(int index, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            if (!flags.HasFlagFast(InspectorFlags.List))
                return null;
            
            return GetElements(flags)[index] as IInspectorListElement;
        }
        
        IEnumerable<IInspectorElement> IInspectable.GetElements(InspectorFlags flags) => GetElements(flags);
    }
}