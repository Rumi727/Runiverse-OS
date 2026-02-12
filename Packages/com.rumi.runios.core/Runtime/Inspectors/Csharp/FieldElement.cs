#nullable enable
using RuniOS.Collections.Handlers;
using RuniOS.Linq;
using RuniOS.Reflection;
using System.Collections;
using System.Reflection;

namespace RuniOS.Inspectors.Csharp
{
    /// <summary>
    /// C# 필드를 나타내는 인스펙터 요소입니다.
    /// </summary>
    public class FieldElement : MemberElement, IInspectorVariableElement
    {
        /// <summary>
        /// 정적 필드를 대상으로 하는 <see cref="FieldElement"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="targetType">검사할 정적 필드가 있는 타입입니다.</param>
        /// <param name="field">검사할 필드 정보입니다.</param>
        public FieldElement(Type targetType, FieldInfo field) : this(new InspectableObject(targetType), field) { }

        /// <summary>
        /// 인스턴스 필드를 대상으로 하는 <see cref="FieldElement"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="instance">검사할 인스턴스입니다.</param>
        /// <param name="field">검사할 필드 정보입니다.</param>
        public FieldElement(object instance, FieldInfo field) : this(new InspectableObject(instance), field) { }

        /// <summary>
        /// <see cref="FieldElement"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="inspectable">이 필드가 속한 검사 가능한 객체입니다.</param>
        /// <param name="field">이 요소가 나타내는 필드 정보입니다.</param>
        public FieldElement(InspectableObject inspectable, FieldInfo field) : base(inspectable, field)
        {
            this.field = field;
            nullabilityInfo = NullabilityInfoContext.Create(field);

            inspectableObjectElement = new InspectableObject(this, variableType);

            if (typeof(IEnumerable).IsAssignableFrom(variableType))
            {
                inspectableListElement = new InspectableList(this, variableType, variableType.IsArray ? nullabilityInfo.elementType : nullabilityInfo.genericTypeArguments.FirstOrDefault());
                if (CollectionHandlerBase.HandlerCheck<DictionaryHandlerBase>(variableType))
                    inspectableDictionaryElement = new InspectableDictionary(this, variableType, nullabilityInfo.genericTypeArguments.Length >= 2 ? nullabilityInfo.genericTypeArguments[1] : null);
            }
        }

        /// <summary>
        /// 필드의 타입을 가져옵니다.
        /// </summary>
        public Type variableType => field.FieldType;

        /// <summary>
        /// 필드의 null 허용 여부 정보를 가져옵니다.
        /// </summary>
        public NullabilityInfo nullabilityInfo { get; }

        /// <summary>
        /// 이 요소가 나타내는 <see cref="FieldInfo"/>를 가져옵니다.
        /// </summary>
        public FieldInfo field { get; }

        /// <summary>
        /// 필드가 공개되어있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public override bool isPublic => field.IsPublic;

        /// <summary>
        /// 필드가 정적인지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public override bool isStatic => field.IsStatic || field.IsLiteral;

        /// <summary>
        /// 엑세스 메소드를 커스텀할 수 있습니다.
        /// </summary>
        public AccessInterceptor accessor { get; private init; } = new AccessInterceptor();

        /// <summary>
        /// 필드의 값을 가져오거나 설정합니다.
        /// </summary>
        /// <exception cref="InspectorElementException">필드 값을 가져오거나 설정하는 동안 예외가 발생할 때 발생합니다.</exception>
        public object? value
        {
            get
            {
                try
                {
                    object? Method() => field.GetValue(inspectable.instance);
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
                    
                    if (isStatic)
                    {
                        field.SetValue(null, value);
                        return;
                    }

                    if (inspectable.parentElement != null && inspectable.parentElement.variableType.IsValueType)
                    {
                        // 값 형식은 참조가 아닌 복사이기에 값 바꿔줘야함
                        inspectable.parentElement.SetValues(inspectable.instances.Select(x =>
                        {
                            field.SetValue(x, value);
                            return x;
                        }));
                    }
                    else
                    {
                        var instances = inspectable.instances;
                        for (int i = 0; i < instances.Count; i++)
                            field.SetValue(instances[i], value);
                    }

                    inspectable.OnValueChangedInvoke();
                }
                catch (Exception e)
                {
                    throw new InspectorElementException($"An exception occurred while writing a value to the {name} field.", name, e);
                }
            }
        }

        /// <summary>
        /// 여러 객체를 검사할 때 필드 값이 혼합되어 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        /// <exception cref="InspectorElementException">필드 값을 읽는 동안 예외가 발생할 때 발생합니다.</exception>
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
                            if (((Pointer)field.GetValue(item)).ToIntPtr() != ((Pointer)value!).ToIntPtr())
                                return true;
                        }
                        else if (!Equals(field.GetValue(item), value))
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

        /// <summary>
        /// 이 필드의 값을 나타내는 <see cref="InspectableObject"/>를 가져옵니다. 필드의 값이 객체일 경우, 해당 객체를 검사할 수 있습니다.
        /// </summary>
        public InspectableObject inspectableObjectElement { get; }
        IInspectableObject IInspectorVariableElement.inspectableObjectElement => inspectableObjectElement;

        /// <summary>
        /// 이 필드가 리스트인 경우, 리스트를 나타내는 <see cref="InspectableList"/>를 가져옵니다.
        /// </summary>
        public InspectableList? inspectableListElement { get; }
        IInspectableList? IInspectorVariableElement.inspectableListElement => inspectableListElement;

        /// <summary>
        /// 이 필드가 딕셔너리인 경우, 딕셔너리를 나타내는 <see cref="InspectableDictionary"/>를 가져옵니다.
        /// </summary>
        public InspectableDictionary? inspectableDictionaryElement { get; }
        IInspectableDictionary? IInspectorVariableElement.inspectableDictionaryElement => inspectableDictionaryElement;

        readonly List<object?> valuesBuffer = new List<object?>();
        readonly List<IEnumerable> collectionsBuffer = new List<IEnumerable>();

        /// <summary>
        /// 검사 중인 모든 객체에서 이 필드의 값 목록을 가져옵니다.
        /// </summary>
        /// <param name="noCopy"></param>
        /// <returns>각 객체의 필드 값 컬렉션입니다.</returns>
        /// <exception cref="InspectorElementException">프로퍼티 값을 읽는 동안 예외가 발생할 때 발생합니다.</exception>
        public IEnumerable<object?> GetValues(bool noCopy = false)
        {
            try
            {
                return accessor.getValuesFunc != null ? accessor.getValuesFunc.Invoke(Method, noCopy) : Method(noCopy);
                
                IEnumerable<object?> Method(bool noCopy)
                {
                    valuesBuffer.Clear();

                    var instances = inspectable.instances;
                    for (int i = 0; i < instances.Count; i++)
                        valuesBuffer.Add(field.GetValue(instances[i]));

                    if (noCopy)
                        return valuesBuffer;

                    return valuesBuffer.ToArray();
                }
            }
            catch (Exception e)
            {
                throw new InspectorElementException($"An exception occurred while reading value from {name} field.", name, e);
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

                if (inspectable.parentElement != null && inspectable.parentElement.variableType.IsValueType)
                {
                    // 값 형식은 참조가 아닌 복사이기에 값 바꿔줘야함
                    inspectable.parentElement.SetValues
                    (
                        inspectable.instances.Zip(values, (instance, value) => (instance, value))
                            .Select(x =>
                            {
                                field.SetValue(x.instance, x.value);
                                return x.instance;
                            })
                    );
                }
                else
                {
                    foreach ((object instance, object? value) in inspectable.instances.WhereNotNull().Zip(values, (instance, value) => (instance, value)))
                        field.SetValue(instance, value);
                }

                inspectable.OnValueChangedInvoke();
            }
            catch (Exception e)
            {
                throw new InspectorElementException($"An exception occurred while writing a value to the {name} field.", name, e);
            }
        }

        public override bool HasFlags(InspectorFlags flags)
        {
            if (!base.HasFlags(flags))
                return false;

            if (!flags.HasFlagFast(InspectorFlags.Field))
                return false;

            if (!IsWritable(flags, true) && !flags.HasFlagFast(InspectorFlags.ReadOnly))
                return false;
            if (!IsReadable(flags, true) && !flags.HasFlagFast(InspectorFlags.WriteOnly))
                return false;

            return true;
        }

        /// <summary>
        /// 필드를 읽을 수 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool IsReadable(InspectorFlags flags = InspectorFlags.PublicAccess, bool noInstanceCheck = false)
        {
            if (accessor.isReadableFunc != null)
                return accessor.isReadableFunc.Invoke(flags, noInstanceCheck);
            
            return (noInstanceCheck || !inspectable.instancesIsEmpty) && flags.HasFlagFast(InspectorFlags.Public);
        }

        /// <summary>
        /// 필드에 쓸 수 있는지 여부를 나타내는 값을 가져옵니다. (예: init-only, literal 필드는 쓰기 불가)
        /// </summary>
        public bool IsWritable(InspectorFlags flags = InspectorFlags.PublicAccess, bool noInstanceCheck = false)
        {
            if (accessor.isWritableFunc != null)
                return accessor.isWritableFunc.Invoke(flags, noInstanceCheck);
            
            if ((!noInstanceCheck && inspectable.instancesIsEmpty) || !flags.HasFlagFast(InspectorFlags.Public))
                return false;

            if ((inspectable.parentElement?.variableType.IsValueType ?? false) && !inspectable.parentElement.IsWritable(flags))
                return false;

            return !field.IsInitOnly && !field.IsLiteral;
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
        public override MemberElement Clone() => new FieldElement(inspectable.Clone(), field) { accessor = accessor.Clone() };
        IInspectorVariableElement IInspectorVariableElement.Clone() => new FieldElement(inspectable.Clone(), field) { accessor = accessor.Clone() };
    }
}