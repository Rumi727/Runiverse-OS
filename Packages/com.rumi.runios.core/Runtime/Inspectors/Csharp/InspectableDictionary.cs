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
    public class InspectableDictionary : IInspectableDictionary
    {
        public InspectableDictionary(IEnumerable instance, NullabilityInfo? elementNullabilityInfo = null) : this(instance.GetType(), Enumerable.Repeat(instance, 1), elementNullabilityInfo) { }

        public InspectableDictionary(Type inspectionType, NullabilityInfo? elementNullabilityInfo = null) : this(inspectionType, Enumerable.Empty<IEnumerable>(), elementNullabilityInfo) { }

        public InspectableDictionary(Type inspectionType, NullabilityInfo? elementNullabilityInfo, params IEnumerable[] instances) : this(inspectionType, instances.ToImmutableArray(), elementNullabilityInfo) { }

        public InspectableDictionary(Type inspectionType, IEnumerable<IEnumerable> instances, NullabilityInfo? elementNullabilityInfo = null)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(inspectionType))
                throw new ArgumentException($"Provided type '{inspectionType.FullName}' is not a enumerable type.", nameof(inspectionType));
            if (!CollectionHandlerBase.HandlerCheck<DictionaryHandlerBase>(inspectionType))
                throw new ArgumentException($"Provided type '{inspectionType.FullName}' is not a dictionary type.", nameof(inspectionType));

            this.inspectionType = inspectionType;
            inspectionElementType = CollectionGenericUtility.GetDictionaryElementType(inspectionType);

            this.instances = instances;

            this.elementNullabilityInfo = elementNullabilityInfo;

            dictionaryHandlers = _dictionaryHandlers.AsReadOnly();
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

                if (value != null)
                    instances = Enumerable.Repeat(value, 1);
                else
                    instances = Array.Empty<IList>();
            }
        }

        public DictionaryHandlerBase? dictionaryHandler
        {
            get
            {
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
        public IEnumerable<IEnumerable> instances
        {
            get
            {
                parentElement?.UpdateChildInspectable();
                return _instances;
            }
            set
            {
                _instances.Clear();
                if (value is ICollection<IEnumerable> col)
                {
                    if (_instances.Capacity < col.Count)
                        _instances.Capacity = col.Count;
                }
                _instances.AddRange(value);
                
                _dictionaryHandlers.Clear();
                for (int i = 0; i < _instances.Count; i++)
                {
                    var instance = _instances[i];
                    if (instance != null)
                        _dictionaryHandlers.Add(DictionaryHandlerBase.FindDictionaryHandler(instance));
                }
            }
        }
        readonly List<IEnumerable> _instances = new List<IEnumerable>();

        public IReadOnlyList<DictionaryHandlerBase> dictionaryHandlers { get; }
        readonly List<DictionaryHandlerBase> _dictionaryHandlers = new List<DictionaryHandlerBase>();

        [MemberNotNullWhen(false, nameof(instance), nameof(dictionaryHandler))]
        public bool instancesIsEmpty => instance == null;

        [MemberNotNullWhen(true, nameof(instance), nameof(dictionaryHandler))]
        public bool instanceIsMultiple => instances.TwoOrMore();

        public int instanceCount => instances.Count();

        public Action? onValueChanged { get; set; }

        public object? this[object key]
        {
            get
            {
                ExceptionUtility.ThrowIfArgumentNull(dictionaryHandler, nameof(dictionaryHandler));
                return dictionaryHandler[key];
            }
            set
            {
                foreach (var list in dictionaryHandlers)
                    list[key] = value;
            }
        }

        public ICollection keys
        {
            get
            {
                ExceptionUtility.ThrowIfArgumentNull(dictionaryHandler, nameof(dictionaryHandler));
                return dictionaryHandler.keys;
            }
        }
        ICollection IDictionary.Keys => keys;

        public ICollection values
        {
            get
            {
                ExceptionUtility.ThrowIfArgumentNull(dictionaryHandler, nameof(dictionaryHandler));
                return dictionaryHandler.values;
            }
        }
        ICollection IDictionary.Values => values;

        public int count
        {
            get
            {
                ExceptionUtility.ThrowIfArgumentNull(dictionaryHandler, nameof(dictionaryHandler));
                return dictionaryHandler.count;
            }
        }
        int ICollection.Count => count;

        public void OnValueChangedInvoke()
        {
            onValueChanged?.SafeInvoke();
            parentElement?.inspectable.OnValueChangedInvoke();
        }

        public void SynchronizeCollections()
        {
            foreach (var item in dictionaryHandlers)
                item.SynchronizeCollections();
        }

        public void UpdateSourceCollections()
        {
            foreach (var item in dictionaryHandlers)
                item.UpdateSourceCollections();
        }

        public void Add(object key, object? value)
        {
            foreach (var list in dictionaryHandlers)
                list.Add(key, value);
        }

        public void Remove(object key)
        {
            foreach (var list in dictionaryHandlers)
                list.Remove(key);
        }

        public void Clear()
        {
            foreach (var list in dictionaryHandlers)
                list.Clear();
        }

        public bool Contains(object key)
        {
            ExceptionUtility.ThrowIfArgumentNull(dictionaryHandler, nameof(dictionaryHandler));
            return dictionaryHandlers.Any(x => x.Contains(key));
        }

        public void RenameKey(object fromKey, object toKey)
        {
            if (!Contains(fromKey))
                throw new KeyNotFoundException($"Key '{fromKey}' not found.");

            foreach (var list in dictionaryHandlers)
            {
                object? value = list[fromKey];

                list.Remove(fromKey);
                list[toKey] = value;
            }

            OnRenamedKey(fromKey, toKey);
        }

        public void OnRenamedKey(object fromKey, object toKey) => cachedElements.RenameKey(fromKey, toKey);

        public IDictionaryEnumerator GetEnumerator()
        {
            ExceptionUtility.ThrowIfArgumentNull(dictionaryHandler, nameof(dictionaryHandler));
            return dictionaryHandler.GetEnumerator();
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
        readonly List<object> elementKeysBuffer = new List<object>();
        public IReadOnlyDictionary<object, IInspectorDictionaryElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            readOnlyCachedElements ??= cachedElements.AsReadOnly();

            if (!flags.HasFlagFast(InspectorFlags.List) || (isReadOnly && !flags.HasFlagFast(InspectorFlags.ReadOnly)) || instancesIsEmpty)
            {
                cachedElements.Clear();
                return readOnlyCachedElements;
            }

            cachedElements.SyncKeysWithEnumerable(dictionaryHandler.keys.Cast<object>(), CreateElement, elementKeysBuffer);
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

        IInspectableDictionary IInspectableDictionary.Clone() => new InspectableDictionary(inspectionType, elementNullabilityInfo) { parentElement = parentElement, instances = instances };
    }
}