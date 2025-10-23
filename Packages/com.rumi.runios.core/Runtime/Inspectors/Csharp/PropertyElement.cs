#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RuniOS.Inspectors.Csharp
{
    public class PropertyElement : MemberElement, IInspectorVariableElement
    {
        public PropertyElement(Type targetType, PropertyInfo property) : this(new InspectableObject(targetType), property) { }
        public PropertyElement(object instance, PropertyInfo property) : this(new InspectableObject(instance), property) { } 
        public PropertyElement(InspectableObject inspectable, PropertyInfo property) : base(inspectable, property)
        {
            this.property = property;
            nullabilityInfo = new NullabilityInfoContext().Create(property);

            _inspectableObjectElement = new InspectableObject(variableType, this);

            if (typeof(IList).IsAssignableFrom(variableType))
                _inspectableList = new InspectableList(variableType, nullabilityInfo.GenericTypeArguments.FirstOrDefault());
        }

        public Type variableType => property.PropertyType;
        public NullabilityInfo nullabilityInfo { get; }
        
        public PropertyInfo property { get; }
        
        public override bool isPublic => property.GetMethod?.IsPublic ?? property.SetMethod?.IsPublic ?? false;
        
        public override bool isStatic => property.GetMethod?.IsStatic ?? property.SetMethod?.IsStatic ?? false;
        
        public bool isReadable => property.GetMethod != null;
        public bool isWritable => property.SetMethod != null;

        public object? value
        {
            get
            {
                try
                {
                    return property.GetValue(inspectable.instance);
                }
                catch (Exception e)
                {
                    throw new InspectorElementException($"An exception occurred while reading value from {name} property.", name, e);
                }
            }
            set
            {
                try
                {
                    if (isStatic)
                    {
                        property.SetValue(null, value);
                        return;
                    }

                    foreach (var item in inspectable.instances)
                        property.SetValue(item, value);
                }
                catch (Exception e)
                {
                    throw new InspectorElementException($"An exception occurred while writing a value to the {name} property.", name, e);
                }
            }
        }

        public bool isMixedValue
        {
            get
            {
                if (isStatic)
                    return false;
                if (!isReadable)
                    return true;
                
                try
                {
                    object? value = this.value;
                    return inspectable.instances.Any(x => !Equals(property.GetValue(x), value));
                }
                catch (Exception e)
                {
                    throw new InspectorElementException($"An exception occurred while reading value from {name} property.", name, e);
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


        public InspectableList? inspectableElementList
        {
            get
            {
                if (_inspectableList == null)
                    return null;

                _inspectableList.instances = GetValues().OfType<IList>();
                return _inspectableList;
            }
        }
        readonly InspectableList? _inspectableList;
        IInspectableList? IInspectorVariableElement.inspectableListElement => inspectableElementList;

        public IEnumerable<object?> GetValues()
        {
            try
            {
                return inspectable.instances.Select(x => property.GetValue(x));
            }
            catch (Exception e)
            {
                throw new InspectorElementException($"An exception occurred while reading value from {name} property.", name, e);
            }
        }
        
        public override bool HasFlags(InspectorFlags flags)
        {
            if (!base.HasFlags(flags))
                return false;

            if (!flags.HasFlagFast(InspectorFlags.Property))
                return false;
            
            if ((isReadable && !isWritable) && !flags.HasFlagFast(InspectorFlags.ReadOnly))
                return false;
            if ((!isReadable && isWritable) && !flags.HasFlagFast(InspectorFlags.WriteOnly))
                return false;

            return true;
        }
    }
}