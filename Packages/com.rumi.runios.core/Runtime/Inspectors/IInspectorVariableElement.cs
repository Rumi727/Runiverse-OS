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
        /// 변수의 읽기/쓰기를 커스텀할 수 있습니다.
        /// <br/><br/>
        /// 구조체처럼 내부 필드는 읽기 전용이지만 새 구조체를 만드는 것과 같이 우회하고 싶을 때 사용할 수 있습니다.
        /// </summary>
        AccessInterceptor accessor { get; }

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
        /// <param name="noCopy"></param>
        /// <returns>각 객체의 변수 값 컬렉션입니다.</returns>
        IEnumerable<object?> GetValues(bool noCopy = false);
        
        void SetValues(IEnumerable<object?> values);
        
        /// <summary>
        /// 지정된 플래그에 따라 변수의 값을 가져오거나, 읽을 수 없는 경우 해당 타입의 기본값을 반환합니다.
        /// </summary>
        /// <param name="flags">읽기 권한을 확인할 때 사용할 <see cref="InspectorFlags"/>입니다.</param>
        /// <returns>
        /// <see cref="IsReadable"/>가 <see langword="true"/>인 경우 현재 <see cref="value"/>를 반환하고, 
        /// 그렇지 않은 경우 <see cref="variableType"/>의 기본값(default)을 반환합니다.
        /// </returns>
        object? GetValueOrDefault(InspectorFlags flags) => IsReadable(flags) ? value : variableType.GetDefaultValue();

        /// <summary>
        /// 변수를 읽을 수 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        /// <param name="flags">읽기 권한을 확인할 때 사용할 <see cref="InspectorFlags"/>입니다.</param>
        /// <param name="noInstanceCheck"></param>
        bool IsReadable(InspectorFlags flags = InspectorFlags.PublicAccess, bool noInstanceCheck = false);

        /// <summary>
        /// 변수에 쓸 수 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        /// <param name="flags">쓰기 권한을 확인할 때 사용할 <see cref="InspectorFlags"/>입니다.</param>
        /// <param name="noInstanceCheck"></param>
        bool IsWritable(InspectorFlags flags = InspectorFlags.PublicAccess, bool noInstanceCheck = false);

        /// <summary>
        /// 자식 <see cref="IInspectableObject"/> 또는 <see cref="IInspectableList"/>에 포함된 인스턴스 목록을 업데이트합니다.
        /// </summary>
        void UpdateChildInspectable();
        
        /// <inheritdoc cref="IInspectorElement.Clone"/>
        new IInspectorVariableElement Clone();
        IInspectorElement IInspectorElement.Clone() => Clone();
    }
}