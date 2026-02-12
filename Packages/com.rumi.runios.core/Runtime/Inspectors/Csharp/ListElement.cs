#nullable enable
using RuniOS.Collections.Handlers;
using RuniOS.Reflection;
using System.Collections;
using System.Reflection;

namespace RuniOS.Inspectors.Csharp
{
    public class ListElement : IInspectorListElement
    {
        public ListElement(InspectableList inspectable, int index)
        {
            displayName = string.Empty;

            this.inspectable = inspectable;
            this.index = index;

            variableType = inspectable.inspectionElementType ?? typeof(object);


            inspectableObjectElement = new InspectableObject(this, variableType);

            if (typeof(IEnumerable).IsAssignableFrom(variableType))
            {
                inspectableListElement = new InspectableList(this, variableType, variableType.IsArray ? nullabilityInfo?.elementType : nullabilityInfo?.genericTypeArguments.FirstOrDefault());
                if (CollectionHandlerBase.HandlerCheck<DictionaryHandlerBase>(variableType))
                    inspectableDictionaryElement = new InspectableDictionary(this, variableType, nullabilityInfo?.genericTypeArguments.Length >= 2 ? nullabilityInfo.genericTypeArguments[1] : null);
            }
        }

        public string name => $"[{index}]";
        public string displayName { get; set; }

        public string path
        {
            get
            {
                if (inspectable.parentElement != null)
                    return $"{inspectable.parentElement.path}{name}";
                else
                    return name;
            }
        }

        public InspectableList inspectable { get; }
        IInspectable IInspectorElement.inspectable => inspectable;

        public Type variableType { get; }
        public Type currentElementType => value?.GetType() ?? variableType;

        public NullabilityInfo? nullabilityInfo => inspectable.elementNullabilityInfo;

        public int index { get; set; }

        public bool isPublic => true;
        public bool isStatic => false;

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
                    object? Method() => inspectable[index];
                    return accessor.readFunc != null ? accessor.readFunc.Invoke(Method) : Method();
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
                    if (accessor.writeAction != null)
                    {
                        accessor.writeAction.Invoke(value);
                        inspectable.OnValueChangedInvoke();
                        
                        return;
                    }
                    
                    inspectable[index] = value;
                    inspectable.OnValueChangedInvoke();
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
                    if (inspectable.instancesIsEmpty)
                        return false;
                    
                    object? value = this.value;
                    var handlers = inspectable.listHandlers;
                    for (int i = 0; i < handlers.Count; i++)
                    {
                        ListHandlerBase? item = handlers[i];
                        if (variableType.IsPointer)
                        {
                            if (((Pointer)item[index]!).ToIntPtr() != ((Pointer)value!).ToIntPtr())
                                return true;
                        }
                        else if (!Equals(item[index], value))
                            return true;
                    }

                    return false;
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

        /// <summary>
        /// 이 필드가 딕셔너리인 경우, 딕셔너리를 나타내는 <see cref="InspectableDictionary"/>를 가져옵니다.
        /// </summary>
        public InspectableDictionary? inspectableDictionaryElement { get; }
        IInspectableDictionary? IInspectorVariableElement.inspectableDictionaryElement => inspectableDictionaryElement;

        readonly List<object?> valuesBuffer = new List<object?>();

        public IEnumerable<object?> GetValues(bool noCopy = false)
        {
            try
            {
                return accessor.getValuesFunc != null ? accessor.getValuesFunc.Invoke(Method, noCopy) : Method(noCopy);

                IEnumerable<object?> Method(bool noCopy)
                {
                    valuesBuffer.Clear();

                    var handlers = inspectable.listHandlers;
                    for (int i = 0; i < handlers.Count; i++)
                    {
                        ListHandlerBase? handler = handlers[i];
                        valuesBuffer.Add(handler[index]);
                    }

                    if (noCopy)
                        return valuesBuffer;

                    return valuesBuffer.ToArray();
                }
            }
            catch (Exception e)
            {
                throw new InspectorElementException($"An exception occurred while reading value from {name} list.", name, e);
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
                
                foreach ((ListHandlerBase instance, object? value) in inspectable.listHandlers.Zip(values, (instance, value) => (instance, value)))
                    instance[index] = value;

                inspectable.OnValueChangedInvoke();
            }
            catch (Exception e)
            {
                throw new InspectorElementException($"An exception occurred while writing a value to the {name} list.", name, e);
            }
        }

        public bool HasFlags(InspectorFlags flags)
        {
            if (flags == InspectorFlags.None)
                return false;

            if (!flags.HasFlagFast(InspectorFlags.Public | InspectorFlags.Instance | InspectorFlags.List))
                return false;

            if (!IsWritable(flags, true) && !flags.HasFlagFast(InspectorFlags.ReadOnly))
                return false;

            return true;
        }

        public bool IsReadable(InspectorFlags flags = InspectorFlags.PublicAccess, bool noInstanceCheck = false)
        {
            if (accessor.isReadableFunc != null)
                return accessor.isReadableFunc.Invoke(flags, noInstanceCheck);
            
            return (noInstanceCheck || !inspectable.instancesIsEmpty) && flags.HasFlagFast(InspectorFlags.Public);
        }
        
        public bool IsWritable(InspectorFlags flags = InspectorFlags.PublicAccess, bool noInstanceCheck = false)
        {
            if (accessor.isWritableFunc != null)
                return accessor.isWritableFunc.Invoke(flags, noInstanceCheck);
            
            return (noInstanceCheck || !inspectable.instancesIsEmpty) && flags.HasFlagFast(InspectorFlags.Public) && !inspectable.isReadOnly;
        }

        readonly List<IEnumerable> collectionsBuffer = new List<IEnumerable>();
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

        /// <inheritdoc cref="IInspectorListElement.Clone"/>
        public ListElement Clone() => new ListElement(inspectable.Clone(), index) { accessor = accessor.Clone() };
        IInspectorListElement IInspectorListElement.Clone() => Clone();
    }
}