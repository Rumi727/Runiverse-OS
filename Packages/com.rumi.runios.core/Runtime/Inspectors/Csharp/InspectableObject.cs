#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace RuniOS.Inspectors.Csharp
{
    public class InspectableObject : IInspectableObject
    {
        public IInspectorVariableElement? parentElement { get; }
        
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

        public IEnumerable<object> instances
        {
            get => _instances.Where(x => inspectionType.IsInstanceOfType(x));
            set => _instances = value;
        }
        IEnumerable<object> _instances;

        [MemberNotNullWhen(false, nameof(instance))]
        public bool instancesIsEmpty => instance == null;

        IEnumerable<object> IInspectableObject.instances => instances;

        public InspectableObject(object instance) : this(instance.GetType(), ImmutableArray.Create(instance)) { }
        public InspectableObject(Type inspectionType, IInspectorVariableElement? parentElement = null) : this(inspectionType, Enumerable.Empty<object>()) => this.parentElement = parentElement; 
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

        public IReadOnlyList<IInspectorElement> GetElements(InspectorFlags flags = InspectorFlags.All)
        {
            if (flags == InspectorFlags.None)
                return ImmutableArray<IInspectorElement>.Empty;
            
            MemberInfo[] members = inspectionType.GetMembers(flags.ToBindingFlags());
            IInspectorElement?[] elements = new IInspectorElement[members.Length];

            bool includeReadOnly = flags.HasFlagFast(InspectorFlags.ReadOnly);
            bool includeWriteOnly = flags.HasFlagFast(InspectorFlags.WriteOnly);
            
            for (int i = 0; i < members.Length; i++)
            {
                elements[i] = members[i] switch
                {
                    PropertyInfo property when flags.HasFlagFast(InspectorFlags.Property) && (property.SetMethod != null || includeReadOnly) && (property.GetMethod != null || includeWriteOnly) => new PropertyElement(this, property),
                    FieldInfo field when flags.HasFlagFast(InspectorFlags.Field) && ((!field.IsInitOnly && !field.IsLiteral) || includeReadOnly) => new FieldElement(this, field),
                    MethodInfo method when flags.HasFlagFast(InspectorFlags.Method) => new MethodElement(this, method),
                    _ => null
                };
            }

            return elements.WhereNotNull().ToImmutableArray();
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
    }
}