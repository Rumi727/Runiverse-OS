#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Collections.Generic;
using RuniOS.Editor.Unity.Serialization;
using RuniOS.Inspectors;
using RuniOS.Reflection;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

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
        
        public IInspectorVariableElement? parentElement { get; set; }

        public Type inspectionType { get; }
        public string inspectionDisplayName => inspectionType.GetTypeDisplayName();
        
        public bool instancesIsEmpty => property.serializedObject.targetObject != null;
        
        public bool instanceIsMultiple => property.serializedObject.isEditingMultipleObjects;
        
        public int instanceCount => property.serializedObject.targetObjects.Length;
        
        public Type inspectionElementType { get; }
        public string inspectionElementDisplayName => inspectionElementType.GetTypeDisplayName();

        public SerializedProperty property { get; }
        public PropertyConverter? converter { get; }

        public Action? onValueChanged { get; set; }

        NullabilityInfo? IInspectableList.elementNullabilityInfo => null;

        bool IList.IsReadOnly => false;
        bool IList.IsFixedSize => false;

        bool IInspectableList.isArray => false;
        
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
        
        public void OnValueChangedInvoke()
        {
            onValueChanged?.SafeInvoke();
            parentElement?.inspectableObjectElement.OnValueChangedInvoke();
        }

        public int Add(object? value)
        {
            int index = count;
            property.InsertArrayElementAtIndex(index);

            OnInsert(index);
            return index;
        }
        
        public void Insert(int index, object value)
        {
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));
            
            property.InsertArrayElementAtIndex(index);
            OnInsert(index);
        }

        public void Remove(object value) => throw new NotImplementedException();
        
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));
            
            property.DeleteArrayElementAtIndex(index);
            OnRemoveAt(index);
        }

        public void Clear()
        {
            property.arraySize = 0;
            OnClear();
        }

        public void Move(int oldIndex, int newIndex)
        {
            property.MoveArrayElement(oldIndex, newIndex);
            OnElementMoved(oldIndex, newIndex);
        }

        public void OnInsert(int index)
        {
            if (index < 0 || index > cachedElements.Count)
                return;
            
            cachedElements.Insert(index, new SerializedListElement(this, property.GetArrayElementAtIndex(index), index));
            
            if (index < cachedElements.Count)
            {
                for (int i = 0; i < cachedElements.Count; i++)
                    cachedElements[i].index = i;
            }
            
            OnValueChangedInvoke();
        }

        public void OnRemoveAt(int index)
        {
            if (index < 0 || index >= cachedElements.Count)
                return;
            
            cachedElements.RemoveAt(index);
            
            for (int i = 0; i < cachedElements.Count; i++)
                cachedElements[i].index = i;
            
            OnValueChangedInvoke();
        }

        public void OnElementMoved(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= cachedElements.Count)
                return;
            
            if (newIndex < 0 || newIndex >= cachedElements.Count)
                return;
            
            cachedElements.Move(oldIndex, newIndex);
            
            for (int i = 0; i < cachedElements.Count; i++)
                cachedElements[i].index = i;
            
            OnValueChangedInvoke();
        }

        public void OnElementChanged(int oldIndex, int newIndex)
        {
            cachedElements.Change(oldIndex, newIndex);
            
            for (int i = 0; i < cachedElements.Count; i++)
                cachedElements[i].index = i;
            
            OnValueChangedInvoke();
        }

        public void OnClear()
        {
            cachedElements.Clear();
            OnValueChangedInvoke();
        }

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
        
        
        
        readonly List<IInspectorListElement> cachedElements = new();
        IReadOnlyList<IInspectorListElement>? readOnlyCachedElements;
        public IReadOnlyList<IInspectorListElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            if (!flags.HasFlagFast(InspectorFlags.List))
                return ImmutableArray<IInspectorListElement>.Empty;
            
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
            
            GetElements();
            IInspectorListElement element = cachedElements[index];
            if (!element.HasFlags(flags))
                return null;

            return element;
        }
        
        IEnumerable<IInspectorElement> IInspectable.GetElements(InspectorFlags flags) => GetElements(flags);
        
        public IInspectableList Clone() => new InspectableSerializedList(property);
        IInspectable IInspectable.Clone() => Clone();
        object ICloneable.Clone() => Clone();
        
        void IInspectableList.SynchronizeCollections() { }
        void IInspectableList.UpdateSourceCollections() { }
    }
}