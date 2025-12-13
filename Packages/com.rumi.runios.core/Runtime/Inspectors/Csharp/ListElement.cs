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

            inspectableObjectElement = new InspectableObject(variableType) { parentElement = this };

            if (typeof(IEnumerable).IsAssignableFrom(variableType))
            {
                inspectableListElement = new InspectableList(variableType, variableType.IsArray ? nullabilityInfo?.elementType : nullabilityInfo?.genericTypeArguments.FirstOrDefault()) { parentElement = this };
                if (CollectionHandlerBase.HandlerCheck<DictionaryHandlerBase>(variableType))
                    inspectableDictionaryElement = new InspectableDictionary(variableType, nullabilityInfo?.genericTypeArguments.Length >= 2 ? nullabilityInfo.genericTypeArguments[1] : null) { parentElement = this };
            }
        }

        public string name => $"[{index}]";
        public string displayName { get; set; }

        public InspectableList inspectable { get; }
        IInspectable IInspectorElement.inspectable => inspectable;

        public Type variableType { get; }
        public Type currentElementType => value?.GetType() ?? variableType;

        public NullabilityInfo? nullabilityInfo => inspectable.elementNullabilityInfo;

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
                    object? value = this.value;
                    foreach (var item in inspectable.listHandlers)
                    {
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
        readonly List<IEnumerable> collectionsBuffer = new List<IEnumerable>();

        public IEnumerable<object?> GetValues()
        {
            valuesBuffer.Clear();
            try
            {
                foreach (var handler in inspectable.listHandlers)
                    valuesBuffer.Add(handler[index]);
            }
            catch (Exception e)
            {
                throw new InspectorElementException($"An exception occurred while reading value from {name} list.", name, e);
            }
            return valuesBuffer;
        }

        public void SetValues(IEnumerable<object?> values)
        {
            try
            {
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

            if (!IsWritable(flags) && !flags.HasFlagFast(InspectorFlags.ReadOnly))
                return false;

            return true;
        }

        public bool IsReadable(InspectorFlags flags = InspectorFlags.Public) => !inspectable.instancesIsEmpty && flags.HasFlagFast(InspectorFlags.Public);
        public bool IsWritable(InspectorFlags flags = InspectorFlags.Public) => !inspectable.instancesIsEmpty && flags.HasFlagFast(InspectorFlags.Public) && !inspectable.isReadOnly;

        public void UpdateChildInspectable()
        {
            if (!IsReadable(InspectorFlags.All))
                return;

            var rawValues = (List<object?>)GetValues();
            inspectableObjectElement.instances = rawValues;

            if (inspectableListElement != null || inspectableDictionaryElement != null)
            {
                collectionsBuffer.Clear();
                for (int i = 0; i < rawValues.Count; i++)
                {
                    object? value = rawValues[i];
                    if (value is IEnumerable enumerable)
                        collectionsBuffer.Add(enumerable);
                }

                if (inspectableListElement != null) inspectableListElement.instances = collectionsBuffer;
                if (inspectableDictionaryElement != null) inspectableDictionaryElement.instances = collectionsBuffer;
            }
        }
    }
}