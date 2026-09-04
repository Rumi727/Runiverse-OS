using Microsoft.CodeAnalysis;
using System.Text;

namespace RuniOS.CodeAnalysis.Generators;

public static partial class TypeSyntaxSerializer
{
    static SerializeErrorResults RenderArray(StringBuilder builder, IArrayTypeSymbol arrayTypeSymbol)
    {
        SerializeErrorResults result = default;
        IArrayTypeSymbol current = arrayTypeSymbol;
        ITypeSymbol elementType;
        int rankCount = 0;

        // Consecutive C# rank specifiers run outermost first. A nullable element
        // array starts a separate group: string[]?[,] is a matrix of nullable vectors.
        while (true)
        {
            if (!current.IsSZArray && current.Rank == 1)
                result |= new SerializeErrorResults(SerializeError.unsupportedArrayType, current);

            rankCount++;
            elementType = current.ElementType.WithNullableAnnotation(current.ElementNullableAnnotation);
            if (elementType is not IArrayTypeSymbol nested || elementType.NullableAnnotation == NullableAnnotation.Annotated)
                break;

            current = nested;
        }

        result |= RenderType(builder, elementType);

        current = arrayTypeSymbol;
        for (int i = 0; i < rankCount; i++)
        {
            builder.Append('[');
            builder.Append(',', current.Rank - 1);
            builder.Append(']');

            if (i + 1 < rankCount)
                current = (IArrayTypeSymbol)current.ElementType;
        }

        RenderNullableAnnotation(builder, arrayTypeSymbol);
        return result;
    }
}
