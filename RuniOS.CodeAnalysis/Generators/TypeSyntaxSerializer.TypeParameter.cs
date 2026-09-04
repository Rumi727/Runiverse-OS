using Microsoft.CodeAnalysis;
using System.Text;

namespace RuniOS.CodeAnalysis.Generators;

public static partial class TypeSyntaxSerializer
{
    static SerializeErrorResults RenderTypeParameter(StringBuilder builder, ITypeParameterSymbol typeParameter)
    {
        SerializeErrorResults result = RenderIdentifier(builder, typeParameter.Name);
        RenderNullableAnnotation(builder, typeParameter);

        return result;
    }
}