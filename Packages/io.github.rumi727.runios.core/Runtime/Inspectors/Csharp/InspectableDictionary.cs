#nullable enable
using RuniOS.Collections.Generic;
using RuniOS.Collections.Handlers;
using RuniOS.Inspectors.Attributes;
using RuniOS.Linq;
using RuniOS.Reflection;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Inspectors.Csharp
{
    public class InspectableDictionary : IInspectableDictionary
    {
        public InspectableDictionary(IEnumerable instance, NullabilityInfo? elementNullabilityInfo = null) : this(instance.GetType(), Enumerable.Repeat(instance, 1), elementNullabilityInfo) { }

        public InspectableDictionary(Type inspectionType, NullabilityInfo? elementNullabilityInfo = null) : this(inspectionType, [], elementNullabilityInfo) { }

        public InspectableDictionary(Type inspectionType, NullabilityInfo? elementNullabilityInfo, params IEnumerable[] instances) : this(inspectionType, [..instances], elementNullabilityInfo) { }

        public InspectableDictionary(Type inspectionType, IEnumerable<IEnumerable> instances, NullabilityInfo? elementNullabilityInfo = null) : this(null, inspectionType, instances, elementNullabilityInfo) { }

        public InspectableDictionary(IInspectorVariableElement? parentElement, Type inspectionType, NullabilityInfo? elementNullabilityInfo = null) : this(parentElement, inspectionType, [], elementNullabilityInfo) { }
        public InspectableDictionary(IInspectorVariableElement? parentElement, Type inspectionType, IEnumerable<IEnumerable> instances, NullabilityInfo? elementNullabilityInfo = null)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(inspectionType))
                throw new ArgumentException($"Provided type '{inspectionType.FullName}' is not a enumerable type.", nameof(inspectionType));
            if (!CollectionHandlerBase.HandlerCheck<DictionaryHandlerBase>(inspectionType))
                throw new ArgumentException($"Provided type '{inspectionType.FullName}' is not a dictionary type.", nameof(inspectionType));

            this.parentElement = parentElement;

            this.inspectionType = inspectionType;
            inspectionElementType = CollectionGenericUtility.GetDictionaryElementType(inspectionType);

            this.elementNullabilityInfo = elementNullabilityInfo;

            readOnlyInstances = _instances.AsReadOnly();
            readonlyDictionaryHandlers = _dictionaryHandlers.AsReadOnly();
            
            SetInstances(instances);
            
            attributes = [
                ..inspectionType.GetCustomAttributes(true)
                    .OfType<IInspectorAttribute>()
                    .InheritFrom(parentElement)
            ];
        }

        public IInspectorVariableElement? parentElement { get; set; }

        public Type inspectionType { get; }
        public string inspectionDisplayName => inspectionType.GetTypeDisplayName();

        /// <remarks>
        /// null을 반환하는 경우, 리스트가 모든 타입 형식을 허용한다는 의미입니다.
        /// </remarks>
        public KeyValuePair<Type, Type>? inspectionElementType { get; }

        public NullabilityInfo? elementNullabilityInfo { get; }

        public bool isReadOnly
        {
            get
            {
                // ReSharper disable once LoopCanBeConvertedToQuery
                for (int i = 0; i < _dictionaryHandlers.Count; i++)
                {
                    if (_dictionaryHandlers[i].isReadOnly)
                        return true;
                }
                
                return false;
            }
        }
        bool IDictionary.IsReadOnly => isReadOnly;

        public bool isFixedSize
        {
            get
            {
                if (parentElement != null && isArray)
                    return false;

                // ReSharper disable once LoopCanBeConvertedToQuery
                for (int i = 0; i < _dictionaryHandlers.Count; i++)
                {
                    if (_dictionaryHandlers[i].isFixedSize)
                        return true;
                }
                return false;
            }
        }
        bool IDictionary.IsFixedSize => isFixedSize;

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
                if (_dictionaryHandlers.Count == 0)
                    return null;

                var minHandler = _dictionaryHandlers[0];
                int minCount = minHandler.count;

                for (int i = 1; i < _dictionaryHandlers.Count; i++)
                {
                    DictionaryHandlerBase? currentHandler = _dictionaryHandlers[i];
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

        public DictionaryHandlerBase? dictionaryHandler
        {
            get
            {
                parentElement?.UpdateChildInspectable();
                if (_dictionaryHandlers.Count == 0)
                    return null;

                var minHandler = _dictionaryHandlers[0];
                int minCount = minHandler.count;

                for (int i = 1; i < _dictionaryHandlers.Count; i++)
                {
                    DictionaryHandlerBase? currentHandler = _dictionaryHandlers[i];
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
        readonly List<IEnumerable> _instances = [];
        readonly List<IEnumerable> staleKeysBuffer = [];

        public IReadOnlyList<DictionaryHandlerBase> dictionaryHandlers
        {
            get
            {
                parentElement?.UpdateChildInspectable();
                return readonlyDictionaryHandlers;
            }
        }
        readonly IReadOnlyList<DictionaryHandlerBase> readonlyDictionaryHandlers;
        readonly List<DictionaryHandlerBase> _dictionaryHandlers = [];
        readonly Dictionary<IEnumerable, DictionaryHandlerBase> handlerMap = new Dictionary<IEnumerable, DictionaryHandlerBase>();

        [MemberNotNullWhen(false, nameof(instance), nameof(dictionaryHandler))]
        public bool instancesIsEmpty => instance == null;

        [MemberNotNullWhen(true, nameof(instance), nameof(dictionaryHandler))]
        public bool instanceIsMultiple => instances.TwoOrMore();

        public int instanceCount => instances.Count();

        public Action<IEnumerable<object?>>? onValueChanged { get; set; }

        public ImmutableArray<IInspectorAttribute> attributes { get; }

        public object? this[object key]
        {
            get
            {
                var handler = dictionaryHandler;
                ExceptionUtility.ThrowIfArgumentNull(handler, nameof(dictionaryHandler));
                return handler[key];
            }
            set
            {
                var handlers = dictionaryHandlers;
                for (int i = 0; i < handlers.Count; i++)
                {
                    DictionaryHandlerBase? handler = handlers[i];
                    handler[key] = value;
                }
            }
        }

        public ICollection keys
        {
            get
            {
                var handler = dictionaryHandler;
                ExceptionUtility.ThrowIfArgumentNull(handler, nameof(dictionaryHandler));
                return handler.keys;
            }
        }
        ICollection IDictionary.Keys => keys;

        public ICollection values
        {
            get
            {
                var handler = dictionaryHandler;
                ExceptionUtility.ThrowIfArgumentNull(handler, nameof(dictionaryHandler));
                return handler.values;
            }
        }
        ICollection IDictionary.Values => values;

        public int count
        {
            get
            {
                var handler = dictionaryHandler;
                ExceptionUtility.ThrowIfArgumentNull(handler, nameof(dictionaryHandler));
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
                    handlerMap.Add(instance, DictionaryHandlerBase.FindDictionaryHandler(instance));
            }

            _dictionaryHandlers.Clear();
            var count = _instances.Count;
            for (int i = 0; i < count; i++)
            {
                var inst = _instances[i];
                if (inst != null && handlerMap.TryGetValue(inst, out var handler))
                    _dictionaryHandlers.Add(handler);
            }
        }

        public void OnValueChangedInvoke()
        {
            onValueChanged?.SafeInvoke(instances);
            parentElement?.inspectable.OnValueChangedInvoke();
        }

        public void SynchronizeCollections()
        {
            var handlers = dictionaryHandlers;
            for (int i = 0; i < handlers.Count; i++)
                handlers[i].SynchronizeCollections();
        }

        public void Add(object key, object? value)
        {
            var handlers = dictionaryHandlers;
            for (int i = 0; i < handlers.Count; i++)
                handlers[i].Add(key, value);
        }

        public void Remove(object key)
        {
            var handlers = dictionaryHandlers;
            for (int i = 0; i < handlers.Count; i++)
                handlers[i].Remove(key);
        }

        public void Clear()
        {
            var handlers = dictionaryHandlers;
            for (int i = 0; i < handlers.Count; i++)
                handlers[i].Clear();
        }

        public bool Contains(object key)
        {
            ExceptionUtility.ThrowIfArgumentNull(dictionaryHandler, nameof(dictionaryHandler));
            var handlers = dictionaryHandlers;
            // ReSharper disable once LoopCanBeConvertedToQuery
            for (int i = 0; i < handlers.Count; i++)
            {
                DictionaryHandlerBase x = handlers[i];
                if (x.Contains(key))
                    return true;
            }
            return false;
        }

        public void RenameKey(object fromKey, object toKey)
        {
            if (!Contains(fromKey))
                throw new KeyNotFoundException($"Key '{fromKey}' not found.");

            var handlers = dictionaryHandlers;
            for (int i = 0; i < handlers.Count; i++)
            {
                DictionaryHandlerBase? list = handlers[i];
                object? value = list[fromKey];

                list.Remove(fromKey);
                list[toKey] = value;
            }

            OnRenamedKey(fromKey, toKey);
        }

        public void OnRenamedKey(object fromKey, object toKey) => cachedElements.RenameKey(fromKey, toKey);

        public IDictionaryEnumerator GetEnumerator()
        {
            var handler = dictionaryHandler;
            ExceptionUtility.ThrowIfArgumentNull(handler, nameof(dictionaryHandler));
            return handler.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void CopyTo(Array array, int index) => throw new NotSupportedException("CopyTo is not implemented for multi-object editing.");

        public bool TryGetInspectionType([NotNullWhen(true)] out Type? type)
        {
            type = inspectionType;
            return true;
        }



        readonly Dictionary<object, IInspectorDictionaryElement> cachedElements = new();
        IReadOnlyDictionary<object, IInspectorDictionaryElement>? readOnlyCachedElements;
        public IReadOnlyDictionary<object, IInspectorDictionaryElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            readOnlyCachedElements ??= cachedElements.AsReadOnly();

            if (!flags.HasFlagFast(InspectorFlags.List) || (isReadOnly && !flags.HasFlagFast(InspectorFlags.ReadOnly)) || instancesIsEmpty)
            {
                cachedElements.Clear();
                return readOnlyCachedElements;
            }

            cachedElements.SyncKeysWithEnumerable(dictionaryHandler.keys.Cast<object>(), CreateElement);
            return readOnlyCachedElements;
        }

        public IInspectorDictionaryElement? GetElement(object key, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            if (!flags.HasFlagFast(InspectorFlags.List) || (isReadOnly && !flags.HasFlagFast(InspectorFlags.ReadOnly)))
                return null;

            GetElements();
            IInspectorDictionaryElement element = cachedElements[key];
            if (!element.HasFlags(flags))
                return null;

            return element;
        }

        protected virtual DictionaryValueElement CreateElement(object key) => new DictionaryValueElement(this, key);

        /// <inheritdoc cref="IInspectableDictionary.Clone"/>
        public InspectableDictionary Clone()
        {
            InspectableDictionary clonedDictionary = new InspectableDictionary(parentElement?.Clone(), inspectionType, instances, elementNullabilityInfo) { onValueChanged = onValueChanged };
            clonedDictionary.SynchronizeCollections();

            return clonedDictionary;
        }
        IInspectableDictionary IInspectableDictionary.Clone() => Clone();
    }
}