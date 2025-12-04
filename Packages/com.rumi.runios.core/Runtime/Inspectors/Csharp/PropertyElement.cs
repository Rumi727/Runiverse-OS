#nullable enable
using RuniOS.Collections.Handlers;
using RuniOS.Linq;
using RuniOS.Reflection;
using System.Collections;
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
            nullabilityInfo = NullabilityInfoContext.Create(property);

            inspectableObjectElement = new InspectableObject(variableType) { parentElement = this };

            if (typeof(IEnumerable).IsAssignableFrom(variableType))
            {
                inspectableListElement = new InspectableList(variableType, variableType.IsArray ? nullabilityInfo.elementType : nullabilityInfo.genericTypeArguments.FirstOrDefault()) { parentElement = this };
                if (CollectionHandlerBase.HandlerCheck<DictionaryHandlerBase>(variableType))
                    inspectableDictionaryElement = new InspectableDictionary(variableType, nullabilityInfo.genericTypeArguments.Length >= 2 ? nullabilityInfo.genericTypeArguments[1] : null) { parentElement = this };
            }
        }

        public Type variableType => property.PropertyType;
        public NullabilityInfo nullabilityInfo { get; }
        
        public PropertyInfo property { get; }
        
        public override bool isPublic => property.GetMethod?.IsPublic ?? property.SetMethod?.IsPublic ?? false;
        
        public override bool isStatic => property.GetMethod?.IsStatic ?? property.SetMethod?.IsStatic ?? false;

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

                    if (inspectable.parentElement != null && inspectable.parentElement.variableType.IsValueType)
                    {
                        // 값 형식은 참조가 아닌 복사이기에 값 바꿔줘야함
                        inspectable.parentElement.SetValues(inspectable.instances.Select(x =>
                        {
                            property.SetValue(x, value);
                            return x;
                        }));
                    }
                    else
                    {
                        foreach (var item in inspectable.instances)
                            property.SetValue(item, value);
                    }
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
                
                try
                {
                    object? value = this.value;
                    if (variableType.IsPointer)
                        return inspectable.instances.Any(x => ((Pointer)property.GetValue(x)).ToIntPtr() != ((Pointer)value!).ToIntPtr());
                    
                    return inspectable.instances.Any(x => !Equals(property.GetValue(x), value));
                }
                catch (Exception e)
                {
                    throw new InspectorElementException($"An exception occurred while reading value from {name} property.", name, e);
                }
            }
        }
        
        public InspectableObject inspectableObjectElement { get; }
        IInspectableObject IInspectorVariableElement.inspectableObjectElement => inspectableObjectElement;

        public InspectableList? inspectableListElement { get; }
        IInspectableList? IInspectorVariableElement.inspectableListElement => inspectableListElement;
        
        /// <summary>
        /// 이 필드가 딕셔너리인 경우, 딕셔너리를 나타내는 <see cref="InspectableDictionary"/>를 가져옵니다.
        /// </summary>
        public InspectableDictionary? inspectableDictionaryElement { get; }
        IInspectableDictionary? IInspectorVariableElement.inspectableDictionaryElement => inspectableDictionaryElement;

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
        
        public void SetValues(IEnumerable<object?> values)
        {
            try
            {
                foreach ((object instance, object? value) in inspectable.instances.Zip(values, (instance, value) => (instance, value)))
                    property.SetValue(instance, value);
                
                // 값 형식은 참조가 아닌 복사이기에 값 바꿔줘야함
                if (inspectable.parentElement != null && inspectable.parentElement.variableType.IsValueType)
                    inspectable.parentElement.SetValues(inspectable.instances);
            }
            catch (Exception e)
            {
                throw new InspectorElementException($"An exception occurred while writing a value to the {name} property.", name, e);
            }
        }
        
        public override bool HasFlags(InspectorFlags flags)
        {
            if (!base.HasFlags(flags))
                return false;

            if (!flags.HasFlagFast(InspectorFlags.Property))
                return false;
            
            if (!IsWritable(flags) && !flags.HasFlagFast(InspectorFlags.ReadOnly))
                return false;
            if (!IsReadable(flags) && !flags.HasFlagFast(InspectorFlags.WriteOnly))
                return false;
            
            if ((property.IsSpecialName || name.Contains('.')) && !flags.HasFlagFast(InspectorFlags.Hidden))
                return false;

            return true;
        }
        
        public bool IsReadable(InspectorFlags flags = InspectorFlags.Public) => property.GetGetMethod(flags.HasFlagFast(InspectorFlags.NonPublic)) != null;
        
        public bool IsWritable(InspectorFlags flags = InspectorFlags.Public)
        {
            if ((inspectable.parentElement?.variableType.IsValueType ?? false) && !inspectable.parentElement.IsWritable(flags))
                return false;
            
            return property.GetSetMethod(flags.HasFlagFast(InspectorFlags.NonPublic)) != null;
        }

        public void UpdateChildInspectable()
        {
            if (!IsReadable(InspectorFlags.All))
                return;
            
            inspectableObjectElement.instances = GetValues().WhereNotNull();
            if (inspectableListElement != null)
                inspectableListElement.instances = GetValues().OfType<IEnumerable>();
            if (inspectableDictionaryElement != null)
                inspectableDictionaryElement.instances = GetValues().OfType<IEnumerable>();
        }
    }
}