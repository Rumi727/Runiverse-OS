#nullable enable
namespace RuniOS.Reflection
{
    /// <summary>
    /// Stores an implementation type and its registration attribute instance.<br/>
    /// 구현 타입과 해당 등록 특성 인스턴스를 저장합니다.
    /// </summary>
    /// <typeparam name="TAttribute">
    /// The registration attribute type stored in the entry.<br/>
    /// 항목에 저장하는 등록 특성 타입입니다.
    /// </typeparam>
    /// <param name="implementationType">
    /// The implementation type associated with the attribute.<br/>
    /// 특성과 연결된 구현 타입입니다.
    /// </param>
    /// <param name="attribute">
    /// The registration attribute associated with the implementation type.<br/>
    /// 구현 타입과 연결된 등록 특성입니다.
    /// </param>
    // 생성기가 이 public 타입으로 배열을 만들고 `implementationType`/`attribute` 순서의 `(Type, TAttribute)` 생성자를 호출합니다.
    public readonly record struct RegistrationEntry<TAttribute>(Type implementationType, TAttribute attribute) where TAttribute : TypeRegistrationAttribute;
}