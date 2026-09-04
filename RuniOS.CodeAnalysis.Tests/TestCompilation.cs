#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuniOS.CodeAnalysis.Generators;
using System.Collections.Immutable;
using Xunit.Sdk;

namespace RuniOS.CodeAnalysis.Tests;

internal sealed record BoundType(CSharpCompilation Compilation, ITypeSymbol Type);
internal sealed record BoundTypePair(CSharpCompilation Compilation, ITypeSymbol Original, ITypeSymbol RoundTrip);

internal static class TestCompilation
{
    public const string DefaultHolder = "public unsafe class SerializerTestHost";

    // Match the production Roslyn 4.3 API and a released language version it supports.
    public static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp10);
    static readonly ImmutableArray<MetadataReference> references = CreateReferences();

    static ImmutableArray<MetadataReference> CreateReferences()
    {
        string paths = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string
            ?? throw new InvalidOperationException("The .NET test host did not provide TRUSTED_PLATFORM_ASSEMBLIES.");
        return paths.Split(Path.PathSeparator)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static path => path, StringComparer.Ordinal)
            .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray();
    }

    public static CSharpCompilation CreateCompilation
    (
        string source,
        NullableContextOptions nullable = NullableContextOptions.Enable,
        params MetadataReference[] additionalReferences
    ) => CSharpCompilation.Create
    (
        "SerializerTestFixture",
        new[] { CSharpSyntaxTree.ParseText(source, ParseOptions, path: "Fixture.cs") },
        references.AddRange(additionalReferences),
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true,
            nullableContextOptions: nullable, concurrentBuild: false)
    );

    static string Source(string members, string declarations, string holder) => $$"""
        using System;
        using System.Collections.Generic;
        {{declarations}}
        {{holder}}
        {
            {{members}}
        }
        """;

    public static BoundType BindField
    (
        string type,
        string declarations = "",
        string holder = DefaultHolder,
        NullableContextOptions nullable = NullableContextOptions.Enable
    )
    {
        CSharpCompilation compilation = CreateCompilation(Source($"public static {type} Value;", declarations, holder), nullable);
        return new BoundType(compilation, FieldType(compilation, "Value"));
    }

    public static BoundTypePair BindFields
    (
        string original,
        string roundTrip,
        string declarations = "",
        string holder = DefaultHolder,
        NullableContextOptions nullable = NullableContextOptions.Enable
    )
    {
        string members = $"public static {original} Original;\npublic static {roundTrip} RoundTrip;";
        CSharpCompilation compilation = CreateCompilation(Source(members, declarations, holder), nullable);
        AssertNoErrors(compilation);
        return new BoundTypePair(compilation, FieldType(compilation, "Original"), FieldType(compilation, "RoundTrip"));
    }

    static ITypeSymbol FieldType(CSharpCompilation compilation, string name)
    {
        SyntaxTree tree = compilation.SyntaxTrees.Single();
        VariableDeclaratorSyntax declaration = tree.GetRoot().DescendantNodes()
            .OfType<VariableDeclaratorSyntax>().Single(node => node.Identifier.ValueText == name);
        IFieldSymbol field = Assert.IsAssignableFrom<IFieldSymbol>(compilation.GetSemanticModel(tree).GetDeclaredSymbol(declaration));
        return field.Type.WithNullableAnnotation(field.NullableAnnotation);
    }

    public static BoundType BindTypeOf(string type, string declarations = "")
    {
        CSharpCompilation compilation = CreateCompilation(Source($"public static Type Value = typeof({type});", declarations, DefaultHolder));
        return new BoundType(compilation, TypeOfTypes(compilation).Single());
    }

    public static BoundTypePair BindTypesOf(string original, string roundTrip, string declarations = "")
    {
        string members = $"public static Type Original = typeof({original});\npublic static Type RoundTrip = typeof({roundTrip});";
        CSharpCompilation compilation = CreateCompilation(Source(members, declarations, DefaultHolder));
        AssertNoErrors(compilation);
        ITypeSymbol[] types = TypeOfTypes(compilation);
        return new BoundTypePair(compilation, types[0], types[1]);
    }

    static ITypeSymbol[] TypeOfTypes(CSharpCompilation compilation)
    {
        SyntaxTree tree = compilation.SyntaxTrees.Single();
        SemanticModel model = compilation.GetSemanticModel(tree);
        return tree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>()
            .Select(node => Assert.IsAssignableFrom<ITypeSymbol>(model.GetTypeInfo(node.Type).Type)).ToArray();
    }

    public static BoundType BindExpression(string expression)
    {
        CSharpCompilation compilation = CreateCompilation(Source($"public static object Value = {expression};", "", DefaultHolder));
        SyntaxTree tree = compilation.SyntaxTrees.Single();
        VariableDeclaratorSyntax variable = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        ITypeSymbol type = Assert.IsAssignableFrom<ITypeSymbol>(compilation.GetSemanticModel(tree).GetTypeInfo(variable.Initializer!.Value).Type);
        return new BoundType(compilation, type);
    }

    public static void AssertNoErrors(CSharpCompilation compilation)
    {
        Diagnostic[] errors = compilation.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.True(errors.Length == 0, $"C# binding failed:\n{FormatDiagnostics(errors)}\n{compilation.SyntaxTrees.Single()}");
    }

    public static void AssertEmits(CSharpCompilation compilation)
    {
        using MemoryStream stream = new();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, $"Round-trip emit failed:\n{FormatDiagnostics(result.Diagnostics)}\n{compilation.SyntaxTrees.Single()}");
    }

    public static string Serialize(ITypeSymbol type)
    {
        var errors = type.TrySerialize(out string text);
        Assert.True(errors.isSuccess, $"Serialization failed.\nSymbol: {type}\nOutput: {text}\nErrors: {FormatErrors(errors)}");
        return text;
    }

    public static void Check
    (
        string input,
        string? expected = null,
        string declarations = "",
        string holder = DefaultHolder,
        NullableContextOptions nullable = NullableContextOptions.Enable,
        bool useTypeOf = false
    )
    {
        string output = "<not serialized>";
        ITypeSymbol? originalSymbol = null;
        ITypeSymbol? reboundSymbol = null;
        CSharpCompilation? lastCompilation = null;
        try
        {
            BoundType original = useTypeOf ? BindTypeOf(input, declarations) : BindField(input, declarations, holder, nullable);
            originalSymbol = original.Type;
            lastCompilation = original.Compilation;
            AssertNoErrors(original.Compilation);
            output = Serialize(original.Type);
            if (expected is not null)
                Assert.Equal(expected, output);

            BoundTypePair pair = useTypeOf ? BindTypesOf(input, output, declarations) : BindFields(input, output, declarations, holder, nullable);
            lastCompilation = pair.Compilation;
            reboundSymbol = pair.RoundTrip;
            AssertNoErrors(pair.Compilation);
            TypeSymbolAssertions.Equivalent(pair.Original, pair.RoundTrip);
            Assert.True(SymbolEqualityComparer.IncludeNullability.Equals(pair.Original, pair.RoundTrip),
                $"Symbol equality failed: {pair.Original} / {pair.RoundTrip}");
            AssertEmits(pair.Compilation);
            Assert.Equal(output, Serialize(pair.RoundTrip));
            Assert.Equal(output, Serialize(original.Type));
        }
        catch (XunitException exception)
        {
            string diagnostics = lastCompilation is null ? "<not bound>" : FormatDiagnostics(lastCompilation.GetDiagnostics());
            throw new XunitException($"Input: {input}\nExpected: {expected ?? "<semantic equality>"}\nOutput: {output}\nOriginal symbol: {originalSymbol}\nRound-trip symbol: {reboundSymbol}\nNullable context: {nullable}\nDeclarations: {declarations}\nHolder: {holder}\nDiagnostics:\n{diagnostics}\n{exception.Message}");
        }
    }

    public static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics) => string.Join("\n", diagnostics);
    public static string FormatErrors(TypeSyntaxSerializer.SerializeErrorResults errors) =>
        string.Join("; ", errors.Select(static e => $"{e.error}: {e.problematicObject ?? "<null>"}"));
}
