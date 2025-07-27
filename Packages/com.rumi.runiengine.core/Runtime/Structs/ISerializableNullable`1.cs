#nullable enable
using RuniEngine.Json.Converters;
using System;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // 명명 스타일
namespace RuniEngine
{
    /// <summary>
    /// 인스펙터상에 표시되려면 'value', 'hasValue' 이름의 가진 직렬화 가능 필드가 있어야 합니다!
    /// <br/>
    /// 이 인터페이스를 구현하는 <see langword="struct"/> 타입은 Json.NET 직렬화 시
    /// <see cref="SerializableNullableConverter"/>를 사용하여
    /// 표준 C# <see cref="Nullable{T}"/> (<see langword="struct"/> T?)와 동일하게
    /// 내부 값(<see cref="Value"/>) 또는 <see langword="null"/>로 처리되는 것을 권장합니다.
    /// <br/>
    /// 또한, <see cref="SerializableNullableConverter"/>가 올바르게 작동하려면
    /// 이 인터페이스를 구현하는 타입은 내부 값(<typeparamref name="T"/> 타입)을 인자로 받는
    /// 생성자를 제공해야 합니다 (예: <c>public MySerializableNullable(T value)</c>).
    /// </summary>
    public interface ISerializableNullable<out T> where T : struct
    {
        T Value { get; }
        bool HasValue { get; }
    }
}
#pragma warning restore IDE1006 // 명명 스타일