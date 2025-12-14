#nullable enable
using RuniOS.Collections.Generic;
using RuniOS.Collections.Handlers;
using RuniOS.Linq;
using RuniOS.Reflection;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Inspectors.Csharp
{
    public class InspectableList : IInspectableList
    {
        public InspectableList(IEnumerable instance, NullabilityInfo? elementNullabilityInfo = null) : this(instance.GetType(), Enumerable.Repeat(instance, 1), elementNullabilityInfo) { }

        public InspectableList(Type inspectionType, NullabilityInfo? elementNullabilityInfo = null) : this(inspectionType, Enumerable.Empty<IEnumerable>(), elementNullabilityInfo) { }

        public InspectableList(Type inspectionType, NullabilityInfo? elementNullabilityInfo, params IEnumerable[] instances) : this(inspectionType, instances.ToImmutableArray(), elementNullabilityInfo) { }

        public InspectableList(Type inspectionType, IEnumerable<IEnumerable> instances, NullabilityInfo? elementNullabilityInfo = null)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(inspectionType))
                throw new ArgumentException($"Provided type '{inspectionType.FullName}' is not a enumerable type.", nameof(inspectionType));

            this.inspectionType = inspectionType;
            inspectionElementType = inspectionType.IsArray ? inspectionType.GetElementType() : CollectionGenericUtility.GetEnumerableElementType(inspectionType);
            
            this.elementNullabilityInfo = elementNullabilityInfo;

            readOnlyInstances = _instances.AsReadOnly();
            readOnlyListHandlers = _listHandlers.AsReadOnly();

            SetInstances(instances);
        }

        public IInspectorVariableElement? parentElement { get; set; }

        public Type inspectionType { get; }
        public string inspectionDisplayName => inspectionType.GetTypeDisplayName();

        /// <remarks>
        /// null을 반환하는 경우, 리스트가 모든 타입 형식을 허용한다는 의미입니다.
        /// </remarks>
        public Type? inspectionElementType { get; }

        public NullabilityInfo? elementNullabilityInfo { get; }

        public bool isReadOnly
        {
            get
            {
                // ReSharper disable once LoopCanBeConvertedToQuery
                for (int i = 0; i < _listHandlers.Count; i++)
                {
                    if (_listHandlers[i].isReadOnly)
                        return true;
                }
                
                return false;
            }
        }
        bool IList.IsReadOnly => isReadOnly;

        public bool isFixedSize
        {
            get
            {
                if (parentElement != null && isArray)
                    return false;

                // ReSharper disable once LoopCanBeConvertedToQuery
                for (int i = 0; i < _listHandlers.Count; i++)
                {
                    if (_listHandlers[i].isFixedSize)
                        return true;
                }
                return false;
            }
        }
        bool IList.IsFixedSize => isFixedSize;

        public bool isArray => inspectionType.IsArray;

        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        /// <summary>
        /// 타입이 <see cref="inspectionType"/>와 동일해야합니다.<br/>
        /// 값이 유효한지 검사하지 않습니다!
        /// </summary>
        public IEnumerable? instance
        {
            get
            {
                parentElement?.UpdateChildInspectable();
                if (_listHandlers.Count == 0)
                    return null;

                var minHandler = _listHandlers[0];
                int minCount = minHandler.count;

                for (int i = 1; i < _listHandlers.Count; i++)
                {
                    ListHandlerBase? currentHandler = _listHandlers[i];
                    if (currentHandler.count < minCount)
                    {
                        minCount = currentHandler.count;
                        minHandler = currentHandler;
                    }
                }

                return minHandler.targetCollection;
            }
            set
            {
                if (value != null && !inspectionType.IsInstanceOfType(value))
                    throw new InspectorException($"Invalid type. Expected '{inspectionType.FullName}', but received '{value.GetType().FullName}'.");

                _instances.Clear();
                if (value != null)
                    _instances.Add(value);
            }
        }

        public ListHandlerBase? listHandler
        {
            get
            {
                parentElement?.UpdateChildInspectable();
                var handlers = listHandlers;
                if (handlers.Count == 0)
                    return null;

                var minHandler = handlers[0];
                int minCount = minHandler.count;

                for (int i = 1; i < handlers.Count; i++)
                {
                    ListHandlerBase? currentHandler = handlers[i];
                    if (currentHandler.count < minCount)
                    {
                        minCount = currentHandler.count;
                        minHandler = currentHandler;
                    }
                }

                return minHandler;
            }
        }

        /// <summary>
        /// 모든 요소의 타입이 <see cref="inspectionType"/>와 동일해야합니다.<br/>
        /// 값이 유효한지 검사하지 않습니다!
        /// </summary>
        public IReadOnlyList<IEnumerable> instances
        {
            get
            {
                parentElement?.UpdateChildInspectable();
                return readOnlyInstances;
            }
        }
        readonly IReadOnlyList<IEnumerable> readOnlyInstances;
        readonly List<IEnumerable> _instances = new List<IEnumerable>();
        readonly List<IEnumerable> staleKeysBuffer = new List<IEnumerable>();

        public IReadOnlyList<ListHandlerBase> listHandlers
        {
            get
            {
                parentElement?.UpdateChildInspectable();
                return readOnlyListHandlers;
            }
        }
        readonly IReadOnlyList<ListHandlerBase> readOnlyListHandlers;
        readonly List<ListHandlerBase> _listHandlers = new List<ListHandlerBase>();
        readonly Dictionary<IEnumerable, ListHandlerBase> handlerMap = new Dictionary<IEnumerable, ListHandlerBase>();

        [MemberNotNullWhen(false, nameof(instance), nameof(listHandler))]
        public bool instancesIsEmpty => instance == null;

        [MemberNotNullWhen(true, nameof(instance), nameof(listHandler))]
        public bool instanceIsMultiple => instances.TwoOrMore();

        public int instanceCount => instances.Count();

        public Action<IEnumerable<object?>>? onValueChanged { get; set; }

        public object? this[int index]
        {
            get
            {
                var handler = listHandler;
                ExceptionUtility.ThrowIfArgumentNull(handler, nameof(listHandler));

                if (index < 0 || index >= count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return handler[index];
            }
            set
            {
                if (index < 0 || index >= count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                var handlers = listHandlers;
                for (int i = 0; i < handlers.Count; i++)
                {
                    ListHandlerBase? list = handlers[i];
                    list[index] = value;
                }
            }
        }

        public int count
        {
            get
            {
                var handler = listHandler;
                ExceptionUtility.ThrowIfArgumentNull(handler, nameof(listHandler));
                return handler.count;
            }
        }
        int ICollection.Count => count;
        
        public void SetInstances(IEnumerable<IEnumerable> instances)
        {
            _instances.Clear();
            _instances.Capacity = instances switch
            {
                ICollection<IEnumerable> genericCollection when _instances.Capacity < genericCollection.Count => genericCollection.Count,
                _ => _instances.Capacity
            };

            switch (instances)
            {
                case IList<IEnumerable> genericList:
                {
                    for (int i = 0; i < genericList.Count; i++)
                        _instances.Add(genericList[i]);
                    break;
                }
                default:
                {
                    _instances.AddRange(instances);
                    break;
                }
            }
            
            // 인스턴스가 동일하면 리스트 핸들러를 새로 만들지 않기 위해 맵으로 관리하는거니 지우지 말것
            
            // -------------------------------------------------------------
            // 맵 동기화 (SyncKeysWithEnumerable 인라인 -> 델리게이트 GC 제거)
            // -------------------------------------------------------------

            // A. 제거
            staleKeysBuffer.Clear();
            foreach (var key in handlerMap.Keys)
            {
                bool isUsed = false;
                
                // _instances는 List<T>이므로 for문 사용 (GC 없음)
                // ReferenceEquals로 빠르게 비교
                var instanceCount = _instances.Count;
                for (int i = 0; i < instanceCount; i++)
                {
                    if (ReferenceEquals(_instances[i], key))
                    {
                        isUsed = true;
                        break;
                    }
                }

                if (!isUsed)
                    staleKeysBuffer.Add(key);
            }

            // 버퍼에 담긴 키 제거
            var removeCount = staleKeysBuffer.Count;
            for (int i = 0; i < removeCount; i++)
                handlerMap.Remove(staleKeysBuffer[i]);

            // B. 추가
            for (int i = 0; i < _instances.Count; i++)
            {
                var instance = _instances[i];
                // 델리게이트(Func) 생성 없이 직접 팩토리 메서드 호출
                if (instance != null && !handlerMap.ContainsKey(instance))
                    handlerMap.Add(instance, ListHandlerBase.FindListHandler(instance));
            }

            _listHandlers.Clear();
            for (int i = 0; i < _instances.Count; i++)
            {
                var instance = _instances[i];
                if (handlerMap.TryGetValue(instance, out var handler))
                    _listHandlers.Add(handler);
            }
        }

        public void OnValueChangedInvoke()
        {
            onValueChanged?.SafeInvoke();
            parentElement?.inspectable.OnValueChangedInvoke();
        }

        public void SynchronizeCollections()
        {
            var handlers = listHandlers;
            for (int i = 0; i < handlers.Count; i++)
            {
                ListHandlerBase? item = handlers[i];
                item.SynchronizeCollections();
            }
        }

        public int Add(object? value)
        {
            int minCount = count;
            if (parentElement == null || !isArray)
            {
                var handlers = listHandlers;
                for (int i = 0; i < handlers.Count; i++)
                {
                    ListHandlerBase? item = handlers[i];
                    item.Add(value);
                }
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
                var handlers = listHandlers;
                for (int i = 0; i < handlers.Count; i++)
                {
                    ListHandlerBase? item = handlers[i];
                    item.Insert(index, value);
                }
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
                var handlers = listHandlers;
                for (int i = 0; i < handlers.Count; i++)
                {
                    ListHandlerBase? item = handlers[i];
                    if (item == minCollectionHandler)
                    {
                        int index = item.IndexOf(value);
                        item.RemoveAt(index);
                        OnRemoveAt(index);
                    }
                    else
                        item.Remove(value);
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
                var handlers = listHandlers;
                for (int i = 0; i < handlers.Count; i++)
                {
                    ListHandlerBase? item = handlers[i];
                    item.RemoveAt(index);
                }
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
                var handlers = listHandlers;
                for (int i = 0; i < handlers.Count; i++)
                {
                    ListHandlerBase? item = handlers[i];
                    item.Clear();
                }
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

            var handlers = listHandlers;
            for (int i = 0; i < handlers.Count; i++)
            {
                ListHandlerBase? item = handlers[i];
                item.Move(oldIndex, newIndex);
            }

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

            OnValueChangedInvoke();
        }

        public virtual void OnRemoveAt(int index)
        {
            if (index < 0 || index >= cachedElements.Count)
                return;

            cachedElements.RemoveAt(index);

            for (int i = 0; i < cachedElements.Count; i++)
                cachedElements[i].index = i;

            OnValueChangedInvoke();
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

            OnValueChangedInvoke();
        }

        public virtual void OnElementChanged(int oldIndex, int newIndex)
        {
            cachedElements.Change(oldIndex, newIndex);

            for (int i = 0; i < cachedElements.Count; i++)
                cachedElements[i].index = i;

            OnValueChangedInvoke();
        }

        public virtual void OnClear()
        {
            cachedElements.Clear();
            OnValueChangedInvoke();
        }

        public bool Contains(object? value)
        {
            var handler = listHandler;
            ExceptionUtility.ThrowIfArgumentNull(handler, nameof(listHandler));
            var handlers = listHandlers;
            // ReSharper disable once LoopCanBeConvertedToQuery
            for (int i = 0; i < handlers.Count; i++)
            {
                ListHandlerBase? item = handlers[i];
                if (item.Contains(value))
                    return true;
            }
            return false;
        }

        public int IndexOf(object value)
        {
            var handler = listHandler;
            ExceptionUtility.ThrowIfArgumentNull(handler, nameof(listHandler));
            return handler.IndexOf(value);
        }

        public IEnumerator GetEnumerator()
        {
            var handler = listHandler;
            ExceptionUtility.ThrowIfArgumentNull(handler, nameof(listHandler));
            return handler.GetEnumerator();
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

        /// <inheritdoc cref="IInspectableList.Clone"/>
        public InspectableList Clone()
        {
            InspectableList clonedList = new InspectableList(inspectionType, instances, elementNullabilityInfo) { parentElement = parentElement?.Clone(), onValueChanged = onValueChanged };
            clonedList.SynchronizeCollections();

            return clonedList;
        }
        IInspectableList IInspectableList.Clone() => Clone();
    }
}