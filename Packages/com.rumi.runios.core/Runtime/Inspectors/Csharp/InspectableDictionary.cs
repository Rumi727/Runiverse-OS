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
            
            _instances = null!;
            this.instances = instances;
            
            this.elementNullabilityInfo = elementNullabilityInfo;

            readonlyDictionaryHandlerTable = _collectionHandlerTable.AsReadOnly();
        }
        
        public IInspectorVariableElement? parentElement { get; set; }
        
        public Type inspectionType { get; }
        public string inspectionDisplayName => inspectionType.GetTypeDisplayName();
        
        /// <remarks>
        /// null을 반환하는 경우, 리스트가 모든 타입 형식을 허용한다는 의미입니다.
        /// </remarks>
        public KeyValuePair<Type, Type>? inspectionElementType { get; }

        public NullabilityInfo? elementNullabilityInfo { get; }

        public bool isReadOnly => dictionaryHandlers.Any(x => x.isReadOnly);
        bool IDictionary.IsReadOnly => isReadOnly;
        
        public bool isFixedSize => dictionaryHandlers.Any(x => (parentElement == null || !isArray) && x.isFixedSize);
        bool IDictionary.IsFixedSize => isFixedSize;
        
        public bool isArray => inspectionType.IsArray;
        
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;
        
        public IEnumerable? instance
        {
            get
            {
                var instances = dictionaryHandlerTable;
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
        
        public DictionaryHandlerBase? dictionaryHandler
        {
            get
            {
                if (instance == null)
                    return null;
                
                return dictionaryHandlerTable[instance];
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
        
        public IEnumerable<DictionaryHandlerBase> dictionaryHandlers => instances.Select(x => dictionaryHandlerTable[x]);

        // 이 InspectableCollection이 관리하는 모든 원본 컬렉션에 매핑되는 CollectionHandler 맵
        public IReadOnlyDictionary<IEnumerable, DictionaryHandlerBase> dictionaryHandlerTable
        {
            get
            {
                _collectionHandlerTable.SyncKeysWithEnumerable(instances, DictionaryHandlerBase.FindDictionaryHandler);
                return readonlyDictionaryHandlerTable;
            }
        }
        readonly IReadOnlyDictionary<IEnumerable, DictionaryHandlerBase> readonlyDictionaryHandlerTable;
        readonly Dictionary<IEnumerable, DictionaryHandlerBase> _collectionHandlerTable = new();

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
        
        IInspectableDictionary IInspectableDictionary.Clone() => new InspectableDictionary(inspectionType, elementNullabilityInfo) { parentElement = parentElement, instances = instances };
    }
}