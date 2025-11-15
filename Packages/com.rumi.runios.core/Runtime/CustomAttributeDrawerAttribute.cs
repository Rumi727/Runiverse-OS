#nullable enable
namespace RuniOS
{
    /// <summary>
    /// This abstract class serves as the base for all attributes used to associate a class with a specific drawer implementation.
    /// <br/>
    /// The inheriting attribute should be placed on a class that derives from <c>AttributeDrawer&lt;TDerived, TAttribute&gt;</c>.
    /// <br/><br/>
    /// 이 추상 클래스는 특정 드로어 구현과 클래스를 연결하는 데 사용되는 모든 특성의 기본 역할을 합니다.
    /// <br/>
    /// 이 특성을 상속하는 클래스는 <c>AttributeDrawer&lt;TDerived, TAttribute&gt;</c>를 상속하는 클래스에 선언되어야 합니다.
    /// </summary>
    /// <remarks>
    /// <see cref="AttributeTargets.Class"/>에만 적용 가능하며, 파생 클래스에서는 상속되지 않고 (<see langword="false"/>), 여러 번 사용 가능합니다 (<see langword="true"/>).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public abstract class CustomAttributeDrawerAttribute : Attribute
    {
        /// <summary>
        /// Gets the target <see cref="Type"/> that this drawer is intended for.
        /// <br/>
        /// 이 드로어가 대상으로 하는 <see cref="Type"/>을 가져옵니다.
        /// </summary>
        public Type targetType { get; }
        
        /// <summary>
        /// Gets a value indicating whether this drawer should be considered for types that are assignable from the <see cref="targetType"/> (i.e., subtypes or derived classes).
        /// <br/>
        /// 이 드로어가 <see cref="targetType"/>으로부터 할당 가능한 타입(예: 서브타입 또는 파생 클래스)에 대해서도 고려되어야 하는지를 나타내는 값을 가져옵니다.
        /// <br/><br/>
        /// <see langword="true"/>인 경우, <see cref="targetType"/>의 파생 타입에도 드로어가 적용될 수 있습니다. <see langword="false"/>인 경우, 정확히 일치하는 타입에만 적용됩니다.
        /// </summary>
        public abstract bool isSubtypeCompatible { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomAttributeDrawerAttribute"/> class.
        /// <br/>
        /// <see cref="CustomAttributeDrawerAttribute"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="targetType">The <see cref="Type"/> this drawer is designed to handle.</param>
        protected CustomAttributeDrawerAttribute(Type targetType) => this.targetType = targetType;
    }
}