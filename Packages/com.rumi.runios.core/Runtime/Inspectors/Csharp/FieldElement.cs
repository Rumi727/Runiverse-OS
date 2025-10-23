#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            nullabilityInfo = new NullabilityInfoContext().Create(field);

            _inspectableObjectElement = new InspectableObject(variableType, this);

            if (typeof(IList).IsAssignableFrom(variableType))
                _inspectableList = new InspectableList(variableType, nullabilityInfo.GenericTypeArguments.FirstOrDefault());
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
        public override bool isStatic => field.IsStatic;

        /// <summary>
        /// 필드를 읽을 수 있는지 여부를 나타내는 값을 가져옵니다. (항상 true)
        /// </summary>
        public bool isReadable => true;

        /// <summary>
        /// 필드에 쓸 수 있는지 여부를 나타내는 값을 가져옵니다. (예: init-only, literal 필드는 쓰기 불가)
        /// </summary>
        public bool isWritable => !field.IsInitOnly && !field.IsLiteral;

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
                    return field.GetValue(inspectable.instance);
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
                    if (isStatic)
                    {
                        field.SetValue(null, value);
                        return;
                    }

                    foreach (var item in inspectable.instances)
                        field.SetValue(item, value);
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
                if (!isReadable)
                    return true;

                try
                {
                    object? value = this.value;
                    return inspectable.instances.Any(x => field.GetValue(x) != value);
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

        /// <summary>
        /// 이 필드가 리스트인 경우, 리스트를 나타내는 <see cref="InspectableList"/>를 가져옵니다.
        /// </summary>
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

        /// <summary>
        /// 검사 중인 모든 객체에서 이 필드의 값 목록을 가져옵니다.
        /// </summary>
        /// <returns>각 객체의 필드 값 컬렉션입니다.</returns>
        /// <exception cref="InspectorElementException">프로퍼티 값을 읽는 동안 예외가 발생할 때 발생합니다.</exception>
        public IEnumerable<object?> GetValues()
        {
            try
            {
                return inspectable.instances.Select(x => field.GetValue(x));
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

            if (!flags.HasFlagFast(InspectorFlags.Field))
                return false;
            
            if ((isReadable && !isWritable) && !flags.HasFlagFast(InspectorFlags.ReadOnly))
                return false;
            if ((!isReadable && isWritable) && !flags.HasFlagFast(InspectorFlags.WriteOnly))
                return false;

            return true;
        }
    }
}