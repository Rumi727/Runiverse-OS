using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using RuniOS.CodeAnalysis.Analyzers;
using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Tests;

public sealed class IdentifierDefaultNamespaceCodeFixProviderTests
{
    [Theory]
    [InlineData("\"gui.name\"", "\"runios:gui.name\"")]
    [InlineData("@\"gui.name\"", "@\"runios:gui.name\"")]
    [InlineData("\"\"\"gui.name\"\"\"", "\"\"\"runios:gui.name\"\"\"")]
    public void AddsNamespaceAfterOpeningQuote(string literal, string expected)
    {
        Assert.Equal
        (
            Source($"Identifier id = {expected};"),
            FixAll(Source($"Identifier id = {literal};"), single: true)
        );
    }

    [Fact]
    public void AddsNamespaceToArgumentLiteral()
    {
        Assert.Equal
        (
            Source("Take(\"runios:gui.name\");"),
            FixAll(Source("Take(\"gui.name\");"), single: true)
        );
    }

    [Fact]
    public void PreservesTriviaAroundLiteral()
    {
        Assert.Equal
        (
            Source("Identifier id = /* a */ \"runios:gui.name\" /* b */;"),
            FixAll(Source("Identifier id = /* a */ \"gui.name\" /* b */;"), single: true)
        );
    }

    [Fact]
    public void MultiLineRawStringHasNoFix()
    {
        Assert.Empty(GetFixes(Source("Identifier id = \"\"\"\n            gui.name\n            \"\"\";")));
    }

    [Fact]
    public void NonLiteralConstantHasNoFix()
    {
        Assert.Empty(GetFixes(Source("Identifier id = \"gui\" + \".name\";")));
    }

    [Fact]
    public void FixAllFixesEveryDiagnosticInDocument()
    {
        Assert.Equal
        (
            Source("Identifier a = \"runios:a\"; Identifier b = \"runios:b\";"),
            FixAll(Source("Identifier a = \"a\"; Identifier b = \"b\";"), single: false)
        );
    }

    static string Source(string statements) => $$"""
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
    """;

    static List<CodeAction> GetFixes(string source)
    {
        (Document document, ImmutableArray<Diagnostic> diagnostics) = Create(source);
        return GetFixes(document, diagnostics);
    }

    static List<CodeAction> GetFixes(Document document, ImmutableArray<Diagnostic> diagnostics)
    {
        List<CodeAction> actions = new();
        IdentifierDefaultNamespaceCodeFixProvider provider = new();

        foreach (Diagnostic diagnostic in diagnostics)
        {
            CodeFixContext context = new(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
            provider.RegisterCodeFixesAsync(context).GetAwaiter().GetResult();
        }

        return actions;
    }

    static string FixAll(string source, bool single)
    {
        (Document document, ImmutableArray<Diagnostic> diagnostics) = Create(source);
        IdentifierDefaultNamespaceCodeFixProvider provider = new();
        CodeAction action;

        if (single)
        {
            action = Assert.Single(GetFixes(document, diagnostics));
        }
        else
        {
            FixAllContext context = new
            (
                document,
                provider,
                FixAllScope.Document,
                nameof(IdentifierDefaultNamespaceCodeFixProvider),
                provider.FixableDiagnosticIds,
                new FixedDiagnosticProvider(diagnostics),
                CancellationToken.None
            );
            action = provider.GetFixAllProvider()!.GetFixAsync(context).GetAwaiter().GetResult()!;
        }

        Solution solution = action.GetOperationsAsync(CancellationToken.None).GetAwaiter().GetResult()
            .OfType<ApplyChangesOperation>().Single().ChangedSolution;
        return solution.GetDocument(document.Id)!.GetTextAsync().GetAwaiter().GetResult().ToString();
    }

    static (Document Document, ImmutableArray<Diagnostic> Diagnostics) Create(string source)
    {
        AdhocWorkspace workspace = new();
        CSharpCompilation compilation = TestCompilation.CreateCompilation(source);
        ProjectInfo projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, "Test", "Test", LanguageNames.CSharp)
            .WithCompilationOptions(compilation.Options)
            .WithParseOptions(TestCompilation.ParseOptions)
            .WithMetadataReferences(compilation.References);
        Project project = workspace.AddProject(projectInfo);
        Document document = project.AddDocument("Test.cs", SourceText.From(source));

        compilation = (CSharpCompilation)document.Project.GetCompilationAsync().GetAwaiter().GetResult()!;
        ImmutableArray<Diagnostic> diagnostics = compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new IdentifierConversionAnalyzer()))
            .GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult()
            .Where(static diagnostic => diagnostic.Id == "ROS0039")
            .ToImmutableArray();

        return (document, diagnostics);
    }

    sealed class FixedDiagnosticProvider(ImmutableArray<Diagnostic> diagnostics) : FixAllContext.DiagnosticProvider
    {
        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);

        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Diagnostic>>(ImmutableArray<Diagnostic>.Empty);
    }
}
