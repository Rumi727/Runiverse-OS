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
            if (!typeof(IEnumerable).IsAssignableFrom(inspectionType))
                throw new ArgumentException($"Provided type '{inspectionType.FullName}' is not a enumerable type.", nameof(inspectionType));
            
            this.inspectionType = inspectionType;
            inspectionElementType = inspectionType.IsArray ? inspectionType.GetElementType() : CollectionGenericUtility.GetEnumerableElementType(inspectionType);
            
            _instances = null!;
            this.instances = instances;
            
            this.elementNullabilityInfo = elementNullabilityInfo;

            readonlyListHandlerTable = _listHandlerTable.AsReadOnly();
        }
        
        public IInspectorVariableElement? parentElement { get; set; }
        
        public Type inspectionType { get; }
        public string inspectionDisplayName => inspectionType.GetTypeDisplayName();
        
        /// <remarks>
        /// null을 반환하는 경우, 리스트가 모든 타입 형식을 허용한다는 의미입니다.
        /// </remarks>
        public Type? inspectionElementType { get; }

        public RuniNullabilityInfo? elementNullabilityInfo { get; }

        public bool isReadOnly => listHandlers.Any(x => x.isReadOnly);
        bool IList.IsReadOnly => isReadOnly;
        
        public bool isFixedSize => listHandlers.Any(x => (parentElement == null || !isArray) && x.isFixedSize);
        bool IList.IsFixedSize => isFixedSize;
        
        public bool isArray => inspectionType.IsArray;
        
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        public IEnumerable? instance
        {
            get
            {
                var instances = listHandlerTable;
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

        public ListHandlerBase? listHandler
        {
            get
            {
                if (instance == null)
                    return null;
                
                return listHandlerTable[instance];
            }
        }

        /// <summary>
        /// 모든 요소의 타입이 <see cref="inspectionType"/>와 동일해야합니다.<br/>
        /// 값이 유효한지 검사하지 않습니다!
        /// </summary>
        public IEnumerable<IEnumerable> instances
        {
            get
            {
                parentElement?.UpdateChildInspectable();
                return _instances;
            }
            set => _instances = value;
        }
        IEnumerable<IEnumerable> _instances;
        
        public IEnumerable<ListHandlerBase> listHandlers => instances.Select(x => listHandlerTable[x]);

        // 이 InspectableCollection이 관리하는 모든 원본 컬렉션에 매핑되는 CollectionHandler 맵
        public IReadOnlyDictionary<IEnumerable, ListHandlerBase> listHandlerTable
        {
            get
            {
                _listHandlerTable.SyncKeysWithEnumerable(instances, ListHandlerBase.FindListHandler);
                return readonlyListHandlerTable;
            }
        }
        readonly IReadOnlyDictionary<IEnumerable, ListHandlerBase> readonlyListHandlerTable;
        readonly Dictionary<IEnumerable, ListHandlerBase> _listHandlerTable = new();

        [MemberNotNullWhen(false, nameof(instance), nameof(listHandler))]
        public bool instancesIsEmpty => instance == null;

        [MemberNotNullWhen(true, nameof(instance), nameof(listHandler))]
        public bool instanceIsMultiple => instances.TwoOrMore();
        
        public int instanceCount => instances.Count();

        public object? this[int index]
        {
            get
            {
                ExceptionUtility.ThrowIfArgumentNull(listHandler, nameof(listHandler));
                
                if (index < 0 || index >= count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                
                return listHandler[index];
            }
            set
            {
                if (index < 0 || index >= count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                
                foreach (var list in listHandlers)
                    list[index] = value;
            }
        }
        
        public int count
        {
            get
            {
                ExceptionUtility.ThrowIfArgumentNull(listHandler, nameof(listHandler));
                return listHandler.count;
            }
        }
        int ICollection.Count => count;
        
        public void SynchronizeCollections()
        {
            foreach (var item in listHandlers)
                item.SynchronizeCollections();
        }
        
        public void UpdateSourceCollections()
        {
            foreach (var item in listHandlers)
                item.UpdateSourceCollections();
        }

        public int Add(object? value)
        {
            int minCount = count;
            if (parentElement == null || !isArray)
            {
                foreach (var list in listHandlers)
                    list.Add(value);
            }
            else
            {
                parentElement.SetValues(instances.Select(list =>
                {
                    if (list is Array array)
                        list = array.Add(value);

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
                foreach (var list in listHandlers)
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

        public void Remove(object value)
        {
            if (parentElement == null || !isArray)
            {
                var minCollectionHandler = listHandler;
                foreach (var list in listHandlers)
                {
                    if (list == minCollectionHandler)
                    {
                        int index = list.IndexOf(value);
                        list.RemoveAt(index);
                        OnRemoveAt(index);
                    }
                    else
                        list.Remove(value);
                }
            }
            else
            {
                var minInstance = instance;
                parentElement.SetValues(instances.Select(list =>
                {
                    if (list is Array array)
                    {
                        if (Equals(list, minInstance))
                        {
                            int index = Array.IndexOf(array, value);
                            list = array.RemoveAt(index);
                            OnRemoveAt(index);
                        }
                        else
                            list = array.Remove(value);
                    }

                    return list;
                }));
            }
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (parentElement == null || !isArray)
            {
                foreach (var list in listHandlers)
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
                foreach (IList list in listHandlers)
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

        public void Move(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= count)
                throw new ArgumentOutOfRangeException(nameof(oldIndex));
            if (newIndex < 0 || newIndex >= count)
                throw new ArgumentOutOfRangeException(nameof(newIndex));
            
            foreach (var list in listHandlers)
                list.Move(oldIndex, newIndex);
            
            OnElementMoved(oldIndex, newIndex);
        }

        public virtual void OnInsert(int index)
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

        public virtual void OnRemoveAt(int index)
        {
            if (index < 0 || index >= cachedElements.Count)
                return;
            
            cachedElements.RemoveAt(index);
            
            for (int i = 0; i < cachedElements.Count; i++)
                cachedElements[i].index = i;
        }

        public virtual void OnElementMoved(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= cachedElements.Count)
                return;
            
            if (newIndex < 0 || newIndex >= cachedElements.Count)
                return;
            
            cachedElements.Move(oldIndex, newIndex);
            
            for (int i = 0; i < cachedElements.Count; i++)
                cachedElements[i].index = i;
        }

        public virtual void OnElementChanged(int oldIndex, int newIndex)
        {
            cachedElements.Change(oldIndex, newIndex);
            
            for (int i = 0; i < cachedElements.Count; i++)
                cachedElements[i].index = i;
        }

        public virtual void OnClear() => cachedElements.Clear();

        public bool Contains(object? value)
        {
            ExceptionUtility.ThrowIfArgumentNull(listHandler, nameof(listHandler));
            return listHandlers.Any(x => x.Contains(value));
        }

        public int IndexOf(object value)
        {
            ExceptionUtility.ThrowIfArgumentNull(listHandler, nameof(listHandler));
            return listHandler.IndexOf(value);
        }

        public IEnumerator GetEnumerator()
        {
            ExceptionUtility.ThrowIfArgumentNull(listHandler, nameof(listHandler));
            return listHandler.GetEnumerator();
        }

        public void CopyTo(Array array, int index) => throw new NotSupportedException("CopyTo is not implemented for multi-object editing.");
        
        public bool TryGetInspectionType([NotNullWhen(true)] out Type? type)
        {
            type = inspectionType;
            return true;
        }
        
        

        readonly List<ListElement> cachedElements = new();
        IReadOnlyList<IInspectorListElement>? readOnlyCachedElements;
        public IReadOnlyList<IInspectorListElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            if (!flags.HasFlagFast(InspectorFlags.List) || (isReadOnly && !flags.HasFlagFast(InspectorFlags.ReadOnly)))
                return Array.Empty<IInspectorListElement>();
            
            readOnlyCachedElements ??= cachedElements.AsReadOnly();
            cachedElements.Resize(count, CreateElement);

            for (int i = 0; i < cachedElements.Count; i++)
            {
                ListElement element = cachedElements[i];
                if (element.variableType != element.currentElementType)
                    cachedElements[i] = CreateElement(i);
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

        protected virtual ListElement CreateElement(int index) => new ListElement(this, index);
        
        IInspectableList IInspectableList.Clone() => new InspectableList(inspectionType, elementNullabilityInfo) { parentElement = parentElement, instances = instances };
    }
}