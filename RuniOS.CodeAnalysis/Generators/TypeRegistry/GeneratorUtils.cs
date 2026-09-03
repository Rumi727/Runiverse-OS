using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RuniOS.CodeAnalysis.Generators.TypeRegistry;

/// <summary>
/// Provides shared symbol formatting and generated-source helpers.<br/>
/// 심볼 형식 지정 및 생성 소스 작성을 위한 공통 도우미를 제공합니다.
/// </summary>
static class GeneratorUtils
{
    /// <summary>
    /// Gets the fully qualified symbol display format used by the generators.<br/>
    /// 생성기가 사용하는 완전 수식 심볼 표시 형식을 가져옵니다.
    /// </summary>
    public static readonly SymbolDisplayFormat fullyQualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    /// <summary>
    /// Gets the metadata name of a named type, including its namespace and containing types.<br/>
    /// 네임스페이스와 포함 타입을 포함한 명명된 타입의 메타데이터 이름을 가져옵니다.
    /// </summary>
    /// <param name="type">
    /// The named type whose metadata name is requested.<br/>
    /// 메타데이터 이름을 가져올 명명된 타입입니다.
    /// </param>
    /// <returns>
    /// The metadata name of <paramref name="type"/>.<br/>
    /// <paramref name="type"/>의 메타데이터 이름입니다.
    /// </returns>
    public static string GetMetadataName(INamedTypeSymbol type)
    {
        StringBuilder result = new();
        AppendMetadataName(result, type);
        return result.ToString();

        static void AppendMetadataName(StringBuilder builder, INamedTypeSymbol current)
        {
            if (current.ContainingType != null)
            {
                AppendMetadataName(builder, current.ContainingType);
                builder.Append('.');
            }
            else if (!current.ContainingNamespace.IsGlobalNamespace)
            {
                builder.Append(current.ContainingNamespace.ToDisplayString()).Append('.');
            }

            builder.Append(current.MetadataName);
        }
    }

    /// <summary>
    /// Computes a deterministic eight-character uppercase hexadecimal hash for a string.<br/>
    /// 문자열에 대해 결정적인 8자리 대문자 16진수 해시를 계산합니다.
    /// </summary>
    /// <param name="value">
    /// The string to hash.<br/>
    /// 해시를 계산할 문자열입니다.
    /// </param>
    /// <returns>
    /// The uppercase hexadecimal hash representation.<br/>
    /// 대문자 16진수로 표현한 해시입니다.
    /// </returns>
    public static string GetShortHash(string value)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (char character in value)
            {
                hash ^= character;
                hash *= 16777619;
            }

            return hash.ToString("X8", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Formats a type symbol as a fully qualified C# type name.<br/>
    /// 타입 심볼을 완전 수식 C# 타입 이름으로 형식화합니다.
    /// </summary>
    /// <param name="type">
    /// The type symbol to format.<br/>
    /// 형식화할 타입 심볼입니다.
    /// </param>
    /// <returns>
    /// The fully qualified display name of <paramref name="type"/>.<br/>
    /// <paramref name="type"/>의 완전 수식 표시 이름입니다.
    /// </returns>
    public static string GetTypeName(ITypeSymbol type) => type.ToDisplayString(fullyQualifiedFormat);

    /// <summary>
    /// Formats a type symbol for use inside a <c>typeof</c> expression.<br/>
    /// 타입 심볼을 <c>typeof</c> 식 안에서 사용할 수 있도록 형식화합니다.
    /// </summary>
    /// <param name="type">
    /// The type symbol to format.<br/>
    /// 형식화할 타입 심볼입니다.
    /// </param>
    /// <returns>
    /// The fully qualified display name suitable for a <c>typeof</c> expression.<br/>
    /// <c>typeof</c> 식에 사용할 수 있는 완전 수식 표시 이름입니다.
    /// </returns>
    public static string GetTypeOfName(ITypeSymbol type)
    {
        if
        (
            type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableType &&
            (
                nullableType.IsUnboundGenericType ||
                nullableType.TypeArguments.Length == 0 ||
                nullableType.TypeArguments.Any(static typeArgument => typeArgument is ITypeParameterSymbol)
            )
        )
            return "global::System.Nullable<>";

        return type.ToDisplayString(fullyQualifiedFormat);
    }

    /// <summary>
    /// Formats a type as an unbound generic type name when it is a constructed named generic type.<br/>
    /// 구성된 명명된 제네릭 타입이면 바인딩되지 않은 제네릭 타입 이름으로 형식화합니다.
    /// </summary>
    /// <param name="type">
    /// The type symbol to format.<br/>
    /// 형식화할 타입 심볼입니다.
    /// </param>
    /// <returns>
    /// An unbound generic type name for a named generic type; otherwise, the regular fully qualified type name.<br/>
    /// 명명된 제네릭 타입이면 바인딩되지 않은 제네릭 타입 이름을, 그렇지 않으면 일반 완전 수식 타입 이름을 반환합니다.
    /// </returns>
    public static string GetTypeOfGenericDefinitionName(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })
                return "global::System.Nullable<>";

            return namedType.ConstructUnboundGenericType().ToDisplayString(fullyQualifiedFormat);
        }

        return GetTypeOfName(type);
    }

    /// <summary>
    /// Escapes a C# keyword or contextual keyword for use as an identifier.<br/>
    /// C# 키워드 또는 상황별 키워드를 식별자로 사용할 수 있도록 이스케이프합니다.
    /// </summary>
    /// <param name="name">
    /// The identifier text to escape.<br/>
    /// 이스케이프할 식별자 텍스트입니다.
    /// </param>
    /// <returns>
    /// The identifier prefixed with <c>@</c> when required; otherwise, the original text.<br/>
    /// 필요한 경우 <c>@</c>를 접두사로 붙인 식별자를, 그렇지 않으면 원본 텍스트를 반환합니다.
    /// </returns>
    public static string EscapeIdentifier(string name)
    {
        return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None
            ? $"@{name}"
            : name;
    }

    /// <summary>
    /// Renders a string as a regular C# string literal with escaped control characters.<br/>
    /// 문자열을 제어 문자를 이스케이프한 일반 C# 문자열 리터럴로 변환합니다.
    /// </summary>
    /// <param name="value">
    /// The string to render.<br/>
    /// 변환할 문자열입니다.
    /// </param>
    /// <returns>
    /// A C# string literal containing <paramref name="value"/>.<br/>
    /// <paramref name="value"/>를 포함하는 C# 문자열 리터럴입니다.
    /// </returns>
    public static string StringLiteral(string value)
    {
        StringBuilder result = new(value.Length + 2);
        result.Append('"');
        foreach (char character in value)
        {
            switch (character)
            {
                case '\0': result.Append("\\0"); break;
                case '\a': result.Append("\\a"); break;
                case '\b': result.Append("\\b"); break;
                case '\t': result.Append("\\t"); break;
                case '\n': result.Append("\\n"); break;
                case '\v': result.Append("\\v"); break;
                case '\f': result.Append("\\f"); break;
                case '\r': result.Append("\\r"); break;
                case '"': result.Append("\\\""); break;
                case '\\': result.Append("\\\\"); break;
                default:
                    if (char.IsControl(character))
                        result.Append("\\u").Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
                    else
                        result.Append(character);
                    break;
            }
        }

        return result.Append('"').ToString();
    }

    /// <summary>
    /// Renders a character as a C# character literal.<br/>
    /// 문자를 C# 문자 리터럴로 변환합니다.
    /// </summary>
    /// <param name="value">
    /// The character to render.<br/>
    /// 변환할 문자입니다.
    /// </param>
    /// <returns>
    /// A C# character literal containing <paramref name="value"/>.<br/>
    /// <paramref name="value"/>를 포함하는 C# 문자 리터럴입니다.
    /// </returns>
    public static string CharLiteral(char value)
    {
        string escaped = value switch
        {
            '\0' => "\\0",
            '\a' => "\\a",
            '\b' => "\\b",
            '\t' => "\\t",
            '\n' => "\\n",
            '\v' => "\\v",
            '\f' => "\\f",
            '\r' => "\\r",
            '\\' => "\\\\",
            '\'' => "\\'",
            _ when char.IsControl(value) => $"\\u{((int)value).ToString("X4", CultureInfo.InvariantCulture)}",
            _ => value.ToString()
        };

        return $"'{escaped}'";
    }

    /// <summary>
    /// Renders the <c>public partial</c> declaration header for a named type.<br/>
    /// 명명된 타입의 <c>public partial</c> 선언 헤더를 변환합니다.
    /// </summary>
    /// <param name="type">
    /// The named type whose declaration header is rendered.<br/>
    /// 선언 헤더를 변환할 명명된 타입입니다.
    /// </param>
    /// <returns>
    /// The declaration header, including modifiers, type parameters, and constraints, without braces.<br/>
    /// 중괄호를 제외하고 한정자, 타입 매개 변수, 제약 조건을 포함한 선언 헤더입니다.
    /// </returns>
    public static string RenderTypeDeclarationHeader(INamedTypeSymbol type)
    {
        StringBuilder result = new("public ");
        if (type.IsStatic)
            result.Append("static ");
        else
        {
            if (type.IsAbstract && type.TypeKind == TypeKind.Class)
                result.Append("abstract ");
            if (type.IsSealed && type.TypeKind == TypeKind.Class)
                result.Append("sealed ");
            if (type.IsReadOnly && type.TypeKind == TypeKind.Struct)
                result.Append("readonly ");
        }

        result.Append("partial ");
        if (type.IsRecord)
            result.Append(type.TypeKind == TypeKind.Struct ? "record struct " : "record class ");
        else
            result.Append(type.TypeKind == TypeKind.Struct ? "struct " : type.TypeKind == TypeKind.Interface ? "interface " : "class ");

        result.Append(EscapeIdentifier(type.Name));
        if (type.TypeParameters.Length != 0)
        {
            result.Append('<');
            for (int index = 0; index < type.TypeParameters.Length; index++)
            {
                if (index != 0)
                    result.Append(", ");
                result.Append(EscapeIdentifier(type.TypeParameters[index].Name));
            }

            result.Append('>');
        }

        foreach (ITypeParameterSymbol parameter in type.TypeParameters)
        {
            string constraint = RenderConstraint(parameter);
            if (constraint.Length != 0)
                result.Append(" where ").Append(EscapeIdentifier(parameter.Name)).Append(" : ").Append(constraint);
        }

        return result.ToString();
    }

    /// <summary>
    /// Renders the constraint text for a type parameter.<br/>
    /// 타입 매개 변수의 제약 조건 텍스트를 변환합니다.
    /// </summary>
    /// <param name="parameter">
    /// The type parameter whose constraints are rendered.<br/>
    /// 제약 조건을 변환할 타입 매개 변수입니다.
    /// </param>
    /// <returns>
    /// A comma-separated constraint list, or an empty string when no constraints are present.<br/>
    /// 쉼표로 구분한 제약 조건 목록이며, 제약 조건이 없으면 빈 문자열입니다.
    /// </returns>
    public static string RenderConstraint(ITypeParameterSymbol parameter)
    {
        List<string> constraints = [];
        if (parameter.HasUnmanagedTypeConstraint)
            constraints.Add("unmanaged");
        else if (parameter.HasValueTypeConstraint)
            constraints.Add("struct");
        else if (parameter.HasReferenceTypeConstraint)
            constraints.Add(parameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class");
        else if (parameter.HasNotNullConstraint)
            constraints.Add("notnull");

        constraints.AddRange(parameter.ConstraintTypes.Select(GetTypeName));
        if (parameter.HasConstructorConstraint)
            constraints.Add("new()");

        return string.Join(", ", constraints);
    }

    /// <summary>
    /// Creates the generated source hint name for a registration group.<br/>
    /// 등록 그룹에 사용할 생성 소스 힌트 이름을 만듭니다.
    /// </summary>
    /// <param name="generatorName">
    /// The name of the generator producing the registration.<br/>
    /// 등록을 생성하는 생성기의 이름입니다.
    /// </param>
    /// <param name="stableId">
    /// The stable identifier hashed into the hint name.<br/>
    /// 힌트 이름에 해시로 포함할 안정 식별자입니다.
    /// </param>
    /// <returns>
    /// A generated source hint name ending in <c>.g.cs</c>.<br/>
    /// <c>.g.cs</c>로 끝나는 생성 소스 힌트 이름입니다.
    /// </returns>
    public static string GetRegistrationHintName(string generatorName, string stableId) => $"RuniOS.{generatorName}.Registration.{GetShortHash(stableId)}.g.cs";

    /// <summary>
    /// Gets the containing type chain from the outermost type to the specified type.<br/>
    /// 가장 바깥쪽 타입부터 지정된 타입까지의 포함 타입 체인을 가져옵니다.
    /// </summary>
    /// <param name="type">
    /// The type whose containing chain is requested.<br/>
    /// 포함 타입 체인을 가져올 타입입니다.
    /// </param>
    /// <returns>
    /// A mutable list containing the containing types in declaration order, including <paramref name="type"/>.<br/>
    /// <paramref name="type"/>를 포함하며 선언 순서대로 정렬된 변경 가능한 포함 타입 목록입니다.
    /// </returns>
    public static List<INamedTypeSymbol> GetContainingTypes(INamedTypeSymbol type)
    {
        List<INamedTypeSymbol> result = [];
        for (INamedTypeSymbol? current = type; current != null; current = current.ContainingType)
            result.Add(current);
        result.Reverse();
        return result;
    }
}
