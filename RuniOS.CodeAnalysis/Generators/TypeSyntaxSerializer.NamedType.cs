using Microsoft.CodeAnalysis;
using System.Text;

namespace RuniOS.CodeAnalysis.Generators;

public static partial class TypeSyntaxSerializer
{
    static SerializeErrorResults RenderNamedType(StringBuilder builder, INamedTypeSymbol namedTypeSymbol)
    {
        SerializeErrorResults result = default;
        if (TryRenderSpecialType(builder, namedTypeSymbol))
        {
            RenderNullableAnnotation(builder, namedTypeSymbol);
            return result;
        }

        if (namedTypeSymbol.IsTupleType)
            return RenderTupleType(builder, namedTypeSymbol);

        if (namedTypeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T && !namedTypeSymbol.IsUnboundGenericType)
        {
            result |= RenderType(builder, namedTypeSymbol.TypeArguments[0]);

            builder.Append('?');
            return result;
        }

        if (namedTypeSymbol.ContainingType is { } containingType)
        {
            result |= RenderNamedType(builder, containingType);
            builder.Append('.');
        }
        else
        {
            result |= RenderNamespace(builder, namedTypeSymbol.ContainingNamespace);
            if (!result.isSuccess)
                return result;

            if (!namedTypeSymbol.ContainingNamespace.IsGlobalNamespace)
                builder.Append('.');
        }

        result |= RenderIdentifier(builder, namedTypeSymbol.Name);
        result |= RenderTypeArguments(builder, namedTypeSymbol);

        RenderNullableAnnotation(builder, namedTypeSymbol);
        return result;
    }

    static SerializeErrorResults RenderNamespace(StringBuilder builder, INamespaceSymbol namespaceSymbol)
    {
        if (namespaceSymbol.IsGlobalNamespace)
        {
            builder.Append("global::");
            return default;
        }

        SerializeErrorResults result = default;
        if (namespaceSymbol.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace)
        {
            result |= RenderNamespace(builder, containingNamespace);
            builder.Append('.');
        }
        else
            builder.Append("global::");

        return result | RenderIdentifier(builder, namespaceSymbol.Name);
    }

    static SerializeErrorResults RenderTypeArguments(StringBuilder builder, INamedTypeSymbol type)
    {
        if (type.Arity == 0)
            return default;

        builder.Append('<');

        SerializeErrorResults result = default;
        if (type.IsUnboundGenericType)
            builder.Append(',', type.Arity - 1);
        else
        {
            for (int i = 0; i < type.TypeArguments.Length; i++)
            {
                if (i != 0)
                    builder.Append(", ");

                result |= RenderType(builder, type.TypeArguments[i].WithNullableAnnotation(type.TypeArgumentNullableAnnotations[i]));
            }
        }

        builder.Append('>');
        return result;
    }

    static bool TryRenderSpecialType(StringBuilder builder, INamedTypeSymbol type)
    {
        string? text = type.SpecialType switch
        {
            SpecialType.System_Void => "void",
            SpecialType.System_Boolean => "bool",
            SpecialType.System_SByte => "sbyte",
            SpecialType.System_Byte => "byte",
            SpecialType.System_Int16 => "short",
            SpecialType.System_UInt16 => "ushort",
            SpecialType.System_Int32 => "int",
            SpecialType.System_UInt32 => "uint",
            SpecialType.System_Int64 => "long",
            SpecialType.System_UInt64 => "ulong",
            SpecialType.System_Decimal => "decimal",
            SpecialType.System_Single => "float",
            SpecialType.System_Double => "double",
            SpecialType.System_Char => "char",
            SpecialType.System_String => "string",
            SpecialType.System_Object => "object",
            SpecialType.System_IntPtr when type.IsNativeIntegerType => "nint",
            SpecialType.System_UIntPtr when type.IsNativeIntegerType => "nuint",
            _ => null
        };

        if (text == null)
            return false;

        builder.Append(text);
        return true;
    }

    static SerializeErrorResults RenderTupleType(StringBuilder builder, INamedTypeSymbol type)
    {
        builder.Append('(');

        SerializeErrorResults result = default;
        for (int i = 0; i < type.TupleElements.Length; i++)
        {
            if (i != 0)
                builder.Append(", ");

            IFieldSymbol element = type.TupleElements[i];
            result |= RenderType(builder, element.Type.WithNullableAnnotation(element.NullableAnnotation));

            // 실제 의미 있는 tuple element name이면 출력
            if (!element.IsImplicitlyDeclared)
            {
                builder.Append(' ');
                result |= RenderIdentifier(builder, element.Name);
            }
        }

        builder.Append(')');
        return result;
    }
}
