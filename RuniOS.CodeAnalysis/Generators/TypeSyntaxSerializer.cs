using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
    public static SerializeErrorResults TrySyntaxSerialize(this ITypeSymbol typeSymbol, out string result)
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
