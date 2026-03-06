#nullable enable
namespace RuniOS.Editor.Unity.Serialization
{
    /// <summary>
    /// Specifies that a class is a custom property converter for a specific type.
    /// This attribute is used to associate a <see cref="PropertyConverter"/> with the type it handles.
    /// <br/>
    /// 클래스가 특정 타입에 대한 커스텀 속성 컨버터임을 지정합니다.
    /// 이 속성은 <see cref="PropertyConverter"/>를 해당 컨버터가 처리하는 타입과 연결하는 데 사용됩니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CustomPropertyConverterAttribute : TypeHandlerAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomPropertyConverterAttribute"/> class.
        /// <br/>
        /// <see cref="CustomPropertyConverterAttribute"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="targetType">The <see cref="Type"/> that this converter is intended to handle.<br/>이 컨버터가 처리하도록 의도된 <see cref="Type"/>입니다.</param>
        /// <param name="isSubtypeCompatible">
        /// A value indicating whether this converter should also handle subtypes of the <paramref name="targetType"/>.
        /// <br/>If <see langword="true"/>, the converter will apply to <paramref name="targetType"/> and its derived types.
        /// <br/>If <see langword="false"/>, the converter will only apply to the exact <paramref name="targetType"/>.
        /// <br/><br/>
        /// 이 컨버터가 <paramref name="targetType"/>의 서브타입도 처리해야 하는지를 나타내는 값입니다.
        /// <br/><see langword="true"/>인 경우, 컨버터는 <paramref name="targetType"/> 및 해당 파생 타입에 적용됩니다.
        /// <br/><see langword="false"/>인 경우, 컨버터는 정확히 <paramref name="targetType"/>에만 적용됩니다.
        /// </param>
        public CustomPropertyConverterAttribute(Type targetType, bool isSubtypeCompatible = false) : base(targetType) => this.isSubtypeCompatible = isSubtypeCompatible;

        /// <summary>
        /// Gets a value indicating whether this property converter is compatible with subtypes of the <see cref="TypeHandlerAttribute.targetType"/>.
        /// <br/>이 컨버터가 <see cref="TypeHandlerAttribute.targetType"/>의 서브타입과 호환되는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public override bool isSubtypeCompatible { get; }
    }
}