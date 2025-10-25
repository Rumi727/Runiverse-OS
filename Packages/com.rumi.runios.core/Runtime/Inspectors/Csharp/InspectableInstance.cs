#nullable enable
/*using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace RuniOS.Inspectors.Csharp
{
    public sealed class InspectableInstance : IInspectableObject
    {
        InspectableInstance(IInspectorVariableElement element) => this.elements = elements;
        
        
        
        public IInspectorVariableElement? parentElement => null;

        public static Type inspectionType => typeof(Dictionary<string, object?>);
        public string inspectionDisplayName => inspectionType.GetTypeDisplayName();

        readonly Dictionary<string, Element> elements;
        
        public bool instancesIsEmpty => false;

        public object? GetValue(string name) => GetValue(name, out _);
        
        public object? GetValue(string name, out Element element) => (element = elements[name]).value;

        public void SetValue(string name, object? value) => elements[name].value = value;

        public bool ContainsKey(string name) => elements.ContainsKey(name);

        public bool TryGetInspectionType([NotNullWhen(true)] out Type? type)
        {
            type = inspectionType;
            return true;
        }

        public IEnumerable<IInspectorElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            if (flags == InspectorFlags.None)
                return ImmutableArray<IInspectorElement>.Empty;

            return elements.Select(x => new WritableVariableElement(this, x.Key, x.Value));
        }

        public class Element
        {
            Element(Type? type, object? value, RuniNullabilityInfo? nullabilityInfo, bool isReadOnly, Action<object?>? valueChangedEvent)
            {
                _type = type;
                this.value = value;
                
                this.nullabilityInfo = nullabilityInfo;
                this.isReadOnly = isReadOnly;

                isValueChanged = valueChangedEvent;
            }
            
            public Type type => _type ?? typeof(void);
            readonly Type? _type;

            public object? value
            {
                get => _value;
                set
                {
                    _value = value;
                    isValueChanged?.SafeInvoke(value);
                }
            }
            object? _value;

            public RuniNullabilityInfo? nullabilityInfo { get; }
            public bool isReadOnly { get; }

            public event Action<object?>? isValueChanged;
            
            public sealed class Builder
            {
                readonly Type type;
                RuniNullabilityInfo? nullabilityInfo = null;
                object? value = null;
                bool isReadOnly = false;

                Action<object?>? isValueChanged;

                public Builder(Type type) => this.type = type;

                public Builder SetNullabilityInfo(RuniNullabilityInfo nullabilityInfo)
                {
                    this.nullabilityInfo = nullabilityInfo;
                    return this;
                }
                
                public Builder SetValue(object value)
                {
                    this.value = value;
                    return this;
                }

                public Builder SetReadOnly()
                {
                    isReadOnly = true;
                    return this;
                }

                public Builder AddValueChangeEvent(Action<object?> action)
                {
                    isValueChanged += action;
                    return this;
                }

                public Element Build() => new Element(type, value, nullabilityInfo, isReadOnly, isValueChanged);
            }
        }

        public sealed class Builder
        {
            Dictionary<string, Element> elements { get; } = new();
            
            public Builder AddValue(string name, Element element)
            {
                elements.Add(name, element);
                return this;
            }

            public InspectableInstance Build() => new InspectableInstance(elements.ToDictionary(x => x.Key, x => x.Value));
        }
    }
}*/