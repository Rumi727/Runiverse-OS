#nullable enable

namespace RuniOS.Reflection
{
    /// <summary>
    /// Provides the base contract for attributes that associate an implementation with a target type.<br/>
    /// 구현 타입을 대상 타입과 연결하는 특성의 기본 계약을 제공합니다.
    /// </summary>
    /// <param name="targetType">
    /// The type targeted by the implementation.<br/>
    /// 구현 타입이 대상으로 하는 타입입니다.
    /// </param>
    // 생성기는 클래스/레코드의 다중 등록 특성을 전제로 하므로 `Class`와 `AllowMultiple` 계약을 유지합니다.
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    // RuniOS.CodeAnalysis는 이 메타데이터 이름으로 특성 기본 타입을 조회합니다.
    // 생성기가 원본 특성의 생성 인자를 재생성하므로 기본 생성자 `(Type targetType)` 계약을 유지합니다.
    public abstract class TypeRegistrationAttribute(Type targetType) : Attribute
    {
        /// <summary>
        /// Gets the type targeted by the implementation.<br/>
        /// 구현 타입이 대상으로 하는 타입을 가져옵니다.
        /// </summary>
        // 생성기가 positional 인자를 재생성하고 런타임 resolve가 이 이름과 `Type` 값을 읽습니다.
        public Type targetType { get; } = targetType;

        /// <summary>
        /// Gets or sets the registration priority used by registry ordering.<br/>
        /// 레지스트리 정렬에 사용하는 등록 우선순위를 가져오거나 설정합니다.
        /// </summary>
        // 생성된 특성 식의 named argument이자 런타임 정렬 키이므로 이름과 `int` 타입을 유지합니다.
        public int priority { get; init; }

        /// <summary>
        /// Gets or sets whether the registration also applies to assignable child types.<br/>
        /// 등록을 할당 가능한 하위 타입에도 적용할지 여부를 가져오거나 설정합니다.
        /// </summary>
        // 생성된 특성 식의 named argument이자 런타임 하위 타입 매칭 플래그이므로 이름과 `bool` 타입을 유지합니다.
        public bool useForChildren { get; init; }
    }
}
