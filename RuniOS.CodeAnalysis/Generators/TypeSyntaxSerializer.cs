using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace RuniOS.CodeAnalysis.Generators;

/// <summary>
/// Serializes type information represented by a Roslyn <see cref="ITypeSymbol"/> into C# type syntax while preserving its meaning as far as possible.<br/>
/// Roslyn의 <see cref="ITypeSymbol"/>이 나타내는 타입 정보를 가능한 한 의미를 보존하는 C# 타입 구문으로 직렬화합니다.
/// </summary>
/// <remarks>
/// The serializer produces syntax that can be inserted into C# source rather than a human-readable display string.<br/>
/// When possible, it writes ordinary named types from <c>global::</c> to avoid context-dependent name resolution and preserves details that contribute to type meaning, including nullable annotations, tuple element names, nested types, generic type arguments, unbound generics, pointers, and function pointers.
/// <br/><br/>
/// The serializer does not validate type symbols. It does not determine whether a symbol is declared, accessible at the current location, <c>CanBeReferencedByName</c>, file-local, or usable at a particular declaration site.<br/>
/// The caller that inserts the serialized result into source is responsible for validating those usage conditions.
/// <br/><br/>
/// If the original type meaning cannot be represented by C# type syntax, or this implementation does not support the representation, the serializer records an error in <see cref="SerializeErrorResults"/> instead of silently replacing the meaning.<br/>
/// Rendering continues where possible, so an error result does not imply that <c>result</c> is an empty string.
/// <br/><br/>
/// 이 직렬화기는 사람이 읽기 좋은 표시 문자열이 아니라 C# 소스에 삽입할 수 있는 구문을 생성합니다.<br/>
/// 가능한 경우 문맥에 따른 이름 해석 차이를 피하기 위해 일반 명명 타입을 <c>global::</c>에서 시작하는 이름으로 출력하고, nullable annotation, tuple 요소 이름, 중첩 타입, 제네릭 타입 인수, unbound generic, 포인터 및 함수 포인터처럼 타입 의미를 구성하는 정보도 보존합니다.
/// <br/><br/>
/// 이 직렬화기는 타입 심볼을 검증하지 않습니다. 심볼의 선언 여부, 현재 위치에서의 접근 가능 여부, <c>CanBeReferencedByName</c> 여부, file-local 여부 또는 특정 선언 위치에서의 사용 가능 여부를 판정하지 않습니다.<br/>
/// 직렬화 결과를 소스에 삽입하는 호출자가 이러한 사용 조건을 검증해야 합니다.
/// <br/><br/>
/// 원래 타입 의미를 C# 타입 구문으로 표현할 수 없거나 이 구현이 해당 표현을 지원하지 않으면 의미를 임의로 대체하지 않고 <see cref="SerializeErrorResults"/>에 오류를 기록합니다.<br/>
/// 가능한 부분은 계속 렌더링하므로 오류 결과가 반환되어도 <c>result</c>가 반드시 빈 문자열인 것은 아닙니다.
/// </remarks>
public static partial class TypeSyntaxSerializer
{
    /// <summary>
    /// Represents a collection of errors found when type meaning cannot be fully preserved as C# type syntax.<br/>
    /// 타입 의미를 C# 타입 구문으로 완전히 보존하지 못할 때 발견된 오류들의 집합을 나타냅니다.
    /// </summary>
    /// <remarks>
    /// The default value represents a successful result with no errors.<br/>
    /// Errors found while recursively rendering nested types are accumulated with the <c>|</c> operator.
    /// <br/><br/>
    /// 기본값은 오류가 없는 성공 상태를 나타냅니다.<br/>
    /// 중첩 타입을 재귀적으로 렌더링하면서 발견된 오류는 <c>|</c> 연산자로 누적됩니다.
    /// </remarks>
    public readonly struct SerializeErrorResults : IReadOnlyList<SerializeErrorResult>
    {
        /// <summary>
        /// Initializes a result containing one serialization error.<br/>
        /// 직렬화 오류 하나를 포함하는 결과를 초기화합니다.
        /// </summary>
        /// <param name="error">
        /// The kind of error that was found.<br/>
        /// 발견된 오류의 종류입니다.
        /// </param>
        /// <param name="problematicObject">
        /// The Roslyn symbol or value directly associated with the error; it may be <see langword="null"/>.<br/>
        /// 오류와 직접 관련된 Roslyn 심볼 또는 값이며, <see langword="null"/>일 수 있습니다.
        /// </param>
        public SerializeErrorResults(SerializeError error, object? problematicObject) => errors = ImmutableArray.Create(new SerializeErrorResult(error, problematicObject));
        SerializeErrorResults(ImmutableArray<SerializeErrorResult> errors) => this.errors = errors;

        ImmutableArray<SerializeErrorResult> normalizedErrors => errors.IsDefault ? ImmutableArray<SerializeErrorResult>.Empty : errors;
        readonly ImmutableArray<SerializeErrorResult> errors;

        /// <summary>
        /// Gets the serialization error at the specified index.<br/>
        /// 지정한 인덱스의 직렬화 오류를 가져옵니다.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the error to retrieve.<br/>
        /// 가져올 오류의 0부터 시작하는 인덱스입니다.
        /// </param>
        /// <returns>
        /// The serialization error at <paramref name="index"/>.<br/>
        /// <paramref name="index"/>에 해당하는 직렬화 오류입니다.
        /// </returns>
        public SerializeErrorResult this[int index] => errors[index];

        /// <summary>
        /// Gets the number of serialization errors in this result.<br/>
        /// 이 결과에 포함된 직렬화 오류의 개수를 가져옵니다.
        /// </summary>
        public int count => normalizedErrors.Length;
        int IReadOnlyCollection<SerializeErrorResult>.Count => count;

        /// <summary>
        /// Gets a value indicating whether this result contains no serialization errors.<br/>
        /// 이 결과에 직렬화 오류가 없는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool isSuccess => count == 0;

        /// <summary>
        /// Returns an enumerator that iterates through the serialization errors.<br/>
        /// 직렬화 오류를 순회하는 열거자를 반환합니다.
        /// </summary>
        /// <returns>
        /// An enumerator for the serialization errors.<br/>
        /// 직렬화 오류를 위한 열거자입니다.
        /// </returns>
        public ImmutableArray<SerializeErrorResult>.Enumerator GetEnumerator() => normalizedErrors.GetEnumerator();
        IEnumerator<SerializeErrorResult> IEnumerable<SerializeErrorResult>.GetEnumerator() => ((IEnumerable<SerializeErrorResult>)normalizedErrors).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)normalizedErrors).GetEnumerator();

        /// <summary>
        /// Combines the errors from two serialization results.<br/>
        /// 두 직렬화 결과의 오류를 결합합니다.
        /// </summary>
        /// <param name="lhs">
        /// The left-hand result whose errors are placed first.<br/>
        /// 오류가 먼저 배치되는 왼쪽 결과입니다.
        /// </param>
        /// <param name="rhs">
        /// The right-hand result whose errors are appended after <paramref name="lhs"/>.<br/>
        /// <paramref name="lhs"/> 뒤에 오류가 추가되는 오른쪽 결과입니다.
        /// </param>
        /// <returns>
        /// The combined result; when either operand is successful, returns the other operand.<br/>
        /// 결합된 결과이며, 두 피연산자 중 하나가 성공 상태이면 다른 피연산자를 반환합니다.
        /// </returns>
        public static SerializeErrorResults operator |(SerializeErrorResults lhs, SerializeErrorResults rhs)
        {
            if (lhs.isSuccess)
                return rhs;

            if (rhs.isSuccess)
                return lhs;

            return new SerializeErrorResults(lhs.errors.AddRange(rhs.errors));
        }
    }

    /// <summary>
    /// Represents one error found while preserving type meaning in C# type syntax.<br/>
    /// 타입 의미를 C# 타입 구문으로 보존하는 과정에서 발견된 단일 오류를 나타냅니다.
    /// </summary>
    /// <param name="error">
    /// The kind of error.<br/>
    /// 오류의 종류입니다.
    /// </param>
    /// <param name="problematicObject">
    /// The symbol or value associated with the error; it may be <see langword="null"/>.<br/>
    /// 오류와 관련된 심볼 또는 값이며, <see langword="null"/>일 수 있습니다.
    /// </param>
    public readonly record struct SerializeErrorResult(SerializeError error, object? problematicObject);

    /// <summary>
    /// Defines errors that can be reported while preserving type meaning as C# type syntax.<br/>
    /// 타입 의미를 C# 타입 구문으로 보존하는 과정에서 보고할 수 있는 오류를 정의합니다.
    /// </summary>
    public enum SerializeError
    {
        /// <summary>
        /// No serialization error occurred.<br/>
        /// 직렬화 오류가 없습니다.
        /// </summary>
        none,
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
        /// A type kind cannot be represented by this serializer as C# type syntax.<br/>
        /// 이 직렬화기가 해당 종류의 타입을 C# 타입 구문으로 표현할 수 없습니다.
        /// </summary>
        unrepresentableType
    }

    /// <summary>
    /// Serializes the specified <see cref="ITypeSymbol"/> into a C# type syntax string while preserving its meaning as far as possible.<br/>
    /// 지정한 <see cref="ITypeSymbol"/>의 타입 정보를 가능한 한 의미를 보존하는 C# 타입 구문 문자열로 직렬화합니다.
    /// </summary>
    /// <param name="typeSymbol">
    /// The type symbol to serialize. The method does not check whether its declaration exists or whether it can be used at the current source location.<br/>
    /// 직렬화할 타입 심볼입니다. 선언의 존재 여부나 현재 소스 위치에서의 사용 가능 여부는 검사하지 않습니다.
    /// </param>
    /// <param name="result">
    /// Receives the C# type syntax that could be constructed from <paramref name="typeSymbol"/>. It may contain partially generated text even when errors are returned.<br/>
    /// <paramref name="typeSymbol"/>에서 구성할 수 있었던 C# 타입 구문을 받습니다. 오류가 반환되어도 부분적으로 생성된 문자열이 포함될 수 있습니다.
    /// </param>
    /// <returns>
    /// The errors for portions whose original type meaning could not be fully preserved. Returns the default successful result when no errors are found.<br/>
    /// 원래 타입 의미를 완전히 보존하지 못한 부분들의 오류입니다. 오류가 없으면 기본값인 성공 상태를 반환합니다.
    /// </returns>
    /// <remarks>
    /// This method does not verify that the result compiles at a particular source location.<br/>
    /// The caller that inserts the result must separately ensure accessibility, file-local scope, language version, <c>unsafe</c> context, and declaration existence.
    /// <br/><br/>
    /// 이 메서드는 결과 문자열이 특정 소스 위치에서 컴파일되는지 검증하지 않습니다.<br/>
    /// 결과를 삽입하는 호출자가 접근성, file-local 범위, 언어 버전, <c>unsafe</c> 문맥 및 선언 자체의 존재 여부를 별도로 보장해야 합니다.
    /// </remarks>
    public static SerializeErrorResults TrySerialize(this ITypeSymbol typeSymbol, out string result)
    {
        StringBuilder builder = new StringBuilder();

        SerializeErrorResults errorResult = RenderType(builder, typeSymbol);
        result = builder.ToString();

        return errorResult;
    }

    static SerializeErrorResults RenderType(StringBuilder builder, ITypeSymbol typeSymbol) => typeSymbol switch
    {
        IArrayTypeSymbol arrayTypeSymbol => RenderArray(builder, arrayTypeSymbol),
        IDynamicTypeSymbol dynamicTypeSymbol => RenderDynamicType(builder, dynamicTypeSymbol),
        IFunctionPointerTypeSymbol functionPointerTypeSymbol => RenderFunctionPointer(builder, functionPointerTypeSymbol),
        INamedTypeSymbol namedTypeSymbol => RenderNamedType(builder, namedTypeSymbol),
        IPointerTypeSymbol pointerTypeSymbol => RenderPointer(builder, pointerTypeSymbol),
        ITypeParameterSymbol typeParameterSymbol => RenderTypeParameter(builder, typeParameterSymbol),
        _ => new SerializeErrorResults(SerializeError.unrepresentableType, typeSymbol)
    };

    static void RenderNullableAnnotation(StringBuilder builder, ITypeSymbol typeSymbol)
    {
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
            builder.Append('?');
    }

    static SerializeErrorResults RenderIdentifier(StringBuilder builder, string identifier)
    {
        if (!SyntaxFacts.IsValidIdentifier(identifier))
            return new SerializeErrorResults(SerializeError.invalidIdentifier, identifier);

        if (SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None)
            builder.Append('@');

        builder.Append(identifier);
        return default;
    }
}
