using Microsoft.CodeAnalysis;
using System.Text;

namespace RuniOS.CodeAnalysis.Generators;

public static class GeneratorUtils
{
    static readonly SymbolDisplayFormat namespaceDisplayFormat = new
    (
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
    );

    public static string? ToDeclarationName(this INamespaceSymbol symbol)
    {
        if (symbol.IsGlobalNamespace)
            return null;

        return symbol.ToDisplayString(namespaceDisplayFormat);
    }

    public static string GetHintName(this INamedTypeSymbol symbol, string? suffix = null)
    {
        StringBuilder builder = new StringBuilder();
        if (symbol.ContainingNamespace.ToDeclarationName() is { } nameSpace)
        {
            builder.Append(nameSpace);
            builder.Append('.');
        }

        AppendType(symbol, builder);

        if (!string.IsNullOrEmpty(suffix))
        {
            builder.Append('.');
            builder.Append(suffix);
        }

        builder.Append(".g.cs");
        return builder.ToString();

        static void AppendType(INamedTypeSymbol symbol, StringBuilder builder)
        {
            if (symbol.ContainingType != null)
            {
                AppendType(symbol.ContainingType, builder);
                builder.Append('.');
            }

            builder.Append(symbol.MetadataName);
        }
    }

    extension(ITypeSymbol type)
    {
        public bool IsUniTask => type is INamedTypeSymbol { Name: "UniTask" } namedType &&
            namedType.ContainingNamespace.ToDeclarationName() == "Cysharp.Threading.Tasks";

        public bool IsNonGenericUniTask => type is INamedTypeSymbol { Name: "UniTask", Arity: 0 } namedType &&
            namedType.ContainingNamespace.ToDeclarationName() == "Cysharp.Threading.Tasks";

        public bool IsGenericUniTask => type is INamedTypeSymbol { Name: "UniTask", Arity: 1 } namedType &&
            namedType.ContainingNamespace.ToDeclarationName() == "Cysharp.Threading.Tasks";
    }
}