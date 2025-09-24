#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RuniOS.Inspectors.Csharp
{
    public class ListElement : IInspectorListElement
    {
        public ListElement(InspectableList inspectable, int index)
        {
            name = $"Element {index}";
            
            this.inspectable = inspectable;
            this.index = index;
            
            _inspectableObjectElement = new InspectableObject(variableType) { parentElement = this };

            if (typeof(IList).IsAssignableFrom(variableType))
                _inspectableElementList = new InspectableList(variableType, nullabilityInfo?.GenericTypeArguments.FirstOrDefault());
        }

        public string name { get; }
        public string displayName => name;

        public InspectableList inspectable { get; }
        IInspectable IInspectorElement.inspectable => inspectable;

        public Type variableType => inspectable.inspectionType;

        public Type? elementType => inspectable.inspectionElementType;
        
        public NullabilityInfo? nullabilityInfo => inspectable.nullabilityInfo;
        
        public int index { get; }

        public bool isStatic => false;

        public bool isReadable => true;
        public bool isWritable => !inspectable.isReadOnly;

        public object? value
        {
            get
            {
                try
                {
                    return inspectable[index];
                }
                catch (Exception e)
                {
                    throw new InspectorElementException($"An exception occurred while reading value from {name} field.", name, e);
                }
            }
            set
            {
                try
                {
                    inspectable[index] = value;
                }
                catch (Exception e)
                {
                    throw new InspectorElementException($"An exception occurred while writing a value to the {name} field.", name, e);
                }
            }
        }

        public bool isMixedValue
        {
            get
            {
                try
                {
                    object? value = this.value;
                    return inspectable.instances.Any(x => inspectable[index] != value);
                }
                catch (Exception e)
                {
                    throw new InspectorElementException($"An exception occurred while reading value from {name} field.", name, e);
                }
            }
        }
        
        public InspectableObject inspectableObjectElement
        {
            get
            {
                _inspectableObjectElement.instances = GetValues().WhereNotNull();
                return _inspectableObjectElement;
            }
        }
        readonly InspectableObject _inspectableObjectElement;
        IInspectableObject IInspectorVariableElement.inspectableObjectElement => inspectableObjectElement;
        

        public InspectableList? inspectableElementElementList
        {
            get
            {
                if (_inspectableElementList == null)
                    return null;

                _inspectableElementList.instances = GetValues().OfType<IList>();
                return _inspectableElementList;
            }
        }
        readonly InspectableList? _inspectableElementList;
        IInspectableList? IInspectorVariableElement.inspectableListElement => inspectableElementElementList;

        public IEnumerable<object?> GetValues()
        {
            try
            {
                return inspectable.instances.Select(x => x[index]);
            }
            catch (Exception e)
            {
                throw new InspectorElementException($"An exception occurred while reading value from {name} property.", name, e);
            }
        }
    }
}