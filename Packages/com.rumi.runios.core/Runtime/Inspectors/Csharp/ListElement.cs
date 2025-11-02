#nullable enable
using RuniOS.Linq;
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

            variableType = currentElementType;
            
            inspectableObjectElement = new InspectableObject(variableType) { parentElement = this };
            
            if (typeof(IEnumerable).IsAssignableFrom(variableType))
                inspectableListElement = new InspectableList(variableType, variableType.IsArray ? nullabilityInfo?.elementType : nullabilityInfo?.genericTypeArguments.FirstOrDefault()) { parentElement = this };
        }

        public string name { get; }
        public string displayName { get; set; }

        public InspectableList inspectable { get; }
        IInspectable IInspectorElement.inspectable => inspectable;

        public Type variableType { get; }
        public Type currentElementType => value?.GetType() ?? typeof(object);

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
                    return inspectable.collectionHandlers.Any(x => !Equals(x[index], value));
                }
                catch (Exception e)
                {
                    throw new InspectorElementException($"An exception occurred while reading value from {name} field.", name, e);
                }
            }
        }
        
        public InspectableObject inspectableObjectElement { get; }
        IInspectableObject IInspectorVariableElement.inspectableObjectElement => inspectableObjectElement;
        

        public InspectableList? inspectableListElement { get; }
        IInspectableList? IInspectorVariableElement.inspectableListElement => inspectableListElement;

        public IEnumerable<object?> GetValues()
        {
            try
            {
                return inspectable.collectionHandlers.Select(x => x[index]);
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
                foreach (var instance in inspectable.collectionHandlers)
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
        
        public bool IsReadable(InspectorFlags flags = InspectorFlags.Public) => flags.HasFlagFast(InspectorFlags.Public);
        public bool IsWritable(InspectorFlags flags = InspectorFlags.Public) => flags.HasFlagFast(InspectorFlags.Public) && !inspectable.isReadOnly;

        public void UpdateChildInspectable()
        {
            if (!IsReadable(InspectorFlags.All))
                return;
            
            inspectableObjectElement.instances = GetValues().WhereNotNull();
            if (inspectableListElement != null)
                inspectableListElement.instances = GetValues().Cast<IEnumerable>();
        }
    }
}