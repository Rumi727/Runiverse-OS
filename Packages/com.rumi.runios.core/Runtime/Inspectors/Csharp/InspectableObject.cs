#nullable enable
using RuniOS.Linq;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace RuniOS.Inspectors.Csharp
{
    public class InspectableObject : IInspectableObject
    {
        public IInspectorVariableElement? parentElement { get; set; }
        
        public Type inspectionType { get; }
        public string inspectionDisplayName => inspectionType.GetTypeDisplayName();
        
        public object? instance
        {
            get => instances.WhereNotNull().FirstOrDefault();
            set
            {
                if (value != null)
                    instances = Enumerable.Repeat(value, 1);
                else
                    instances = Enumerable.Empty<object>();
            }
        }

        /// <summary>
        /// 모든 요소의 타입이 <see cref="inspectionType"/>와 동일해야합니다.<br/>
        /// 값이 유효한지 검사하지 않습니다!
        /// </summary>
        public IEnumerable<object> instances
        {
            get
            {
                parentElement?.UpdateChildInspectable();
                return _instances.Where(x => inspectionType.IsInstanceOfType(x));
            }
            set => _instances = value;
        }
        IEnumerable<object> _instances;

        [MemberNotNullWhen(false, nameof(instance))]
        public bool instancesIsEmpty => instance == null;

        public bool instanceIsMultiple => instances.TwoOrMore();
        
        public int instanceCount => instances.Count();

        public InspectableObject(object instance) : this(instance.GetType(), ImmutableArray.Create(instance)) { }
        public InspectableObject(Type inspectionType) : this(inspectionType, Enumerable.Empty<object>()) { }
        public InspectableObject(Type inspectionType, params object?[] instances) : this(inspectionType, instances.WhereNotNull()) { }
        
        public InspectableObject(Type inspectionType, IEnumerable<object> instances)
        {
            this.inspectionType = inspectionType;
            
            _instances = null!;
            this.instances = instances;
        }

        public bool TryGetInspectionType([NotNullWhen(true)] out Type? type)
        {
            type = inspectionType;
            return true;
        }

        public IEnumerable<IInspectorElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            if (flags == InspectorFlags.None)
                return ImmutableArray<IInspectorElement>.Empty;
            
            return inspectionType.GetRuntimeProperties().Where(x => x.GetIndexParameters().IsEmpty()).Select(x => (IInspectorElement)new PropertyElement(this, x))
                .Concat(inspectionType.GetRuntimeFields().Select(x => (IInspectorElement)new FieldElement(this, x)))
                .Concat(inspectionType.GetRuntimeMethods().Select(x => (IInspectorElement)new MethodElement(this, x)))
                .WhereNotNull().Where(x => x.HasFlags(flags));
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

        public IInspectableObject Clone() => new InspectableObject(inspectionType) { parentElement = parentElement, instances = instances };
        IInspectable IInspectable.Clone() => Clone();
        object ICloneable.Clone() => Clone();
    }
}