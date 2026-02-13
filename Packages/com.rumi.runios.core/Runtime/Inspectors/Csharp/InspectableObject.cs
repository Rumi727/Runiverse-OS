#nullable enable
using RuniOS.Linq;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace RuniOS.Inspectors.Csharp
{
    public class InspectableObject : IInspectableObject
    {
        public IInspectorVariableElement? parentElement { get; }

        public Type inspectionType { get; }
        public string inspectionDisplayName => inspectionType.GetTypeDisplayName();

        /// <summary>
        /// 타입이 <see cref="inspectionType"/>와 동일해야합니다.<br/>
        /// 값이 유효한지 검사하지 않습니다!
        /// </summary>
        public object? instance
        {
            get
            {
                parentElement?.UpdateChildInspectable();
                for (int i = 0; i < _instances.Count; i++)
                {
                    var item = _instances[i];
                    if (!item.IsNull())
                        return item;
                }

                return null;
            }
            set
            {
                _instances.Clear();
                if (value != null)
                    _instances.Add(value);
            }
        }

        /// <summary>
        /// 모든 요소의 타입이 <see cref="inspectionType"/>와 동일해야합니다.<br/>
        /// 값이 유효한지 검사하지 않습니다!
        /// </summary>
        public IReadOnlyList<object?> instances
        {
            get
            {
                parentElement?.UpdateChildInspectable();
                return readOnlyInstances;
            }
        }
        readonly IReadOnlyList<object?> readOnlyInstances;
        readonly List<object?> _instances = new List<object?>();

        [MemberNotNullWhen(false, nameof(instance))]
        public bool instancesIsEmpty => instance.IsNull();

        public bool instanceIsMultiple => instances.TwoOrMore();

        public int instanceCount => instances.Count;

        public Action<IEnumerable<object?>>? onValueChanged { get; set; }

        public ImmutableArray<IInspectorElement> elements
        {
            get
            {
                if (_elements.IsDefault)
                {
                    _elements =
                        inspectionType.GetRuntimeProperties()
                            .Where(x => x.GetIndexParameters().IsEmpty())
                            .Select(IInspectorElement (x) => new PropertyElement(this, x))
                            .Concat
                            (
                                inspectionType.GetRuntimeFields()
                                    .Select(IInspectorElement (x) => new FieldElement(this, x))
                            )
                            .Concat
                            (
                                inspectionType.GetRuntimeMethods()
                                    .Select(IInspectorElement (x) => new MethodElement(this, x))
                            )
                            .ToImmutableArray();
                }
                
                return _elements;
            }
        }
        ImmutableArray<IInspectorElement> _elements;

        public ImmutableDictionary<string, IInspectorVariableElement> variableElements =>
            _variableElements ??= elements
                .OfType<IInspectorVariableElement>()
                .ToImmutableDictionary(x => x.name, x => x);
        ImmutableDictionary<string, IInspectorVariableElement>? _variableElements;

        public InspectableObject(object instance) : this(instance.GetType(), Enumerable.Repeat(instance, 1)) { }
        public InspectableObject(Type inspectionType) : this(inspectionType, Enumerable.Empty<object>()) { }
        public InspectableObject(Type inspectionType, params object?[] instances) : this(inspectionType, instances.WhereNotNull()) { }

        public InspectableObject(Type inspectionType, IEnumerable instances) : this(null, inspectionType, instances) { }
        
        public InspectableObject(IInspectorVariableElement? parentElement, Type inspectionType) : this(parentElement, inspectionType, Enumerable.Empty<object?>()) { }
        public InspectableObject(IInspectorVariableElement? parentElement, Type inspectionType, IEnumerable instances)
        {
            this.parentElement = parentElement;

            this.inspectionType = inspectionType;
            readOnlyInstances = _instances.AsReadOnly();
            
            SetInstances(instances);

            attributes = parentElement?.attributes.Where(x => !x.applyToSelf).ToImmutableArray() ?? ImmutableArray<IInspectorAttribute>.Empty;
        }

        public void SetInstances(IEnumerable instances)
        {
            _instances.Clear();
            _instances.Capacity = instances switch
            {
                ICollection collection when _instances.Capacity < collection.Count => collection.Count,
                ICollection<object> genericCollection when _instances.Capacity < genericCollection.Count => genericCollection.Count,
                _ => _instances.Capacity
            };

            switch (instances)
            {
                case IList list:
                {
                    for (int i = 0; i < list.Count; i++)
                        _instances.Add(list[i]);
                    break;
                }
                case IList<object?> genericList:
                {
                    for (int i = 0; i < genericList.Count; i++)
                        _instances.Add(genericList[i]);
                    break;
                }
                default:
                {
                    foreach (var instance in instances)
                        _instances.Add(instance);
                    break;
                }
            }
        }

        public bool TryGetInspectionType([NotNullWhen(true)] out Type? type)
        {
            type = inspectionType;
            return true;
        }

        public void OnValueChangedInvoke()
        {
            onValueChanged?.SafeInvoke(instances);
            parentElement?.inspectable.OnValueChangedInvoke();
        }

        public IEnumerable<IInspectorElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            if (flags == InspectorFlags.None)
                return Array.Empty<IInspectorElement>();

            return elements.Where(x => x.HasFlags(flags));
        }

        public IInspectorVariableElement GetVariableElement(string name, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            if (!variableElements.TryGetValue(name, out IInspectorVariableElement? value))
                throw new InvalidOperationException($"Could not find variable element named {name}!");

            return value;
        }

        public void InstanceTypeCheck()
        {
            if (_instances.Any(x => !inspectionType.IsInstanceOfType(x)))
            {
                string invalidTypes = string.Join
                (
                    ", ",
                    _instances
                        .Where(x => x != null && !inspectionType.IsInstanceOfType(x))
                        .Select(static x => $"'{x!.GetType().FullName}'")
                        .Distinct()
                );

                throw new InspectorException($"One or more elements in the collection have invalid types. Expected '{inspectionType.FullName}', but received the following: {invalidTypes}.");
            }
        }

        /// <inheritdoc cref="IInspectableObject.Clone"/>
        public InspectableObject Clone() => new InspectableObject(parentElement?.Clone(), inspectionType, instances) { onValueChanged = onValueChanged };
        IInspectableObject IInspectableObject.Clone() => Clone();
    }
}