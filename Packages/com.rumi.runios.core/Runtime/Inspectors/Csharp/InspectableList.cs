#nullable enable
using RuniOS.Collections.Generic;
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
        public InspectableList(IList instance, RuniNullabilityInfo? nullabilityInfo = null) : this(instance.GetType(), nullabilityInfo, ImmutableArray.Create(instance)) { }
        
        public InspectableList(Type inspectionType, RuniNullabilityInfo? nullabilityInfo, params IList[] instances) : this(inspectionType, instances.ToImmutableArray(), nullabilityInfo) { }
        
        public InspectableList(Type inspectionType, IEnumerable<IList> instances, RuniNullabilityInfo? nullabilityInfo = null)
        {
            if (!typeof(IList).IsAssignableFrom(inspectionType))
                throw new ArgumentException($"Provided type '{inspectionType.FullName}' is not a list type.", nameof(inspectionType));
            
            this.inspectionType = inspectionType;
            inspectionElementType = inspectionType.IsArray ? inspectionType.GetElementType() : CollectionGenericUtility.GetListElementType(inspectionType);

            _instances = null!;
            this.instances = instances;

            this.nullabilityInfo = nullabilityInfo;
        }
        
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

        public RuniNullabilityInfo? nullabilityInfo { get; }

        public bool isReadOnly => instances.All(x => !x.IsReadOnly);
        bool IList.IsReadOnly => isReadOnly;
        
        public bool isFixedSize => instances.Any(x => (parentElement == null || !x.GetType().IsArray) && x.IsFixedSize);
        bool IList.IsFixedSize => isFixedSize;
        
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        public IList? instance
        {
            get
            {
                parentElement?.UpdateChildInspectable();
                
                if (_instances.Any())
                    return _instances.MinBy(static x => x.Count);
                
                return null;
            }
            set
            {
                if (value != null && inspectionType != value.GetType())
                    throw new InspectorException($"Invalid type. Expected '{inspectionType.FullName}', but received '{value.GetType().FullName}'.");
                
                if (value != null)
                    instances = Enumerable.Repeat(value, 1);
                else
                    instances = Array.Empty<IList>();
            }
        }

        public IEnumerable<IList> instances
        {
            get
            {
                parentElement?.UpdateChildInspectable();
                return _instances;
            }
            set
            {
                if (value.Any(x => inspectionType != x.GetType()))
                {
                    string invalidTypes = string.Join(", ", value.Where(x => x != null && !inspectionType.IsInstanceOfType(x))
                        .Select(static x => $"'{x!.GetType().FullName}'")
                        .Distinct());
                                            
                    throw new InspectorException($"One or more elements in the collection have invalid types. Expected '{inspectionType.FullName}', but received the following: {invalidTypes}.");
                }

                _instances = value;
            }
        }
        IEnumerable<IList> _instances;

        [MemberNotNullWhen(false, nameof(instance))]
        public bool instancesIsEmpty => instance == null;

        public object? this[int index]
        {
            get
            {
                ExceptionUtility.ThrowIfArgumentNull(instance, nameof(instance));
                
                if (index < 0 || index >= count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return instance[index];
            }
            set
            {
                if (index < 0 || index >= count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                
                foreach (var list in instances.WhereNotNull())
                {
                    if (list.Count > index)
                        list[index] = value;
                }
            }
        }
        
        public int count
        {
            get
            {
                ExceptionUtility.ThrowIfArgumentNull(instance, nameof(instance));
                return instance.Count;
            }
            set
            {
                foreach (var list in instances)
                {
                    if (list == null)
                        continue;

                    bool add = list.Count < value;
                    int count = (list.Count - value).Abs();
                    
                    for (int i = 0; i < count; i++)
                    {
                        if (add)
                        {
                            if (nullabilityInfo?.writeState == RuniNullabilityState.NotNull)
                                list.Add((inspectionElementType ?? typeof(object)).GetDefaultValueNotNull());
                            else
                                list.Add((inspectionElementType ?? typeof(object)).GetDefaultValue());
                        }
                        else
                            list.RemoveAt(list.Count - 1);
                    }
                }
            }
        }
        int ICollection.Count => count;
        
        

        public int Add(object? value)
        {
            int minCount = count;
            foreach (IList list in instances)
            {
                if (minCount == list.Count && (parentElement == null || !list.GetType().IsArray))
                    list.Add(value);
            }
            
            parentElement?.SetValues(instances.Select(list =>
            {
                if (minCount == list.Count && list.GetType().IsArray)
                    list = ((Array)list).Add(value);

                return list;
            }));
            
            OnInsert(minCount);
            return minCount;
        }
        
        public void Insert(int index, object value)
        {
            if (index < 0 || index > count)
                throw new ArgumentOutOfRangeException(nameof(index));
            
            foreach (IList list in instances)
            {
                if (parentElement == null || !list.GetType().IsArray)
                    list.Insert(index, value);
            }

            parentElement?.SetValues(instances.Select(list =>
            {
                if (list.GetType().IsArray)
                    list = ((Array)list).Insert(index, value);

                return list;
            }));
            
            OnInsert(index);
        }

        public void Remove(object value) => RemoveAt(IndexOf(value));
        
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));

            foreach (IList list in instances.WhereNotNull())
            {
                if (parentElement == null || !list.GetType().IsArray)
                    list.RemoveAt(index);
            }

            parentElement?.SetValues(instances.Select(list =>
            {
                if (list.GetType().IsArray)
                    list = ((Array)list).RemoveAt(index);

                return list;
            }));
            
            OnRemoveAt(index);
        }

        public void Clear()
        {
            foreach (IList list in instances)
            {
                if (parentElement == null || !list.GetType().IsArray)
                    list.Clear();
            }

            parentElement?.SetValues(instances.Select(list =>
            {
                if (list.GetType().IsArray)
                    list = ((Array)list).RemoveAll();

                return list;
            }));
            
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
            ExceptionUtility.ThrowIfArgumentNull(instance, nameof(instance));
            return instance.Contains(value);
        }

        public int IndexOf(object value)
        {
            ExceptionUtility.ThrowIfArgumentNull(instance, nameof(instance));
            return instance.IndexOf(value);
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
        
        

        readonly List<IInspectorListElement> cachedElements = new();
        IReadOnlyList<IInspectorListElement>? readOnlyCachedElements;
        public IReadOnlyList<IInspectorElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            if (!flags.HasFlagFast(InspectorFlags.List) || (isReadOnly && !flags.HasFlagFast(InspectorFlags.ReadOnly)))
                return ImmutableArray<IInspectorElement>.Empty;
            
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
                    cachedElements.Add(new ListElement(this, i));
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
            if (!flags.HasFlagFast(InspectorFlags.List) || (isReadOnly && !flags.HasFlagFast(InspectorFlags.ReadOnly)))
                return null;

            GetElements();
            IInspectorListElement element = cachedElements[index];
            if (!element.HasFlags(flags))
                return null;

            return element;
        }
        
        IEnumerable<IInspectorElement> IInspectable.GetElements(InspectorFlags flags) => GetElements(flags);
        
        public IInspectableList Clone() => new InspectableList(inspectionType, nullabilityInfo) { parentElement = parentElement, instances = instances };
        IInspectable IInspectable.Clone() => Clone();
        object ICloneable.Clone() => Clone();
    }
}