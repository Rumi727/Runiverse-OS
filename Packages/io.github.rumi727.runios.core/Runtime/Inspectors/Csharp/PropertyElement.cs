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

            inspectableObjectElement = new InspectableObject(this, variableType);

            if (typeof(IEnumerable).IsAssignableFrom(variableType))
            {
                inspectableListElement = new InspectableList(this, variableType, variableType.IsArray ? nullabilityInfo.elementType : nullabilityInfo.genericTypeArguments.FirstOrDefault());
                if (CollectionHandlerBase.HandlerCheck<DictionaryHandlerBase>(variableType))
                    inspectableDictionaryElement = new InspectableDictionary(this, variableType, nullabilityInfo.genericTypeArguments.Length >= 2 ? nullabilityInfo.genericTypeArguments[1] : null);
            }
        }

        public Type variableType => property.PropertyType;
        public NullabilityInfo nullabilityInfo { get; }

        public PropertyInfo property { get; }

        public override bool isPublic => property.GetMethod?.IsPublic ?? property.SetMethod?.IsPublic ?? false;

        public override bool isStatic => property.GetMethod?.IsStatic ?? property.SetMethod?.IsStatic ?? false;
        
        /// <summary>
        /// 엑세스 메소드를 커스텀할 수 있습니다.
        /// </summary>
        public AccessInterceptor accessor { get; private init; } = new AccessInterceptor();

        public object? value
        {
            get
            {
                try
                {
                    object? Method() => property.GetValue(inspectable.instance);
                    return accessor.readFunc != null ? accessor.readFunc.Invoke(Method) : Method();
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
                    if (accessor.writeAction != null)
                    {
                        accessor.writeAction.Invoke(value);
                        inspectable.OnValueChangedInvoke();
                        
                        return;
                    }
                    
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
                        var instances = inspectable.instances;
                        for (int i = 0; i < instances.Count; i++)
                            property.SetValue(instances[i], value);
                    }

                    inspectable.OnValueChangedInvoke();
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
                    if (inspectable.instancesIsEmpty)
                        return false;
                    
                    object? value = this.value;
                    var instances = inspectable.instances;
                    for (int i = 0; i < instances.Count; i++)
                    {
                        object? item = instances[i];
                        if (variableType.IsPointer)
                        {
                            if (((Pointer)property.GetValue(item)).ToIntPtr() != ((Pointer)value!).ToIntPtr())
                                return true;
                        }
                        else if (!Equals(property.GetValue(item), value))
                            return true;
                    }

                    return false;
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

        readonly List<object?> valuesBuffer = new List<object?>();
        readonly List<IEnumerable> collectionsBuffer = new List<IEnumerable>();

        public IEnumerable<object?> GetValues(bool noCopy = false)
        {
            try
            {
                return accessor.getValuesFunc != null ? accessor.getValuesFunc.Invoke(Method, noCopy) : Method(noCopy);

                IEnumerable<object?> Method(bool noCopy = false)
                {
                    valuesBuffer.Clear();

                    var instances = inspectable.instances;
                    for (int i = 0; i < instances.Count; i++)
                        valuesBuffer.Add(property.GetValue(instances[i]));

                    if (noCopy)
                        return valuesBuffer;

                    return valuesBuffer.ToArray();
                }
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
                if (accessor.setValuesAction != null)
                {
                    accessor.setValuesAction.Invoke(values);
                    inspectable.OnValueChangedInvoke();
                    
                    return;
                }
                
                foreach ((object instance, object? value) in inspectable.instances.WhereNotNull().Zip(values, (instance, value) => (instance, value)))
                    property.SetValue(instance, value);

                // 값 형식은 참조가 아닌 복사이기에 값 바꿔줘야함
                if (inspectable.parentElement != null && inspectable.parentElement.variableType.IsValueType)
                    inspectable.parentElement.SetValues(inspectable.instances);

                inspectable.OnValueChangedInvoke();
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

            if (!IsWritable(flags, true) && !flags.HasFlagFast(InspectorFlags.ReadOnly))
                return false;
            if (!IsReadable(flags, true) && !flags.HasFlagFast(InspectorFlags.WriteOnly))
                return false;

            if ((property.IsSpecialName || name.Contains('.')) && !flags.HasFlagFast(InspectorFlags.Hidden))
                return false;

            return true;
        }

        public bool IsReadable(InspectorFlags flags = InspectorFlags.PublicAccess, bool noInstanceCheck = false)
        {
            if (accessor.isReadableFunc != null)
                return accessor.isReadableFunc.Invoke(flags, noInstanceCheck);
            
            return (noInstanceCheck || !inspectable.instancesIsEmpty) && property.GetGetMethod(flags.HasFlagFast(InspectorFlags.NonPublic)) != null;
        }

        public bool IsWritable(InspectorFlags flags = InspectorFlags.PublicAccess, bool noInstanceCheck = false)
        {
            if (accessor.isWritableFunc != null)
                return accessor.isWritableFunc.Invoke(flags, noInstanceCheck);
            
            if (!noInstanceCheck && inspectable.instancesIsEmpty)
                return false;

            if ((inspectable.parentElement?.variableType.IsValueType ?? false) && !inspectable.parentElement.IsWritable(flags))
                return false;

            return property.GetSetMethod(flags.HasFlagFast(InspectorFlags.NonPublic)) != null;
        }

        public void UpdateChildInspectable()
        {
            if (!IsReadable(InspectorFlags.All))
                return;

            var rawValues = (IList<object?>)GetValues(true);
            inspectableObjectElement.SetInstances(rawValues);

            if (inspectableListElement != null || inspectableDictionaryElement != null)
            {
                collectionsBuffer.Clear();
                for (int i = 0; i < rawValues.Count; i++)
                {
                    object? value = rawValues[i];
                    if (value is IEnumerable enumerable)
                        collectionsBuffer.Add(enumerable);
                }

                inspectableListElement?.SetInstances(collectionsBuffer);
                inspectableDictionaryElement?.SetInstances(collectionsBuffer);
            }
        }

        /// <inheritdoc cref="IInspectorVariableElement.Clone"/>
        public override MemberElement Clone() => new PropertyElement(inspectable.Clone(), property) { accessor = accessor.Clone() };
        IInspectorVariableElement IInspectorVariableElement.Clone() => new PropertyElement(inspectable.Clone(), property) { accessor = accessor.Clone() };
    }
}