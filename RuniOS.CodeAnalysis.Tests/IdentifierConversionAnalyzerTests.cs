using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RuniOS.CodeAnalysis.Analyzers;
using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Tests;

public sealed class IdentifierConversionAnalyzerTests
{
    [Theory]
    [InlineData("runios:gui.name")]
    [InlineData("runios-editor:inspector/a_b-c.d")]
    [InlineData("runios:")]
    [InlineData("")]
    public void ValidIdentifiersHaveNoDiagnostics(string identifier)
    {
        Assert.Empty(Analyze($"Identifier id = \"{identifier}\";"));
    }

    [Theory]
    [InlineData("runios:gui:name", "ROS0036")]
    [InlineData("Runios:gui.name", "ROS0037")]
    [InlineData("run ios:gui.name", "ROS0037")]
    [InlineData("runios:gui.Name", "ROS0038")]
    [InlineData("gui name", "ROS0038")]
    [InlineData("gui.name", "ROS0039")]
    [InlineData(":gui.name", "ROS0039")]
    public void ReportedIdentifiersHaveSingleDiagnostic(string identifier, string id)
    {
        Diagnostic diagnostic = Assert.Single(Analyze($"Identifier id = \"{identifier}\";"));
        Assert.Equal(id, diagnostic.Id);
    }

    [Fact]
    public void DefaultNamespaceDiagnosticShowsResolvedIdentifier()
    {
        Diagnostic diagnostic = Assert.Single(Analyze("""Identifier id = "gui.name";"""));
        string message = diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture);

        Assert.Contains("'runios:gui.name'", message);
        Assert.Equal(message, diagnostic.Descriptor.Title.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void DiagnosticIsLocatedOnTheConvertedString()
    {
        CSharpCompilation compilation = CreateCompilation("""Identifier id = "Runios:gui.name";""");
        Diagnostic diagnostic = Assert.Single(Analyze(compilation));
        LiteralExpressionSyntax literal = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Single(static literal => literal.IsKind(SyntaxKind.StringLiteralExpression));

        Assert.Equal(literal.GetLocation().SourceSpan, diagnostic.Location.SourceSpan);
    }

    [Fact]
    public void ConstantsArgumentsAndExplicitCastsAreAnalyzed()
    {
        ImmutableArray<Diagnostic> diagnostics = Analyze
        ("""
        const string key = "Runios:a";
        Identifier fromConstant = key;
        Take("runios:B");
        Identifier fromCast = (Identifier)"a:b:c";
        """);

        Assert.Equal
        (
            new[] { "ROS0036", "ROS0037", "ROS0038" },
            diagnostics.Select(static diagnostic => diagnostic.Id).OrderBy(static id => id).ToArray()
        );
    }

    [Fact]
    public void NonConstantStringsAreIgnored()
    {
        Assert.Empty(Analyze("""
        string value = "Runios:a";
        Identifier id = value;
        """));
    }

    static ImmutableArray<Diagnostic> Analyze(string statements) => Analyze(CreateCompilation(statements));

    static ImmutableArray<Diagnostic> Analyze(CSharpCompilation compilation)
    {
        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers
        (
            ImmutableArray.Create<DiagnosticAnalyzer>(new IdentifierConversionAnalyzer())
        );
        return withAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
    }

    static CSharpCompilation CreateCompilation(string statements) => TestCompilation.CreateCompilation
    ($$"""
    using RuniOS.Resource;

    namespace RuniOS.Resource
    {
        public struct Identifier
        {
            public static implicit operator Identifier(string identifier) => default;
        }
    }

    public static class Host
    {
        static void Take(Identifier identifier) { }

        static void Method()
        {
            {{statements}}
        }
    }
    """);
}
