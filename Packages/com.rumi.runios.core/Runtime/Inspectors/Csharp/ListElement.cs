#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.Inspectors.Csharp
{
    public class ListElement : IInspectorListElement
    {
        public ListElement(InspectableList inspectable, int index)
        {
            name = string.Empty;
            displayName = string.Empty;
            
            this.inspectable = inspectable;
            this.index = index;
            
            _inspectableObjectElement = new InspectableObject(variableType) { parentElement = this };

            if (typeof(IList).IsAssignableFrom(variableType))
                _inspectableListElement = new InspectableList(variableType, variableType.IsArray ? nullabilityInfo?.elementType : nullabilityInfo?.genericTypeArguments.FirstOrDefault()) { parentElement = this };
        }

        public string name { get; }
        public string displayName { get; set; }

        public InspectableList inspectable { get; }
        IInspectable IInspectorElement.inspectable => inspectable;

        public Type variableType => inspectable.inspectionElementType ?? typeof(object);
        
        public RuniNullabilityInfo? nullabilityInfo => inspectable.elementNullabilityInfo;
        
        public int index { get; set; }

        public bool isPublic => true;
        public bool isStatic => false;

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
                    return inspectable.instances.Any(x => !Equals(x[index], value));
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
        

        public InspectableList? inspectableListElement
        {
            get
            {
                if (_inspectableListElement == null)
                    return null;

                _inspectableListElement.instances = GetValues().OfType<IList>();
                return _inspectableListElement;
            }
        }
        readonly InspectableList? _inspectableListElement;
        IInspectableList? IInspectorVariableElement.inspectableListElement => inspectableListElement;

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
        
        public void SetValues(IEnumerable<object?> values)
        {
            try
            {
                using IEnumerator<object?> valueEnumerator = values.GetEnumerator();
                foreach (var instance in inspectable.instances)
                {
                    if (!valueEnumerator.MoveNext())
                        return;
                    
                    instance[index] = valueEnumerator.Current;
                }
            }
            catch (Exception e)
            {
                throw new InspectorElementException($"An exception occurred while writing a value to the {name} field.", name, e);
            }
        }

        public bool HasFlags(InspectorFlags flags)
        {
            if (flags == InspectorFlags.None)
                return false;
            
            if (!flags.HasFlagFast(InspectorFlags.Public | InspectorFlags.Instance | InspectorFlags.List))
                return false;
            
            if (!IsWritable(flags) && !flags.HasFlagFast(InspectorFlags.ReadOnly))
                return false;

            return true;
        }
        
        public bool IsReadable(InspectorFlags flags = InspectorFlags.Public) => true;
        public bool IsWritable(InspectorFlags flags = InspectorFlags.Public) => !inspectable.isReadOnly;
        
        public void UpdateChildInspectable()
        {
            if (!IsReadable(InspectorFlags.All))
                return;
            
            inspectableObjectElement.instances = GetValues().WhereNotNull();
            if (inspectableListElement != null)
                inspectableListElement.instances = GetValues().OfType<IList>();
        }
    }
}