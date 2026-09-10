using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using RuniOS.CodeAnalysis.Analyzers;
using RuniOS.CodeAnalysis.Generators.Milestones;
using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Tests;

public sealed class MilestoneMethodAnalyzerTests
{
    [Fact]
    public void ValidVoidAndUniTaskMethodsHaveNoDiagnostics()
    {
        ImmutableArray<Diagnostic> diagnostics = Analyze
        ("""
        public partial class Valid
        {
            [OnResourcesReady] public static void VoidMethod() { }
            [OnResourcesReady] public static UniTask TaskMethod() => default;
        }
        """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void EachInvalidContractIsReportedSeparately()
    {
        ImmutableArray<Diagnostic> diagnostics = Analyze
        ("""
        public class Invalid<T>
        {
            [OnResourcesReady] public UniTask<T> Method<U>(int value) => default;
        }
        """);

        Assert.Equal
        (
            new[] { "ROS0029", "ROS0030", "ROS0031", "ROS0032", "ROS0033", "ROS0034" },
            diagnostics.Select(static diagnostic => diagnostic.Id).OrderBy(static id => id).ToArray()
        );
    }

    [Fact]
    public void EveryMilestoneDiagnosticIsLocatedOnTheAttribute()
    {
        CSharpCompilation compilation = CreateCompilation
        ("""
        public class Invalid<T>
        {
            [OnResourcesReady] public UniTask<T> Method<U>(int value) => default;
        }
        """);
        DiagnosticAnalyzer analyzer = new MilestoneMethodAnalyzer();
        ImmutableArray<Diagnostic> diagnostics = compilation.WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
        SyntaxTree syntaxTree = compilation.SyntaxTrees.Single();
        TextSpan attributeSpan = syntaxTree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Single().GetLocation().SourceSpan;

        Assert.Equal(6, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal(attributeSpan, diagnostic.Location.SourceSpan));
    }

    [Fact]
    public void EveryNestedContainingTypeIsValidated()
    {
        ImmutableArray<Diagnostic> diagnostics = Analyze
        ("""
        public class Outer<T>
        {
            public class Inner<U>
            {
                [OnResourcesReady] public static void Method() { }
            }
        }
        """);

        Assert.Equal
        (
            new[] { "ROS0032", "ROS0032", "ROS0033", "ROS0033" },
            diagnostics.Select(static diagnostic => diagnostic.Id).OrderBy(static id => id).ToArray()
        );
    }

    [Fact]
    public void ContainingTypeDiagnosticIsReportedAsSemanticDocumentDiagnostic()
    {
        CSharpCompilation compilation = CreateCompilation
        ("""
        public class Invalid
        {
            [OnResourcesReady] public static void Method() { }
        }
        """);
        DiagnosticAnalyzer analyzer = new MilestoneMethodAnalyzer();
        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
        SyntaxTree syntaxTree = compilation.SyntaxTrees.Single();
        SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);

        AnalysisResult result = withAnalyzers.GetAnalysisResultAsync(semanticModel, null, default).GetAwaiter().GetResult();

        Assert.True(result.SemanticDiagnostics.TryGetValue(syntaxTree, out var diagnosticsByAnalyzer));
        Assert.True(diagnosticsByAnalyzer!.TryGetValue(analyzer, out ImmutableArray<Diagnostic> semanticDiagnostics));
        Assert.Contains(semanticDiagnostics, static diagnostic => diagnostic.Id == "ROS0032");
        Assert.False
        (
            result.CompilationDiagnostics.TryGetValue(analyzer, out ImmutableArray<Diagnostic> compilationDiagnostics) &&
            compilationDiagnostics.Any(static diagnostic => diagnostic.Id == "ROS0032")
        );
    }

    [Fact]
    public void ContainingTypesMustBePublicOrInternal()
    {
        ImmutableArray<Diagnostic> diagnostics = Analyze
        ("""
        public partial class Container
        {
            public partial class ValidPublic
            {
                [OnResourcesReady] public static void Method() { }
            }

            internal partial class ValidInternal
            {
                [OnResourcesReady] public static void Method() { }
            }

            private partial class InvalidPrivate
            {
                [OnResourcesReady] public static void Method() { }
            }

            protected partial class InvalidProtected
            {
                [OnResourcesReady] public static void Method() { }
            }

            protected internal partial class InvalidProtectedInternal
            {
                [OnResourcesReady] public static void Method() { }
            }

            private protected partial class InvalidPrivateProtected
            {
                [OnResourcesReady] public static void Method() { }
            }

            private partial class InvalidOuter
            {
                public partial class NestedMethod
                {
                    [OnResourcesReady] public static void Method() { }
                }
            }
        }
        """);

        Assert.Equal
        (
            new[] { "ROS0035", "ROS0035", "ROS0035", "ROS0035", "ROS0035" },
            diagnostics.Select(static diagnostic => diagnostic.Id).OrderBy(static id => id).ToArray()
        );
    }

    [Fact]
    public void GeneratorSkipsMethodsThatDoNotSatisfyItsExistingFastPathOrSemanticValidation()
    {
        ImmutableArray<GeneratedSourceResult> generatedSources = Generate
        ("""
        public partial class Valid
        {
            [OnResourcesReady] public static void VoidMethod() { }
            [OnResourcesReady] public static UniTask TaskMethod() => default;
        }

        partial class ValidImplicitInternal
        {
            [OnResourcesReady] public static void Method() { }
        }

        public partial class AccessibilityContainer
        {
            public partial class ValidPublic
            {
                [OnResourcesReady] public static void Method() { }
            }

            internal partial class ValidInternal
            {
                [OnResourcesReady] public static void Method() { }
            }

            private partial class InvalidPrivate
            {
                [OnResourcesReady] public static void Method() { }
            }

            protected partial class InvalidProtected
            {
                [OnResourcesReady] public static void Method() { }
            }

            protected internal partial class InvalidProtectedInternal
            {
                [OnResourcesReady] public static void Method() { }
            }

            private protected partial class InvalidPrivateProtected
            {
                [OnResourcesReady] public static void Method() { }
            }

            private partial class InvalidOuter
            {
                public partial class NestedMethod
                {
                    [OnResourcesReady] public static void Method() { }
                }
            }
        }

        public partial class InvalidReturn
        {
            [OnResourcesReady] public static UniTask<int> Method() => default;
        }

        public partial class InvalidInstance
        {
            [OnResourcesReady] public void Method() { }
        }

        public partial class InvalidParameters
        {
            [OnResourcesReady] public static void Method(int value) { }
        }

        public partial class InvalidGenericMethod
        {
            [OnResourcesReady] public static void Method<T>() { }
        }

        public partial class InvalidGenericOwner<T>
        {
            [OnResourcesReady] public static void Method() { }
        }

        public class InvalidNonPartial
        {
            [OnResourcesReady] public static void Method() { }
        }
        """);

        Assert.Equal(5, generatedSources.Length);
    }

    static ImmutableArray<Diagnostic> Analyze(string declarations)
    {
        CSharpCompilation compilation = CreateCompilation(declarations);
        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers
        (
            ImmutableArray.Create<DiagnosticAnalyzer>(new MilestoneMethodAnalyzer())
        );
        return withAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
    }

    static ImmutableArray<GeneratedSourceResult> Generate(string declarations)
    {
        CSharpCompilation compilation = CreateCompilation(declarations);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new MilestoneGenerator().AsSourceGenerator());
        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult().Results.Single().GeneratedSources;
    }

    static CSharpCompilation CreateCompilation(string declarations) => TestCompilation.CreateCompilation
    ($$"""
    using System;
    using Cysharp.Threading.Tasks;
    using RuniOS.Milestones;

    namespace Cysharp.Threading.Tasks
    {
        public struct UniTask { }
        public struct UniTask<T> { }
    }

    namespace RuniOS.Milestones
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        public sealed class OnResourcesReadyAttribute : Attribute { }
    }

    {{declarations}}
    """);
}
