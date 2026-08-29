using Microsoft.CodeAnalysis;
using RuniOS.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RuniOS.CodeAnalysis.Generators.TypeRegistry;

/// <summary>
/// Converts Roslyn attribute data into C# expressions that can be emitted by a source generator.<br/>
/// Roslyn 특성 데이터를 소스 생성기가 출력할 수 있는 C# 식으로 변환합니다.
/// </summary>
static class AttributeLiteralEmitter
{
    /// <summary>
    /// Tries to render an attribute construction expression and reports an explanatory diagnostic on failure.<br/>
    /// 특성 생성 식 변환을 시도하고 실패하면 원인을 설명하는 진단을 보고합니다.
    /// </summary>
    /// <param name="attribute">
    /// The attribute data to render.<br/>
    /// 변환할 특성 데이터입니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation used to validate accessibility of the attribute and its referenced types and members.<br/>
    /// 특성과 참조 타입 및 멤버의 접근성을 검증하는 데 사용할 컴파일입니다.
    /// </param>
    /// <param name="expression">
    /// Receives the generated C# expression when rendering succeeds; otherwise, an empty string.<br/>
    /// 변환에 성공하면 생성된 C# 식을 받고, 실패하면 빈 문자열입니다.
    /// </param>
    /// <param name="diagnostic">
    /// Receives the diagnostic explaining a rendering failure; otherwise, <see langword="null"/>.<br/>
    /// 변환 실패를 설명하는 진단을 받고, 성공하면 <see langword="null"/>입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the attribute can be reproduced as C# source; otherwise, <see langword="false"/>.<br/>
    /// 특성을 C# 소스로 재생성할 수 있으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    public static bool TryRender(AttributeData attribute, Compilation compilation, out string expression, out Diagnostic? diagnostic)
    {
        expression = string.Empty;
        diagnostic = null;
        Location diagnosticLocation = TypeRegistrySymbolHelpers.GetLocation(attribute);

        if (attribute.AttributeClass is not { } attributeType || attribute.AttributeConstructor == null)
        {
            diagnostic = TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.unemittableAttributeArgument,
                diagnosticLocation,
                compilation,
                (object?)attribute.AttributeClass ?? "<unknown>",
                "attribute constructor or type is unresolved"
            );
            return false;
        }

        if (!TypeRegistrySymbolHelpers.IsAccessibleFromGeneratedCode(attributeType, compilation) || !TypeRegistrySymbolHelpers.IsAccessibleFromGeneratedCode(attribute.AttributeConstructor, compilation))
        {
            diagnostic = TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.inaccessibleAttribute,
                diagnosticLocation,
                compilation,
                attributeType
            );
            return false;
        }

        // 생성 등록 소스는 특성 생성자와 쓰기 가능한 named member를 정확히 재현해야 합니다.
        List<string> constructorArguments = [];
        foreach (TypedConstant argument in attribute.ConstructorArguments)
        {
            if (!TryRenderConstant(argument, compilation, diagnosticLocation, out string value, out string reason))
            {
                diagnostic = TypeRegistryDiagnostics.Create
                (
                    TypeRegistryDiagnostics.unemittableAttributeArgument,
                    diagnosticLocation,
                    compilation,
                    attributeType,
                    reason
                );
                return false;
            }

            constructorArguments.Add(value);
        }

        List<string> namedArguments = [];
        foreach (KeyValuePair<string, TypedConstant> namedArgument in attribute.NamedArguments)
        {
            string name = namedArgument.Key;
            TypedConstant argument = namedArgument.Value;
            ISymbol? member = FindMember(attributeType, name);
            if (!IsWritableNamedMember(member, compilation))
            {
                diagnostic = TypeRegistryDiagnostics.Create
                (
                    TypeRegistryDiagnostics.inaccessibleAttribute,
                    diagnosticLocation,
                    compilation,
                    attributeType
                );
                return false;
            }

            if (!TryRenderConstant(argument, compilation, diagnosticLocation, out string value, out string reason))
            {
                diagnostic = TypeRegistryDiagnostics.Create
                (
                    TypeRegistryDiagnostics.unemittableAttributeArgument,
                    diagnosticLocation,
                    compilation,
                    attributeType,
                    reason
                );
                return false;
            }

            namedArguments.Add($"{GeneratorUtils.EscapeIdentifier(name)} = {value}");
        }

        expression = $"new {GeneratorUtils.GetTypeName(attributeType)}({string.Join(", ", constructorArguments)})";
        if (namedArguments.Count != 0)
            expression += $" {{ {string.Join(", ", namedArguments)} }}";

        return true;
    }

    /// <summary>
    /// Tries to render a Roslyn typed constant as a C# expression.<br/>
    /// Roslyn 형식화 상수를 C# 식으로 변환합니다.
    /// </summary>
    /// <param name="constant">
    /// The typed constant to render.<br/>
    /// 변환할 형식화 상수입니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation used to validate referenced type accessibility.<br/>
    /// 참조 타입의 접근성을 검증하는 데 사용할 컴파일입니다.
    /// </param>
    /// <param name="diagnosticLocation">
    /// The source location used to resolve the shortest display name for diagnostic type arguments.<br/>
    /// 진단 타입 인수의 가장 짧은 표시 이름을 확인하는 데 사용할 소스 위치입니다.
    /// </param>
    /// <param name="value">
    /// Receives the generated expression when rendering succeeds; otherwise, an empty string.<br/>
    /// 변환에 성공하면 생성된 식을 받고, 실패하면 빈 문자열입니다.
    /// </param>
    /// <param name="reason">
    /// Receives the failure reason when rendering is unsupported.<br/>
    /// 지원되지 않는 변환이면 실패 이유를 받습니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the constant is supported; otherwise, <see langword="false"/>.<br/>
    /// 상수가 지원되면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    static bool TryRenderConstant(TypedConstant constant, Compilation compilation, Location diagnosticLocation, out string value, out string reason)
    {
        value = string.Empty;
        reason = string.Empty;

        if (constant.IsNull)
        {
            value = "null";
            return true;
        }

        if (constant.Kind == TypedConstantKind.Error)
        {
            reason = "typed constant has an error kind";
            return false;
        }

        switch (constant.Kind)
        {
            case TypedConstantKind.Type:
            {
                if (constant.Value is not ITypeSymbol type)
                {
                    reason = "type constant is unresolved";
                    return false;
                }
                if (!TypeRegistrySymbolHelpers.IsAccessibleFromGeneratedCode(type, compilation))
                {
                    reason = $"type '{TypeRegistryDiagnostics.FormatTypeName(type, compilation, diagnosticLocation)}' is inaccessible from generated code";
                    return false;
                }

                value = $"typeof({GeneratorUtils.GetTypeOfName(type)})";
                return true;
            }
            case TypedConstantKind.Enum:
            {
                if (constant.Type is not INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
                {
                    reason = "enum type is unresolved";
                    return false;
                }
                if (!TypeRegistrySymbolHelpers.IsAccessibleFromGeneratedCode(enumType, compilation))
                {
                    reason = $"enum type '{TypeRegistryDiagnostics.FormatTypeName(enumType, compilation, diagnosticLocation)}' is inaccessible from generated code";
                    return false;
                }

                value = RenderEnum(constant, enumType);
                return true;
            }
            case TypedConstantKind.Array:
            {
                if (constant.Type is not IArrayTypeSymbol { Rank: 1 } arrayType)
                {
                    reason = "only one-dimensional arrays can be generated";
                    return false;
                }
                if (!TypeRegistrySymbolHelpers.IsAccessibleFromGeneratedCode(arrayType.ElementType, compilation))
                {
                    reason = $"array element type '{TypeRegistryDiagnostics.FormatTypeName(arrayType.ElementType, compilation, diagnosticLocation)}' is inaccessible from generated code";
                    return false;
                }

                List<string> elements = [];
                foreach (TypedConstant element in constant.Values)
                {
                    if (!TryRenderConstant(element, compilation, diagnosticLocation, out string elementValue, out reason))
                        return false;
                    elements.Add(elementValue);
                }

                value = $"new {GeneratorUtils.GetTypeName(arrayType.ElementType)}[] {{ {string.Join(", ", elements)} }}";
                return true;
            }
            case TypedConstantKind.Primitive:
            {
                if (constant.Type == null || !TryRenderPrimitive(constant.Value, constant.Type.SpecialType, out value))
                {
                    reason = $"primitive value of type '{(constant.Type is { } constantType ? TypeRegistryDiagnostics.FormatTypeName(constantType, compilation, diagnosticLocation) : "<unknown>")}' is unsupported";
                    return false;
                }

                return true;
            }
            default:
            {
                reason = $"typed constant kind '{constant.Kind}' is unsupported";
                return false;
            }
        }
    }

    /// <summary>
    /// Tries to render a supported primitive value as a C# literal.<br/>
    /// 지원되는 기본 값을 C# 리터럴로 변환합니다.
    /// </summary>
    /// <param name="value">
    /// The primitive value to render.<br/>
    /// 변환할 기본 값입니다.
    /// </param>
    /// <param name="type">
    /// The Roslyn special type describing <paramref name="value"/>.<br/>
    /// <paramref name="value"/>를 설명하는 Roslyn 특수 타입입니다.
    /// </param>
    /// <param name="result">
    /// Receives the generated literal when the value is supported; otherwise, an empty string.<br/>
    /// 값이 지원되면 생성된 리터럴을 받고, 그렇지 않으면 빈 문자열입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the primitive type is supported and the value is available; otherwise, <see langword="false"/>.<br/>
    /// 기본 타입이 지원되고 값이 있으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    static bool TryRenderPrimitive(object? value, SpecialType type, out string result)
    {
        result = string.Empty;
        if (value == null)
            return false;

        switch (type)
        {
            case SpecialType.System_Boolean:
            {
                result = (bool)value ? "true" : "false";
                return true;
            }
            case SpecialType.System_Char:
            {
                result = GeneratorUtils.CharLiteral((char)value);
                return true;
            }
            case SpecialType.System_String:
            {
                result = GeneratorUtils.StringLiteral((string)value);
                return true;
            }
            case SpecialType.System_SByte:
            {
                result = $"(sbyte){Convert.ToString(value, CultureInfo.InvariantCulture)}";
                return true;
            }
            case SpecialType.System_Byte:
            {
                result = $"(byte){Convert.ToString(value, CultureInfo.InvariantCulture)}";
                return true;
            }
            case SpecialType.System_Int16:
            {
                result = $"(short){Convert.ToString(value, CultureInfo.InvariantCulture)}";
                return true;
            }
            case SpecialType.System_UInt16:
            {
                result = $"(ushort){Convert.ToString(value, CultureInfo.InvariantCulture)}";
                return true;
            }
            case SpecialType.System_Int32:
            {
                result = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";
                return true;
            }
            case SpecialType.System_UInt32:
            {
                result = $"{Convert.ToString(value, CultureInfo.InvariantCulture)}U";
                return true;
            }
            case SpecialType.System_Int64:
            {
                result = $"{Convert.ToString(value, CultureInfo.InvariantCulture)}L";
                return true;
            }
            case SpecialType.System_UInt64:
            {
                result = $"{Convert.ToString(value, CultureInfo.InvariantCulture)}UL";
                return true;
            }
            case SpecialType.System_Single:
            {
                return TryRenderSingle(Convert.ToSingle(value, CultureInfo.InvariantCulture), out result);
            }
            case SpecialType.System_Double:
            {
                return TryRenderDouble(Convert.ToDouble(value, CultureInfo.InvariantCulture), out result);
            }
            case SpecialType.System_Decimal:
            {
                result = $"{Convert.ToString(value, CultureInfo.InvariantCulture)}M";
                return true;
            }
            default:
                return false;
        }
    }

    /// <summary>
    /// Renders a single-precision value, including its non-finite constants.<br/>
    /// 단정밀도 값을 변환하며, 비유한 상수도 처리합니다.
    /// </summary>
    /// <param name="value">
    /// The single-precision value to render.<br/>
    /// 변환할 단정밀도 값입니다.
    /// </param>
    /// <param name="result">
    /// Receives the C# expression representing <paramref name="value"/>.<br/>
    /// <paramref name="value"/>를 나타내는 C# 식을 받습니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> because all single-precision values handled here have a representable expression.<br/>
    /// 여기서 처리하는 모든 단정밀도 값은 표현 가능한 식을 가지므로 <see langword="true"/>를 반환합니다.
    /// </returns>
    static bool TryRenderSingle(float value, out string result)
    {
        if (float.IsNaN(value))
        {
            result = "float.NaN";
            return true;
        }
        if (float.IsPositiveInfinity(value))
        {
            result = "float.PositiveInfinity";
            return true;
        }
        if (float.IsNegativeInfinity(value))
        {
            result = "float.NegativeInfinity";
            return true;
        }

        result = $"{value.ToString("R", CultureInfo.InvariantCulture)}F";
        return true;
    }

    /// <summary>
    /// Renders a double-precision value, including its non-finite constants.<br/>
    /// 배정밀도 값을 변환하며, 비유한 상수도 처리합니다.
    /// </summary>
    /// <param name="value">
    /// The double-precision value to render.<br/>
    /// 변환할 배정밀도 값입니다.
    /// </param>
    /// <param name="result">
    /// Receives the C# expression representing <paramref name="value"/>.<br/>
    /// <paramref name="value"/>를 나타내는 C# 식을 받습니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> because all double-precision values handled here have a representable expression.<br/>
    /// 여기서 처리하는 모든 배정밀도 값은 표현 가능한 식을 가지므로 <see langword="true"/>를 반환합니다.
    /// </returns>
    static bool TryRenderDouble(double value, out string result)
    {
        if (double.IsNaN(value))
        {
            result = "double.NaN";
            return true;
        }
        if (double.IsPositiveInfinity(value))
        {
            result = "double.PositiveInfinity";
            return true;
        }
        if (double.IsNegativeInfinity(value))
        {
            result = "double.NegativeInfinity";
            return true;
        }

        result = $"{value.ToString("R", CultureInfo.InvariantCulture)}D";
        return true;
    }

    /// <summary>
    /// Renders an enum constant using an exact member, a flags combination, or a numeric cast.<br/>
    /// 열거형 상수를 정확한 멤버, 플래그 조합, 숫자 캐스트 중 하나로 변환합니다.
    /// </summary>
    /// <param name="constant">
    /// The enum constant to render.<br/>
    /// 변환할 열거형 상수입니다.
    /// </param>
    /// <param name="enumType">
    /// The enum type containing the named members.<br/>
    /// 명명된 멤버를 포함하는 열거형 타입입니다.
    /// </param>
    /// <returns>
    /// A C# enum expression that represents the constant value.<br/>
    /// 상수 값을 나타내는 C# 열거형 식입니다.
    /// </returns>
    static string RenderEnum(TypedConstant constant, INamedTypeSymbol enumType)
    {
        ulong target = ToEnumBits(constant.Value, enumType.EnumUnderlyingType?.SpecialType ?? SpecialType.System_Int32);
        string enumName = GeneratorUtils.GetTypeName(enumType);
        List<(IFieldSymbol field, ulong value, int index)> fields = enumType.GetMembers().OfType<IFieldSymbol>()
            .Where(x => x.HasConstantValue && x.ConstantValue != null)
            .Select((field, index) => (field, value: ToEnumBits(field.ConstantValue, enumType.EnumUnderlyingType?.SpecialType ?? SpecialType.System_Int32), index))
            .ToList();

        (IFieldSymbol field, ulong value, int index)? exact = fields.FirstOrDefault(x => x.value == target);
        if (exact.HasValue)
            return $"{enumName}.{GeneratorUtils.EscapeIdentifier(exact.Value.field.Name)}";

        ulong remaining = target;
        List<string> names = [];
        foreach ((IFieldSymbol field, ulong value, int _) in fields.Where(x => x.value != 0).OrderByDescending(x => CountBits(x.value)).ThenBy(x => x.index))
        {
            if ((remaining & value) != value)
                continue;

            names.Add($"{enumName}.{GeneratorUtils.EscapeIdentifier(field.Name)}");
            remaining &= ~value;
            if (remaining == 0)
                break;
        }

        if (remaining == 0 && names.Count != 0)
            return string.Join(" | ", names);

        string underlying = TryRenderPrimitive(constant.Value, enumType.EnumUnderlyingType?.SpecialType ?? SpecialType.System_Int32, out string numeric)
            ? numeric
            : "0";
        return $"({enumName}){underlying}";
    }

    /// <summary>
    /// Converts an enum underlying value to its unsigned bit representation.<br/>
    /// 열거형 기반 값을 부호 없는 비트 표현으로 변환합니다.
    /// </summary>
    /// <param name="value">
    /// The underlying enum value; <see langword="null"/> is treated as zero.<br/>
    /// 열거형 기반 값이며, <see langword="null"/>이면 0으로 처리합니다.
    /// </param>
    /// <param name="type">
    /// The special type of the enum's underlying value.<br/>
    /// 열거형 기반 값의 특수 타입입니다.
    /// </param>
    /// <returns>
    /// The unsigned bit representation of the value.<br/>
    /// 값의 부호 없는 비트 표현입니다.
    /// </returns>
    static ulong ToEnumBits(object? value, SpecialType type)
    {
        if (value == null)
            return 0;

        return type switch
        {
            SpecialType.System_SByte => unchecked((ulong)Convert.ToSByte(value, CultureInfo.InvariantCulture)),
            SpecialType.System_Byte => Convert.ToByte(value, CultureInfo.InvariantCulture),
            SpecialType.System_Int16 => unchecked((ulong)Convert.ToInt16(value, CultureInfo.InvariantCulture)),
            SpecialType.System_UInt16 => Convert.ToUInt16(value, CultureInfo.InvariantCulture),
            SpecialType.System_Int32 => unchecked((ulong)Convert.ToInt32(value, CultureInfo.InvariantCulture)),
            SpecialType.System_UInt32 => Convert.ToUInt32(value, CultureInfo.InvariantCulture),
            SpecialType.System_Int64 => unchecked((ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture)),
            SpecialType.System_UInt64 => Convert.ToUInt64(value, CultureInfo.InvariantCulture),
            _ => 0
        };
    }

    /// <summary>
    /// Counts the set bits in an unsigned integer.<br/>
    /// 부호 없는 정수에서 설정된 비트 수를 셉니다.
    /// </summary>
    /// <param name="value">
    /// The value whose set bits are counted.<br/>
    /// 설정된 비트를 셀 값입니다.
    /// </param>
    /// <returns>
    /// The number of set bits in <paramref name="value"/>.<br/>
    /// <paramref name="value"/>에서 설정된 비트 수입니다.
    /// </returns>
    static int CountBits(ulong value)
    {
        int count = 0;
        while (value != 0)
        {
            count += (int)(value & 1);
            value >>= 1;
        }

        return count;
    }

    /// <summary>
    /// Finds the first field or property with a name in a type and its base types.<br/>
    /// 타입과 기본 타입에서 이름이 일치하는 첫 번째 필드 또는 속성을 찾습니다.
    /// </summary>
    /// <param name="type">
    /// The type hierarchy to search.<br/>
    /// 검색할 타입 계층입니다.
    /// </param>
    /// <param name="name">
    /// The member name to find.<br/>
    /// 찾을 멤버 이름입니다.
    /// </param>
    /// <returns>
    /// The first matching field or property, or <see langword="null"/> when none exists.<br/>
    /// 처음 일치하는 필드 또는 속성이며, 없으면 <see langword="null"/>입니다.
    /// </returns>
    static ISymbol? FindMember(INamedTypeSymbol type, string name)
    {
        for (INamedTypeSymbol? current = type; current != null; current = current.BaseType)
        {
            ISymbol? member = current.GetMembers(name).FirstOrDefault(x => x is IPropertySymbol or IFieldSymbol);
            if (member != null)
                return member;
        }

        return null;
    }

    /// <summary>
    /// Determines whether a named attribute member is writable and accessible to generated code.<br/>
    /// 이름 있는 특성 멤버가 쓰기 가능하고 생성된 코드에서 접근 가능한지 확인합니다.
    /// </summary>
    /// <param name="member">
    /// The field or property selected for the named argument; <see langword="null"/> is rejected.<br/>
    /// 이름 있는 인수에 선택된 필드 또는 속성이며, <see langword="null"/>이면 거부합니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation used for accessibility checks.<br/>
    /// 접근성 검사에 사용할 컴파일입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the member can receive a generated named argument; otherwise, <see langword="false"/>.<br/>
    /// 멤버가 생성된 이름 있는 인수를 받을 수 있으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    static bool IsWritableNamedMember(ISymbol? member, Compilation compilation)
    {
        if (member is IPropertySymbol property)
            return property.SetMethod != null && TypeRegistrySymbolHelpers.IsAccessibleFromGeneratedCode(property.SetMethod, compilation);
        if (member is IFieldSymbol field)
            return !field.IsReadOnly && !field.IsConst && TypeRegistrySymbolHelpers.IsAccessibleFromGeneratedCode(field, compilation);
        return false;
    }
}
