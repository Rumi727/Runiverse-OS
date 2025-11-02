#nullable enable
using RuniOS.Collections.Generic;
using RuniOS.Collections.Handlers;
using RuniOS.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace RuniOS.Inspectors.Csharp
{
    public class InspectableList : IInspectableList
    {
        public InspectableList(IEnumerable instance, RuniNullabilityInfo? elementNullabilityInfo = null) : this(instance.GetType(), Enumerable.Repeat(instance, 1), elementNullabilityInfo) { }

        public InspectableList(Type inspectionType, RuniNullabilityInfo? elementNullabilityInfo = null) : this(inspectionType, Enumerable.Empty<IEnumerable>(), elementNullabilityInfo) { }
        
        public InspectableList(Type inspectionType, RuniNullabilityInfo? elementNullabilityInfo, params IEnumerable[] instances) : this(inspectionType, instances.ToImmutableArray(), elementNullabilityInfo) { }
        
        public InspectableList(Type inspectionType, IEnumerable<IEnumerable> instances, RuniNullabilityInfo? elementNullabilityInfo = null)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(inspectionType) || !CollectionHandler.FindDrawerType(inspectionType, out Type? resolvedDrawerTargetType, out Type? drawerType))
                throw new ArgumentException($"Provided type '{inspectionType.FullName}' is not a enumerable type.", nameof(inspectionType));

            this.resolvedDrawerTargetType = resolvedDrawerTargetType;
            this.drawerType = drawerType;
            
            this.inspectionType = inspectionType;
            inspectionElementType = inspectionType.IsArray ? inspectionType.GetElementType() : CollectionGenericUtility.GetEnumerableElementType(inspectionType);
            
            _instances = null!;
            this.instances = instances;
            
            this.elementNullabilityInfo = elementNullabilityInfo;

            readonlyCollectionHandlerTable = _collectionHandlerTable.AsReadOnly();
        }
        
        readonly Type resolvedDrawerTargetType;
        readonly Type drawerType;
        
        public IInspectorVariableElement? parentElement { get; set; }
        
        public Type inspectionType { get; }
        public string inspectionDisplayName => inspectionType.GetTypeDisplayName();
        
        /// <remarks>
        /// null을 반환하는 경우, 리스트가 모든 타입 형식을 허용한다는 의미입니다.
        /// </remarks>
        public Type? inspectionElementType { get; }
        
        /// <remarks>
        /// null을 반환하는 경우, 리스트가 모든 타입 형식을 허용한다는 의미입니다.
        /// </remarks>
        public string? inspectionElementDisplayName => inspectionElementType?.GetTypeDisplayName();

        public RuniNullabilityInfo? elementNullabilityInfo { get; }

        public bool isReadOnly => collectionHandlers.Any(x => x.isReadOnly);
        bool IList.IsReadOnly => isReadOnly;
        
        public bool isFixedSize => collectionHandlers.Any(x => (parentElement == null || !isArray) && x.isFixedSize);
        bool IList.IsFixedSize => isFixedSize;
        
        public bool isArray => inspectionType.IsArray;
        
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        public IEnumerable? instance
        {
            get
            {
                var instances = collectionHandlerTable;
                if (instances.Any())
                    return instances.MinBy(static x => x.Value.count).Key;
                
                return null;
            }
            set
            {
                if (value != null && !inspectionType.IsInstanceOfType(value))
                    throw new InspectorException($"Invalid type. Expected '{inspectionType.FullName}', but received '{value.GetType().FullName}'.");
                
                if (value != null)
                    instances = Enumerable.Repeat(value, 1);
                else
                    instances = Array.Empty<IList>();
            }
        }

        public CollectionHandler? collectionHandler
        {
            get
            {
                if (instance == null)
                    return null;
                
                return collectionHandlerTable[instance];
            }
        }

        public IEnumerable<IEnumerable> instances
        {
            get
            {
                parentElement?.UpdateChildInspectable();
                return _instances;
            }
            set
            {
                if (value.Any(x => x != null && !inspectionType.IsInstanceOfType(x)))
                {
                    string invalidTypes = string.Join(", ", value.Where(x => x != null && !inspectionType.IsInstanceOfType(x))
                        .Select(x => $"'{x!.GetType().FullName}'")
                        .Distinct());
                    
                    throw new InspectorException($"One or more elements in the collection have invalid types. Expected '{inspectionType.FullName}', but received the following: {invalidTypes}.");
                }

                _instances = value;
            }
        }
        IEnumerable<IEnumerable> _instances;
        
        public IEnumerable<CollectionHandler> collectionHandlers => instances.Select(x => collectionHandlerTable[x]);

        // 이 InspectableCollection이 관리하는 모든 원본 컬렉션에 매핑되는 CollectionHandler 맵
        public IReadOnlyDictionary<IEnumerable, CollectionHandler> collectionHandlerTable
        {
            get
            {
                _collectionHandlerTable.SyncKeysWithEnumerable(instances, x => (CollectionHandler)Activator.CreateInstance(drawerType, resolvedDrawerTargetType, x));
                return readonlyCollectionHandlerTable;
            }
        }
        readonly IReadOnlyDictionary<IEnumerable, CollectionHandler> readonlyCollectionHandlerTable;
        readonly Dictionary<IEnumerable, CollectionHandler> _collectionHandlerTable = new();

        [MemberNotNullWhen(false, nameof(instance))]
        public bool instancesIsEmpty => instance == null;
        
        public bool instanceIsMultiple
        {
            get
            {
                int count = 0;
                foreach (var _ in instances)
                {
                    count++;
                    if (count > 1)
                        return true;
                }

                return false;
            }
        }

        public object? this[int index]
        {
            get
            {
                ExceptionUtility.ThrowIfArgumentNull(collectionHandler, nameof(collectionHandler));
                
                if (index < 0 || index >= count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                
                return collectionHandler[index];
            }
            set
            {
                if (index < 0 || index >= count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                
                foreach (var list in collectionHandlers)
                {
                    if (list.count > index)
                        list[index] = value;
                }
            }
        }
        
        public int count
        {
            get
            {
                ExceptionUtility.ThrowIfArgumentNull(collectionHandler, nameof(collectionHandler));
                return collectionHandler.count;
            }
            set
            {
                if (parentElement == null || !isArray)
                {
                    foreach (var list in collectionHandlers)
                        list.Resize(value, Activator);
                }
                else
                {
                    parentElement.SetValues(instances.Select(list =>
                    {
                        if (list is Array array)
                            list = array.Resize(value, Activator);

                        return list;
                    }));
                }

                object? Activator(int _)
                {
                    if (elementNullabilityInfo?.writeState == RuniNullabilityState.NotNull)
                        return (inspectionElementType ?? typeof(object)).GetDefaultValueNotNull();
                    else
                        return (inspectionElementType ?? typeof(object)).GetDefaultValue();
                }
            }
        }
        int ICollection.Count => count;
        
        
        
        public void SynchronizeCollections()
        {
            foreach (var item in collectionHandlers)
                item.SynchronizeCollections();
        }
        
        public void UpdateSourceCollections()
        {
            foreach (var item in collectionHandlers)
                item.UpdateSourceCollections();
        }

        public int Add(object? value)
        {
            int minCount = count;
            if (parentElement == null || !isArray)
            {
                foreach (IList list in collectionHandlers)
                {
                    if (minCount == list.Count)
                        list.Add(value);
                }
            }
            else
            {
                parentElement.SetValues(instances.Select(list =>
                {
                    if (list is Array array)
                    {
                        if (minCount == array.Length)
                            list = array.Add(value);
                    }

                    return list;
                }));
            }

            OnInsert(minCount);
            return minCount;
        }
        
        public void Insert(int index, object value)
        {
            if (index < 0 || index > count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (parentElement == null || !isArray)
            {
                foreach (var list in collectionHandlers)
                    list.Insert(index, value);
            }
            else
            {
                parentElement.SetValues(instances.Select(list =>
                {
                    if (list is Array array)
                        list = array.Insert(index, value);

                    return list;
                }));
            }

            OnInsert(index);
        }

        public void Remove(object value) => RemoveAt(IndexOf(value));
        
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (parentElement == null || !isArray)
            {
                foreach (var list in collectionHandlers)
                    list.RemoveAt(index);
       
            }
            else
            {
                parentElement.SetValues(instances.Select(list =>
                {
                    if (list is Array array)
                        list = array.RemoveAt(index);

                    return list;
                }));
            }

            OnRemoveAt(index);
        }

        public void Clear()
        {
            if (parentElement == null || !isArray)
            {
                foreach (IList list in collectionHandlers)
                    list.Clear();
            }
            else
            {
                parentElement.SetValues(instances.Select(list =>
                {
                    if (list is Array array)
                        list = array.RemoveAll();

                    return list;
                }));
            }
            
            OnClear();
        }
        
        public void OnInsert(int index)
        {
            if (index < 0 || index > cachedElements.Count)
                return;
            
            cachedElements.Insert(index, new ListElement(this, index));
            
            if (index < cachedElements.Count)
            {
                for (int i = 0; i < cachedElements.Count; i++)
                    cachedElements[i].index = i;
            }
        }

        public void OnRemoveAt(int index)
        {
            if (index < 0 || index >= cachedElements.Count)
                return;
            
            cachedElements.RemoveAt(index);
            
            for (int i = 0; i < cachedElements.Count; i++)
                cachedElements[i].index = i;
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
        }

        public void OnElementChanged(int oldIndex, int newIndex)
        {
            cachedElements.Change(oldIndex, newIndex);
            
            for (int i = 0; i < cachedElements.Count; i++)
                cachedElements[i].index = i;
        }

        public void OnClear() => cachedElements.Clear();

        public bool Contains(object? value)
        {
            ExceptionUtility.ThrowIfArgumentNull(collectionHandler, nameof(collectionHandler));
            return collectionHandler.Contains(value);
        }

        public int IndexOf(object value)
        {
            ExceptionUtility.ThrowIfArgumentNull(collectionHandler, nameof(collectionHandler));
            return collectionHandler.IndexOf(value);
        }

        public IEnumerator GetEnumerator()
        {
            ExceptionUtility.ThrowIfArgumentNull(instance, nameof(instance));
            return instance.GetEnumerator();
        }

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
        
        

        readonly List<ListElement> cachedElements = new();
        IReadOnlyList<IInspectorListElement>? readOnlyCachedElements;
        public IReadOnlyList<IInspectorElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            if (!flags.HasFlagFast(InspectorFlags.List) || (isReadOnly && !flags.HasFlagFast(InspectorFlags.ReadOnly)))
                return Array.Empty<IInspectorElement>();
            
            readOnlyCachedElements ??= cachedElements.AsReadOnly();
            cachedElements.Resize(count, x => new ListElement(this, x));

            for (int i = 0; i < cachedElements.Count; i++)
            {
                ListElement element = cachedElements[i];
                if (element.variableType != element.currentElementType)
                    cachedElements[i] = new ListElement(this, i);
            }
            
            return readOnlyCachedElements;
        }

        public IInspectorListElement? GetElement(int index, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            if (!flags.HasFlagFast(InspectorFlags.List) || (isReadOnly && !flags.HasFlagFast(InspectorFlags.ReadOnly)))
                return null;

            GetElements();
            IInspectorListElement element = cachedElements[index];
            if (!element.HasFlags(flags))
                return null;

            return element;
        }
        
        IEnumerable<IInspectorElement> IInspectable.GetElements(InspectorFlags flags) => GetElements(flags);
        
        public IInspectableList Clone() => new InspectableList(inspectionType, elementNullabilityInfo) { parentElement = parentElement, instances = instances };
        IInspectable IInspectable.Clone() => Clone();
        object ICloneable.Clone() => Clone();
    }
}