#nullable enable
using RuniOS.Json.Converters;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // 명명 스타일
namespace RuniOS;

/// <summary>
/// 인스펙터상에 표시되려면 'value', 'hasValue' 이름의 직렬화 가능 필드가 있어야 합니다!
/// <br/><br/>
/// 이 인터페이스를 구현하는 <see langword="struct"/> 타입은 Json.NET 직렬화 시
/// <see cref="SerializableNullableConverter"/>를 사용하여
/// 표준 C# <see cref="Nullable{T}"/> (<see langword="struct"/> T?)와 동일하게
/// 내부 값(<see cref="Value"/>) 또는 <see langword="null"/>로 처리됩니다.
/// <br/><br/>
/// <see cref="SerializableNullableConverter"/>가 올바르게 작동하려면
/// 이 인터페이스를 구현하는 타입은 내부 값(<typeparamref name="T"/> 타입)을 인자로 받는
/// 생성자를 제공해야 합니다 (예: <c>public MySerializableNullable(T value)</c>).
/// </summary>
/// <typeparam name="T">값이 될 수 있는 기본 값 타입입니다.</typeparam>
public interface ISerializableNullable<out T> : ISerializableNullable where T : struct
{
    /// <summary>
    /// 이 <see cref="ISerializableNullable{T}"/> 인스턴스에 할당된 값을 가져옵니다.
    /// <br/>
    /// <see cref="ISerializableNullable.HasValue"/>가 <see langword="false"/>일 경우, 이 속성에 접근하면 <see cref="InvalidOperationException"/>이 발생합니다.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="ISerializableNullable.HasValue"/>가 <see langword="false"/>일 때 발생합니다.</exception>
    T Value { get; }
}
#pragma warning restore IDE1006 // 명명 스타일