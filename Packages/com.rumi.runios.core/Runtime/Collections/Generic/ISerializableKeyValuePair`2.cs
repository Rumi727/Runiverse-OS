#nullable enable
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // 명명 스타일
namespace RuniOS
{
    /// <summary>
    /// 제네릭 키와 값을 가진 직렬화 가능한 키-값 쌍의 인터페이스입니다.
    /// <br/>
    /// 이 인터페이스를 구현하는 타입은 유니티 인스펙터상에 올바르게 표시되려면
    /// 'key'와 'value'라는 이름의 직렬화 가능한 필드를 가져야 합니다.
    /// </summary>
    /// <typeparam name="TKey">키의 타입입니다.</typeparam>
    /// <typeparam name="TValue">값의 타입입니다.</typeparam>
    public interface ISerializableKeyValuePair<TKey, TValue>
    {
        /// <summary>
        /// 키를 가져오거나 설정합니다.
        /// </summary>
        TKey Key { get; set; }
        /// <summary>
        /// 값을 가져오거나 설정합니다.
        /// </summary>
        TValue Value { get; set; }
    }
}
#pragma warning restore IDE1006 // 명명 스타일