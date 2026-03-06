#nullable enable
namespace RuniOS
{
    /// <summary>
    /// This abstract class serves as the base for all attributes used to associate a class with a specific type handler implementation.
    /// <br/>
    /// The inheriting attribute should be placed on a class that serves as a handler logic, often managed by <c>AttributeTypeResolver</c>.
    /// <br/><br/>
    /// 이 추상 클래스는 특정 타입 핸들러 구현과 클래스를 연결하는 데 사용되는 모든 특성의 기반 역할을 합니다.
    /// <br/>
    /// 이 특성을 상속하는 클래스는 주로 <c>AttributeTypeResolver</c>에 의해 관리되는 핸들러 로직을 담당하는 클래스에 선언되어야 합니다.
    /// </summary>
    /// <remarks>
    /// <see cref="AttributeTargets.Class"/>에만 적용 가능하며, 파생 클래스에서는 상속되지 않고 (<see langword="false"/>), 여러 번 사용 가능합니다 (<see langword="true"/>).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public abstract class TypeHandlerAttribute : Attribute
    {
        /// <summary>
        /// Gets the target <see cref="Type"/> that this handler is intended for.
        /// <br/>
        /// 이 핸들러가 대상으로 하는 <see cref="Type"/>을 가져옵니다.
        /// </summary>
        public Type targetType { get; }

        /// <summary>
        /// Gets the explicit sorting order or priority of this handler.<br/>
        /// This allows manual control over the handler selection order when multiple handlers target the same type.<br/>
        /// Higher values indicate higher priority.
        /// <br/><br/>
        /// 이 핸들러의 명시적인 정렬 순서 또는 우선순위를 가져옵니다.<br/>
        /// 이 값은 동일한 타입을 대상으로 하는 여러 핸들러가 존재할 때, 핸들러의 선택 순서를 수동으로 제어하는 데 사용됩니다.<br/>
        /// 값이 높을수록 우선순위가 높습니다.
        /// </summary>
        public int priority { get; set; } = 0;

        /// <summary>
        /// Gets a value indicating whether this handler should be considered for types that are assignable from the <see cref="targetType"/> (i.e., subtypes or derived classes).
        /// <br/>
        /// 이 핸들러가 <see cref="targetType"/>으로부터 할당 가능한 타입(예: 서브타입 또는 파생 클래스)에 대해서도 고려되어야 하는지를 나타내는 값을 가져옵니다.
        /// <br/><br/>
        /// <see langword="true"/>인 경우, <see cref="targetType"/>의 파생 타입에도 핸들러가 적용될 수 있습니다. <see langword="false"/>인 경우, 정확히 일치하는 타입에만 적용됩니다.
        /// </summary>
        public abstract bool isSubtypeCompatible { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeHandlerAttribute"/> class.
        /// <br/>
        /// <see cref="TypeHandlerAttribute"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="targetType">The <see cref="Type"/> this handler is designed to handle.</param>
        protected TypeHandlerAttribute(Type targetType) => this.targetType = targetType;
    }
}