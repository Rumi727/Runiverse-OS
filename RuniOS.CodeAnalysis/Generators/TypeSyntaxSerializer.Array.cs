using Microsoft.CodeAnalysis;
using System.Text;

namespace RuniOS.CodeAnalysis.Generators;

public static partial class TypeSyntaxSerializer
{
    static SerializeErrorResults RenderArray(StringBuilder builder, IArrayTypeSymbol arrayTypeSymbol)
    {
        SerializeErrorResults result = default;
        if (!arrayTypeSymbol.IsSZArray && arrayTypeSymbol.Rank == 1)
            result |= new SerializeErrorResults(SerializeError.unsupportedArrayType, arrayTypeSymbol);

        result |= RenderType(builder, arrayTypeSymbol.ElementType.WithNullableAnnotation(arrayTypeSymbol.ElementNullableAnnotation));

        builder.Append('[');
        builder.Append(',', arrayTypeSymbol.Rank - 1);
        builder.Append(']');

        RenderNullableAnnotation(builder, arrayTypeSymbol);
        return result;
    }
}