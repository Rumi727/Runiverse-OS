#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace RuniOS.Inspectors.Csharp
{
    public class InspectableObject : IInspectableObject
    {
        public IInspectorVariableElement? parentElement { get; init; }
        
        public Type inspectionType { get; }
        public string inspectionDisplayName => inspectionType.GetTypeDisplayName();
        
        public object? instance
        {
            get => instances.FirstOrDefault();
            set
            {
                if (value != null && !inspectionType.IsInstanceOfType(value))
                    throw new InspectorException($"Invalid type. Expected '{inspectionType.FullName}', but received '{value.GetType().FullName}'.");
                
                if (value != null)
                    instances = Enumerable.Repeat(value, 1);
                else
                    instances = Enumerable.Empty<object>();
            }
        }

        public IEnumerable<object> instances
        {
            get => _instances;
            set
            {
                if (value.Any(x => !inspectionType.IsInstanceOfType(x)))
                {
                    string invalidTypes = string.Join(", ", value.Where(x => x != null && !inspectionType.IsInstanceOfType(x))
                        .Select(static x => $"'{x!.GetType().FullName}'")
                        .Distinct());
                                            
                    throw new InspectorException($"One or more elements in the collection have invalid types. Expected '{inspectionType.FullName}', but received the following: {invalidTypes}.");
                }

                _instances = value;
            }
        }
        IEnumerable<object> _instances;

        IEnumerable<object> IInspectableObject.instances => instances;

        public InspectableObject(object instance) : this(instance.GetType(), ImmutableArray.Create(instance)) { }
        public InspectableObject(Type inspectionType) : this(inspectionType, Enumerable.Empty<object>()) { }
        public InspectableObject(Type inspectionType, params object?[] instances) : this(inspectionType, instances.WhereNotNull()) { }
        
        public InspectableObject(Type inspectionType, IEnumerable<object> instances)
        {
            this.inspectionType = inspectionType;
            
            _instances = null!;
            this.instances = instances;
        }

        public ImmutableArray<IInspectorElement> GetElements(InspectorFlags flags = InspectorFlags.All)
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
    }
}