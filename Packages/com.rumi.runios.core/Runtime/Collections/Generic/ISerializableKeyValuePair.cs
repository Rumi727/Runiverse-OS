#nullable enable
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // 명명 스타일
namespace RuniOS.Collections.Generic
{
    /// <summary>
    /// 키와 값을 일반 <see cref="object"/> 타입으로 노출하는 직렬화 가능한 키-값 쌍의 비제네릭 인터페이스입니다.<br/>
    /// 이는 제네릭 <see cref="ISerializableKeyValuePair{TKey, TValue}"/>의 기본 인터페이스 역할을 합니다.
    /// <br/><br/>
    /// 이 인터페이스를 구현하는 타입은 유니티 인스펙터상에 올바르게 표시되려면
    /// 'key'와 'value'라는 이름의 직렬화 가능한 필드를 가져야 합니다.
    /// </summary>
    public interface ISerializableKeyValuePair
    {
        // 필드랑 프로퍼티 이름 바꾸지 마세요.
        // 직렬화에 사용합니다.
        
        /// <summary>
        /// 키를 가져오거나 설정합니다.
        /// </summary>
        object? Key { get; set; }
        /// <summary>
        /// 값을 가져오거나 설정합니다.
        /// </summary>
        object? Value { get; set; }
        
        ISerializableKeyValuePair CreateInstance(object? key, object? value);
    }
}
#pragma warning restore IDE1006 // 명명 스타일