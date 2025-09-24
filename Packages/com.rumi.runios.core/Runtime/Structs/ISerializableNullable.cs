#nullable enable
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // 명명 스타일
namespace RuniOS
{
    /// <summary>
    /// <see cref="ISerializableNullable{T}"/>의 타입을 알 수 없을 때, null 값을 확인하기 위한 간단한 인터페이스입니다.
    /// </summary>
    public interface ISerializableNullable
    {
        /// <summary>
        /// 이 <see cref="ISerializableNullable"/> 인스턴스에 값이 할당되었는지 여부를 가져옵니다.
        /// </summary>
        bool HasValue { get; }
    }
}
#pragma warning restore IDE1006 // 명명 스타일