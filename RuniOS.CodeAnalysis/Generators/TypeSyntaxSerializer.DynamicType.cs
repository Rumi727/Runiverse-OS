using Microsoft.CodeAnalysis;
using System.Text;

namespace RuniOS.CodeAnalysis.Generators;

public static partial class TypeSyntaxSerializer
{
    static SerializeErrorResults RenderDynamicType(StringBuilder builder, IDynamicTypeSymbol dynamicTypeSymbol)
    {
        builder.Append("dynamic");
        RenderNullableAnnotation(builder, dynamicTypeSymbol);

        return default;
    }
}