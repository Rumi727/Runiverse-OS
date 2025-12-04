#nullable enable
using RuniOS.Reflection;

namespace RuniOS.Inspectors
{
    /// <summary>
    /// 인스펙터에 표시되는 변수 요소를 정의하는 인터페이스입니다.
    /// </summary>
    public interface IInspectorVariableElement : IInspectorElement
    {
        /// <summary>
        /// 변수의 타입을 가져옵니다.
        /// <br/><br/>
        /// 동적 타입일 경우 (리스트 등) 요소 생성 시점의 <b><see cref="value"/></b>의 타입을 가져옵니다.<br/>
        /// 생성 시점에 <b><see cref="value"/></b>의 값을 읽을 수 없거나 <see langword="null"/> 값인 경우에는 <see cref="object"/> 타입을 반환합니다.
        /// </summary>
        Type variableType { get; }

        /// <summary>
        /// 변수의 null 허용 여부 정보를 가져옵니다.
        /// </summary>
        NullabilityInfo? nullabilityInfo { get; }

        /// <summary>
        /// 변수의 값을 가져오거나 설정합니다.
        /// </summary>
        object? value { get; set; }

        /// <summary>
        /// 여러 객체를 검사할 때 값이 혼합되어 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        bool isMixedValue { get; }

        /// <summary>
        /// 이 변수의 값을 나타내는 <see cref="IInspectableObject"/>를 가져옵니다.<br/>
        /// 변수의 값이 객체일 경우, 해당 객체를 검사할 수 있습니다.
        /// </summary>
        IInspectableObject inspectableObjectElement { get; }

        /// <summary>
        /// 이 변수가 리스트인 경우, 리스트를 나타내는 <see cref="IInspectableList"/>를 가져옵니다.<br/>
        /// 리스트가 아닌 경우 <see langword="null"/>을 반환합니다.
        /// </summary>
        IInspectableList? inspectableListElement { get; }
        
        /// <summary>
        /// 이 변수가 딕셔너리인 경우, 딕셔너리를 나타내는 <see cref="IInspectableDictionary"/>를 가져옵니다.<br/>
        /// 딕셔너리가 아닌 경우 <see langword="null"/>을 반환합니다.
        /// </summary>
        IInspectableDictionary? inspectableDictionaryElement { get; }

        /// <summary>
        /// 검사 중인 모든 객체에서 이 변수의 값 목록을 가져옵니다.
        /// </summary>
        /// <returns>각 객체의 변수 값 컬렉션입니다.</returns>
        IEnumerable<object?> GetValues();
        
        void SetValues(IEnumerable<object?> values);

        /// <summary>
        /// 변수를 읽을 수 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        bool IsReadable(InspectorFlags flags = InspectorFlags.PublicAccess);

        /// <summary>
        /// 변수에 쓸 수 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        bool IsWritable(InspectorFlags flags = InspectorFlags.PublicAccess);

        /// <summary>
        /// 자식 <see cref="IInspectableObject"/> 또는 <see cref="IInspectableList"/>에 포함된 인스턴스 목록을 업데이트합니다.
        /// </summary>
        void UpdateChildInspectable();
    }
}