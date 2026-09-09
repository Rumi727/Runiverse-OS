namespace RuniOS.CodeAnalysis.Generators;

/// <summary>
/// Defines errors that can be reported while preserving the original meaning during serialization.<br/>
/// 직렬화 과정에서 원래 의미를 보존하지 못할 때 보고할 수 있는 오류를 정의합니다.
/// </summary>
public enum SerializeError
{
    /// <summary>
    /// A symbol name cannot be represented as a C# identifier without changing its meaning.<br/>
    /// 심볼 이름을 의미를 변경하지 않는 C# 식별자로 표현할 수 없습니다.
    /// </summary>
    invalidIdentifier,
    /// <summary>
    /// An array type with a non-vector one-dimensional shape cannot be represented by equivalent C# array syntax.<br/>
    /// 벡터가 아닌 1차원 배열 타입을 동일한 의미의 C# 배열 타입 구문으로 표현할 수 없습니다.
    /// </summary>
    unsupportedArrayType,
    /// <summary>
    /// A function pointer's calling convention or signature cannot be represented by equivalent C# function pointer syntax.<br/>
    /// 함수 포인터의 호출 규약 또는 시그니처를 동일한 의미의 C# 함수 포인터 구문으로 표현할 수 없습니다.
    /// </summary>
    unsupportedFunctionPointer,
    /// <summary>
    /// A type or declaration kind cannot be represented by the serializer.<br/>
    /// 직렬화기가 해당 타입 또는 선언 종류를 표현할 수 없습니다.
    /// </summary>
    unrepresentableType
}