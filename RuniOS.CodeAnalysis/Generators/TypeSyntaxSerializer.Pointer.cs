using Microsoft.CodeAnalysis;
using System.Text;

namespace RuniOS.CodeAnalysis.Generators;

public static partial class TypeSyntaxSerializer
{
    static SerializeErrorResults RenderPointer(StringBuilder builder, IPointerTypeSymbol pointerTypeSymbol)
    {
        SerializeErrorResults result = RenderType(builder, pointerTypeSymbol.PointedAtType);
        builder.Append('*');

        return result;
    }
}