#nullable enable
#pragma warning disable IDE1006 // 명명 스타일
namespace RuniEngine
{
    /// <summary>
    /// 인스펙터상에 표시되려면 value, hasValue 이름의 가진 직렬화 가능 필드가 있어야합니다!
    /// </summary>
    public interface ISerializableNullable<T> where T : struct
    {
        T Value { get; }
        bool HasValue { get; }
    }
}
#pragma warning restore IDE1006 // 명명 스타일