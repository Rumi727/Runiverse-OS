using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Generators;

public static class PartialTypeDeclarationsExtensions
{
    public static PartialTypeDeclarations ToPartialTypeDeclarations(this INamedTypeSymbol symbol)
    {
        int depth = 0;
        for (INamedTypeSymbol? current = symbol; current != null; current = current.ContainingType)
            depth++;

        var declarations = ImmutableArray.CreateBuilder<PartialTypeDeclaration>(depth);
        declarations.Count = depth;

        INamedTypeSymbol? type = symbol;
        for (int i = depth - 1; i >= 0; i--)
        {
            var typeParameters = ImmutableArray.CreateBuilder<string>(type.TypeParameters.Length);
            for (int j = 0; j < type.TypeParameters.Length; j++)
                typeParameters.Add(type.TypeParameters[j].Name);

            declarations[i] = new PartialTypeDeclaration
            (
                type.TypeKind,
                type.IsRecord,
                type.Name,
                typeParameters.MoveToImmutable()
            );

            type = type.ContainingType;
        }

        return new PartialTypeDeclarations
        (
            symbol.ContainingNamespace.ToDeclarationName(),
            declarations.MoveToImmutable()
        );
    }
}